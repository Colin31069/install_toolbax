using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InstallToolbox.Models;
using InstallToolbox.Services;

namespace InstallToolbox.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly ConfigService _configService;
    private readonly InstallEngine _installEngine;

    [ObservableProperty]
    private string _themeIcon = "WeatherMoon24";

    private List<AppItem> _allApps = new();

    [ObservableProperty]
    private ObservableCollection<AppGroupViewModel> _groups = new();

    [ObservableProperty]
    private AppGroupViewModel? _selectedGroup;

    [ObservableProperty]
    private AppSectionViewModel? _selectedSection;

    partial void OnSelectedGroupChanged(AppGroupViewModel? value)
    {
        SelectedSection = value?.Sections.FirstOrDefault();
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
        _installEngine = new InstallEngine();
        LoadData();
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

                // Append "All Apps" group
                var allAppsGroup = new AppGroupViewModel { Id = "all-apps", Name = "所有程式", Description = "所有支援的軟體清單" };
                var allAppsSection = new AppSectionViewModel { Id = "all-apps-section", Name = "所有程式清單", Description = "不分類顯示所有軟體" };
                foreach (var app in repo.Apps)
                {
                    allAppsSection.Apps.Add(app);
                }
                allAppsGroup.Sections.Add(allAppsSection);
                Groups.Add(allAppsGroup);

                foreach (var g in repo.Groups)
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

                foreach (var p in repo.Presets)
                {
                    Presets.Add(p);
                }

                SelectedGroup = Groups.FirstOrDefault();
            }
        }
        catch (Exception ex)
        {
            GlobalStatusMessage = "讀取配置失敗: " + ex.Message;
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
    }

    [RelayCommand]
    private void ToggleTheme()
    {
        System.Diagnostics.Debug.WriteLine("[Theme] ToggleThemeCommand called!");
        var currentTheme = Wpf.Ui.Appearance.ApplicationThemeManager.GetAppTheme();
        var newTheme = currentTheme == Wpf.Ui.Appearance.ApplicationTheme.Dark 
            ? Wpf.Ui.Appearance.ApplicationTheme.Light 
            : Wpf.Ui.Appearance.ApplicationTheme.Dark;
        
        System.Diagnostics.Debug.WriteLine($"[Theme] Changing from {currentTheme} to {newTheme}");
        Wpf.Ui.Appearance.ApplicationThemeManager.Apply(newTheme);

        ThemeIcon = newTheme == Wpf.Ui.Appearance.ApplicationTheme.Dark ? "WeatherSun24" : "WeatherMoon24";
        System.Diagnostics.Debug.WriteLine($"[Theme] Finished toggling theme. New Icon is {ThemeIcon}");
    }
}
