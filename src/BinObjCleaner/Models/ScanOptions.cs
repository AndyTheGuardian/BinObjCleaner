namespace BinObjCleaner.Models;

/// <summary>
/// Parameter für einen Scanlauf.
/// </summary>
public sealed record ScanOptions
{
    public required string RootPath { get; init; }

    /// <summary>Ordner-Namen, die als Kandidaten gelten (Standard: bin, obj).</summary>
    public IReadOnlyList<string> FolderNames { get; init; } = ["bin", "obj"];

    /// <summary>Unterordner des Stammverzeichnisses durchsuchen.</summary>
    public bool IncludeSubdirectories { get; init; } = true;

    /// <summary>Versteckte Ordner (Windows-Attribut <c>Hidden</c>) einbeziehen.</summary>
    public bool IncludeHidden { get; init; }

    /// <summary>Symbolischen Links (Reparse Points) folgen.</summary>
    public bool FollowSymbolicLinks { get; init; }

    public IEnumerable<string> NormalizedNames => FolderNames
        .Where(n => !string.IsNullOrWhiteSpace(n))
        .Select(n => n.Trim())
        .Distinct(StringComparer.OrdinalIgnoreCase);
}

/// <summary>Fortschrittsinformationen eines Scanlaufs.</summary>
/// <param name="ScannedDirectories">Bisher untersuchte Verzeichnisse.</param>
/// <param name="FoundFolders">Bisher gefundene Kandidaten.</param>
public readonly record struct ScanProgress(int ScannedDirectories, int FoundFolders);

/// <summary>Ergebnis eines Komplettdurchlaufs (Kandidaten + Gesamtgröße des Stamms).</summary>
/// <param name="Folders">Gefundene Löschkandidaten.</param>
/// <param name="RootTotalBytes">Gesamtgröße des Stammverzeichnisses inklusive Kandidaten.</param>
/// <param name="RootFileCount">Anzahl der Dateien im Stammverzeichnis.</param>
/// <param name="Elapsed">Dauer des Scans.</param>
public readonly record struct ScanResult(
    IReadOnlyList<ArtifactFolder> Folders,
    long RootTotalBytes,
    int RootFileCount,
    TimeSpan Elapsed);
