using System.Globalization;
using Avalonia.Data.Converters;
using BinObjCleaner.Services;

namespace BinObjCleaner.ViewModels;

/// <summary>
/// Wandelt einen Anteil (0..1) in eine Prozentangabe.
/// </summary>
public sealed class PercentOfTotalConverter : IValueConverter
{
    public static readonly PercentOfTotalConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var ratio = value switch
        {
            double d => d,
            float f => f,
            int i => i,
            long l => l,
            _ => 0d,
        };

        return ByteSizeFormatter.FormatPercent(ratio, culture);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
