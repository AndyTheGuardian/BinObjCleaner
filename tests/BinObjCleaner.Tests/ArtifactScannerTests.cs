using BinObjCleaner.Models;
using BinObjCleaner.Services;

namespace BinObjCleaner.Tests;

public sealed class ArtifactScannerTests
{
    private static ArtifactScanner CreateScanner() => new(new DirectorySizeCalculator());

    [Fact]
    public async Task Findet_Bin_Und_Obj_Ordner_Rekursiv()
    {
        using var temp = new TempDirectory();
        temp.CreateFile("src/Api/bin/Api.dll", 2048);
        temp.CreateFile("src/Api/obj/project.assets.json", 1024);
        temp.CreateFile("src/Core/bin/Core.dll", 512);

        var result = await CreateScanner().ScanAsync(new ScanOptions { RootPath = temp.Path });

        Assert.Equal(3, result.Folders.Count);
        Assert.Contains(result.Folders, f => f.Category == "bin" && f.RelativePath.EndsWith("Api".ToString()) is false);
        Assert.Equal(2, result.Folders.Count(f => f.Category == "bin"));
        Assert.Equal(1, result.Folders.Count(f => f.Category == "obj"));
    }

    [Fact]
    public async Task Beachtet_Konfigurierte_Ordnernamen()
    {
        using var temp = new TempDirectory();
        temp.CreateFile("a/bin/x.dll", 100);
        temp.CreateFile("a/obj/y.dll", 100);
        temp.CreateFile("a/target/z.dll", 100);

        var result = await CreateScanner().ScanAsync(new ScanOptions
        {
            RootPath = temp.Path,
            FolderNames = ["target"],
        });

        Assert.Single(result.Folders);
        Assert.Equal("target", result.Folders[0].Category);
    }

    [Fact]
    public async Task Steigt_Nicht_In_Kandidaten_Ab()
    {
        using var temp = new TempDirectory();
        temp.CreateFile("bin/unterordner/tief.dll", 100);
        temp.CreateFile("bin/obj/verschachtert.dll", 100);

        var result = await CreateScanner().ScanAsync(new ScanOptions { RootPath = temp.Path });

        // "bin" ist der einzige Kandidat - das darunter liegende "obj" wird nicht erneut gefunden.
        Assert.Single(result.Folders);
        Assert.Equal("bin", result.Folders[0].Category);
    }

    [Fact]
    public async Task Ohne_Unterordner_Wird_Nur_Direkt_Ebene_Geprueft()
    {
        using var temp = new TempDirectory();
        temp.CreateFile("bin/a.dll", 100);
        temp.CreateFile("projekt/bin/b.dll", 100);

        var result = await CreateScanner().ScanAsync(new ScanOptions
        {
            RootPath = temp.Path,
            IncludeSubdirectories = false,
        });

        Assert.Single(result.Folders);
        Assert.Equal("bin", result.Folders[0].RelativePath);
    }

    [Fact]
    public async Task Summiert_Gesamtgroesse_Und_Dateianzahl()
    {
        using var temp = new TempDirectory();
        temp.CreateFile("bin/a.dll", 1000);
        temp.CreateFile("bin/b.dll", 500);
        temp.CreateFile("quelle/c.dll", 250);

        var result = await CreateScanner().ScanAsync(new ScanOptions { RootPath = temp.Path });

        Assert.Equal(1750, result.RootTotalBytes);
        Assert.Equal(3, result.RootFileCount);
        Assert.Equal(1500, result.Folders.Single().SizeInBytes);
    }

    [Fact]
    public async Task Sortiert_Ergebnisse_Nach_Groesse_Absteigend()
    {
        using var temp = new TempDirectory();
        temp.CreateFile("klein/bin/a.dll", 10);
        temp.CreateFile("gross/bin/a.dll", 5000);

        var result = await CreateScanner().ScanAsync(new ScanOptions { RootPath = temp.Path });

        Assert.True(result.Folders[0].SizeInBytes >= result.Folders[1].SizeInBytes);
    }

    [Fact]
    public async Task Verzeichnis_Kommt_Nicht_Vor_Bei_Ungueltigem_Pfad()
    {
        using var temp = new TempDirectory();

        var result = await CreateScanner().ScanAsync(new ScanOptions
        {
            RootPath = System.IO.Path.Combine(temp.Path, "gibt-es-nicht"),
        });

        Assert.Empty(result.Folders);
        Assert.Equal(0, result.RootTotalBytes);
    }
}
