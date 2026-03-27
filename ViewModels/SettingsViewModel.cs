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
                    }
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
            window.Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"儲存失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
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
