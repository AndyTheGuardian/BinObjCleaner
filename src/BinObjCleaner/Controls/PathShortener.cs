namespace BinObjCleaner.Controls;

/// <summary>
/// Kürzt Verzeichnisanteile von Pfaden an Verzeichnisgrenzen.
/// </summary>
/// <remarks>
/// Gekürzt wird von vorne, damit der aussagekräftige Teil des Pfades (der Projektordner)
/// erhalten bleibt - ein Abschneiden am Ende würde mitten im Ordnernamen enden.
/// </remarks>
public static class PathShortener
{
    private const char Separator = '\\';
    private const string Ellipsis = "…";

    /// <summary>
    /// Kürzt <paramref name="path"/> so weit, dass er in <paramref name="maxWidth"/> passt.
    /// </summary>
    /// <param name="path">Der zu kürzende Verzeichnisanteil.</param>
    /// <param name="maxWidth">Verfügbare Breite.</param>
    /// <param name="measure">Misst die Breite eines Textes.</param>
    public static string Shorten(string path, double maxWidth, Func<string, double> measure)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(measure);

        if (path.Length == 0 || maxWidth <= 0)
        {
            return path;
        }

        if (measure(path) <= maxWidth)
        {
            return path;
        }

        var segments = path.Split(Separator);

        // Von hinten prüfen, wie viele Verzeichnisebenen noch hineinpassen.
        for (var count = segments.Length - 1; count >= 1; count--)
        {
            var candidate = Ellipsis + string.Join(Separator, segments, segments.Length - count, count);

            if (measure(candidate) <= maxWidth)
            {
                return candidate;
            }
        }

        return Ellipsis;
    }
}
