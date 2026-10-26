using System.Diagnostics;
using BinObjCleaner.Models;

namespace BinObjCleaner.Services;

/// <summary>
/// Standardimplementierung von <see cref="IArtifactScanner"/>.
/// Die Traverse läuft iterativ (Stack), damit auch tief verschachtelte Verzeichnisse
/// keine StackOverflow auslösen. Kandidatenverzeichnisse werden nicht weiter betreten –
/// so wird jede Datei genau einmal gezählt.
/// </summary>
public sealed class ArtifactScanner : IArtifactScanner
{
    private const int ProgressInterval = 25;

    private readonly IDirectorySizeCalculator _sizeCalculator;

    public ArtifactScanner(IDirectorySizeCalculator sizeCalculator) =>
        _sizeCalculator = sizeCalculator ?? throw new ArgumentNullException(nameof(sizeCalculator));

    public Task<ScanResult> ScanAsync(
        ScanOptions options,
        IProgress<ScanProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        return Task.Run(() => Scan(options, progress, cancellationToken), cancellationToken);
    }

    private ScanResult Scan(ScanOptions options, IProgress<ScanProgress>? progress, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        var rootPath = options.RootPath;
        var names = new HashSet<string>(options.NormalizedNames, StringComparer.OrdinalIgnoreCase);
        var folders = new List<ArtifactFolder>();

        long nonCandidateBytes = 0;
        var nonCandidateFiles = 0;
        var scannedDirectories = 0;

        if (!Directory.Exists(rootPath))
        {
            return new ScanResult(folders, 0, 0, stopwatch.Elapsed);
        }

        var pending = new Stack<string>();
        pending.Push(rootPath);

        while (pending.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var current = pending.Pop();
            scannedDirectories++;

            if (scannedDirectories % ProgressInterval == 0)
            {
                progress?.Report(new ScanProgress(scannedDirectories, folders.Count));
            }

            string[] children;
            try
            {
                children = Directory.GetDirectories(current);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                continue;
            }

            foreach (var child in children)
            {
                cancellationToken.ThrowIfCancellationRequested();

                FileAttributes attributes;
                try
                {
                    attributes = File.GetAttributes(child);
                }
                catch (Exception e) when (e is IOException or UnauthorizedAccessException)
                {
                    continue;
                }

                var isHidden = (attributes & FileAttributes.Hidden) == FileAttributes.Hidden;
                var isReparsePoint = (attributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint;

                if (!options.IncludeHidden && isHidden)
                {
                    continue;
                }

                var name = Path.GetFileName(child);

                if (names.Contains(name))
                {
                    var size = isReparsePoint && !options.FollowSymbolicLinks
                        ? new DirectorySize(0, 0)
                        : _sizeCalculator.Calculate(child, options.FollowSymbolicLinks);

                    folders.Add(new ArtifactFolder(
                        child,
                        MakeRelative(rootPath, child),
                        name,
                        size.Bytes,
                        size.FileCount));

                    continue;
                }

                if (isReparsePoint && !options.FollowSymbolicLinks)
                {
                    continue;
                }

                // Ohne Unterordner wird ausschließlich die direkte Ebene des Stamms geprüft.
                if (!options.IncludeSubdirectories)
                {
                    continue;
                }

                pending.Push(child);
            }

            // Dateien direkt in diesem Verzeichnis zählen.
            // Kandidatenverzeichnisse werden nie betreten, ihre Dateien also genau einmal gezählt.
            try
            {
                foreach (var file in Directory.EnumerateFiles(current))
                {
                    try
                    {
                        nonCandidateBytes += new FileInfo(file).Length;
                        nonCandidateFiles++;
                    }
                    catch (Exception e) when (e is IOException or UnauthorizedAccessException)
                    {
                        // Datei gesperrt - überspringen.
                    }
                }
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                // Verzeichnis nicht zugreifbar - überspringen.
            }
        }

        stopwatch.Stop();

        progress?.Report(new ScanProgress(scannedDirectories, folders.Count));

        var rootTotal = nonCandidateBytes + folders.Sum(f => f.SizeInBytes);
        var rootFiles = nonCandidateFiles + folders.Sum(f => f.FileCount);
        var ordered = folders.OrderByDescending(f => f.SizeInBytes).ThenBy(f => f.RelativePath, StringComparer.CurrentCulture).ToList();

        return new ScanResult(ordered, rootTotal, rootFiles, stopwatch.Elapsed);
    }

    internal static string MakeRelative(string rootPath, string fullPath)
    {
        var relative = Path.GetRelativePath(rootPath, fullPath);
        return relative.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
    }
}
