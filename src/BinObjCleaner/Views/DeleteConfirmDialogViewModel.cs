using BinObjCleaner.Models;
using BinObjCleaner.Services;
using BinObjCleaner.ViewModels;

namespace BinObjCleaner.Views;

/// <summary>
/// ViewModel des Lösch-Bestätigungsdialogs.
/// </summary>
public sealed class DeleteConfirmDialogViewModel : ObservableObject
{
    private bool _useRecycleBin = true;
    private bool _isConfirmed;

    public DeleteConfirmDialogViewModel(ILocalizationService localization, DeleteConfirmationRequest request)
    {
        ArgumentNullException.ThrowIfNull(localization);
        ArgumentNullException.ThrowIfNull(request);

        Folders = request.Folders;
        TotalBytes = request.Folders.Sum(f => f.SizeInBytes);
        TotalText = ByteSizeFormatter.Format(TotalBytes);
        RootTotalText = ByteSizeFormatter.Format(request.RootTotalBytes);
        ShareText = ByteSizeFormatter.FormatPercent(
            request.RootTotalBytes > 0 ? (double)TotalBytes / request.RootTotalBytes : 0d);
        FileCount = request.Folders.Sum(f => f.FileCount);
        RecycleBinAvailable = request.RecycleBinAvailable;
        UseRecycleBin = request.RecycleBinAvailable && request.UseRecycleBinByDefault;

        TitleText = localization.Get("ConfirmDeleteTitle", request.Folders.Count);
        MessageText = localization.Get("ConfirmDeleteText");
        SummaryText = localization.Get("ConfirmSummary", request.Folders.Count, FileCount);
        ShareOfRootText = localization.Get("ShareOfRoot", RootTotalText, ShareText);
        MoreFoldersText = localization.Get("MoreFolders", Math.Max(0, request.Folders.Count - 4));
        RecycleBinText = localization.Get("UseRecycleBin");
        RecycleBinHintText = request.RecycleBinAvailable
            ? localization.Get("RecycleBinHint")
            : localization.Get("RecycleBinUnavailable");
        CancelText = localization.Get("Cancel");
        ConfirmText = localization.Get("FinalDelete");
    }

    public IReadOnlyList<ArtifactFolder> Folders { get; }

    public long TotalBytes { get; }

    public int FileCount { get; }

    public string TotalText { get; }

    public string RootTotalText { get; }

    public string ShareText { get; }

    public bool RecycleBinAvailable { get; }

    public string TitleText { get; }

    public string MessageText { get; }

    public string SummaryText { get; }

    public string ShareOfRootText { get; }

    public string MoreFoldersText { get; }

    public string RecycleBinText { get; }

    public string RecycleBinHintText { get; }

    public string CancelText { get; }

    public string ConfirmText { get; }

    /// <summary>Soll in den Papierkorb verschoben werden?</summary>
    public bool UseRecycleBin
    {
        get => _useRecycleBin;
        set => SetProperty(ref _useRecycleBin, value);
    }

    /// <summary>Hat der Benutzer bestätigt?</summary>
    public bool IsConfirmed
    {
        get => _isConfirmed;
        set => SetProperty(ref _isConfirmed, value);
    }
}
