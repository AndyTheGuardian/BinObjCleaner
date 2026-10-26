using Avalonia;
using Avalonia.Headless;

namespace BinObjCleaner.Tests;

/// <summary>
/// Minimale Avalonia-Anwendung für die Headless-Tests.
/// Bewusst ohne die echte <c>App</c>, damit die Tests keine Einstellungen laden
/// oder Fenster erzeugen.
/// </summary>
public class TestApp : Application
{
    public override void Initialize()
    {
        // Keine Styles nötig - die Tests prüfen reine Steuerelement-Logik.
    }
}

/// <summary>
/// Initialisiert Avalonia im Headless-Modus. Avalonia-Steuerelemente benötigen für
/// ihre Geometrie eine registrierte Rendering-Plattform.
/// </summary>
public sealed class AvaloniaFixture : IDisposable
{
    public AvaloniaFixture()
    {
        AppBuilder
            .Configure<TestApp>()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions())
            .SetupWithoutStarting();
    }

    public void Dispose()
    {
        // Keine Aufräumarbeiten nötig.
    }
}

/// <summary>
/// Testsammlung, die die Headless-Plattform bereitstellt.
/// </summary>
[CollectionDefinition(Name)]
public sealed class AvaloniaCollection : ICollectionFixture<AvaloniaFixture>
{
    public const string Name = "Avalonia";
}
