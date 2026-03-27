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
            var defaultSettings = new UserSettings();
            ApplyDefaultDarkMigration(defaultSettings);
            return defaultSettings;
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
            var settings = JsonSerializer.Deserialize<UserSettings>(json, options) ?? new UserSettings();

            if (ApplyDefaultDarkMigration(settings))
            {
                SaveSettings(settings);
            }

            return settings;
        }
        catch
        {
            var fallbackSettings = new UserSettings();
            ApplyDefaultDarkMigration(fallbackSettings);
            return fallbackSettings;
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

    private static bool ApplyDefaultDarkMigration(UserSettings settings)
    {
        if (settings.HasAppliedDefaultDarkMigration)
        {
            return false;
        }

        settings.HasAppliedDefaultDarkMigration = true;

        if (settings.ThemeMode == ThemeMode.Light)
        {
            settings.ThemeMode = ThemeMode.Dark;
        }

        return true;
    }
}
