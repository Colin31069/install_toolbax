using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using InstallToolbox.Models;

namespace InstallToolbox.Services;

public class ConfigService
{
    private string _configFilePath;

    public ConfigService()
    {
        // 為了在開發期間直接讀取專案根目錄或建置目錄下的 json
        _configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "apps_repository.json");
    }

    public AppsRepository? LoadConfig()
    {
        if (!File.Exists(_configFilePath))
        {
            // 若執行檔目錄下沒有，嘗試去上一層或專案根目錄找 (僅供發環境容錯)
            string backupPath = Path.Combine(Environment.CurrentDirectory, "apps_repository.json");
            if (File.Exists(backupPath))
                _configFilePath = backupPath;
            else
                throw new FileNotFoundException($"找不到配置檔: {_configFilePath}");
        }

        string json = File.ReadAllText(_configFilePath);

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        // JsonStringEnumConverter 已在 AppItem 屬性上指定，但這裡也可以全局加
        options.Converters.Add(new JsonStringEnumConverter());

        var repo = JsonSerializer.Deserialize<AppsRepository>(json, options);
        return repo;
    }
}
