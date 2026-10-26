using System.Collections.Specialized;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using Avalonia.Threading;
using BinObjCleaner.Models;
using SkiaSharp;

namespace BinObjCleaner.Controls;

/// <summary>
/// Balkendiagramm (Label, Wert, Balken) - gezeichnet über SkiaSharp.
/// </summary>
/// <remarks>
/// Wie <see cref="DonutChart"/> gilt auch hier: Avalonia 12 führt
/// <see cref="ICustomDrawOperation.Render"/> auf dem Render-Thread aus. Alle für die
/// Darstellung benötigten Werte werden deshalb im UI-Thread in Felder kopiert.
/// </remarks>
public class BarChart : Control, ICustomDrawOperation
{
    /// <summary>Darzustellende Einträge.</summary>
    public static readonly StyledProperty<IReadOnlyList<BarChartItem>?> ItemsProperty =
        AvaloniaProperty.Register<BarChart, IReadOnlyList<BarChartItem>?>(nameof(Items));

    /// <summary>Höhe einer Zeile in Device-independent Pixels.</summary>
    public static readonly StyledProperty<double> RowHeightProperty =
        AvaloniaProperty.Register<BarChart, double>(nameof(RowHeight), 34d);

    /// <summary>Höhe des Balkens in Device-independent Pixels.</summary>
    public static readonly StyledProperty<double> BarHeightProperty =
        AvaloniaProperty.Register<BarChart, double>(nameof(BarHeight), 7d);

    /// <summary>Schriftfamilie für Beschriftungen.</summary>
    public static readonly StyledProperty<string> FontFamilyProperty =
        AvaloniaProperty.Register<BarChart, string>(nameof(FontFamily), "Segoe UI");

    /// <summary>Schriftgröße für Beschriftungen.</summary>
    public static readonly StyledProperty<double> FontSizeProperty =
        AvaloniaProperty.Register<BarChart, double>(nameof(FontSize), 12d);

    /// <summary>Farbe der Beschriftungen (Pfad).</summary>
    public static readonly StyledProperty<IBrush?> LabelBrushProperty =
        AvaloniaProperty.Register<BarChart, IBrush?>(nameof(LabelBrush));

    /// <summary>Farbe der Werte (Größe).</summary>
    public static readonly StyledProperty<IBrush?> ValueBrushProperty =
        AvaloniaProperty.Register<BarChart, IBrush?>(nameof(ValueBrush));

    /// <summary>Farbe der Balken-Grundlage.</summary>
    public static readonly StyledProperty<IBrush?> TrackBrushProperty =
        AvaloniaProperty.Register<BarChart, IBrush?>(nameof(TrackBrush));

    /// <summary>Farbe der Balken der Kategorie <c>bin</c>.</summary>
    public static readonly StyledProperty<IBrush?> BinBrushProperty =
        AvaloniaProperty.Register<BarChart, IBrush?>(nameof(BinBrush));

    /// <summary>Farbe der Balken der Kategorie <c>obj</c>.</summary>
    public static readonly StyledProperty<IBrush?> ObjBrushProperty =
        AvaloniaProperty.Register<BarChart, IBrush?>(nameof(ObjBrush));

    /// <summary>Farbe der Balken sonstiger Kategorien.</summary>
    public static readonly StyledProperty<IBrush?> FallbackBrushProperty =
        AvaloniaProperty.Register<BarChart, IBrush?>(nameof(FallbackBrush));

    static BarChart()
    {
        AffectsRender<BarChart>(ItemsProperty, RowHeightProperty, BarHeightProperty, FontFamilyProperty, FontSizeProperty);
        AffectsRender<BarChart>(LabelBrushProperty, ValueBrushProperty, TrackBrushProperty, BinBrushProperty, ObjBrushProperty, FallbackBrushProperty);
        AffectsMeasure<BarChart>(ItemsProperty, RowHeightProperty);
    }

    private INotifyCollectionChanged? _observed;

    // --- Render-Snapshot: wird ausschließlich im UI-Thread befüllt ---
    private IReadOnlyList<BarChartItem>? _items;
    private float _rowHeight;
    private float _barHeight;
    private float _fontSize;
    private string _fontFamily = "Segoe UI";
    private SKColor _labelColor;
    private SKColor _valueColor;
    private SKColor _trackColor;
    private SKColor _binColor;
    private SKColor _objColor;
    private SKColor _fallbackColor;
    private CultureInfo _culture = CultureInfo.CurrentCulture;
    private Rect _bounds;

    public IReadOnlyList<BarChartItem>? Items
    {
        get => GetValue(ItemsProperty);
        set => SetValue(ItemsProperty, value);
    }

    public double RowHeight
    {
        get => GetValue(RowHeightProperty);
        set => SetValue(RowHeightProperty, value);
    }

    public double BarHeight
    {
        get => GetValue(BarHeightProperty);
        set => SetValue(BarHeightProperty, value);
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

    public IBrush? LabelBrush
    {
        get => GetValue(LabelBrushProperty);
        set => SetValue(LabelBrushProperty, value);
    }

    public IBrush? ValueBrush
    {
        get => GetValue(ValueBrushProperty);
        set => SetValue(ValueBrushProperty, value);
    }

    public IBrush? TrackBrush
    {
        get => GetValue(TrackBrushProperty);
        set => SetValue(TrackBrushProperty, value);
    }

    public IBrush? BinBrush
    {
        get => GetValue(BinBrushProperty);
        set => SetValue(BinBrushProperty, value);
    }

    public IBrush? ObjBrush
    {
        get => GetValue(ObjBrushProperty);
        set => SetValue(ObjBrushProperty, value);
    }

    public IBrush? FallbackBrush
    {
        get => GetValue(FallbackBrushProperty);
        set => SetValue(FallbackBrushProperty, value);
    }

    /// <summary>
    /// Das Diagramm ist ein einzelnes Control und für StackPanel-Layouts zuständig -
    /// ohne eigenes Measure hat es keine Höhe und würde gar nichts anzeigen.
    /// </summary>
    protected override Size MeasureOverride(Size availableSize)
    {
        var count = _items?.Count ?? 0;
        var height = count == 0 ? 0 : (count * _rowHeight) + 4;

        return new Size(availableSize.Width, Math.Max(0, height));
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == BoundsProperty)
        {
            // Die Bounds werden vom Compositor gelesen - daher zwischenspeichern.
            _bounds = Bounds;
        }

        if (change.Property == ItemsProperty)
        {
            // Die Einträge kommen üblicherweise aus einer ObservableCollection, die nach dem
            // ersten Rendern befüllt wird - daher auf Änderungen der Liste reagieren.
            if (_observed is not null)
            {
                _observed.CollectionChanged -= OnItemsChanged;
                _observed = null;
            }

            if (Items is INotifyCollectionChanged observable)
            {
                _observed = observable;
                _observed.CollectionChanged += OnItemsChanged;
            }
        }

        UpdateRenderSnapshot();
        InvalidateMeasure();
        InvalidateVisual();
    }

    private void OnItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        UpdateRenderSnapshot();
        InvalidateMeasure();
        InvalidateVisual();
    }

    /// <summary>
    /// Kopiert die benötigten Werte in Felder, damit der Render-Thread
    /// nicht auf Avalonia-Properties zugreifen muss.
    /// </summary>
    private void UpdateRenderSnapshot()
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            return;
        }

        _items = Items;
        _rowHeight = (float)RowHeight;
        _barHeight = (float)BarHeight;
        _fontSize = (float)FontSize;
        _fontFamily = FontFamily;
        _labelColor = ToSkia(LabelBrush, new SKColor(0xA2, 0xAB, 0xBD));
        _valueColor = ToSkia(ValueBrush, new SKColor(0xE7, 0xEB, 0xF3));
        _trackColor = ToSkia(TrackBrush, new SKColor(0x33, 0x3C, 0x4D));
        _binColor = ToSkia(BinBrush, new SKColor(0x5B, 0x8C, 0xFF));
        _objColor = ToSkia(ObjBrush, new SKColor(0xB0, 0x6B, 0xFF));
        _fallbackColor = ToSkia(FallbackBrush, _binColor);

        // Die Kultur des UI-Threads übernehmen, damit die Größenangabe
        // unabhängig vom Render-Thread formatiert wird.
        _culture = CultureInfo.CurrentCulture;
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        _bounds = Bounds;
        context.Custom(this);
    }

    public bool Equals(ICustomDrawOperation? other) => ReferenceEquals(this, other);

    public bool HitTest(Point p) => false;

    Rect ICustomDrawOperation.Bounds => _bounds;

    public void Dispose()
    {
        // Keine unmanaged Ressourcen zu freigeben.
    }

    public void Render(ImmediateDrawingContext context)
    {
        // Ab hier darf nur noch auf den Render-Snapshot zugegriffen werden.
        var items = _items;

        if (items is null || items.Count == 0 || Bounds.Width <= 0 || Bounds.Height <= 0)
        {
            return;
        }

        using var lease = context.TryGetFeature<ISkiaSharpApiLeaseFeature>()?.Lease();
        if (lease is null)
        {
            return;
        }

        var canvas = lease.SkCanvas;

        var typeface = SKTypeface.FromFamilyName(_fontFamily) ?? SKTypeface.Default;
        using var font = new SKFont(typeface, _fontSize);
        using var labelPaint = new SKPaint { IsAntialias = true, Color = _labelColor };
        using var valuePaint = new SKPaint { IsAntialias = true, Color = _valueColor };
        using var trackPaint = new SKPaint { IsAntialias = true, Color = _trackColor };

        var maxValue = items.Max(i => Math.Max(0, i.Value));
        if (maxValue <= 0)
        {
            return;
        }

        var rowHeight = _rowHeight;
        var barHeight = _barHeight;
        var width = (float)Bounds.Width;

        var valueWidth = 0f;
        foreach (var item in items)
        {
            valueWidth = Math.Max(valueWidth, font.MeasureText(FormatSize(item.Value)));
        }

        var labelWidth = Math.Max(20f, width - valueWidth - 12f);

        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            var top = i * rowHeight;

            if (top > Bounds.Height)
            {
                break;
            }

            var baseline = top + _fontSize;

            var label = Ellipsize(item.Label, font, labelWidth);
            canvas.DrawText(label, 0f, baseline, font, labelPaint);

            var text = FormatSize(item.Value);
            var textWidth = font.MeasureText(text);
            canvas.DrawText(text, width - textWidth, baseline, font, valuePaint);

            var barTop = top + _fontSize + 7f;
            var trackRect = new SKRect(0f, barTop, width, barTop + barHeight);

            using var trackPath = new SKPath();
            trackPath.AddRoundRect(trackRect, barHeight / 2f, barHeight / 2f);
            canvas.DrawPath(trackPath, trackPaint);

            var ratio = (float)(item.Value / (double)maxValue);
            var fillWidth = Math.Max(barHeight, width * ratio);

            using var fillPaint = new SKPaint
            {
                IsAntialias = true,
                Color = item.ColorKey switch
                {
                    "bin" => _binColor,
                    "obj" => _objColor,
                    _ => _fallbackColor,
                },
            };

            using var fillPath = new SKPath();
            fillPath.AddRoundRect(new SKRect(0f, barTop, fillWidth, barTop + barHeight), barHeight / 2f, barHeight / 2f);
            canvas.DrawPath(fillPath, fillPaint);
        }
    }

    private string FormatSize(long bytes) => Services.ByteSizeFormatter.Format(bytes, _culture);

    private static string Ellipsize(string text, SKFont font, float maxWidth)
    {
        if (string.IsNullOrEmpty(text) || font.MeasureText(text) <= maxWidth)
        {
            return text;
        }

        const string ellipsis = "…";
        var budget = maxWidth - font.MeasureText(ellipsis);

        if (budget <= 0)
        {
            return ellipsis;
        }

        var length = text.Length;
        while (length > 0 && font.MeasureText(text[..length]) > budget)
        {
            length--;
        }

        return string.Concat(text.AsSpan(0, length), ellipsis);
    }

    private static SKColor ToSkia(IBrush? brush, SKColor fallback) =>
        brush is SolidColorBrush solid ? solid.Color.ToSKColor() : fallback;
}
