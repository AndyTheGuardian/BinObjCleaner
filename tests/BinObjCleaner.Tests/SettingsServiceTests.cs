using BinObjCleaner.Services;

namespace BinObjCleaner.Tests;

public sealed class SettingsServiceTests
{
    [Fact]
    public void Speichert_Und_Liest_Einstellungen()
    {
        using var temp = new TempDirectory();
        var path = System.IO.Path.Combine(temp.Path, "settings.json");
        var service = new SettingsService(path);

        service.Save(new AppSettings
        {
            Theme = "Light",
            Language = "en",
            LastRootPath = "C:\\Projekt",
            FolderNames = ["bin", "obj", "target"],
            DryRun = false,
            ViewMode = "Tree",
        });

        var loaded = service.Load();

        Assert.Equal("Light", loaded.Theme);
        Assert.Equal("en", loaded.Language);
        Assert.Equal("C:\\Projekt", loaded.LastRootPath);
        Assert.Equal(["bin", "obj", "target"], loaded.FolderNames);
        Assert.False(loaded.DryRun);
        Assert.Equal("Tree", loaded.ViewMode);
    }

    [Fact]
    public void Liefert_Standardwerte_Bei_Fehlender_Datei()
    {
        using var temp = new TempDirectory();
        var service = new SettingsService(System.IO.Path.Combine(temp.Path, "gibt-es-nicht.json"));

        var settings = service.Load();

        Assert.Equal("Dark", settings.Theme);
        Assert.Equal("de", settings.Language);
        Assert.Equal(["bin", "obj"], settings.FolderNames);
    }
}
