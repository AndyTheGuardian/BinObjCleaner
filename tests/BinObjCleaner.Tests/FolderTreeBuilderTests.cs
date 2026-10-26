using BinObjCleaner.Models;
using BinObjCleaner.Services;

namespace BinObjCleaner.Tests;

public sealed class FolderTreeBuilderTests
{
    [Fact]
    public void Baut_Hierarchie_Aus_Relativen_Pfaden()
    {
        var folders = new[]
        {
            new ArtifactFolder("C:\\root\\src\\Api\\bin", "src\\Api\\bin", "bin", 100, 1),
            new ArtifactFolder("C:\\root\\src\\Api\\obj", "src\\Api\\obj", "obj", 50, 1),
            new ArtifactFolder("C:\\root\\tests\\Unit\\bin", "tests\\Unit\\bin", "bin", 25, 1),
        };

        var roots = FolderTreeBuilder.Build(folders);

        Assert.Equal(2, roots.Count);

        var src = roots.Single(r => r.Name == "src");
        Assert.Equal(150, src.SizeInBytes);
        Assert.False(src.IsCandidate);

        var api = src.Children.Single();
        Assert.Equal("Api", api.Name);
        Assert.Equal(150, api.SizeInBytes);
        Assert.Equal(2, api.Children.Count);
        Assert.All(api.Children, child => Assert.True(child.IsCandidate));
    }

    [Fact]
    public void Liefert_Leere_Liste_Fuer_Keine_Eintraege()
    {
        Assert.Empty(FolderTreeBuilder.Build([]));
    }
}
