using Avalonia.Controls;
using Avalonia.Platform.Storage;
using BinObjCleaner.Models;
using BinObjCleaner.Services;

namespace BinObjCleaner.Views;

/// <summary>
/// Steht in Verbindung mit dem Hauptfenster und stellt Ordnerwahl sowie
/// Löschbestätigung bereit.
/// </summary>
public sealed class UserInteractionService : IUserInteractionService
{
    private readonly Func<Window?> _windowAccessor;
    private readonly ILocalizationService _localization;

    public UserInteractionService(Func<Window?> windowAccessor, ILocalizationService? localization = null)
    {
        _windowAccessor = windowAccessor ?? throw new ArgumentNullException(nameof(windowAccessor));
        _localization = localization ?? new LocalizationService();
    }

    public async Task<string?> PickFolderAsync()
    {
        var window = _windowAccessor();
        if (window?.StorageProvider is null)
        {
            return null;
        }

        var folders = await window.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = _localization.Get("ChooseFolderTitle"),
            AllowMultiple = false,
        });

        return folders.Count > 0 ? folders[0].TryGetLocalPath() : null;
    }

    public async Task<DeleteConfirmationResult> ConfirmDeletionAsync(DeleteConfirmationRequest request)
    {
        var window = _windowAccessor();
        if (window is null)
        {
            return new DeleteConfirmationResult(false, false);
        }

        var dialog = new DeleteConfirmDialog
        {
            DataContext = new DeleteConfirmDialogViewModel(_localization, request),
        };

        var confirmed = await dialog.ShowDialog<DeleteConfirmDialogViewModel>(window);
        var viewModel = confirmed ?? dialog.DataContext as DeleteConfirmDialogViewModel;

        return viewModel is null
            ? new DeleteConfirmationResult(false, false)
            : new DeleteConfirmationResult(viewModel.IsConfirmed, viewModel.UseRecycleBin);
    }
}
