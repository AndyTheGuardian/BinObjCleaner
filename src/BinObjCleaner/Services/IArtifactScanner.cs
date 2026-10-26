using BinObjCleaner.Models;

namespace BinObjCleaner.Services;

/// <summary>
/// Sucht rekursiv nach Verzeichnissen mit vorgegebenen Namen (z. B. <c>bin</c> und <c>obj</c>).
/// </summary>
public interface IArtifactScanner
{
    Task<ScanResult> ScanAsync(
        ScanOptions options,
        IProgress<ScanProgress>? progress = null,
        CancellationToken cancellationToken = default);
}
