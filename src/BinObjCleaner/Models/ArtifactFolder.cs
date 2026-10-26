using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BinObjCleaner.Models;

/// <summary>
/// Ein gefundener Löschkandidat (z. B. ein <c>bin</c>- oder <c>obj</c>-Ordner).
/// </summary>
public sealed class ArtifactFolder : INotifyPropertyChanged
{
    private bool _isSelected = true;

    public ArtifactFolder(string fullPath, string relativePath, string category, long sizeInBytes, int fileCount)
    {
        FullPath = fullPath ?? throw new ArgumentNullException(nameof(fullPath));
        RelativePath = relativePath ?? throw new ArgumentNullException(nameof(relativePath));
        Category = category ?? throw new ArgumentNullException(nameof(category));
        SizeInBytes = sizeInBytes;
        FileCount = fileCount;
    }

    /// <summary>Absoluter Pfad auf der Platte.</summary>
    public string FullPath { get; }

    /// <summary>Relativer Pfad zum Stammverzeichnis (für die Anzeige).</summary>
    public string RelativePath { get; }

    /// <summary>Name des Ordners, der die Kategorie ausgelöst hat (z. B. <c>bin</c>).</summary>
    public string Category { get; }

    /// <summary>Rekursiv ermittelte Größe in Bytes.</summary>
    public long SizeInBytes { get; }

    /// <summary>Anzahl der enthaltenen Dateien.</summary>
    public int FileCount { get; }

    /// <summary>Gibt an, ob der Ordner in die nächste Löschaktion einbezogen wird.</summary>
    public bool IsSelected
    {
        get => _isSelected;
        set => SetField(ref _isSelected, value);
    }

    /// <summary>Normalisierte Kategorie (klein geschrieben) - wird für die Farbzuordnung verwendet.</summary>
    public string CategoryKey => Category.ToLowerInvariant();

    /// <summary>Übergeordneter Pfad (für die abgesetzte Darstellung im Mockup).</summary>
    public string DirectoryPath
    {
        get
        {
            var index = RelativePath.LastIndexOf(Path.DirectorySeparatorChar);
            return index > 0 ? RelativePath[..index] : string.Empty;
        }
    }

    /// <summary>Letzte Pfadkomponente (z. B. <c>bin</c>).</summary>
    public string LeafName
    {
        get
        {
            var index = RelativePath.LastIndexOf(Path.DirectorySeparatorChar);
            return index >= 0 ? RelativePath[(index + 1)..] : RelativePath;
        }
    }

    /// <summary>Ist die Kategorie <c>bin</c>? (wird für die Farbzuordnung verwendet)</summary>
    public bool IsBinCategory => CategoryKey == "bin";

    /// <summary>Ist die Kategorie <c>obj</c>? (wird für die Farbzuordnung verwendet)</summary>
    public bool IsObjCategory => CategoryKey == "obj";

    /// <summary>
    /// Anteil dieses Ordners am größten gefundenen Ordner (0..1) - dient der BalkenDarstellung.
    /// </summary>
    public double Share
    {
        get => _share;
        set => SetField(ref _share, value);
    }

    private double _share;

    public event PropertyChangedEventHandler? PropertyChanged;

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public override string ToString() => RelativePath;
}
