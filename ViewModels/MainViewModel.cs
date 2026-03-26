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
    private ObservableCollection<AppCategory> _categories = new();

    [ObservableProperty]
    private AppCategory? _selectedCategory;

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
                var dict = new Dictionary<string, AppCategory>();
                foreach (var c in repo.Categories)
                {
                    dict[c] = new AppCategory { Name = c };
                }

                foreach (var app in repo.Apps)
                {
                    if (dict.TryGetValue(app.Category, out var cat))
                    {
                        cat.Apps.Add(app);
                    }
                }

                foreach (var cat in dict.Values)
                {
                    if (cat.Apps.Count > 0)
                        Categories.Add(cat);
                }

                SelectedCategory = Categories.FirstOrDefault();
            }
        }
        catch (Exception ex)
        {
            GlobalStatusMessage = "讀取配置失敗: " + ex.Message;
        }
    }

    [RelayCommand]
    private async Task StartBatchInstallAsync()
    {
        if (IsInstalling) return;

        var selectedApps = Categories.SelectMany(c => c.Apps).Where(a => a.IsSelected && (a.Status == AppStatus.Pending || a.Status == AppStatus.Failed)).ToList();

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

        // MVP 先以循序方式安裝 (未處理完整的 DAG 拓樸解析，僅單純依照勾選清單安裝)
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
}
