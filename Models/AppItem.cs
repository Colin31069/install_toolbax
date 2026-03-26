using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace InstallToolbox.Models;

public partial class AppItem : ObservableObject
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public SourceType SourceType { get; set; }
    
    public string Source { get; set; } = string.Empty;
    
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public InstallerType InstallerType { get; set; }
    
    public string InstallArgs { get; set; } = string.Empty;
    
    public bool RequiresAdmin { get; set; } = true;
    
    public List<string> Dependencies { get; set; } = new();
    
    public int RetryCount { get; set; } = 2;
    
    public InstallCheck? InstallCheck { get; set; }
    
    public string CachePolicy { get; set; } = "DeleteOnSuccess";

    // 狀態相關 (UI 繫結用)
    [ObservableProperty]
    [JsonIgnore]
    private AppStatus _status = AppStatus.Pending;

    [ObservableProperty]
    [JsonIgnore]
    private int _progress = 0;

    [ObservableProperty]
    [JsonIgnore]
    private string _errorMessage = string.Empty;

    // 是否被使用者勾選
    [ObservableProperty]
    [JsonIgnore]
    private bool _isSelected = false;
}
