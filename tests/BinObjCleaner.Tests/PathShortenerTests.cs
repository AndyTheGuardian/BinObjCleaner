using BinObjCleaner.Controls;

namespace BinObjCleaner.Tests;

public sealed class PathShortenerTests
{
    private static string Shorten(string path, double maxLength) =>
        PathShortener.Shorten(path, maxLength, text => text.Length);

    [Fact]
    public void Ein_Passender_Pfad_bleibt_Unveraendert()
    {
        Assert.Equal("src\\Shop.Api", Shorten("src\\Shop.Api", 100));
    }

    [Fact]
    public void Kuerzt_Von_Vorne_An_Verzeichnisgrenzen()
    {
        // "…Demo\ChatDemo_Provider" ist kürzer als der vollständige Pfad,
        // behält aber den Projektordner.
        var result = Shorten("Tag 02 - Lang\\Demo\\ChatDemo_Provider", 23);

        Assert.Equal("…Demo\\ChatDemo_Provider", result);
    }

    [Fact]
    public void Kuerzt_Weiter_Wenn_Der_Ordner_Nicht_Reicht()
    {
        var result = Shorten("Tag 02 - Lang\\Demo\\ChatDemo_Provider", 18);

        Assert.Equal("…ChatDemo_Provider", result);
    }

    [Fact]
    public void Beendet_Mit_Einem_Auslassungszeichen()
    {
        var result = Shorten("a\\b\\c\\d\\e", 6);

        Assert.StartsWith("…", result);
        Assert.True(result.Length <= 6);
    }

    [Fact]
    public void Passt_Nichts_Hinein_Wird_Abgekuerzt()
    {
        var result = Shorten("a\\b", 1);

        Assert.Equal("…", result);
    }

    [Fact]
    public void Ein_Leerer_Pfad_Bleibt_Leer()
    {
        Assert.Equal(string.Empty, Shorten(string.Empty, 50));
    }
}
