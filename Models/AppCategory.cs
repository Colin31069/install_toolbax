namespace InstallToolbox.Models;

public class AppCategory
{
    public string Name { get; set; } = string.Empty;
    public List<AppItem> Apps { get; set; } = new();
}
