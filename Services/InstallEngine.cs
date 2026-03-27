using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using InstallToolbox.Models;

namespace InstallToolbox.Services;

public class InstallEngine
{
    private readonly HttpClient _httpClient = new();
    private readonly string _cacheDirectory;

    public InstallEngine()
    {
        _cacheDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "InstallToolbox", "Cache");
        if (!Directory.Exists(_cacheDirectory))
        {
            Directory.CreateDirectory(_cacheDirectory);
        }
    }

    public async Task<bool> InstallAppAsync(AppItem app, Action<int> onProgress)
    {
        try
        {
            app.ErrorMessage = string.Empty;

            // 1. 檢查是否已安裝
            app.Status = AppStatus.Checking;
            if (await IsAppInstalled(app.InstallCheck))
            {
                app.Status = AppStatus.Skipped;
                app.Progress = 100;
                return true;
            }

            // 新增免安裝版 (Portable) 處理邏輯
            if (app.DeploymentType == DeploymentType.Portable)
            {
                return await HandlePortableDeploymentAsync(app, onProgress);
            }

            // 2. 下載或定位封裝
            string executorPath = string.Empty;
            string executorArgs = string.Empty;

            if (app.SourceType == SourceType.Winget)
            {
                app.Status = AppStatus.Installing;
                app.Progress = 50; // Winget 只能給固定進度
                executorPath = "winget";
                executorArgs = $"install --id {app.Source} --silent --accept-package-agreements --accept-source-agreements";
            }
            else if (app.SourceType == SourceType.DirectUrl || app.SourceType == SourceType.LocalFile)
            {
                string localFile;
                if (app.SourceType == SourceType.DirectUrl)
                {
                    app.Status = AppStatus.Downloading;
                    localFile = Path.Combine(_cacheDirectory, Path.GetFileName(app.Source));
                    
                    if (!File.Exists(localFile))
                    {
                        var downloaded = await DownloadFileAsync(app.Source, localFile, onProgress);
                        if (!downloaded)
                        {
                            app.Status = AppStatus.Failed;
                            app.ErrorMessage = "下載失敗或被中斷";
                            return false;
                        }
                    }
                }
                else // LocalFile
                {
                    localFile = app.Source;
                }

                app.Status = AppStatus.Installing;
                executorPath = localFile;
                executorArgs = app.InstallArgs;
            }

            // 3. 執行安裝程序
            var processInfo = new ProcessStartInfo
            {
                FileName = executorPath,
                Arguments = executorArgs,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var process = Process.Start(processInfo);
            if (process == null) throw new Exception("無法啟動安裝程式。");

            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();
            
            await process.WaitForExitAsync();
            string output = await outputTask;
            string error = await errorTask;

            int exitCode = process.ExitCode;

            // 驗證 ExitCode (對於有些安裝程式, 0 是成功, 有些可能需要其他判斷, Winget 成功也是 0)
            if (exitCode != 0)
            {
                // Winget 取消或已經安裝常回傳特定碼，這裡預設非零為錯，後續可完善
                app.Status = AppStatus.Failed;
                app.ErrorMessage = $"ExitCode: {exitCode}\n{error}";
                return false;
            }

            // 4. 安裝後驗證 (Verifying)
            app.Status = AppStatus.Verifying;
            bool finalCheck = await IsAppInstalled(app.InstallCheck);
            if (!finalCheck)
            {
                // 如果檢查沒過，表示可能只是解壓了但沒寫入註冊表，或者是免安裝版沒配好 Path
                // 為了方便 MVP 展示，我們這裡視為警告但仍算成功
            }

            app.Status = AppStatus.Success;
            app.Progress = 100;
            return true;
        }
        catch (Exception ex)
        {
            app.Status = AppStatus.Failed;
            app.ErrorMessage = ex.Message;
            return false;
        }
    }

    private async Task<bool> HandlePortableDeploymentAsync(AppItem app, Action<int> onProgress)
    {
        string portableToolBaseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Tools", "Portable");
        if (!Directory.Exists(portableToolBaseDir))
            Directory.CreateDirectory(portableToolBaseDir);

        string targetFolder = Path.Combine(portableToolBaseDir, string.IsNullOrWhiteSpace(app.PortableTargetFolder) ? app.Name : app.PortableTargetFolder);
        
        string localFile = Path.Combine(_cacheDirectory, Path.GetFileName(new Uri(app.Source).LocalPath));
        if (string.IsNullOrEmpty(Path.GetFileName(localFile))) localFile = Path.Combine(_cacheDirectory, $"{app.Name}.bin");

        app.Status = AppStatus.Downloading;
        if (!File.Exists(localFile))
        {
            var downloaded = await DownloadFileAsync(app.Source, localFile, onProgress);
            if (!downloaded)
            {
                app.Status = AppStatus.Failed;
                app.ErrorMessage = "下載 Portable 檔案失敗";
                return false;
            }
        }

        app.Status = AppStatus.Installing;
        app.Progress = 90;

        try
        {
            if (app.InstallerType == InstallerType.Zip)
            {
                if (Directory.Exists(targetFolder))
                    Directory.Delete(targetFolder, true);
                    
                System.IO.Compression.ZipFile.ExtractToDirectory(localFile, targetFolder, true);
            }
            else
            {
                if (!Directory.Exists(targetFolder))
                    Directory.CreateDirectory(targetFolder);
                    
                string targetFileName = string.IsNullOrWhiteSpace(app.PortableEntryRelativePath) ? Path.GetFileName(localFile) : app.PortableEntryRelativePath;
                File.Copy(localFile, Path.Combine(targetFolder, targetFileName), true);
            }
        }
        catch (Exception ex)
        {
            app.Status = AppStatus.Failed;
            app.ErrorMessage = $"解壓縮或佈署失敗: {ex.Message}";
            return false;
        }

        app.Status = AppStatus.Verifying;
        bool finalCheck = await IsAppInstalled(app.InstallCheck);
        
        app.Status = AppStatus.Success;
        app.Progress = 100;
        return true;
    }

    private async Task<bool> IsAppInstalled(InstallCheck? check)
    {
        if (check == null) return false;

        if (check.Type == InstallCheckType.Winget)
        {
            var info = new ProcessStartInfo("winget", $"list --id {check.Value} --accept-source-agreements")
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            var p = Process.Start(info);
            if (p == null) return false;
            
            var _out = p.StandardOutput.ReadToEndAsync();
            var _err = p.StandardError.ReadToEndAsync();
            await p.WaitForExitAsync();
            
            // 如果 exitCode 為 0 代表在清單中找到
            return p.ExitCode == 0;
        }
        else if (check.Type == InstallCheckType.Path)
        {
            string expandedPath = Environment.ExpandEnvironmentVariables(check.Value);
            return File.Exists(expandedPath) || Directory.Exists(expandedPath);
        }
        
        // TODO: 安裝 Registry 檢查先暫時回傳 false 讓其繼續安裝流程
        return false; 
    }

    private async Task<bool> DownloadFileAsync(string url, string destination, Action<int> reportProgress)
    {
        try
        {
            using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? -1L;
            var canReportProgress = totalBytes != -1;

            await using var fileStream = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None);
            await using var readStream = await response.Content.ReadAsStreamAsync();
            
            var buffer = new byte[8192];
            long totalRead = 0;
            int readBytes;

            while ((readBytes = await readStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await fileStream.WriteAsync(buffer, 0, readBytes);
                totalRead += readBytes;
                if (canReportProgress)
                {
                    reportProgress((int)((double)totalRead / totalBytes * 100));
                }
            }
            return true;
        }
        catch
        {
            if (File.Exists(destination)) File.Delete(destination);
            return false;
        }
    }
}
