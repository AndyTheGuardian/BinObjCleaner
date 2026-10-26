using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace BinObjCleaner.Controls;

/// <summary>
/// Stellt einen Pfad als Verzeichnisanteil (gedimmt) + Trennzeichen + Ordnernamen (fett) dar.
/// </summary>
/// <remarks>
/// <para>
/// Die Kürzung erfolgt zur Render-Zeit anhand der tatsächlich verfügbaren Breite.
/// Das ist nötig, weil die während des Measure-Durchlaufs bekannte Breite nicht der
/// endgültigen Spaltenbreite entspricht - der Text würde sonst in die Nachbarspalte
/// hineinlaufen.
/// </para>
/// <para>
/// Gekürzt wird an Verzeichnisgrenzen und von vorne, damit der aussagekräftige Teil
/// des Pfades (Projektordner) erhalten bleibt. Der Ordnername ist immer sichtbar.
/// </para>
/// </remarks>
public sealed class PathText : Control
{
    private const char Separator = '\\';
    private const string Ellipsis = "…";

    /// <summary>Verzeichnisanteil des Pfades (ohne Trennzeichen und Ordnernamen).</summary>
    public static readonly StyledProperty<string?> DirectoryProperty =
        AvaloniaProperty.Register<PathText, string?>(nameof(Directory));

    /// <summary>Letzte Pfadkomponente (z. B. <c>bin</c>).</summary>
    public static readonly StyledProperty<string?> LeafProperty =
        AvaloniaProperty.Register<PathText, string?>(nameof(Leaf));

    /// <summary>Farbe des Verzeichnisanteils.</summary>
    public static readonly StyledProperty<IBrush?> DirectoryBrushProperty =
        AvaloniaProperty.Register<PathText, IBrush?>(nameof(DirectoryBrush));

    /// <summary>Farbe des Ordnernamens.</summary>
    public static readonly StyledProperty<IBrush?> LeafBrushProperty =
        AvaloniaProperty.Register<PathText, IBrush?>(nameof(Leaf));

    /// <summary>Schriftfamilie (mit Fallback-Liste).</summary>
    public static readonly StyledProperty<string> FontFamilyProperty =
        AvaloniaProperty.Register<PathText, string>(nameof(FontFamily), "Cascadia Mono, Consolas");

    /// <summary>Schriftgröße.</summary>
    public static readonly StyledProperty<double> FontSizeProperty =
        AvaloniaProperty.Register<PathText, double>(nameof(FontSize), 12d);

    /// <summary>Schreibstärke des Ordnernamens.</summary>
    public static readonly StyledProperty<FontWeight> LeafFontWeightProperty =
        AvaloniaProperty.Register<PathText, FontWeight>(nameof(LeafFontWeight), FontWeight.SemiBold);

    static PathText()
    {
        AffectsMeasure<PathText>(DirectoryProperty, LeafProperty, FontFamilyProperty, FontSizeProperty, LeafFontWeightProperty);
        AffectsRender<PathText>(DirectoryProperty, LeafProperty, DirectoryBrushProperty, LeafBrushProperty, FontFamilyProperty, FontSizeProperty, LeafFontWeightProperty);
    }

    private FormattedText? _directory;
    private FormattedText? _separator;
    private FormattedText? _leaf;
    private double _formattedForWidth = -1;
    private double _height;

    public string? Directory
    {
        get => GetValue(DirectoryProperty);
        set => SetValue(DirectoryProperty, value);
    }

    public string? Leaf
    {
        get => GetValue(LeafProperty);
        set => SetValue(LeafProperty, value);
    }

    public IBrush? DirectoryBrush
    {
        get => GetValue(DirectoryBrushProperty);
        set => SetValue(DirectoryBrushProperty, value);
    }

    public IBrush? LeafBrush
    {
        get => GetValue(LeafBrushProperty);
        set => SetValue(LeafBrushProperty, value);
    }

    public string FontFamily
    {
        get => GetValue(FontFamilyProperty);
        set => SetValue(FontFamilyProperty, value);
    }

    public double FontSize
    {
        get => GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }

    public FontWeight LeafFontWeight
    {
        get => GetValue(LeafFontWeightProperty);
        set => SetValue(LeafFontWeightProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        // Nur die Höhe melden - die Breite wird von der Spalte vergeben.
        _height = MeasureHeight();
        _formattedForWidth = -1;

        return new Size(0, _height);
    }

    public override void Render(DrawingContext context)
    {
        var width = Bounds.Width;

        if (width <= 0)
        {
            return;
        }

        EnsureFormattedTexts(width);

        if (_directory is null || _separator is null || _leaf is null)
        {
            return;
        }

        var x = 0d;
        const double y = 0d;

        // Wichtig: Erst zeichnen, dann die Breite auslesen. Die FormattedText berechnet
        // ihre Breite erst beim Zeichnen - vorher stünde dort noch die ungekürzte Breite
        // und Trennzeichen/Ordnername würden zu weit rechts landen.
        context.DrawText(_directory, new Point(x, y));
        x += _directory.WidthIncludingTrailingWhitespace;

        context.DrawText(_separator, new Point(x, y));
        x += _separator.WidthIncludingTrailingWhitespace;

        context.DrawText(_leaf, new Point(x, y));
    }

    /// <summary>
    /// Erzeugt die Texte für die aktuelle Breite neu, falls sich diese geändert hat.
    /// </summary>
    private void EnsureFormattedTexts(double width)
    {
        if (Math.Abs(_formattedForWidth - width) < 0.5 && _directory is not null)
        {
            return;
        }

        _formattedForWidth = width;

        var typeface = new Typeface(new FontFamily(FontFamily), FontStyle.Normal, FontWeight.Normal);
        var leafTypeface = new Typeface(new FontFamily(FontFamily), FontStyle.Normal, LeafFontWeight);

        var separator = CreateText(Separator.ToString(), typeface, DirectoryBrush);
        var leaf = CreateText(Leaf ?? string.Empty, leafTypeface, LeafBrush);

        // Der Verzeichnisanteil bekommt den Rest der Breite - Trennzeichen und
        // Ordnername bleiben immer sichtbar.
        var fixedWidth = separator.WidthIncludingTrailingWhitespace + leaf.WidthIncludingTrailingWhitespace;
        var available = Math.Max(0, width - fixedWidth);

        var directory = CreateText(
            PathShortener.Shorten(Directory ?? string.Empty, available, text => Measure(text, typeface)),
            typeface,
            DirectoryBrush);
        directory.MaxTextWidth = available;

        _directory = directory;
        _separator = separator;
        _leaf = leaf;
    }

    /// <summary>
    /// Misst die Breite eines Textes in der verwendeten Schrift.
    /// </summary>
    private double Measure(string text, Typeface typeface) =>
        CreateText(text, typeface, null).Width;

    private double MeasureHeight()
    {
        var typeface = new Typeface(new FontFamily(FontFamily), FontStyle.Normal, FontWeight.Normal);
        var leafTypeface = new Typeface(new FontFamily(FontFamily), FontStyle.Normal, LeafFontWeight);

        var directory = CreateText(Directory ?? string.Empty, typeface, DirectoryBrush);
        var leaf = CreateText(Leaf ?? string.Empty, leafTypeface, LeafBrush);

        return Math.Max(directory.Height, leaf.Height);
    }

    private FormattedText CreateText(string text, Typeface typeface, IBrush? brush)
    {
        var formatted = new FormattedText(
            text,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            typeface,
            FontSize,
            brush ?? Brushes.Gray)
        {
            // FormattedText bricht sonst um - mit MaxLineCount wird stattdessen
            // auf eine Zeile gekürzt und das Auslassungszeichen angehängt.
            MaxLineCount = 1,
            Trimming = TextTrimming.CharacterEllipsis,
        };

        return formatted;
    }
}
