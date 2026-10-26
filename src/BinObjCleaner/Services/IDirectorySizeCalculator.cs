namespace BinObjCleaner.Services;

/// <summary>
/// Größe eines Verzeichnisses inklusive Unterverzeichnissen.
/// </summary>
/// <param name="Bytes">Summe der Dateigrößen.</param>
/// <param name="FileCount">Anzahl der Dateien.</param>
public readonly record struct DirectorySize(long Bytes, int FileCount);

/// <summary>
/// Ermittelt die rekursive Größe eines Verzeichnisses.
/// </summary>
public interface IDirectorySizeCalculator
{
    /// <param name="path">Pfad des Verzeichnisses.</param>
    /// <param name="followSymbolicLinks">
    /// Wenn <see langword="true"/>, werden symbolische Links (Reparse Points) mitgezählt.
    /// </param>
    DirectorySize Calculate(string path, bool followSymbolicLinks = false);
}
