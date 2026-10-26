using BinObjCleaner.Models;
using BinObjCleaner.Services;

namespace BinObjCleaner.Tests;

public sealed class RecycleBinTests
{
    [Fact]
    public async Task Verschiebt_Ordner_In_Den_Papierkorb()
    {
        if (!RecycleBin.IsSupported)
        {
            // Auf Nicht-Windows-Systemen gibt es keinen Papierkorb.
            return;
        }

        using var temp = new TempDirectory();
        var target = temp.CreateDirectory("papierkorb-test");
        temp.CreateFile("papierkorb-test/a.dll", 64);

        var report = await new DeletionService().DeleteAsync(
        [
            new ArtifactFolder(target, "papierkorb-test", "bin", 64, 1)
        ], useRecycleBin: true);

        Assert.True(report.UsedRecycleBin);
        Assert.Empty(report.Failures);
        Assert.Equal(1, report.DeletedCount);
        Assert.False(Directory.Exists(target));
    }
}
