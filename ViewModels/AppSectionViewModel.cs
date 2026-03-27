using System.Collections.ObjectModel;
using InstallToolbox.Models;

namespace InstallToolbox.ViewModels;

public class AppSectionViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ObservableCollection<AppItem> Apps { get; set; } = new();
}
