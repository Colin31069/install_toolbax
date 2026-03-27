using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using InstallToolbox.Models;

namespace InstallToolbox.Services;

public class SettingsService
{
    private readonly string _settingsFilePath;

    public SettingsService()
    {
        _settingsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "user_settings.json");
    }

    public UserSettings LoadSettings()
    {
        if (!File.Exists(_settingsFilePath))
        {
            return new UserSettings();
        }

        try
        {
            string json = File.ReadAllText(_settingsFilePath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            };
            options.Converters.Add(new JsonStringEnumConverter());
            return JsonSerializer.Deserialize<UserSettings>(json, options) ?? new UserSettings();
        }
        catch
        {
            return new UserSettings();
        }
    }

    public void SaveSettings(UserSettings settings)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            options.Converters.Add(new JsonStringEnumConverter());

            string json = JsonSerializer.Serialize(settings, options);
            File.WriteAllText(_settingsFilePath, json, System.Text.Encoding.UTF8);
        }
        catch { }
    }
}
