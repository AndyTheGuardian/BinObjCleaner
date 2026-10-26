using BinObjCleaner.Models;
using BinObjCleaner.Services;

namespace BinObjCleaner.Tests;

public sealed class DirectorySizeCalculatorTests
{
    [Fact]
    public void Summiert_Dateigroessen_Und_Zaehlt_Dateien()
    {
        using var temp = new TempDirectory();
        temp.CreateFile("a.txt", 100);
        temp.CreateFile("unter/b.txt", 200);
        temp.CreateFile("unter/tiefer/c.txt", 300);

        var result = new DirectorySizeCalculator().Calculate(temp.Path);

        Assert.Equal(600, result.Bytes);
        Assert.Equal(3, result.FileCount);
    }

    [Fact]
    public void Liefert_Nullwerte_Fuer_Ungueltigen_Pfad()
    {
        var result = new DirectorySizeCalculator().Calculate("C:\\gibt-es-nicht\\xyz");

        Assert.Equal(0, result.Bytes);
        Assert.Equal(0, result.FileCount);
    }
}
