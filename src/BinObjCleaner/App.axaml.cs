using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using BinObjCleaner.Services;
using BinObjCleaner.ViewModels;
using BinObjCleaner.Views;

namespace BinObjCleaner;

public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        var settings = new SettingsService();
        var localization = new LocalizationService(settings.Load().Language);
        var themeService = new AppThemeService();

        var scanner = new ArtifactScanner(new DirectorySizeCalculator());
        var deletionService = new DeletionService();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();

            var viewModel = new MainWindowViewModel(
                scanner,
                deletionService,
                settings,
                localization,
                themeService,
                new UserInteractionService(() => desktop.MainWindow, localization));

            desktop.MainWindow.DataContext = viewModel;

            Infrastructure.SnapshotCapture.AttachIfRequested(desktop.MainWindow, viewModel, localization);
        }

        base.OnFrameworkInitializationCompleted();
    }
}
