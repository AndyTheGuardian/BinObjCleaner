using BinObjCleaner.Models;

namespace BinObjCleaner.Services;

/// <summary>
/// Löscht Verzeichnisse - wahlweise unwiederbringlich oder über den Papierkorb.
/// </summary>
public interface IDeletionService
{
    Task<DeletionReport> DeleteAsync(
        IReadOnlyList<ArtifactFolder> folders,
        bool useRecycleBin,
        IProgress<DeletionProgress>? progress = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Fortschritt eines Löschlaufs.
/// </summary>
/// <param name="Index">Index des aktuell bearbeiteten Ordners (0-basiert).</param>
/// <param name="Total">Anzahl der zu löschenden Ordner.</param>
/// <param name="CurrentPath">Pfad des aktuell bearbeiteten Ordners.</param>
public readonly record struct DeletionProgress(int Index, int Total, string CurrentPath);

/// <summary>
/// Ergebnis eines Löschlaufs.
/// </summary>
/// <param name="DeletedCount">Anzahl erfolgreich gelöschter Ordner.</param>
/// <param name="FreedBytes">Freigegebener Speicherplatz (Näherung).</param>
/// <param name="Failures">Fehlgeschlagene Löschvorgänge.</param>
/// <param name="UsedRecycleBin">Der Papierkorb wurde verwendet.</param>
public sealed record DeletionReport(
    int DeletedCount,
    long FreedBytes,
    IReadOnlyList<DeletionFailure> Failures,
    bool UsedRecycleBin);

/// <summary>
/// Ein fehlgeschlagener Löschvorgang.
/// </summary>
/// <param name="Path">Pfad des Ordners.</param>
/// <param name="Reason">Fehlerbeschreibung.</param>
public readonly record struct DeletionFailure(string Path, string Reason);
