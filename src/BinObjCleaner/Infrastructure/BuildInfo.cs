using System.Reflection;

namespace BinObjCleaner.Infrastructure;

/// <summary>
/// Angaben zum laufenden Build - wird in der Fußleiste angezeigt, damit sofort
/// erkennbar ist, welche Version gerade läuft.
/// </summary>
public static class BuildInfo
{
    /// <summary>Version der Anwendung (z. B. <c>1.0.0</c>).</summary>
    public static string Version { get; } = ResolveVersion();

    /// <summary>Zeitpunkt des Builds (lokal, z. B. <c>22.10.2026 15:12</c>).</summary>
    public static string BuildTime { get; } = ResolveBuildTime();

    /// <summary>Kurzform für die Anzeige, z. B. <c>v1.0.0 · 22.10. 15:12</c>.</summary>
    public static string ShortText => $"v{Version} · {BuildTime}";

    /// <summary>Ausführliche Angaben für den Tooltip.</summary>
    public static string Details =>
        $"BinObj Cleaner {Version}{Environment.NewLine}Build: {BuildTime}{Environment.NewLine}{Path.GetDirectoryName(AssemblyLocation)}";

    private static string AssemblyLocation => Assembly.GetExecutingAssembly().Location;

    private static string ResolveVersion()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        return version is null ? "0.0.0" : $"{version.Major}.{version.Minor}.{version.Build}";
    }

    private static string ResolveBuildTime()
    {
        try
        {
            var location = AssemblyLocation;
            if (!string.IsNullOrEmpty(location) && File.Exists(location))
            {
                return File.GetLastWriteTime(location).ToString("dd.MM.yyyy HH:mm");
            }
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // Zeitstempel ist nur informativ.
        }

        return "unbekannt";
    }
}
