using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BinObjCleaner.Models;

/// <summary>
/// Knoten der hierarchischen Ergebnisansicht.
/// </summary>
public sealed class FolderTreeNode : INotifyPropertyChanged
{
    private bool _isSelected = true;
    private bool _isExpanded;

    public FolderTreeNode(string name, string relativePath, long sizeInBytes, bool isCandidate, bool isArtifact)
    {
        Name = name;
        RelativePath = relativePath;
        SizeInBytes = sizeInBytes;
        IsCandidate = isCandidate;
        IsArtifact = isArtifact;
    }

    /// <summary>Anzeigename des Knotens.</summary>
    public string Name { get; }

    /// <summary>Relativer Pfad ab der Wurzel des Stammverzeichnisses.</summary>
    public string RelativePath { get; }

    /// <summary>Summierte Größe aller enthaltenen Kandidaten.</summary>
    public long SizeInBytes { get; internal set; }

    /// <summary>Der Knoten ist selbst ein Löschkandidat (bin/obj).</summary>
    public bool IsCandidate { get; }

    /// <summary>Der Knoten enthält mindestens einen Löschkandidaten.</summary>
    public bool IsArtifact { get; }

    /// <summary>Kinderknoten.</summary>
    public List<FolderTreeNode> Children { get; } = [];

    /// <summary>Ob der Knoten in die nächste Löschaktion einbezogen wird.</summary>
    public bool IsSelected
    {
        get => _isSelected;
        set => SetField(ref _isSelected, value);
    }

    /// <summary>Ob der Knoten in der Baumansicht aufgeklappt ist.</summary>
    public bool IsExpanded
    {
        get => _isExpanded;
        set => SetField(ref _isExpanded, value);
    }

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
