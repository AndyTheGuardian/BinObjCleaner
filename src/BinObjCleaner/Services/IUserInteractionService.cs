namespace BinObjCleaner.Services;

/// <summary>
/// Verfügbare Designs.
/// </summary>
public enum AppTheme
{
    Dark,
    Light,
}

/// <summary>
/// Wendet das Design auf die Anwendung an.
/// </summary>
public interface IThemeService
{
    AppTheme CurrentTheme { get; }

    void Apply(AppTheme theme);
}

/// <summary>
/// Rückmeldung an den Benutzer - Ordnerwahl und Löschbestätigung.
/// </summary>
public interface IUserInteractionService
{
    /// <summary>
    /// Öffnet einen Ordnerdialog. Gibt <see langword="null"/> zurück, wenn abgebrochen wurde.
    /// </summary>
    Task<string?> PickFolderAsync();

    /// <summary>
    /// Fragt die Löschbestätigung ab.
    /// </summary>
    Task<Models.DeleteConfirmationResult> ConfirmDeletionAsync(Models.DeleteConfirmationRequest request);
}
