using System.Text.Json;

namespace BinObjCleaner.Services;
/// <summary>
/// Vom Benutzer gewählte Einstellungen.
/// </summary>
public sealed class AppSettings
{
    /// <summary>Design: <c>Dark</c> oder <c>Light</c>.</summary>
    public string Theme { get; set; } = "Dark";

    /// <summary>Sprache: <c>de</c> oder <c>en</c>.</summary>
    public string Language { get; set; } = "de";

    /// <summary>Zuletzt verwendetes Stammverzeichnis.</summary>
    public string? LastRootPath { get; set; }

    /// <summary>Gesuchte Ordner-Namen.</summary>
    public List<string> FolderNames { get; set; } = ["bin", "obj"];

    public bool IncludeHidden { get; set; }

    public bool FollowSymbolicLinks { get; set; }

    /// <summary>Löschvorgänge über den Papierkorb ausführen.</summary>
    public bool UseRecycleBin { get; set; } = true;

    /// <summary>Nur simulieren, nicht löschen.</summary>
    public bool DryRun { get; set; } = true;

    /// <summary>Ansicht: <c>Flat</c> oder <c>Tree</c>.</summary>
    public string ViewMode { get; set; } = "Flat";
}

/// <summary>
/// Persistiert die Benutzereinstellungen.
/// </summary>
public interface ISettingsService
{
    AppSettings Load();

    void Save(AppSettings settings);
}

/// <summary>
/// Standardimplementierung: JSON-Datei im Anwenderprofil.
/// </summary>
public sealed class SettingsService : ISettingsService
{
    private readonly string _settingsPath;

    public SettingsService(string? settingsPath = null)
    {
        _settingsPath = settingsPath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "BinObjCleaner",
            "settings.json");
    }

    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(_settingsPath))
            {
                return new AppSettings();
            }

            var json = File.ReadAllText(_settingsPath);
            return JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException)
        {
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        try
        {
            var directory = Path.GetDirectoryName(_settingsPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(_settingsPath, JsonSerializer.Serialize(settings, JsonOptions));
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // Einstellungen sind nicht kritisch - ein Fehler darf die App nicht beenden.
        }
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
    };
}
