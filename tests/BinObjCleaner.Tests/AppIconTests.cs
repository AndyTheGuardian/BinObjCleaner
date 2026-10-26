using BinObjCleaner.Controls;

namespace BinObjCleaner.Tests;

[Collection(AvaloniaCollection.Name)]
public sealed class AppIconTests
{
    [Fact]
    public void Jedes_Symbol_Setzt_Seine_Geometrie()
    {
        foreach (var kind in Enum.GetValues<AppIconKind>())
        {
            var icon = new AppIcon { Kind = kind };

            Assert.True(icon.Data is not null, $"Die Geometrie von '{kind}' wurde nicht gesetzt.");
        }
    }

    [Fact]
    public void Das_Standardsymbol_Hat_Bereits_Im_Konstruktor_Eine_Geometrie()
    {
        // "Search" ist der Standardwert des Enums. Wird er aus dem XAML gesetzt, ändert
        // sich der Wert nicht und es feuert keine Property-Anderung - die Geometrie
        // muss deshalb schon im Konstruktor gesetzt werden.
        var icon = new AppIcon();

        Assert.Equal(AppIconKind.Search, icon.Kind);
        Assert.True(icon.Data is not null, "Das Standardsymbol hat keine Geometrie.");
    }

    [Fact]
    public void Das_Wechseln_Des_Symbols_Aendert_Die_Geometrie()
    {
        var icon = new AppIcon { Kind = AppIconKind.Search };
        var search = icon.Data;

        icon.Kind = AppIconKind.Trash;

        Assert.NotSame(search, icon.Data);
    }

    [Fact]
    public void Jede_Geometrie_Hat_Eine_Ausdehnung()
    {
        foreach (var kind in Enum.GetValues<AppIconKind>())
        {
            var icon = new AppIcon { Kind = kind };

            var bounds = icon.Data!.Bounds;

            Assert.True(bounds.Width > 0 && bounds.Height > 0, $"Die Geometrie von '{kind}' ist leer.");
        }
    }
}
