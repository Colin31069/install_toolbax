using System.Windows;
using InstallToolbox.Models;
using InstallToolbox.Services;
using Wpf.Ui.Appearance;

namespace InstallToolbox;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        ApplyInitialTheme();

        base.OnStartup(e);

        MainWindow = new MainWindow();
        MainWindow.Show();
    }

    internal static ApplicationTheme ResolveApplicationTheme(ThemeMode themeMode)
    {
        return themeMode switch
        {
            ThemeMode.Light => ApplicationTheme.Light,
            ThemeMode.Dark => ApplicationTheme.Dark,
            _ => ApplicationThemeManager.GetSystemTheme() == SystemTheme.Dark
                ? ApplicationTheme.Dark
                : ApplicationTheme.Light
        };
    }

    private static void ApplyInitialTheme()
    {
        var settingsService = new SettingsService();
        var settings = settingsService.LoadSettings();

        ApplicationThemeManager.Apply(ResolveApplicationTheme(settings.ThemeMode));
    }
}
