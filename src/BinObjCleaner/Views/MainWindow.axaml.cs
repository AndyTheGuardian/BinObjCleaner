using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using BinObjCleaner.ViewModels;

namespace BinObjCleaner.Views;

public partial class MainWindow : Window
{
    public MainWindow() => InitializeComponent();

    private void OnRemoveFolderNameClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { CommandParameter: string name } &&
            DataContext is MainWindowViewModel viewModel)
        {
            viewModel.RemoveFolderNameCommand.Execute(name);
        }
    }

    /// <summary>
    /// Hebt den Rahmen des Eingabefelds hervor, wenn es den Fokus erhält.
    /// Der Fokus-Indikator des Themes ist abgeschaltet, damit der Rahmen
    /// nicht über die Feldgrenzen hinausragt.
    /// </summary>
    private void OnFieldGotFocus(object? sender, RoutedEventArgs e)
    {
        SetFieldFocus(sender, true);

        if (sender is Control { } control && control.Classes.Contains("newfolder") &&
            DataContext is MainWindowViewModel viewModel)
        {
            viewModel.IsNewFolderFocused = true;
        }
    }

    private void OnFieldLostFocus(object? sender, RoutedEventArgs e)
    {
        SetFieldFocus(sender, false);

        if (sender is Control { } control && control.Classes.Contains("newfolder") &&
            DataContext is MainWindowViewModel viewModel)
        {
            viewModel.IsNewFolderFocused = false;

            // Beim Verlassen wird ein eingetragener Name uebernommen.
            if (!string.IsNullOrWhiteSpace(viewModel.NewFolderName))
            {
                viewModel.AddFolderNameCommand.Execute(viewModel.NewFolderName);
            }
        }
    }

    /// <summary>
    /// Blendet das Eingabefeld fuer einen zusaetzlichen Ordnernamen ein.
    /// </summary>
    private void OnAddFolderNameClick(object? sender, PointerPressedEventArgs e)
    {
        NewFolderNameBox.Focus();
    }

    /// <summary>
    /// Enter bestaetigt den neuen Ordnernamen.
    /// </summary>
    private void OnNewFolderNameKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key is not Key.Enter || DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        e.Handled = true;

        if (!string.IsNullOrWhiteSpace(viewModel.NewFolderName))
        {
            viewModel.AddFolderNameCommand.Execute(viewModel.NewFolderName);
        }

        NewFolderNameBox.Focus();
    }

    private static void SetFieldFocus(object? sender, bool focused)
    {
        if (sender is not Control control)
        {
            return;
        }

        foreach (var border in control.GetVisualAncestors().OfType<Border>())
        {
            if (border.Classes.Contains("field"))
            {
                border.Classes.Set("focused", focused);
                return;
            }
        }
    }
}
