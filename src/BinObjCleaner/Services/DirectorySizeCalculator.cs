namespace BinObjCleaner.Services;

/// <summary>
/// Standardimplementierung von <see cref="IDirectorySizeCalculator"/>.
/// Berücksichtigt symbolische Links nur, wenn dies ausdrücklich erlaubt ist,
/// und ignoriert Verzeichnisse, auf die nicht zugegriffen werden kann.
/// </summary>
public sealed class DirectorySizeCalculator : IDirectorySizeCalculator
{
    public DirectorySize Calculate(string path, bool followSymbolicLinks = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        long bytes = 0;
        var files = 0;

        if (!Directory.Exists(path))
        {
            return new DirectorySize(0, 0);
        }

        var pending = new Stack<string>();
        pending.Push(path);

        while (pending.Count > 0)
        {
            var current = pending.Pop();

            try
            {
                foreach (var file in Directory.EnumerateFiles(current))
                {
                    try
                    {
                        bytes += new FileInfo(file).Length;
                        files++;
                    }
                    catch (Exception e) when (e is IOException or UnauthorizedAccessException)
                    {
                        // Datei ist gesperrt oder nicht lesbar - überspringen.
                    }
                }

                foreach (var directory in Directory.EnumerateDirectories(current))
                {
                    if (!followSymbolicLinks && IsReparsePoint(directory))
                    {
                        continue;
                    }

                    pending.Push(directory);
                }
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                // Verzeichnis nicht zugreifbar - überspringen.
            }
        }

        return new DirectorySize(bytes, files);
    }

    private static bool IsReparsePoint(string directory)
    {
        try
        {
            return (new DirectoryInfo(directory).Attributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return true;
        }
    }
}
