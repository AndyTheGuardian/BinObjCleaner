using System.Globalization;
using Avalonia.Media;
using BinObjCleaner.Tests;

namespace BinObjCleaner.Tests;

[Collection(AvaloniaCollection.Name)]
public sealed class FormattedTextTests
{
    [Fact]
    public void MaxTextWidth_Begrenzt_Die_Breite_Auch_Mit_MaxLineCount()
    {
        var formatted = new FormattedText(
            "Ein sehr langer Verzeichnisname, der in keiner Zeile hineinpasst",
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface("Segoe UI"),
            12,
            Brushes.White)
        {
            MaxLineCount = 1,
            Trimming = TextTrimming.CharacterEllipsis,
        };

        formatted.MaxTextWidth = 120;

        Assert.True(formatted.Width <= 120, $"Die Breite war {formatted.Width}.");
        Assert.True(formatted.Height < 30, $"Die Höhe war {formatted.Height} (erwartet: eine Zeile).");
    }

    [Fact]
    public void Ohne_MaxTextWidth_Bleibt_Die_Volle_Breite()
    {
        var formatted = new FormattedText(
            "Ein sehr langer Verzeichnisname, der in keiner Zeile hineinpasst",
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface("Segoe UI"),
            12,
            Brushes.White)
        {
            MaxLineCount = 1,
            Trimming = TextTrimming.CharacterEllipsis,
        };

        Assert.True(formatted.Width > 120, $"Die Breite war {formatted.Width}.");
    }
}
