using System.Collections.ObjectModel;
using System.Globalization;
using BinObjCleaner.Infrastructure;
using BinObjCleaner.Models;
using BinObjCleaner.Services;

namespace BinObjCleaner.ViewModels;

/// <summary>
/// ViewModel des Hauptfensters.
/// </summary>
public sealed class MainWindowViewModel : ObservableObject
{
    private readonly IArtifactScanner _scanner;
    private readonly IDeletionService _deletionService;
    private readonly ISettingsService _settingsService;
    private readonly ILocalizationService _localization;
    private readonly IThemeService _themeService;
    private readonly IUserInteractionService _interactionService;

    private readonly List<ArtifactFolder> _results = [];
    private readonly ObservableCollection<FolderTreeNode> _treeRoots = [];

    private string _rootPath = string.Empty;
    private string _filterText = string.Empty;
    private string _statusText = string.Empty;
    private string _sortColumn = "size";
    private bool _sortAscending;
    private bool _includeSubdirectories = true;
    private bool _includeHidden;
    private bool _followSymbolicLinks;
    private bool _useRecycleBin = true;
    private bool _dryRun = true;
    private bool _isBusy;
    private bool _isTreeView;
    private long _rootTotalBytes;
    private int _rootFileCount;
    private int _selectedCount;
    private AppTheme _theme = AppTheme.Dark;

    public MainWindowViewModel(
        IArtifactScanner scanner,
        IDeletionService deletionService,
        ISettingsService settingsService,
        ILocalizationService localization,
        IThemeService themeService,
        IUserInteractionService interactionService)
    {
        _scanner = scanner ?? throw new ArgumentNullException(nameof(scanner));
        _deletionService = deletionService ?? throw new ArgumentNullException(nameof(deletionService));
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _localization = localization ?? throw new ArgumentNullException(nameof(localization));
        _themeService = themeService ?? throw new ArgumentNullException(nameof(themeService));
        _interactionService = interactionService ?? throw new ArgumentNullException(nameof(interactionService));

        BrowseCommand = new RelayCommand(Browse);
        ScanCommand = new RelayCommand(() => _ = ScanAsync());
        CancelCommand = new RelayCommand(() => _cts?.Cancel(), () => IsBusy);
        DeleteSelectedCommand = new RelayCommand(() => _ = DeleteAsync(selectedOnly: true));
        DeleteAllCommand = new RelayCommand(() => _ = DeleteAsync(selectedOnly: false));
        SelectAllCommand = new RelayCommand(SelectAll);
        ClearSelectionCommand = new RelayCommand(ClearSelection);
        ClearPathCommand = new RelayCommand(ClearPath);
        SetThemeCommand = new RelayCommand(SetTheme);
        SetLanguageCommand = new RelayCommand(SetLanguage);
        SetViewModeCommand = new RelayCommand(SetViewMode);
        SortCommand = new RelayCommand(Sort);
        AddFolderNameCommand = new RelayCommand(AddFolderName);
        RemoveFolderNameCommand = new RelayCommand(RemoveFolderName);

        _localization.LanguageChanged += (_, _) => OnLanguageChanged();

        LoadSettings();
        _statusText = Text("StatusIdle");

        // Zuletzt verwendeten Stammordner automatisch erneut scannen.
        if (!string.IsNullOrWhiteSpace(RootPath) && Directory.Exists(RootPath))
        {
            _ = ScanAsync();
        }
    }

    private CancellationTokenSource? _cts;

    #region Ergebnisse

    /// <summary>Alle gefundenen Kandidaten (ungefiltert).</summary>
    public IReadOnlyList<ArtifactFolder> Results => _results;

    /// <summary>Wurzelknoten der Baumansicht.</summary>
    public IReadOnlyList<FolderTreeNode> TreeRoots => _treeRoots;

    /// <summary>Sortierte und gefilterte Treffer für die flache Liste.</summary>
    public IEnumerable<ArtifactFolder> VisibleResults
    {
        get
        {
            IEnumerable<ArtifactFolder> query = _results;

            if (!string.IsNullOrWhiteSpace(_filterText))
            {
                query = query.Where(f => f.RelativePath.Contains(_filterText, StringComparison.CurrentCultureIgnoreCase));
            }

            query = _sortColumn switch
            {
                "path" => _sortAscending
                    ? query.OrderBy(f => f.RelativePath, StringComparer.CurrentCulture)
                    : query.OrderByDescending(f => f.RelativePath, StringComparer.CurrentCulture),
                _ => _sortAscending
                    ? query.OrderBy(f => f.SizeInBytes)
                    : query.OrderByDescending(f => f.SizeInBytes),
            };

            return query;
        }
    }

    #endregion

    #region Diagramme

    public ObservableCollection<ChartSegment> Segments { get; } = [];

    public ObservableCollection<BarChartItem> TopFolders { get; } = [];

    public ObservableCollection<ProjectBreakdownItem> ProjectBreakdown { get; } = [];

    #endregion

    #region Optionen

    public ObservableCollection<string> FolderNames { get; } = [];

    /// <summary>Suchfilter für die Ergebnisliste.</summary>
    public string FilterText
    {
        get => _filterText;
        set
        {
            if (SetProperty(ref _filterText, value))
            {
                OnPropertyChanged(nameof(VisibleResults));
            }
        }
    }

    public bool IncludeSubdirectories
    {
        get => _includeSubdirectories;
        set => SetProperty(ref _includeSubdirectories, value);
    }

    public bool IncludeHidden
    {
        get => _includeHidden;
        set => SetProperty(ref _includeHidden, value);
    }

    public bool FollowSymbolicLinks
    {
        get => _followSymbolicLinks;
        set => SetProperty(ref _followSymbolicLinks, value);
    }

    /// <summary>Löschungen über den Papierkorb ausführen.</summary>
    public bool UseRecycleBin
    {
        get => _useRecycleBin;
        set => SetProperty(ref _useRecycleBin, value);
    }

    /// <summary>Nur simulieren, nicht tatsächlich löschen.</summary>
    public bool DryRun
    {
        get => _dryRun;
        set => SetProperty(ref _dryRun, value);
    }

    public bool IsTreeView
    {
        get => _isTreeView;
        set
        {
            if (value)
            {
                SetViewMode(true);
            }
            else
            {
                OnPropertyChanged(nameof(IsTreeView));
            }
        }
    }

    public bool IsFlatView
    {
        get => !_isTreeView;
        set
        {
            if (value)
            {
                SetViewMode(false);
            }
            else
            {
                OnPropertyChanged(nameof(IsFlatView));
            }
        }
    }

    /// <summary>Aktives Design.</summary>
    public AppTheme CurrentTheme => _theme;

    /// <summary>Ist das dunkle Design aktiv?</summary>
    public bool IsDarkTheme
    {
        get => _theme == AppTheme.Dark;
        set
        {
            if (value)
            {
                SetTheme(AppTheme.Dark);
            }
            else
            {
                // Ablehnung des Schreibvorgangs melden, damit die Anzeige synchron bleibt.
                OnPropertyChanged(nameof(IsDarkTheme));
            }
        }
    }

    /// <summary>Ist das helle Design aktiv?</summary>
    public bool IsLightTheme
    {
        get => _theme == AppTheme.Light;
        set
        {
            if (value)
            {
                SetTheme(AppTheme.Light);
            }
            else
            {
                OnPropertyChanged(nameof(IsLightTheme));
            }
        }
    }

    /// <summary>Ist die Oberfläche auf Deutsch?</summary>
    public bool IsGerman
    {
        get => string.Equals(_localization.CurrentLanguage, "de", StringComparison.OrdinalIgnoreCase);
        set
        {
            if (value)
            {
                SetLanguage("de");
            }
            else
            {
                OnPropertyChanged(nameof(IsGerman));
            }
        }
    }

    /// <summary>Ist die Oberfläche auf Englisch?</summary>
    public bool IsEnglish
    {
        get => string.Equals(_localization.CurrentLanguage, "en", StringComparison.OrdinalIgnoreCase);
        set
        {
            if (value)
            {
                SetLanguage("en");
            }
            else
            {
                OnPropertyChanged(nameof(IsEnglish));
            }
        }
    }

    /// <summary>Ist ein Stammverzeichnis eingetragen?</summary>
    public bool HasRootPath => !string.IsNullOrWhiteSpace(RootPath);

    /// <summary>Ist das Eingabefeld fuer einen zusaetzlichen Ordnernamen aktiv?</summary>
    public bool IsNewFolderFocused
    {
        get => _isNewFolderFocused;
        set => SetProperty(ref _isNewFolderFocused, value);
    }

    private bool _isNewFolderFocused;

    /// <summary>Name eines neuen Ordner-Kandidaten (für die Eingabe in der Optionsleiste).</summary>
    public string NewFolderName
    {
        get => _newFolderName;
        set => SetProperty(ref _newFolderName, value);
    }

    private string _newFolderName = string.Empty;

    #endregion

    #region Status und Kennzahlen

    public string RootPath
    {
        get => _rootPath;
        set
        {
            if (SetProperty(ref _rootPath, value))
            {
                OnPropertyChanged(nameof(HasRootPath));
            }
        }
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                BrowseCommand.RaiseCanExecuteChanged();
                ScanCommand.RaiseCanExecuteChanged();
                DeleteSelectedCommand.RaiseCanExecuteChanged();
                DeleteAllCommand.RaiseCanExecuteChanged();
                CancelCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public int FoundCount => _results.Count;

    public int BinCount => _results.Count(f => string.Equals(f.Category, "bin", StringComparison.OrdinalIgnoreCase));

    public int ObjCount => _results.Count(f => string.Equals(f.Category, "obj", StringComparison.OrdinalIgnoreCase));

    public long CandidatesTotalBytes => _results.Sum(f => f.SizeInBytes);

    public long RootTotalBytes => _rootTotalBytes;

    public int RootFileCount => _rootFileCount;

    public int SelectedCount => _selectedCount;

    /// <summary>Sind alle Treffer ausgewählt?</summary>
    public bool IsAllSelected
    {
        get => _results.Count > 0 && _results.All(f => f.IsSelected);
        set
        {
            foreach (var folder in _results)
            {
                folder.IsSelected = value;
            }
        }
    }

    /// <summary>Anteil der Kandidaten am Stammverzeichnis (0..1).</summary>
    public double SavingsRatio => _rootTotalBytes > 0 ? (double)CandidatesTotalBytes / _rootTotalBytes : 0d;

    public string FoundCountText => FoundCount.ToString(CultureInfo.CurrentCulture);

    /// <summary>Sprache der Oberfläche.</summary>
    public string CurrentLanguage => _localization.CurrentLanguage;

    public string CandidatesTotalText => ByteSizeFormatter.Format(CandidatesTotalBytes);

    public string CandidatesBytesText => string.Format(CultureInfo.CurrentCulture, "{0:N0}", CandidatesTotalBytes);

    public string RootTotalText => ByteSizeFormatter.Format(_rootTotalBytes);

    public string SavingsRatioText => ByteSizeFormatter.FormatPercent(SavingsRatio);

    public string BinObjSplitText => Text("KpiBinObjSplit", BinCount, ObjCount);

    public string SelectedCountText => SelectedCount.ToString(CultureInfo.CurrentCulture);

    public string CandidatesTotalValueText => Text("KpiCandidates");

    public string ChartCenterValueText => ByteSizeFormatter.Format(CandidatesTotalBytes);

    public string ChartCenterSubText => Text("OfTotal", ByteSizeFormatter.Format(_rootTotalBytes));

    #endregion

    #region Befehle

    public RelayCommand BrowseCommand { get; }

    public RelayCommand ScanCommand { get; }

    public RelayCommand CancelCommand { get; }

    public RelayCommand DeleteSelectedCommand { get; }

    public RelayCommand DeleteAllCommand { get; }

    public RelayCommand SelectAllCommand { get; }

    public RelayCommand ClearSelectionCommand { get; }

    public RelayCommand ClearPathCommand { get; }

    public RelayCommand SetThemeCommand { get; }

    public RelayCommand SetLanguageCommand { get; }

    public RelayCommand SetViewModeCommand { get; }

    public RelayCommand SortCommand { get; }

    public RelayCommand AddFolderNameCommand { get; }

    public RelayCommand RemoveFolderNameCommand { get; }

    #endregion

    #region Lokalisierte Texte

    public string AppTitleText => Text("AppTitle");

    /// <summary>Fenstitel inklusive Build-Information - macht den laufenden Stand erkennbar.</summary>
    public string WindowTitle => $"{AppTitleText} · {BuildInfo.ShortText}";

    public string BrowseText => Text("Browse");

    public string RescanText => Text("Rescan");

    public string ClearPathText => Text("ClearPath");

    public string FolderNamesText => Text("FolderNames");

    public string AddFolderNameText => Text("AddFolderName");

    public string IncludeSubdirectoriesText => Text("IncludeSubdirectories");

    public string IncludeHiddenText => Text("IncludeHidden");

    public string FollowSymbolicLinksText => Text("FollowSymbolicLinks");

    public string UseRecycleBinOptionText => Text("UseRecycleBinOption");

    public string KpiFoundFoldersText => Text("KpiFoundFolders");

    public string KpiCandidatesText => Text("KpiCandidates");

    public string KpiRootTotalText => Text("KpiRootTotal");

    public string KpiSavingsText => Text("KpiSavings");

    public string KpiRootTotalSubText => Text("KpiRootTotalSub");

    public string KpiSavingsSubText => Text("KpiSavingsSub");

    public string ResultsText => Text("Results");

    public string SelectAllText => Text("SelectAll");

    public string FilterPlaceholderText => Text("FilterPlaceholder");

    public string ColumnTypeText => Text("ColumnType");

    public string ColumnPathText => Text("ColumnPath");

    public string ColumnShareText => Text("ColumnShare");

    public string ColumnSizeText => Text("ColumnSize");

    /// <summary>Beschriftung des Sortier-Buttons für die Größe (zeigt die Sortierrichtung).</summary>
    public string SortSizeText => _sortColumn == "size"
        ? (_sortAscending ? Text("ColumnSizeAsc") : Text("ColumnSizeDesc"))
        : Text("ColumnSize");

    /// <summary>Beschriftung des Sortier-Buttons für den Pfad (zeigt die Sortierrichtung).</summary>
    public string SortPathText => _sortColumn == "path"
        ? (_sortAscending ? Text("SortPathAsc") : Text("SortPathDesc"))
        : Text("SortPath");

    public string SavingsTitleText => Text("SavingsTitle");

    public string SavingsHintText => Text("SavingsHint");

    public string TopFoldersText => Text("TopFolders");

    public string ByProjectText => Text("ByProject");

    public string DryRunText => Text("DryRun");

    /// <summary>Kurzform der Build-Information (für die Fußleiste).</summary>
    public string BuildInfoText => BuildInfo.ShortText;

    /// <summary>Ausführliche Build-Information (Tooltip).</summary>
    public string BuildInfoDetails => BuildInfo.Details;

    public string DeleteSelectionText => Text("DeleteSelection", _selectedCount);

    public string DeleteAllText => Text("DeleteAll");

    public string ThemeText => Text("Theme");

    public string LanguageText => Text("Language");

    public string ViewModeText => Text("ViewMode");

    public string ViewFlatText => Text("ViewFlat");

    public string ViewTreeText => Text("ViewTree");

    #endregion

    /// <summary>
    /// Liefert einen lokalisierten Text.
    /// </summary>
    public string Text(string key, params object?[] args) => _localization.Get(key, args);

    private async void Browse()
    {
        var path = await _interactionService.PickFolderAsync();

        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        RootPath = path;
        await ScanAsync();
    }

    private async Task ScanAsync()
    {
        if (IsBusy || string.IsNullOrWhiteSpace(RootPath))
        {
            return;
        }

        if (!Directory.Exists(RootPath))
        {
            StatusText = Text("InvalidPath", RootPath);
            return;
        }

        IsBusy = true;

        try
        {
            await ExecuteScanAsync();
        }
        finally
        {
            IsBusy = false;
            SaveSettings();
        }
    }

    /// <summary>
    /// Führt den eigentlichen Scan durch. Der Busy-Zustand wird vom Aufrufer verwaltet.
    /// </summary>
    private async Task ExecuteScanAsync()
    {
        _cts?.Dispose();
        _cts = new CancellationTokenSource();

        StatusText = Text("StatusScanning", 0, 0);

        var progress = new Progress<ScanProgress>(p =>
            StatusText = Text("StatusScanning", p.ScannedDirectories, p.FoundFolders));

        try
        {
            var options = new ScanOptions
            {
                RootPath = RootPath,
                FolderNames = FolderNames.ToList(),
                IncludeSubdirectories = IncludeSubdirectories,
                IncludeHidden = IncludeHidden,
                FollowSymbolicLinks = FollowSymbolicLinks,
            };

            var result = await _scanner.ScanAsync(options, progress, _cts.Token);

            ApplyResults(result);
            StatusText = _results.Count == 0
                ? Text("StatusNothingFound")
                : Text("StatusDone", _results.Count, _rootFileCount, FormatDuration(result.Elapsed));
        }
        catch (OperationCanceledException)
        {
            StatusText = Text("StatusCancelled");
        }
        catch (Exception e)
        {
            StatusText = Text("StatusFailed", e.Message);
        }
    }

    private void ApplyResults(ScanResult result)
    {
        foreach (var folder in _results)
        {
            folder.PropertyChanged -= OnFolderPropertyChanged;
        }

        _results.Clear();
        _rootTotalBytes = result.RootTotalBytes;
        _rootFileCount = result.RootFileCount;

        foreach (var folder in result.Folders)
        {
            folder.PropertyChanged += OnFolderPropertyChanged;
            _results.Add(folder);
        }

        _treeRoots.Clear();
        foreach (var node in FolderTreeBuilder.Build(_results))
        {
            node.IsExpanded = true;
            _treeRoots.Add(node);
        }

        var maxSize = _results.Count == 0 ? 0 : _results.Max(f => f.SizeInBytes);
        foreach (var folder in _results)
        {
            folder.Share = maxSize > 0 ? (double)folder.SizeInBytes / maxSize : 0d;
        }

        UpdateCharts();
        UpdateSelectionCount();

        OnPropertyChanged(nameof(FoundCount));
        OnPropertyChanged(nameof(BinCount));
        OnPropertyChanged(nameof(ObjCount));
        OnPropertyChanged(nameof(CandidatesTotalBytes));
        OnPropertyChanged(nameof(CandidatesTotalText));
        OnPropertyChanged(nameof(CandidatesBytesText));
        OnPropertyChanged(nameof(RootTotalBytes));
        OnPropertyChanged(nameof(RootTotalText));
        OnPropertyChanged(nameof(SavingsRatio));
        OnPropertyChanged(nameof(SavingsRatioText));
        OnPropertyChanged(nameof(FoundCountText));
        OnPropertyChanged(nameof(BinObjSplitText));
        OnPropertyChanged(nameof(ChartCenterValueText));
        OnPropertyChanged(nameof(ChartCenterSubText));
        OnPropertyChanged(nameof(VisibleResults));
    }

    private void UpdateCharts()
    {
        Segments.Clear();

        var byCategory = _results
            .GroupBy(f => f.Category, StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var remaining = Math.Max(0, _rootTotalBytes - CandidatesTotalBytes);
        var total = byCategory.Sum(g => g.Sum(x => x.SizeInBytes)) + remaining;

        foreach (var group in byCategory)
        {
            var value = group.Sum(g => g.SizeInBytes);
            var key = group.Key.ToLowerInvariant();
            Segments.Add(new ChartSegment(
                group.Key,
                value,
                key,
                total > 0 ? (double)value / total : 0d,
                key == "bin",
                key == "obj"));
        }

        Segments.Add(new ChartSegment(
            Text("LegendRemaining"),
            remaining,
            "remaining",
            total > 0 ? (double)remaining / total : 0d));

        TopFolders.Clear();
        foreach (var folder in _results
                     .OrderByDescending(f => f.SizeInBytes)
                     .ThenBy(f => f.RelativePath, StringComparer.CurrentCulture)
                     .Take(5))
        {
            TopFolders.Add(new BarChartItem(folder.RelativePath, folder.SizeInBytes, folder.Category.ToLowerInvariant()));
        }

        ProjectBreakdown.Clear();
        foreach (var group in _results
                     .GroupBy(f => ProjectNameOf(f.RelativePath), StringComparer.CurrentCultureIgnoreCase)
                     .OrderByDescending(g => g.Sum(x => x.SizeInBytes))
                     .ThenBy(g => g.Key, StringComparer.CurrentCulture))
        {
            ProjectBreakdown.Add(new ProjectBreakdownItem(group.Key, group.Sum(x => x.SizeInBytes)));
        }
    }

    private static string ProjectNameOf(string relativePath)
    {
        var segments = relativePath.Split(
            [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
            StringSplitOptions.RemoveEmptyEntries);

        return segments.Length >= 2 ? segments[^2] : segments.FirstOrDefault() ?? string.Empty;
    }

    private void OnFolderPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ArtifactFolder.IsSelected))
        {
            UpdateSelectionCount();
        }
    }

    private void UpdateSelectionCount()
    {
        _selectedCount = _results.Count(f => f.IsSelected);
        OnPropertyChanged(nameof(SelectedCount));
        OnPropertyChanged(nameof(SelectedCountText));
        OnPropertyChanged(nameof(DeleteSelectionText));
        OnPropertyChanged(nameof(IsAllSelected));
    }

    private void SelectAll()
    {
        foreach (var folder in _results)
        {
            folder.IsSelected = true;
        }
    }

    private void ClearSelection()
    {
        foreach (var folder in _results)
        {
            folder.IsSelected = false;
        }
    }

    private async Task DeleteAsync(bool selectedOnly)
    {
        if (IsBusy)
        {
            return;
        }

        var targets = (selectedOnly ? _results.Where(f => f.IsSelected) : _results.AsEnumerable()).ToList();

        if (targets.Count == 0)
        {
            StatusText = Text("NothingSelected");
            return;
        }

        var request = new DeleteConfirmationRequest(targets, _rootTotalBytes, RecycleBin.IsSupported, UseRecycleBin);
        var confirmation = await _interactionService.ConfirmDeletionAsync(request);

        if (!confirmation.Confirmed)
        {
            return;
        }

        // Die Papierkorb-Wahl aus dem Dialog für den nächsten Durchlauf merken.
        if (UseRecycleBin != confirmation.UseRecycleBin)
        {
            UseRecycleBin = confirmation.UseRecycleBin;
            SaveSettings();
        }

        if (DryRun)
        {
            var size = targets.Sum(t => t.SizeInBytes);
            StatusText = Text("StatusDryRun", targets.Count, ByteSizeFormatter.Format(size));
            return;
        }

        IsBusy = true;
        _cts?.Dispose();
        _cts = new CancellationTokenSource();

        var progress = new Progress<DeletionProgress>(p =>
            StatusText = Text("StatusDeleting", p.Index + 1, p.Total));

        try
        {
            var report = await _deletionService.DeleteAsync(targets, confirmation.UseRecycleBin, progress, _cts.Token);

            StatusText = report.Failures.Count == 0
                ? Text("StatusDeleted", report.DeletedCount, ByteSizeFormatter.Format(report.FreedBytes))
                : Text("StatusDeleteFailed", report.Failures.Count);

            await ExecuteScanAsync();
        }
        catch (OperationCanceledException)
        {
            StatusText = Text("StatusCancelled");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void SetTheme(object? parameter)
    {
        if (parameter is string name && Enum.TryParse<AppTheme>(name, ignoreCase: true, out var theme))
        {
            SetTheme(theme);
        }
    }

    private void SetTheme(AppTheme theme)
    {
        if (_theme == theme)
        {
            return;
        }

        _theme = theme;
        _themeService.Apply(theme);
        OnPropertyChanged(nameof(IsDarkTheme));
        OnPropertyChanged(nameof(IsLightTheme));
        SaveSettings();
    }

    private void SetLanguage(object? parameter)
    {
        if (parameter is string language)
        {
            SetLanguage(language);
        }
    }

    private void SetLanguage(string language)
    {
        if (string.Equals(_localization.CurrentLanguage, language, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _localization.SetLanguage(language);
        SaveSettings();
    }

    private void SetViewMode(object? parameter)
    {
        if (parameter is not string mode)
        {
            return;
        }

        SetViewMode(string.Equals(mode, "Tree", StringComparison.OrdinalIgnoreCase));
    }

    private void SetViewMode(bool treeView)
    {
        if (_isTreeView == treeView)
        {
            return;
        }

        _isTreeView = treeView;
        OnPropertyChanged(nameof(IsTreeView));
        OnPropertyChanged(nameof(IsFlatView));
        SaveSettings();
    }

    private void Sort(object? parameter)
    {
        if (parameter is not string column)
        {
            return;
        }

        if (string.Equals(_sortColumn, column, StringComparison.OrdinalIgnoreCase))
        {
            _sortAscending = !_sortAscending;
        }
        else
        {
            _sortColumn = column;
            _sortAscending = true;
        }

        OnPropertyChanged(nameof(VisibleResults));
        OnPropertyChanged(nameof(SortSizeText));
        OnPropertyChanged(nameof(SortPathText));
    }

    private void ClearPath()
    {
        RootPath = string.Empty;
        ClearResults();
    }

    private void ClearResults()
    {
        foreach (var folder in _results)
        {
            folder.PropertyChanged -= OnFolderPropertyChanged;
        }

        _results.Clear();
        _treeRoots.Clear();
        _rootTotalBytes = 0;
        _rootFileCount = 0;

        UpdateCharts();
        UpdateSelectionCount();

        OnPropertyChanged(nameof(FoundCount));
        OnPropertyChanged(nameof(BinCount));
        OnPropertyChanged(nameof(ObjCount));
        OnPropertyChanged(nameof(CandidatesTotalBytes));
        OnPropertyChanged(nameof(CandidatesTotalText));
        OnPropertyChanged(nameof(CandidatesBytesText));
        OnPropertyChanged(nameof(RootTotalBytes));
        OnPropertyChanged(nameof(RootTotalText));
        OnPropertyChanged(nameof(SavingsRatio));
        OnPropertyChanged(nameof(SavingsRatioText));
        OnPropertyChanged(nameof(FoundCountText));
        OnPropertyChanged(nameof(BinObjSplitText));
        OnPropertyChanged(nameof(ChartCenterValueText));
        OnPropertyChanged(nameof(ChartCenterSubText));
        OnPropertyChanged(nameof(VisibleResults));

        StatusText = Text("StatusIdle");
    }

    private void AddFolderName(object? parameter)
    {
        var name = (parameter as string)?.Trim() ?? NewFolderName.Trim();

        if (string.IsNullOrEmpty(name))
        {
            return;
        }

        if (!FolderNames.Contains(name, StringComparer.OrdinalIgnoreCase))
        {
            FolderNames.Add(name);
        }

        NewFolderName = string.Empty;
        SaveSettings();
    }

    private void RemoveFolderName(object? parameter)
    {
        if (parameter is string name)
        {
            FolderNames.Remove(name);
            SaveSettings();
        }
    }

    private void LoadSettings()
    {
        var settings = _settingsService.Load();

        RootPath = settings.LastRootPath ?? string.Empty;

        FolderNames.Clear();
        foreach (var name in settings.FolderNames.Where(n => !string.IsNullOrWhiteSpace(n)))
        {
            FolderNames.Add(name);
        }

        if (FolderNames.Count == 0)
        {
            FolderNames.Add("bin");
            FolderNames.Add("obj");
        }

        IncludeHidden = settings.IncludeHidden;
        FollowSymbolicLinks = settings.FollowSymbolicLinks;
        UseRecycleBin = settings.UseRecycleBin;
        DryRun = settings.DryRun;
        IsTreeView = string.Equals(settings.ViewMode, "Tree", StringComparison.OrdinalIgnoreCase);

        _localization.SetLanguage(settings.Language);

        _theme = Enum.TryParse<AppTheme>(settings.Theme, ignoreCase: true, out var theme) ? theme : AppTheme.Dark;
        _themeService.Apply(_theme);
    }

    private void SaveSettings()
    {
        _settingsService.Save(new AppSettings
        {
            Theme = _theme.ToString(),
            Language = _localization.CurrentLanguage,
            LastRootPath = RootPath,
            FolderNames = FolderNames.ToList(),
            IncludeHidden = IncludeHidden,
            FollowSymbolicLinks = FollowSymbolicLinks,
            UseRecycleBin = UseRecycleBin,
            DryRun = DryRun,
            ViewMode = IsTreeView ? "Tree" : "Flat",
        });
    }

    private void OnLanguageChanged()
    {
        OnAllPropertiesChanged();
        UpdateCharts();
    }

    private static string FormatDuration(TimeSpan elapsed) =>
        elapsed.TotalSeconds < 1
            ? string.Format(CultureInfo.CurrentCulture, "{0:F0} ms", elapsed.TotalMilliseconds)
            : string.Format(CultureInfo.CurrentCulture, "{0:F1} s", elapsed.TotalSeconds);
}
