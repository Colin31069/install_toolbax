using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace InstallToolbox.Converters;

public class IconResolverConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        string path = value as string ?? string.Empty;
        string defaultPackUri = "pack://application:,,,/Assets/Icons/default.png";

        BitmapImage? TryLoadPackUri(string uriStr)
        {
            try
            {
                var img = new BitmapImage();
                img.BeginInit();
                img.UriSource = new Uri(uriStr, UriKind.Absolute);
                img.CacheOption = BitmapCacheOption.OnLoad;
                img.EndInit();
                return img;
            }
            catch { return null; }
        }

        BitmapImage? TryLoadFileDir(string localPath)
        {
            try
            {
                if (!File.Exists(localPath)) return null;
                var img = new BitmapImage();
                img.BeginInit();
                img.UriSource = new Uri(localPath, UriKind.Absolute);
                img.CacheOption = BitmapCacheOption.OnLoad;
                img.EndInit();
                return img;
            }
            catch { return null; }
        }

        BitmapImage defaultIcon = TryLoadPackUri(defaultPackUri) 
            ?? TryLoadFileDir(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Icons", "default.png"))!;

        if (string.IsNullOrWhiteSpace(path))
            return defaultIcon;

        if (path.StartsWith("/Assets/Icons/"))
        {
            string packUri = "pack://application:,,," + path;
            var img = TryLoadPackUri(packUri);
            return img ?? defaultIcon;
        }

        if (path.StartsWith("UserAssets/Icons/") || path.StartsWith("UserAssets\\Icons\\"))
        {
            string absoluteUserAssetPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path.Replace('/', '\\'));
            var img = TryLoadFileDir(absoluteUserAssetPath);
            return img ?? defaultIcon;
        }

        var absoluteImg = TryLoadFileDir(path);
        return absoluteImg ?? defaultIcon;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
