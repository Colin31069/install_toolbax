using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InstallToolbox.Models;
using InstallToolbox.Services;
using System.Windows;
using System.Linq;
using System;
using System.IO;
using Microsoft.Win32;

namespace InstallToolbox.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ConfigService _configService;
    private readonly SettingsService _settingsService;
    private AppsRepository? _currentRepo;
    private UserSettings _currentSettings;

    // --- Manage Existing Apps Properties ---
    [ObservableProperty]
    private ObservableCollection<AppItem> _existingApps = new();

    [ObservableProperty]
    private AppItem? _selectedExistingApp;

    [ObservableProperty] private string _editAppId = string.Empty;
    [ObservableProperty] private string _editAppName = string.Empty;
    [ObservableProperty] private string _editAppDescription = string.Empty;
    [ObservableProperty] private SourceType _editSelectedSourceType = SourceType.Winget;
    [ObservableProperty] private string _editSource = string.Empty;
    [ObservableProperty] private InstallerType _editSelectedInstallerType = InstallerType.Exe;
    [ObservableProperty] private bool _editRequiresAdmin = true;
    [ObservableProperty] private DeploymentType _editSelectedDeploymentType = DeploymentType.Installed;
    [ObservableProperty] private string _editInstallArgs = string.Empty;
    [ObservableProperty] private ObservableCollection<AppGroupSelection> _editGroupSelections = new();
    [ObservableProperty] private string _editSelectedIconFilePath = string.Empty;

    partial void OnSelectedExistingAppChanged(AppItem? value)
    {
        if (value != null)
        {
            EditAppId = value.Id;
            EditAppName = value.Name;
            EditAppDescription = value.Description;
            EditSelectedSourceType = value.SourceType;
            EditSource = value.Source;
            EditSelectedInstallerType = value.InstallerType;
            EditRequiresAdmin = value.RequiresAdmin;
            EditSelectedDeploymentType = value.DeploymentType;
            EditInstallArgs = value.InstallArgs;
            EditSelectedIconFilePath = value.IconPath;

            if (_currentRepo != null)
            {
                foreach (var selection in EditGroupSelections)
                {
                    var group = _currentRepo.Groups.FirstOrDefault(g => g.Id == selection.GroupId);
                    var section = group?.Sections.FirstOrDefault(s => s.Id == selection.SectionId);
                    selection.IsSelected = section != null && section.AppIds.Contains(value.Id);
                }
            }
        }
        else
        {
            EditAppId = string.Empty;
            EditAppName = string.Empty;
            EditAppDescription = string.Empty;
            EditSource = string.Empty;
            EditInstallArgs = string.Empty;
            EditSelectedIconFilePath = string.Empty;
            foreach (var sel in EditGroupSelections) sel.IsSelected = false;
        }
    }

    // --- Add App Properties ---
    [ObservableProperty]
    private string _appId = string.Empty;

    [ObservableProperty]
    private string _appName = string.Empty;

    [ObservableProperty]
    private string _appDescription = string.Empty;

    [ObservableProperty]
    private SourceType _selectedSourceType = SourceType.Winget;

    public IEnumerable<SourceType> SourceTypes => Enum.GetValues(typeof(SourceType)).Cast<SourceType>();

    [ObservableProperty]
    private string _source = string.Empty;

    [ObservableProperty]
    private InstallerType _selectedInstallerType = InstallerType.Exe;

    public IEnumerable<InstallerType> InstallerTypes => Enum.GetValues(typeof(InstallerType)).Cast<InstallerType>();

    [ObservableProperty]
    private bool _requiresAdmin = true;

    [ObservableProperty]
    private DeploymentType _selectedDeploymentType = DeploymentType.Installed;

    public IEnumerable<DeploymentType> DeploymentTypes => Enum.GetValues(typeof(DeploymentType)).Cast<DeploymentType>();

    [ObservableProperty]
    private string _installArgs = string.Empty;

    [ObservableProperty]
    private ObservableCollection<AppGroupSelection> _groupSelections = new();

    [ObservableProperty]
    private string _selectedIconFilePath = string.Empty;

    // --- General Settings Properties ---
    [ObservableProperty]
    private ThemeMode _selectedThemeMode;
    public IEnumerable<ThemeMode> ThemeModes => Enum.GetValues(typeof(ThemeMode)).Cast<ThemeMode>();

    [ObservableProperty]
    private string _portableInstallRoot = string.Empty;

    [ObservableProperty]
    private PostInstallSelectionBehavior _selectedPostInstallBehavior;
    public IEnumerable<PostInstallSelectionBehavior> PostInstallBehaviors => Enum.GetValues(typeof(PostInstallSelectionBehavior)).Cast<PostInstallSelectionBehavior>();

    [ObservableProperty]
    private bool _rememberLastNavigation;

    [ObservableProperty]
    private UiScalePreset _selectedUiScalePreset;
    public IEnumerable<UiScalePreset> UiScalePresets => Enum.GetValues(typeof(UiScalePreset)).Cast<UiScalePreset>();

    [ObservableProperty]
    private ListDensity _selectedListDensity;
    public IEnumerable<ListDensity> ListDensities => Enum.GetValues(typeof(ListDensity)).Cast<ListDensity>();

    public SettingsViewModel()
    {
        _configService = new ConfigService();
        _settingsService = new SettingsService();
        _currentSettings = _settingsService.LoadSettings();

        SelectedThemeMode = _currentSettings.ThemeMode;
        PortableInstallRoot = _currentSettings.PortableInstallRoot;
        SelectedPostInstallBehavior = _currentSettings.PostInstallSelectionBehavior;
        RememberLastNavigation = _currentSettings.RememberLastNavigation;
        SelectedUiScalePreset = _currentSettings.UiScalePreset;
        SelectedListDensity = _currentSettings.ListDensity;

        LoadConfig();
    }

    private void LoadConfig()
    {
        try
        {
            _currentRepo = _configService.LoadConfig();
            if (_currentRepo != null)
            {
                foreach (var group in _currentRepo.Groups)
                {
                    foreach (var section in group.Sections)
                    {
                        GroupSelections.Add(new AppGroupSelection
                        {
                            GroupName = group.Name,
                            SectionName = section.Name,
                            GroupId = group.Id,
                            SectionId = section.Id,
                            IsSelected = false
                        });
                        
                        EditGroupSelections.Add(new AppGroupSelection
                        {
                            GroupName = group.Name,
                            SectionName = section.Name,
                            GroupId = group.Id,
                            SectionId = section.Id,
                            IsSelected = false
                        });
                    }
                }
                
                ExistingApps.Clear();
                foreach (var app in _currentRepo.Apps)
                {
                    ExistingApps.Add(app);
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"讀取設定失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void BrowseIcon()
    {
        OpenFileDialog openFileDialog = new OpenFileDialog
        {
            Title = "選擇自訂圖示",
            Filter = "圖示檔案|*.png;*.jpg;*.jpeg;*.ico"
        };
        
        if (openFileDialog.ShowDialog() == true)
        {
            SelectedIconFilePath = openFileDialog.FileName;
        }
    }

    [RelayCommand]
    private void ResetIcon()
    {
        SelectedIconFilePath = string.Empty;
    }

    [RelayCommand]
    private void BrowseEditIcon()
    {
        OpenFileDialog openFileDialog = new OpenFileDialog
        {
            Title = "選擇自訂圖示",
            Filter = "圖示檔案|*.png;*.jpg;*.jpeg;*.ico"
        };
        
        if (openFileDialog.ShowDialog() == true)
        {
            EditSelectedIconFilePath = openFileDialog.FileName;
        }
    }

    [RelayCommand]
    private void ResetEditIcon()
    {
        EditSelectedIconFilePath = string.Empty;
    }

    [RelayCommand]
    private void SaveSettings(Window window)
    {
        _currentSettings.ThemeMode = SelectedThemeMode;
        _currentSettings.PortableInstallRoot = PortableInstallRoot.Trim();
        _currentSettings.PostInstallSelectionBehavior = SelectedPostInstallBehavior;
        _currentSettings.RememberLastNavigation = RememberLastNavigation;
        _currentSettings.UiScalePreset = SelectedUiScalePreset;
        _currentSettings.ListDensity = SelectedListDensity;
        
        _settingsService.SaveSettings(_currentSettings);
        
        MessageBox.Show("設定已儲存！部分設定可能需要重新啟動程式才會生效。", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    [RelayCommand]
    private void SaveApp(Window window)
    {
        if (string.IsNullOrWhiteSpace(AppId) || string.IsNullOrWhiteSpace(AppName) || string.IsNullOrWhiteSpace(Source))
        {
            MessageBox.Show("請填寫必填欄位 (Id, Name, Source)!", "驗證失敗", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (_currentRepo == null) return;

        string sanitizedId = AppId.Trim();
        if (_currentRepo.Apps.Any(a => a.Id.Equals(sanitizedId, StringComparison.OrdinalIgnoreCase)))
        {
            MessageBox.Show($"ID為 '{sanitizedId}' 的應用程式已存在！", "驗證失敗", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        string finalIconPath = string.Empty;
        if (!string.IsNullOrWhiteSpace(SelectedIconFilePath))
        {
            try
            {
                string userAssetsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "UserAssets", "Icons");
                if (!Directory.Exists(userAssetsDir))
                    Directory.CreateDirectory(userAssetsDir);

                string ext = Path.GetExtension(SelectedIconFilePath);
                string safeId = string.Join("_", sanitizedId.Split(Path.GetInvalidFileNameChars()));
                string newFileName = $"{safeId}{ext}";
                string destination = Path.Combine(userAssetsDir, newFileName);

                File.Copy(SelectedIconFilePath, destination, true);
                finalIconPath = $"UserAssets/Icons/{newFileName}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"圖示複製失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
        }
        else
        {
            finalIconPath = "/Assets/Icons/default.png";
        }

        var newApp = new AppItem
        {
            Id = sanitizedId,
            Name = AppName.Trim(),
            Description = AppDescription.Trim(),
            SourceType = SelectedSourceType,
            Source = Source.Trim(),
            InstallerType = SelectedInstallerType,
            RequiresAdmin = RequiresAdmin,
            DeploymentType = SelectedDeploymentType,
            InstallArgs = InstallArgs.Trim(),
            IconPath = finalIconPath
        };

        _currentRepo.Apps.Add(newApp);

        foreach (var selection in GroupSelections.Where(g => g.IsSelected))
        {
            var group = _currentRepo.Groups.FirstOrDefault(g => g.Id == selection.GroupId);
            var section = group?.Sections.FirstOrDefault(s => s.Id == selection.SectionId);
            if (section != null)
            {
                section.AppIds.Add(newApp.Id);
            }
        }

        try
        {
            _configService.SaveConfig(_currentRepo);
            MessageBox.Show("應用程式新增成功！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            
            // Automatically add to existing apps list to avoid restart parsing
            ExistingApps.Add(newApp);
            
            window.Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"儲存失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void SaveEditApp()
    {
        if (SelectedExistingApp == null) return;

        if (string.IsNullOrWhiteSpace(EditAppId) || string.IsNullOrWhiteSpace(EditAppName) || string.IsNullOrWhiteSpace(EditSource))
        {
            MessageBox.Show("請填寫必填欄位 (Id, Name, Source)!", "驗證失敗", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (_currentRepo == null) return;

        string sanitizedId = EditAppId.Trim();
        string oldId = SelectedExistingApp.Id;

        if (oldId != sanitizedId && _currentRepo.Apps.Any(a => a.Id.Equals(sanitizedId, StringComparison.OrdinalIgnoreCase)))
        {
            MessageBox.Show($"ID為 '{sanitizedId}' 的應用程式已存在！", "驗證失敗", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        string finalIconPath = EditSelectedIconFilePath;
        
        if (!string.IsNullOrWhiteSpace(EditSelectedIconFilePath) && Path.IsPathRooted(EditSelectedIconFilePath) && File.Exists(EditSelectedIconFilePath))
        {
            try
            {
                string userAssetsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "UserAssets", "Icons");
                if (!Directory.Exists(userAssetsDir))
                    Directory.CreateDirectory(userAssetsDir);

                string ext = Path.GetExtension(EditSelectedIconFilePath);
                string safeId = string.Join("_", sanitizedId.Split(Path.GetInvalidFileNameChars()));
                string newFileName = $"{safeId}{ext}";
                string destination = Path.Combine(userAssetsDir, newFileName);

                File.Copy(EditSelectedIconFilePath, destination, true);
                finalIconPath = $"UserAssets/Icons/{newFileName}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"圖示複製失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
        }
        else if (string.IsNullOrWhiteSpace(EditSelectedIconFilePath))
        {
            finalIconPath = "/Assets/Icons/default.png";
        }
        else if (oldId != sanitizedId && EditSelectedIconFilePath.StartsWith("UserAssets/Icons/"))
        {
             try
             {
                 string oldFileName = Path.GetFileName(EditSelectedIconFilePath);
                 string ext = Path.GetExtension(oldFileName);
                 string safeNewId = string.Join("_", sanitizedId.Split(Path.GetInvalidFileNameChars()));
                 string newFileName = $"{safeNewId}{ext}";
                 
                 string userAssetsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "UserAssets", "Icons");
                 string oldPath = Path.Combine(userAssetsDir, oldFileName);
                 string newPath = Path.Combine(userAssetsDir, newFileName);
                 
                 if (File.Exists(oldPath) && !File.Exists(newPath))
                 {
                     File.Move(oldPath, newPath);
                     finalIconPath = $"UserAssets/Icons/{newFileName}";
                 }
                 else if (File.Exists(oldPath) && File.Exists(newPath))
                 {
                     File.Copy(oldPath, newPath, true);
                     finalIconPath = $"UserAssets/Icons/{newFileName}";
                 }
             }
             catch
             {
                 finalIconPath = EditSelectedIconFilePath;
             }
        }

        var app = _currentRepo.Apps.First(a => a.Id == oldId);
        app.Id = sanitizedId;
        app.Name = EditAppName.Trim();
        app.Description = EditAppDescription.Trim();
        app.SourceType = EditSelectedSourceType;
        app.Source = EditSource.Trim();
        app.InstallerType = EditSelectedInstallerType;
        app.RequiresAdmin = EditRequiresAdmin;
        app.DeploymentType = EditSelectedDeploymentType;
        app.InstallArgs = EditInstallArgs.Trim();
        app.IconPath = finalIconPath;

        foreach (var selection in EditGroupSelections)
        {
            var group = _currentRepo.Groups.FirstOrDefault(g => g.Id == selection.GroupId);
            var section = group?.Sections.FirstOrDefault(s => s.Id == selection.SectionId);
            if (section != null)
            {
                if (selection.IsSelected)
                {
                    if (!section.AppIds.Contains(oldId) && !section.AppIds.Contains(sanitizedId))
                        section.AppIds.Add(sanitizedId);
                    else if (oldId != sanitizedId && section.AppIds.Contains(oldId))
                    {
                        int index = section.AppIds.IndexOf(oldId);
                        section.AppIds[index] = sanitizedId;
                    }
                }
                else
                {
                    section.AppIds.Remove(oldId);
                    section.AppIds.Remove(sanitizedId);
                }
            }
        }

        if (oldId != sanitizedId)
        {
            foreach (var preset in _currentRepo.Presets)
            {
                for (int i = 0; i < preset.AppIds.Count; i++)
                {
                    if (preset.AppIds[i] == oldId)
                        preset.AppIds[i] = sanitizedId;
                }
            }
        }

        try
        {
            _configService.SaveConfig(_currentRepo);
            MessageBox.Show("應用程式更新成功！關閉設定視窗後將自動重新載入。", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            
            int index = ExistingApps.IndexOf(SelectedExistingApp);
            if (index >= 0)
            {
                ExistingApps[index] = app;
                SelectedExistingApp = app;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"儲存失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void DeleteApp()
    {
        if (SelectedExistingApp == null) return;
        
        var result = MessageBox.Show($"確定要刪除應用程式 '{SelectedExistingApp.Name}' 嗎？此操作將會從所有分類以及預設集中移除此應用程式。", "確認刪除", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes) return;
        
        if (_currentRepo == null) return;
        
        string targetId = SelectedExistingApp.Id;
        string iconPath = SelectedExistingApp.IconPath;

        var app = _currentRepo.Apps.FirstOrDefault(a => a.Id == targetId);
        if (app != null)
            _currentRepo.Apps.Remove(app);
            
        foreach (var group in _currentRepo.Groups)
        {
            foreach (var section in group.Sections)
            {
                section.AppIds.Remove(targetId);
            }
        }
        
        foreach (var preset in _currentRepo.Presets)
        {
            preset.AppIds.Remove(targetId);
        }
        
        if (!string.IsNullOrEmpty(iconPath) && iconPath.StartsWith("UserAssets/Icons/"))
        {
             bool isIconUsed = _currentRepo.Apps.Any(a => a.IconPath == iconPath);
             if (!isIconUsed)
             {
                 try
                 {
                     string fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, iconPath.Replace('/', Path.DirectorySeparatorChar));
                     if (File.Exists(fullPath))
                     {
                         File.Delete(fullPath);
                     }
                 }
                 catch { }
             }
        }
        
        try
        {
            _configService.SaveConfig(_currentRepo);
            ExistingApps.Remove(SelectedExistingApp);
            SelectedExistingApp = null;
            
            MessageBox.Show("應用程式已刪除！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"刪除失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}

public partial class AppGroupSelection : ObservableObject
{
    public string GroupName { get; set; } = string.Empty;
    public string SectionName { get; set; } = string.Empty;
    public string GroupId { get; set; } = string.Empty;
    public string SectionId { get; set; } = string.Empty;

    [ObservableProperty]
    private bool _isSelected;
}
