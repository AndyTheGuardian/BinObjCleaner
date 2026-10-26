using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Styling;
using BinObjCleaner.Services;

namespace BinObjCleaner.Views;

/// <summary>
/// Setzt das Design der Anwendung.
/// </summary>
public sealed class AppThemeService : IThemeService
{
    public AppTheme CurrentTheme { get; private set; } = AppTheme.Dark;

    public void Apply(AppTheme theme)
    {
        CurrentTheme = theme;

        var application = Application.Current;
        if (application is null)
        {
            return;
        }

        application.RequestedThemeVariant = theme == AppTheme.Dark
            ? ThemeVariant.Dark
            : ThemeVariant.Light;
    }
}
