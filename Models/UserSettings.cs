using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace InstallToolbox.Models;

public enum ThemeMode { System, Light, Dark }
public enum PostInstallSelectionBehavior { KeepAll, ClearSucceeded, ClearAll }
public enum UiScalePreset { Small, Medium, Large }
public enum ListDensity { Compact, Comfortable }

public class UserSettings
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ThemeMode ThemeMode { get; set; } = ThemeMode.Dark; // Default to Dark as per new requirement

    public string PortableInstallRoot { get; set; } = string.Empty;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public PostInstallSelectionBehavior PostInstallSelectionBehavior { get; set; } = PostInstallSelectionBehavior.KeepAll;

    public bool RememberLastNavigation { get; set; } = true;
    public string LastSelectedGroupId { get; set; } = string.Empty;
    public string LastSelectedSectionId { get; set; } = string.Empty;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public UiScalePreset UiScalePreset { get; set; } = UiScalePreset.Medium;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ListDensity ListDensity { get; set; } = ListDensity.Comfortable;

    public List<string> PresetOrder { get; set; } = new();
}
