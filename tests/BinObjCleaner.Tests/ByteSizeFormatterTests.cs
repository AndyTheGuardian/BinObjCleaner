using System.Globalization;
using BinObjCleaner.Services;

namespace BinObjCleaner.Tests;

public sealed class ByteSizeFormatterTests
{
    [Theory]
    [InlineData(0, "0 B")]
    [InlineData(512, "512 B")]
    [InlineData(1024, "1,0 KB")]
    [InlineData(1536, "1,5 KB")]
    [InlineData(1048576, "1,0 MB")]
    [InlineData(1073741824, "1,0 GB")]
    public void Formatiert_Groessen_Im_Deutschen_Format(long bytes, string expected)
    {
        var result = ByteSizeFormatter.Format(bytes, CultureInfo.GetCultureInfo("de-DE"));

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Formatiert_Prozentwerte()
    {
        var result = ByteSizeFormatter.FormatPercent(0.2543, CultureInfo.GetCultureInfo("de-DE"));

        Assert.Equal("25,4 %", result);
    }

    [Fact]
    public void Formatiert_Negative_Werte()
    {
        var result = ByteSizeFormatter.Format(-1024, CultureInfo.GetCultureInfo("de-DE"));

        Assert.StartsWith("-", result);
    }
}
