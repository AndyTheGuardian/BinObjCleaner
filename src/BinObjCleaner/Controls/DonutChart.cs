using System.Collections.Specialized;
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
/// Donut-Diagramm, das die Verteilung der Löschkandidaten (und den verbleibenden
/// Anteil des Stammverzeichnisses) als Ring darstellt. Gezeichnet wird über SkiaSharp.
/// </summary>
/// <remarks>
/// Avalonia 12 führt <see cref="ICustomDrawOperation.Render"/> auf dem Render-Thread des
/// Compositors aus - nicht auf dem UI-Thread. Deshalb werden alle benötigten Werte im
/// UI-Thread in Felder kopiert (<see cref="UpdateRenderSnapshot"/>); der Render-Code darf
/// keine Avalonia-Properties mehr lesen.
/// </remarks>
public class DonutChart : Control, ICustomDrawOperation
{
    /// <summary>Segmente des Ringes.</summary>
    public static readonly StyledProperty<IReadOnlyList<ChartSegment>?> SegmentsProperty =
        AvaloniaProperty.Register<DonutChart, IReadOnlyList<ChartSegment>?>(nameof(Segments));

    /// <summary>Ringdicke in Device-independent Pixels.</summary>
    public static readonly StyledProperty<double> ThicknessProperty =
        AvaloniaProperty.Register<DonutChart, double>(nameof(Thickness), 18d);

    /// <summary>Lücke zwischen den Segmenten in Grad.</summary>
    public static readonly StyledProperty<double> GapDegreesProperty =
        AvaloniaProperty.Register<DonutChart, double>(nameof(GapDegrees), 2d);

    /// <summary>Farbe der Segmente der Kategorie <c>bin</c>.</summary>
    public static readonly StyledProperty<IBrush?> BinBrushProperty =
        AvaloniaProperty.Register<DonutChart, IBrush?>(nameof(BinBrush));

    /// <summary>Farbe der Segmente der Kategorie <c>obj</c>.</summary>
    public static readonly StyledProperty<IBrush?> ObjBrushProperty =
        AvaloniaProperty.Register<DonutChart, IBrush?>(nameof(ObjBrush));

    /// <summary>Farbe des verbleibenden Anteils (Rest des Stammordners).</summary>
    public static readonly StyledProperty<IBrush?> RemainingBrushProperty =
        AvaloniaProperty.Register<DonutChart, IBrush?>(nameof(RemainingBrush));

    /// <summary>Farbe für sonstige Kategorien.</summary>
    public static readonly StyledProperty<IBrush?> FallbackBrushProperty =
        AvaloniaProperty.Register<DonutChart, IBrush?>(nameof(FallbackBrush));

    static DonutChart()
    {
        AffectsRender<DonutChart>(SegmentsProperty, ThicknessProperty, GapDegreesProperty);
        AffectsRender<DonutChart>(BinBrushProperty, ObjBrushProperty, RemainingBrushProperty, FallbackBrushProperty);
    }

    private INotifyCollectionChanged? _observed;

    // --- Render-Snapshot: wird ausschließlich im UI-Thread befüllt ---
    private IReadOnlyList<ChartSegment>? _segments;
    private float _thickness;
    private float _gapDegrees;
    private SKColor _binColor;
    private SKColor _objColor;
    private SKColor _remainingColor;
    private SKColor _fallbackColor;
    private Rect _bounds;

    public IReadOnlyList<ChartSegment>? Segments
    {
        get => GetValue(SegmentsProperty);
        set => SetValue(SegmentsProperty, value);
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

    public IBrush? RemainingBrush
    {
        get => GetValue(RemainingBrushProperty);
        set => SetValue(RemainingBrushProperty, value);
    }

    public IBrush? FallbackBrush
    {
        get => GetValue(FallbackBrushProperty);
        set => SetValue(FallbackBrushProperty, value);
    }

    public double Thickness
    {
        get => GetValue(ThicknessProperty);
        set => SetValue(ThicknessProperty, value);
    }

    public double GapDegrees
    {
        get => GetValue(GapDegreesProperty);
        set => SetValue(GapDegreesProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == BoundsProperty)
        {
            // Die Bounds werden vom Compositor gelesen - daher zwischenspeichern.
            _bounds = Bounds;
        }

        if (change.Property == SegmentsProperty)
        {
            // Die Segmente stammen üblicherweise aus einer ObservableCollection, die nach dem
            // ersten Rendern befüllt wird - daher auf Änderungen der Liste reagieren.
            if (_observed is not null)
            {
                _observed.CollectionChanged -= OnSegmentsChanged;
                _observed = null;
            }

            if (Segments is INotifyCollectionChanged observable)
            {
                _observed = observable;
                _observed.CollectionChanged += OnSegmentsChanged;
            }
        }

        UpdateRenderSnapshot();
        InvalidateVisual();
    }

    private void OnSegmentsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        UpdateRenderSnapshot();
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

        _segments = Segments;
        _thickness = (float)Thickness;
        _gapDegrees = (float)GapDegrees;
        _binColor = ToSkia(BinBrush, new SKColor(0x5B, 0x8C, 0xFF));
        _objColor = ToSkia(ObjBrush, new SKColor(0xB0, 0x6B, 0xFF));
        _remainingColor = ToSkia(RemainingBrush, new SKColor(0x33, 0x3C, 0x4D));
        _fallbackColor = ToSkia(FallbackBrush, _remainingColor);
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
        var segments = _segments;

        if (segments is null || segments.Count == 0 || Bounds.Width <= 0 || Bounds.Height <= 0)
        {
            return;
        }

        using var lease = context.TryGetFeature<ISkiaSharpApiLeaseFeature>()?.Lease();
        if (lease is null)
        {
            return;
        }

        var canvas = lease.SkCanvas;

        var total = segments.Sum(s => Math.Max(0, s.Value));
        if (total <= 0)
        {
            return;
        }

        var thickness = _thickness;
        var radius = (float)(Math.Min(Bounds.Width, Bounds.Height) / 2d) - thickness / 2f;

        if (radius <= 0)
        {
            return;
        }

        var center = new SKPoint((float)Bounds.Width / 2f, (float)Bounds.Height / 2f);
        var rect = new SKRect(center.X - radius, center.Y - radius, center.X + radius, center.Y + radius);

        using var trackPaint = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            StrokeWidth = thickness,
            IsAntialias = true,
            Color = _remainingColor,
        };

        canvas.DrawCircle(center, radius, trackPaint);

        var startAngle = -90f;
        var gap = _gapDegrees;

        foreach (var segment in segments)
        {
            if (segment.Value <= 0)
            {
                continue;
            }

            var sweep = (float)(360d * segment.Value / total);
            var drawSweep = Math.Max(0.5f, sweep - gap);

            using var paint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                StrokeWidth = thickness,
                IsAntialias = true,
                StrokeCap = SKStrokeCap.Butt,
                Color = segment.ColorKey switch
                {
                    "bin" => _binColor,
                    "obj" => _objColor,
                    "remaining" => _remainingColor,
                    _ => _fallbackColor,
                },
            };

            canvas.DrawArc(rect, startAngle, drawSweep, useCenter: false, paint);
            startAngle += sweep;
        }
    }

    private static SKColor ToSkia(IBrush? brush, SKColor fallback) =>
        brush is SolidColorBrush solid ? solid.Color.ToSKColor() : fallback;
}
