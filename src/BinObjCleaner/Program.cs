using Avalonia;

namespace BinObjCleaner;

internal static class Program
{
    // Avalonia-Konfiguration, nicht verändern.
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    // Öffentlich, damit auch der Avalonia-Designer (und XAML-Preview) darauf zugreifen kann.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
}
