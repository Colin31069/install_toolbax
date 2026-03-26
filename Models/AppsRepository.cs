namespace InstallToolbox.Models;

public class AppsRepository
{
    public string Version { get; set; } = string.Empty;
    public List<string> Categories { get; set; } = new();
    public List<AppItem> Apps { get; set; } = new();
}
