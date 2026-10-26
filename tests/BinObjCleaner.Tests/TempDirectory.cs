namespace BinObjCleaner.Tests;

/// <summary>
/// Erzeugt eine temporäre Verzeichnisstruktur für Tests und räumt sie anschließend auf.
/// </summary>
public sealed class TempDirectory : IDisposable
{
    public TempDirectory()
    {
        Path = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            "binobjcleaner-tests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(Path);
    }

    /// <summary>Wurzelverzeichnis des Testbestands.</summary>
    public string Path { get; }

    /// <summary>
    /// Legt eine Datei mit fester Größe an (inklusive aller Elternverzeichnisse).
    /// </summary>
    public string CreateFile(string relativePath, int sizeInBytes = 1024)
    {
        var fullPath = System.IO.Path.Combine(Path, relativePath.Replace('/', System.IO.Path.DirectorySeparatorChar));
        var directory = System.IO.Path.GetDirectoryName(fullPath);

        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllBytes(fullPath, new byte[sizeInBytes]);
        return fullPath;
    }

    /// <summary>
    /// Legt ein Verzeichnis an (inklusive aller Elternverzeichnisse).
    /// </summary>
    public string CreateDirectory(string relativePath)
    {
        var fullPath = System.IO.Path.Combine(Path, relativePath.Replace('/', System.IO.Path.DirectorySeparatorChar));
        Directory.CreateDirectory(fullPath);
        return fullPath;
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
        catch (IOException)
        {
            // Testverzeichnisse dürfen den Testlauf nicht fehlschlagen lassen.
        }
    }
}
