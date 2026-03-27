namespace InstallToolbox.Models;

public class AppsRepository
{
    public string Version { get; set; } = "2.0";
    public List<AppItem> Apps { get; set; } = new();
    public List<AppGroup> Groups { get; set; } = new();
    public List<AppPreset> Presets { get; set; } = new();
}
