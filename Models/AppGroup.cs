namespace InstallToolbox.Models;

public class AppGroup
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<AppSection> Sections { get; set; } = new();
}
