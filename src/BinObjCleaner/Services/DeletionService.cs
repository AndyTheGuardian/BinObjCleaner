using BinObjCleaner.Models;

namespace BinObjCleaner.Services;

/// <summary>
/// Standardimplementierung von <see cref="IDeletionService"/>.
/// </summary>
public sealed class DeletionService : IDeletionService
{
    public Task<DeletionReport> DeleteAsync(
        IReadOnlyList<ArtifactFolder> folders,
        bool useRecycleBin,
        IProgress<DeletionProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(folders);

        return Task.Run(() => Delete(folders, useRecycleBin, progress, cancellationToken), cancellationToken);
    }

    private static DeletionReport Delete(
        IReadOnlyList<ArtifactFolder> folders,
        bool useRecycleBin,
        IProgress<DeletionProgress>? progress,
        CancellationToken cancellationToken)
    {
        var failures = new List<DeletionFailure>();
        var deleted = 0;
        long freed = 0;

        // Der Papierkorb steht nur unter Windows zur Verfügung.
        var recycle = useRecycleBin && RecycleBin.IsSupported;

        for (var i = 0; i < folders.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var folder = folders[i];
            progress?.Report(new DeletionProgress(i, folders.Count, folder.FullPath));

            try
            {
                if (recycle && OperatingSystem.IsWindows())
                {
                    RecycleBin.SendToRecycleBin(folder.FullPath);
                }
                else
                {
                    Directory.Delete(folder.FullPath, recursive: true);
                }

                deleted++;
                freed += folder.SizeInBytes;
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                failures.Add(new DeletionFailure(folder.FullPath, e.Message));
            }
        }

        return new DeletionReport(deleted, freed, failures, recycle);
    }
}
