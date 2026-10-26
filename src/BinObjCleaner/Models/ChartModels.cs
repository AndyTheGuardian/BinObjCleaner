namespace BinObjCleaner.Models;

/// <summary>
/// Ein Segment des Donut-Diagramms.
/// </summary>
/// <param name="Label">Anzeigename (z. B. <c>bin</c>).</param>
/// <param name="Value">Wert in Bytes.</param>
/// <param name="ColorKey">Schlüssel für die Farbzuteilung (<c>bin</c>, <c>obj</c>, <c>remaining</c>, ...).</param>
/// <param name="Share">Anteil am Gesamtwert (0..1).</param>
/// <param name="IsBin">Gehört das Segment zur Kategorie <c>bin</c>?</param>
/// <param name="IsObj">Gehört das Segment zur Kategorie <c>obj</c>?</param>
public readonly record struct ChartSegment(
    string Label,
    long Value,
    string ColorKey,
    double Share,
    bool IsBin = false,
    bool IsObj = false);

/// <summary>
/// Ein Eintrag des Balkendiagramms.
/// </summary>
/// <param name="Label">Anzeigename.</param>
/// <param name="Value">Wert in Bytes.</param>
/// <param name="ColorKey">Schlüssel für die Farbzuteilung.</param>
public readonly record struct BarChartItem(string Label, long Value, string ColorKey);

/// <summary>
/// Ein Eintrag der Projekt-Aufteilung.
/// </summary>
/// <param name="Name">Projektname.</param>
/// <param name="Value">Größe in Bytes.</param>
public readonly record struct ProjectBreakdownItem(string Name, long Value);

/// <summary>
/// Anfrage an den Benutzer, eine Löschung zu bestätigen.
/// </summary>
/// <param name="Folders">Betroffene Ordner.</param>
/// <param name="RootTotalBytes">Gesamtgröße des Stammverzeichnisses.</param>
/// <param name="RecycleBinAvailable">Steht ein Papierkorb zur Verfügung?</param>
/// <param name="UseRecycleBinByDefault">Vorgabe für den Papierkorb-Schalter im Dialog.</param>
public sealed record DeleteConfirmationRequest(
    IReadOnlyList<ArtifactFolder> Folders,
    long RootTotalBytes,
    bool RecycleBinAvailable,
    bool UseRecycleBinByDefault = true);

/// <summary>
/// Antwort des Benutzers auf eine Löschbestätigung.
/// </summary>
/// <param name="Confirmed">Der Benutzer hat bestätigt.</param>
/// <param name="UseRecycleBin">Der Papierkorb soll verwendet werden.</param>
public readonly record struct DeleteConfirmationResult(bool Confirmed, bool UseRecycleBin);
