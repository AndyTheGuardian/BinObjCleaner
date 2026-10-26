using BinObjCleaner.Models;

namespace BinObjCleaner.Services;

/// <summary>
/// Baut aus den gefundenen Kandidaten eine hierarchische Baumstruktur.
/// </summary>
public static class FolderTreeBuilder
{
    /// <summary>
    /// Erzeugt die Wurzelknoten des Baums. Jeder Knoten summiert die Größe
    /// aller Kandidaten in seinem Teilbaum.
    /// </summary>
    public static IReadOnlyList<FolderTreeNode> Build(IEnumerable<ArtifactFolder> folders)
    {
        ArgumentNullException.ThrowIfNull(folders);

        var roots = new List<FolderTreeNode>();
        var index = new Dictionary<string, FolderTreeNode>(StringComparer.OrdinalIgnoreCase);

        foreach (var folder in folders.OrderBy(f => f.RelativePath, StringComparer.CurrentCulture))
        {
            var segments = folder.RelativePath
                .Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);

            if (segments.Length == 0)
            {
                continue;
            }

            FolderTreeNode? parent = null;
            var currentPath = string.Empty;

            for (var i = 0; i < segments.Length; i++)
            {
                var segment = segments[i];
                currentPath = currentPath.Length == 0 ? segment : Path.Combine(currentPath, segment);
                var isCandidate = i == segments.Length - 1;

                if (!index.TryGetValue(currentPath, out var node))
                {
                    node = new FolderTreeNode(segment, currentPath, 0, isCandidate, isCandidate);
                    index[currentPath] = node;

                    if (parent is null)
                    {
                        roots.Add(node);
                    }
                    else
                    {
                        parent.Children.Add(node);
                    }
                }

                node.SizeInBytes += folder.SizeInBytes;
                parent = node;
            }
        }

        return roots;
    }
}
