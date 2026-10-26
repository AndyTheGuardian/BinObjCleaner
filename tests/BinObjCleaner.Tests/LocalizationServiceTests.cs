using BinObjCleaner.Services;

namespace BinObjCleaner.Tests;

public sealed class LocalizationServiceTests
{
    [Fact]
    public void Liefert_Deutschen_Text_Bei_Sprache_De()
    {
        var service = new LocalizationService("de");

        Assert.Equal("Durchsuchen…", service.Get("Browse"));
    }

    [Fact]
    public void Liefert_Englischen_Text_Bei_Sprache_En()
    {
        var service = new LocalizationService("en");

        Assert.Equal("Browse…", service.Get("Browse"));
    }

    [Fact]
    public void Wechselt_Die_Sprache_Zur_Laufzeit_Und_Meldet_Das()
    {
        var service = new LocalizationService("de");
        var changed = false;
        service.LanguageChanged += (_, _) => changed = true;

        service.SetLanguage("en");

        Assert.Equal("en", service.CurrentLanguage);
        Assert.True(changed);
        Assert.Equal("Browse…", service.Get("Browse"));
    }

    [Fact]
    public void Setzt_Platzhalter_Fuer_Unbekannte_Schluessel()
    {
        var service = new LocalizationService("de");

        Assert.Equal("Unbekannt", service.Get("Unbekannt"));
    }

    [Fact]
    public void Formatiert_Platzhalter_Mit_Argumenten()
    {
        var service = new LocalizationService("de");

        var text = service.Get("ConfirmDeleteTitle", 12);

        Assert.Contains("12", text);
    }
}
