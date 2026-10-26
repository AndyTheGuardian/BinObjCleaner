using Avalonia.Controls;
using Avalonia.Interactivity;

namespace BinObjCleaner.Views;

public partial class DeleteConfirmDialog : Window
{
    public DeleteConfirmDialog() => InitializeComponent();

    private DeleteConfirmDialogViewModel? ViewModel => DataContext as DeleteConfirmDialogViewModel;

    private void OnConfirmClick(object? sender, RoutedEventArgs e)
    {
        if (ViewModel is { } viewModel)
        {
            viewModel.IsConfirmed = true;
            Close(viewModel);
        }
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        if (ViewModel is { } viewModel)
        {
            viewModel.IsConfirmed = false;
            Close(viewModel);
        }
    }
}
