using System.Globalization;

namespace BinObjCleaner.Services;

/// <summary>
/// Formatiert Byte-Werte in lesbare Größenangaben.
/// </summary>
public static class ByteSizeFormatter
{
    private static readonly string[] Units = ["B", "KB", "MB", "GB", "TB", "PB"];

    /// <summary>
    /// Formatiert eine Größe, z. B. <c>312,7 MB</c> (de) bzw. <c>312.7 MB</c> (en).
    /// </summary>
    public static string Format(long bytes, IFormatProvider? formatProvider = null)
    {
        formatProvider ??= CultureInfo.CurrentCulture;

        if (bytes < 0)
        {
            return "-" + Format(-bytes, formatProvider);
        }

        double value = bytes;
        var unitIndex = 0;

        while (value >= 1024 && unitIndex < Units.Length - 1)
        {
            value /= 1024;
            unitIndex++;
        }

        return unitIndex == 0
            ? string.Create(formatProvider, $"{bytes} {Units[unitIndex]}")
            : string.Create(formatProvider, $"{value:N1} {Units[unitIndex]}");
    }

    /// <summary>
    /// Formatiert eine Größe in Prozent mit einer Nachkommastelle.
    /// </summary>
    public static string FormatPercent(double ratio, IFormatProvider? formatProvider = null)
    {
        formatProvider ??= CultureInfo.CurrentCulture;
        return string.Create(formatProvider, $"{ratio * 100:N1} %");
    }
}
