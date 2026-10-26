using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using BinObjCleaner.Models;
using BinObjCleaner.Services;
using BinObjCleaner.ViewModels;
using BinObjCleaner.Views;

namespace BinObjCleaner.Infrastructure;

/// <summary>
/// Hilfsfunktion, um das Hauptfenster (und optional den Bestätigungsdialog)
/// als PNG-Datei zu rendern. Wird über die Umgebungsvariable
/// <c>BINOBJCLEANER_SNAPSHOT</c> aktiviert (Pfad der Zieldatei) und ist
/// für Dokumentation und CI-Screenshots gedacht.
/// </summary>
public static class SnapshotCapture
{
    public const string EnvironmentVariable = "BINOBJCLEANER_SNAPSHOT";

    /// <summary>
    /// Prüft, ob ein Snapshot angefordert wurde, und startet die Aufnahme, falls ja.
    /// </summary>
    public static void AttachIfRequested(Window window, MainWindowViewModel? viewModel = null, ILocalizationService? localization = null)
    {
        var path = Environment.GetEnvironmentVariable(EnvironmentVariable);

        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        window.Opened += async (_, _) =>
        {
            await Task.Delay(2500);


            await Dispatcher.UIThread.InvokeAsync(() => Capture(window, path));

            if (viewModel is not null && viewModel.Results.Count > 0)
            {
                await Task.Delay(500);
                await Dispatcher.UIThread.InvokeAsync(() => CaptureDialog(viewModel, path, localization));
            }

            window.Close();
        };
    }

    private static void Capture(Window window, string path)
    {
        try
        {
            var size = window.ClientSize;

            if (size.Width <= 0 || size.Height <= 0)
            {
                File.WriteAllText(path + ".log", $"ClientSize war 0: {size}");
                return;
            }

            var pixelSize = new PixelSize((int)Math.Round(size.Width), (int)Math.Round(size.Height));
            var target = new RenderTargetBitmap(pixelSize, new Vector(96, 96));
            target.Render(window);

            Write(target, path);
        }
        catch (Exception e)
        {
            File.WriteAllText(path + ".log", e.ToString());
        }
    }

    private static void CaptureDialog(MainWindowViewModel viewModel, string path, ILocalizationService? localization)
    {
        try
        {
            var request = new DeleteConfirmationRequest(
                viewModel.Results.Where(r => r.IsSelected).ToList(),
                viewModel.RootTotalBytes,
                RecycleBin.IsSupported,
                viewModel.UseRecycleBin);

            var dialog = new DeleteConfirmDialog
            {
                DataContext = new DeleteConfirmDialogViewModel(
                    localization ?? new LocalizationService(viewModel.CurrentLanguage),
                    request),
            };

            dialog.Show();

            // Der Dialog braucht einen Layout-Durchlauf, bevor er gerendert werden kann.
            Dispatcher.UIThread.RunJobs();

            var size = dialog.ClientSize;
            var pixelSize = new PixelSize((int)Math.Round(size.Width), (int)Math.Round(size.Height));
            var target = new RenderTargetBitmap(pixelSize, new Vector(96, 96));
            target.Render(dialog);

            Write(target, Path.Combine(
                Path.GetDirectoryName(path) ?? string.Empty,
                Path.GetFileNameWithoutExtension(path) + "-dialog.png"));

            dialog.Close();
        }
        catch (Exception e)
        {
            File.WriteAllText(path + ".dialog.log", e.ToString());
        }
    }

    private static void Write(RenderTargetBitmap target, string path)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var stream = File.Create(path);
        target.Save(stream, new PngBitmapEncoderOptions());
    }
}
