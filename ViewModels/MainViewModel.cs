using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InstallToolbox.Models;
using InstallToolbox.Services;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InstallToolbox.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly ConfigService _configService;
    private readonly InstallEngine _installEngine;
    private readonly SettingsService _settingsService;
    private UserSettings _currentSettings;

    [ObservableProperty]
    private string _themeIcon = "WeatherMoon24";

    private List<AppItem> _allApps = new();

    [ObservableProperty]
    private ObservableCollection<AppGroupViewModel> _groups = new();

    [ObservableProperty]
    private AppGroupViewModel? _selectedGroup;

    [ObservableProperty]
    private AppSectionViewModel? _selectedSection;

    [ObservableProperty]
    private double _uiScaleRate = 1.0;

    [ObservableProperty]
    private double _listDensityLineHeight = 50.0;

    partial void OnSelectedGroupChanged(AppGroupViewModel? value)
    {
        SelectedSection = value?.Sections.FirstOrDefault();
        
        if (_currentSettings != null && _currentSettings.RememberLastNavigation && value != null)
        {
            _currentSettings.LastSelectedGroupId = value.Id;
            _settingsService.SaveSettings(_currentSettings);
        }
    }

    partial void OnSelectedSectionChanged(AppSectionViewModel? value)
    {
        if (_currentSettings != null && _currentSettings.RememberLastNavigation && value != null)
        {
            _currentSettings.LastSelectedSectionId = value.Id;
            _settingsService.SaveSettings(_currentSettings);
        }
    }

    [ObservableProperty]
    private ObservableCollection<AppPreset> _presets = new();

    [ObservableProperty]
    private string _globalStatusMessage = "就緒";

    [ObservableProperty]
    private int _globalProgress = 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotInstalling))]
    private bool _isInstalling = false;

    public bool IsNotInstalling => !IsInstalling;

    public MainViewModel()
    {
        _configService = new ConfigService();
        _settingsService = new SettingsService();
        _currentSettings = _settingsService.LoadSettings();
        _installEngine = new InstallEngine();
        
        ApplyUserSettings();
        LoadData();
        RestoreNavigation();
    }

    private void ApplyUserSettings()
    {
        var currentTheme = Wpf.Ui.Appearance.ApplicationThemeManager.GetAppTheme();
        Wpf.Ui.Appearance.ApplicationTheme targetTheme = currentTheme;

        if (_currentSettings.ThemeMode == ThemeMode.System)
        {
            var systemTheme = Wpf.Ui.Appearance.ApplicationThemeManager.GetSystemTheme();
            targetTheme = systemTheme == Wpf.Ui.Appearance.SystemTheme.Dark 
                ? Wpf.Ui.Appearance.ApplicationTheme.Dark 
                : Wpf.Ui.Appearance.ApplicationTheme.Light;
        }
        else if (_currentSettings.ThemeMode == ThemeMode.Light)
            targetTheme = Wpf.Ui.Appearance.ApplicationTheme.Light;
        else if (_currentSettings.ThemeMode == ThemeMode.Dark)
            targetTheme = Wpf.Ui.Appearance.ApplicationTheme.Dark;

        Wpf.Ui.Appearance.ApplicationThemeManager.Apply(targetTheme);
        ThemeIcon = targetTheme == Wpf.Ui.Appearance.ApplicationTheme.Dark ? "WeatherSunny24" : "WeatherMoon24";

        UiScaleRate = _currentSettings.UiScalePreset switch
        {
            UiScalePreset.Small => 0.85,
            UiScalePreset.Large => 1.25,
            _ => 1.0
        };

        ListDensityLineHeight = _currentSettings.ListDensity switch
        {
            ListDensity.Compact => 40.0,
            ListDensity.Comfortable => 60.0,
            _ => 50.0
        };
    }

    private void RestoreNavigation()
    {
        if (!_currentSettings.RememberLastNavigation) return;

        var targetGroup = Groups.FirstOrDefault(g => g.Id == _currentSettings.LastSelectedGroupId);
        if (targetGroup != null)
        {
            SelectedGroup = targetGroup;
            var targetSection = targetGroup.Sections.FirstOrDefault(s => s.Id == _currentSettings.LastSelectedSectionId);
            if (targetSection != null)
            {
                SelectedSection = targetSection;
            }
        }
    }

    private void LoadData()
    {
        try
        {
            var repo = _configService.LoadConfig();
            if (repo != null)
            {
                _allApps = repo.Apps;
                var dict = new Dictionary<string, AppItem>();
                foreach (var app in repo.Apps)
                {
                    dict[app.Id] = app;
                }

                Groups.Clear();
                Presets.Clear();

                var allAppsGroup = new AppGroupViewModel { Id = "all-apps", Name = "所有程式", Description = "所有支援的軟體清單" };
                var allAppsSection = new AppSectionViewModel { Id = "all-apps-section", Name = "所有程式清單", Description = "不分類顯示所有軟體" };
                foreach (var app in repo.Apps)
                {
                    allAppsSection.Apps.Add(app);
                }
                allAppsGroup.Sections.Add(allAppsSection);
                Groups.Add(allAppsGroup);

                Groups.Add(new AppGroupViewModel { IsSeparator = true });

                var generatedPresets = new List<AppPreset>();

                foreach (var g in repo.Groups)
                {
                    if (g.Id == "portable-toolkit")
                    {
                        var groupVm = new AppGroupViewModel { Id = g.Id, Name = g.Name, Description = g.Description };
                        foreach (var s in g.Sections)
                        {
                            var sectionVm = new AppSectionViewModel { Id = s.Id, Name = s.Name, Description = s.Description };
                            foreach (var id in s.AppIds)
                            {
                                if (dict.TryGetValue(id, out var appItem))
                                {
                                    sectionVm.Apps.Add(appItem);
                                }
                            }
                            groupVm.Sections.Add(sectionVm);
                        }
                        Groups.Add(groupVm);
                    }
                    else
                    {
                        // Convert sections to Groups
                        foreach (var s in g.Sections)
                        {
                            var newGroupVm = new AppGroupViewModel { Id = s.Id, Name = s.Name, Description = s.Description };
                            var newSectionVm = new AppSectionViewModel { Id = s.Id + "-sec", Name = s.Name, Description = s.Description };
                            foreach (var id in s.AppIds)
                            {
                                if (dict.TryGetValue(id, out var appItem))
                                {
                                    newSectionVm.Apps.Add(appItem);
                                }
                            }
                            newGroupVm.Sections.Add(newSectionVm);
                            Groups.Add(newGroupVm);
                        }

                        // Convert former parent groups to presets
                        var groupPreset = new AppPreset 
                        { 
                            Id = "group-preset-" + g.Id, 
                            Name = g.Name, 
                            Description = g.Description 
                        };
                        var presetAppIds = new HashSet<string>();
                        foreach (var s in g.Sections)
                        {
                            foreach (var id in s.AppIds) presetAppIds.Add(id);
                        }
                        groupPreset.AppIds.AddRange(presetAppIds);
                        generatedPresets.Add(groupPreset);
                    }
                }

                var allPresets = new List<AppPreset>();
                allPresets.AddRange(generatedPresets);
                allPresets.AddRange(repo.Presets);

                if (_currentSettings.PresetOrder != null && _currentSettings.PresetOrder.Count > 0)
                {
                    allPresets = allPresets.OrderBy(p => 
                    {
                        int index = _currentSettings.PresetOrder.IndexOf(p.Id);
                        return index != -1 ? index : int.MaxValue;
                    }).ToList();
                }

                foreach (var p in allPresets)
                {
                    Presets.Add(p);
                }

                if (SelectedGroup == null || !Groups.Contains(SelectedGroup))
                {
                    SelectedGroup = Groups.FirstOrDefault(g => !g.IsSeparator);
                }
            }
        }
        catch (Exception ex)
        {
            GlobalStatusMessage = "讀取配置失敗: " + ex.Message;
        }
    }

    public void SavePresetOrder()
    {
        if (_currentSettings != null)
        {
            _currentSettings.PresetOrder = Presets.Select(p => p.Id).ToList();
            _settingsService.SaveSettings(_currentSettings);
        }
    }

    [RelayCommand]
    private void ApplyPreset(AppPreset? preset)
    {
        if (preset == null) return;

        foreach (var id in preset.AppIds)
        {
            var app = _allApps.FirstOrDefault(a => a.Id == id);
            if (app != null)
            {
                app.IsSelected = true;
            }
        }
    }

    [RelayCommand]
    private async Task StartBatchInstallAsync()
    {
        if (IsInstalling) return;

        var selectedApps = _allApps.Where(a => a.IsSelected && (a.Status == AppStatus.Pending || a.Status == AppStatus.Failed)).ToList();

        if (selectedApps.Count == 0)
        {
            GlobalStatusMessage = "請先勾選需安裝的應用程式";
            return;
        }

        IsInstalling = true;
        GlobalStatusMessage = "開始解析依賴關係並準備安裝...";
        GlobalProgress = 0;

        int total = selectedApps.Count;
        int completed = 0;

        foreach (var app in selectedApps)
        {
            GlobalStatusMessage = $"正在處理: {app.Name}";
            
            bool result = await _installEngine.InstallAppAsync(app, (p) => 
            {
                app.Progress = p;
            });

            if (result)
            {
                app.Status = AppStatus.Success;
                app.Progress = 100;
            }

            completed++;
            GlobalProgress = (int)((double)completed / total * 100);
        }

        GlobalStatusMessage = "所有選擇的項目處理完畢";
        IsInstalling = false;

        if (_currentSettings.PostInstallSelectionBehavior == PostInstallSelectionBehavior.ClearSucceeded)
        {
            foreach (var app in selectedApps.Where(a => a.Status == AppStatus.Success || a.Status == AppStatus.Skipped))
            {
                 app.IsSelected = false;
            }
        }
        else if (_currentSettings.PostInstallSelectionBehavior == PostInstallSelectionBehavior.ClearAll)
        {
            foreach (var app in selectedApps)
            {
                 app.IsSelected = false;
            }
        }
    }

    [RelayCommand]
    private void ToggleTheme()
    {
        var currentTheme = Wpf.Ui.Appearance.ApplicationThemeManager.GetAppTheme();
        var newTheme = currentTheme == Wpf.Ui.Appearance.ApplicationTheme.Dark 
            ? Wpf.Ui.Appearance.ApplicationTheme.Light 
            : Wpf.Ui.Appearance.ApplicationTheme.Dark;
        
        Wpf.Ui.Appearance.ApplicationThemeManager.Apply(newTheme);
        ThemeIcon = newTheme == Wpf.Ui.Appearance.ApplicationTheme.Dark ? "WeatherSunny24" : "WeatherMoon24";
        
        _currentSettings.ThemeMode = newTheme == Wpf.Ui.Appearance.ApplicationTheme.Dark ? ThemeMode.Dark : ThemeMode.Light;
        _settingsService.SaveSettings(_currentSettings);
    }

    [RelayCommand]
    private void OpenSettings()
    {
        var settingsWindow = new SettingsWindow
        {
            Owner = App.Current.MainWindow
        };
        settingsWindow.ShowDialog();
        
        _currentSettings = _settingsService.LoadSettings();
        ApplyUserSettings();
        LoadData();
    }
}
