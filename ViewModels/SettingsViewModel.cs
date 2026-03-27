using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InstallToolbox.Models;
using InstallToolbox.Services;
using System.Windows;
using System.Linq;
using System;

namespace InstallToolbox.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ConfigService _configService;
    private AppsRepository? _currentRepo;

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

    public SettingsViewModel()
    {
        _configService = new ConfigService();
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
    private void SaveApp(Window window)
    {
        if (string.IsNullOrWhiteSpace(AppId) || string.IsNullOrWhiteSpace(AppName) || string.IsNullOrWhiteSpace(Source))
        {
            MessageBox.Show("請填寫必填欄位 (Id, Name, Source)!", "驗證失敗", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (_currentRepo == null) return;

        if (_currentRepo.Apps.Any(a => a.Id.Equals(AppId, StringComparison.OrdinalIgnoreCase)))
        {
            MessageBox.Show($"ID為 '{AppId}' 的應用程式已存在！", "驗證失敗", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var newApp = new AppItem
        {
            Id = AppId.Trim(),
            Name = AppName.Trim(),
            Description = AppDescription.Trim(),
            SourceType = SelectedSourceType,
            Source = Source.Trim(),
            InstallerType = SelectedInstallerType,
            RequiresAdmin = RequiresAdmin,
            DeploymentType = SelectedDeploymentType,
            InstallArgs = InstallArgs.Trim()
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
