using Avalonia;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using IOPath = System.IO.Path;

namespace BinObjCleaner.Controls;

/// <summary>
/// Verfügbare Symbole der Anwendung.
/// </summary>
public enum AppIconKind
{
    Search,
    Folder,
    Refresh,
    Trash,
    BarChart,
    Layers,
    Close,
    Plus,
    Warning,
    Moon,
    Sun,
}

/// <summary>
/// Schlanke Icon-Control. Die Symbole sind Strichzeichnungen (24x24) und übernehmen
/// die Vordergrundfarbe des umgebenden Controls - wie im Mockup.
/// </summary>
public sealed class AppIcon : Avalonia.Controls.Shapes.Path
{
    private static readonly Dictionary<AppIconKind, StreamGeometry> Geometries = new()
    {
        [AppIconKind.Search] = Parse("M11 4a7 7 0 1 0 0 14 7 7 0 0 0 0-14ZM20 20l-3.2-3.2"),
        [AppIconKind.Folder] = Parse("M3 7a2 2 0 0 1 2-2h4l2 2h8a2 2 0 0 1 2 2v8a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z"),
        [AppIconKind.Refresh] = Parse("M12 5V2L8 6l4 4V7a5 5 0 1 1-5 5H5a7 7 0 1 0 7-7Z"),
        [AppIconKind.Trash] = Parse("M4 7h16M10 11v6M14 11v6M5 7l1 13h12l1-13M9 7V4h6v3"),
        [AppIconKind.BarChart] = Parse("M3 3v18h18M7 10h3v8H7zM12 6h3v12h-3zM17 12h3v6h-3z"),
        [AppIconKind.Layers] = Parse("M4 4h16v6H4zM4 14h16v6H4z"),
        [AppIconKind.Close] = Parse("M5 5l10 10M15 5 5 15"),
        [AppIconKind.Plus] = Parse("M12 5v14M5 12h14"),
        [AppIconKind.Warning] = Parse("M12 9v5M12 17.5v.5M10.3 3.9 2.5 17.4A2 2 0 0 0 4.2 20.4h15.6a2 2 0 0 0 1.7-3L13.7 3.9a2 2 0 0 0-3.4 0Z"),
        [AppIconKind.Moon] = Parse("M20 14.5A8.5 8.5 0 0 1 9.5 4a8.5 8.5 0 1 0 10.5 20a8.5 8.5 0 0 0 0-5.5Z"),
        [AppIconKind.Sun] = Parse("M16 12a4 4 0 1 1-8 0 4 4 0 0 1 8 0ZM12 2v2M12 20v2M4.9 4.9l1.4 1.4M17.7 17.7l1.4 1.4M2 12h2M20 12h2M4.9 19.1l1.4-1.4M17.7 6.3l1.4-1.4"),
    };

    /// <summary>Art des Symbols.</summary>
    public static readonly StyledProperty<AppIconKind> KindProperty =
        AvaloniaProperty.Register<AppIcon, AppIconKind>(nameof(Kind));

    /// <summary>Farbe der Strichzeichnung. Ohne Angabe wird die Vordergrundfarbe verwendet.</summary>
    public static readonly StyledProperty<IBrush?> TintProperty =
        AvaloniaProperty.Register<AppIcon, IBrush?>(nameof(Tint));

    static AppIcon()
    {
        AffectsRender<AppIcon>(KindProperty, TintProperty);
    }

    public AppIcon()
    {
        StrokeThickness = 1.8;
        StrokeLineCap = PenLineCap.Round;
        StrokeJoin = PenLineJoin.Round;
        Fill = null;
        Stretch = Stretch.Uniform;
        Width = 16;
        Height = 16;

        // Wichtig: Kind kann den Standardwert bereits haben - dann feuert beim Setzen
        // aus dem XAML keine Property-Anderung. Die Geometrie deshalb hier setzen.
        ApplyGeometry(Kind);
    }

    public AppIconKind Kind
    {
        get => GetValue(KindProperty);
        set => SetValue(KindProperty, value);
    }

    public IBrush? Tint
    {
        get => GetValue(TintProperty);
        set => SetValue(TintProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == KindProperty)
        {
            ApplyGeometry(Kind);
        }
    }

    private void ApplyGeometry(AppIconKind kind)
    {
        if (Geometries.TryGetValue(kind, out var geometry))
        {
            Data = geometry;
        }
    }

    private static StreamGeometry Parse(string path) => StreamGeometry.Parse(path);
}
