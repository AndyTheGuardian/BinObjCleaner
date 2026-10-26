using System.Globalization;
using System.Resources;

namespace BinObjCleaner.Services;

/// <summary>
/// Stellt lokalisierte Texte bereit.
/// </summary>
public interface ILocalizationService
{
    /// <summary>Sprachcode der aktuellen Sprache (<c>de</c> oder <c>en</c>).</summary>
    string CurrentLanguage { get; }

    /// <summary>Wird ausgelöst, wenn die Sprache gewechselt wurde.</summary>
    event EventHandler? LanguageChanged;

    /// <summary>
    /// Liefert den Text für <paramref name="key"/>. Fehlt der Schlüssel, wird er selbst zurückgegeben.
    /// </summary>
    string Get(string key, params object?[] args);

    /// <summary>Wechselt die Sprache (und damit auch das Zahlenformat).</summary>
    void SetLanguage(string language);
}

/// <summary>
/// Standardimplementierung auf Basis von <see cref="ResourceManager"/>.
/// </summary>
public sealed class LocalizationService : ILocalizationService
{
    private static readonly ResourceManager ResourceManagerInstance =
        new("BinObjCleaner.Strings.Strings", typeof(LocalizationService).Assembly);

    private static readonly string[] SupportedLanguages = ["de", "en"];

    public LocalizationService(string? language = null)
    {
        CurrentLanguage = Resolve(language);
        ApplyCulture(CurrentLanguage);
    }

    public string CurrentLanguage { get; private set; }

    public event EventHandler? LanguageChanged;

    public string Get(string key, params object?[] args)
    {
        var text = ResourceManagerInstance.GetString(key, CultureInfo.CurrentUICulture) ?? key;

        if (args.Length == 0)
        {
            return text;
        }

        try
        {
            return string.Format(CultureInfo.CurrentCulture, text, args);
        }
        catch (FormatException)
        {
            return text;
        }
    }

    public void SetLanguage(string language)
    {
        var resolved = Resolve(language);

        if (string.Equals(resolved, CurrentLanguage, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        CurrentLanguage = resolved;
        ApplyCulture(resolved);
        LanguageChanged?.Invoke(this, EventArgs.Empty);
    }

    private static string Resolve(string? language)
    {
        if (string.IsNullOrWhiteSpace(language))
        {
            return "de";
        }

        var twoLetter = language.Split('-')[0].ToLowerInvariant();
        return SupportedLanguages.Contains(twoLetter) ? twoLetter : "de";
    }

    private static void ApplyCulture(string language)
    {
        var culture = new CultureInfo(language);
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
    }
}
