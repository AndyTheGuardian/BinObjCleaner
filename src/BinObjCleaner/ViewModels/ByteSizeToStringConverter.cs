using System.Globalization;
using Avalonia.Data.Converters;
using BinObjCleaner.Services;

namespace BinObjCleaner.ViewModels;

/// <summary>
/// Wandelt eine Byte-Anzahl in eine lesbare Größenangabe (z. B. <c>312,7 MB</c>).
/// </summary>
public sealed class ByteSizeToStringConverter : IValueConverter
{
    public static readonly ByteSizeToStringConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var bytes = value switch
        {
            long l => l,
            int i => i,
            double d => (long)d,
            _ => 0L,
        };

        return ByteSizeFormatter.Format(bytes, culture);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
