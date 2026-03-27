using System.Collections.ObjectModel;

namespace InstallToolbox.ViewModels;

public class AppGroupViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsSeparator { get; set; } = false;
    public ObservableCollection<AppSectionViewModel> Sections { get; set; } = new();
}
