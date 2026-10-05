using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using GameLauncher.Models;
using GameLauncher.ViewModels;

namespace GameLauncher.Converters;

/// <summary>GameCatalogItem → 侧栏图标 ImageSource（Assets/icons/{game_id}.*）</summary>
public sealed class GameIconConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is GameCatalogItem g)
            return GameDetailViewModel.ResolveIcon(g);
        if (value is string id)
            return GameDetailViewModel.ResolveIcon(new GameCatalogItem { GameId = id });
        return null;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>有图标则 Visible，否则 Collapsed（invert 参数反过来）</summary>
public sealed class NullImageToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var has = value is ImageSource;
        if (parameter as string == "invert") has = !has;
        return has
            ? System.Windows.Visibility.Visible
            : System.Windows.Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
