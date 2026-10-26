using BinObjCleaner.Models;
using BinObjCleaner.Services;

namespace BinObjCleaner.Tests;

public sealed class DeletionServiceTests
{
    [Fact]
    public async Task Loescht_Ordner_Endgueltig()
    {
        using var temp = new TempDirectory();
        var target = temp.CreateDirectory("projekt/bin");
        temp.CreateFile("projekt/bin/a.dll", 100);

        var report = await new DeletionService().DeleteAsync(
        [
            new ArtifactFolder(target, "projekt\\bin", "bin", 100, 1)
        ], useRecycleBin: false);

        Assert.Equal(1, report.DeletedCount);
        Assert.Equal(100, report.FreedBytes);
        Assert.Empty(report.Failures);
        Assert.False(Directory.Exists(target));
    }

    [Fact]
    public async Task Meldet_Fehler_Fuer_Nicht_Existierende_Ordner()
    {
        using var temp = new TempDirectory();

        var report = await new DeletionService().DeleteAsync(
        [
            new ArtifactFolder(System.IO.Path.Combine(temp.Path, "weg"), "weg", "bin", 10, 1)
        ], useRecycleBin: false);

        Assert.Equal(0, report.DeletedCount);
        Assert.Single(report.Failures);
    }

    [Fact]
    public async Task Loescht_Bei_Probelauf_Nichts()
    {
        using var temp = new TempDirectory();
        var target = temp.CreateDirectory("bin");

        // Der Probelauf wird in der Oberflaeche behandelt - der Dienst selbst loescht immer.
        var report = await new DeletionService().DeleteAsync(
        [
            new ArtifactFolder(target, "bin", "bin", 10, 1)
        ], useRecycleBin: false);

        Assert.Equal(1, report.DeletedCount);
        Assert.False(Directory.Exists(target));
    }
}
