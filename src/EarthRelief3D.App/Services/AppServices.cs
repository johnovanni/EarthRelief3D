using System.IO;
using System.Windows.Threading;
using EarthRelief3D.App.Curated;
using EarthRelief3D.App.Presentation;
using EarthRelief3D.Core.Curated;
using EarthRelief3D.App.Settings;
using EarthRelief3D.App.ViewModels;
using EarthRelief3D.Core.Feeds;
using EarthRelief3D.Core.Model;
using EarthRelief3D.Core.Store;
using GlobeCore.Feeds;
using GlobeCore.Earth;
using GlobeCore.Logging;
using GlobeCore.Regions;
using GlobeCore.Storage;
using GlobeCore.Time;

namespace EarthRelief3D.App.Services;

/// <summary>
/// Everything below the window (the composition root's non-UI part, so tests can build it): the crisis store and the
/// country-value store (Plan 3) filled from their caches, the cache writers, the feeds and their scheduler, and the view
/// models wired to them. Feed status
/// changes arrive on the scheduler's threads; the status dots queue them onto the dispatcher.
/// </summary>
public sealed class AppServices : IDisposable
{
    /// <summary>The status dots, in this order: the feeds' <see cref="IFeed.Name"/>s (the four sources, the two needs feeds, then Borders).</summary>
    public static IReadOnlyList<string> FeedNames { get; } = [.. SourceDates.SourceNames, .. SourceDates.NeedsFeedNames, SourceDates.BordersFeedName];

    /// <summary>
    /// The bundled extracts (Task 6, P3-10) in <c>data\datasets</c>: each one's status dot name (the layer's source name,
    /// <see cref="LayerCatalog.SourceName(ShadingLayer)"/>) and folder. They are loaded once at start and change only with the app.
    /// </summary>
    public static IReadOnlyList<(string Name, string Folder)> BundledDatasets { get; } =
    [
        (LayerCatalog.SourceName(ShadingLayer.Hunger), "fao-food-security"),
        (LayerCatalog.SourceName(ShadingLayer.OilProduction), "eia-crude-oil"),
        (LayerCatalog.SourceName(ShadingLayer.WaterStress), "aqueduct-water-stress"),
    ];

    /// <summary>The status dots: the feeds' (<see cref="FeedNames"/>), then the bundled datasets'.</summary>
    public static IReadOnlyList<string> StatusDotNames { get; } = [.. FeedNames, .. BundledDatasets.Select(d => d.Name)];

    private readonly CacheWriter _cacheWriter;
    private readonly CacheWriter _indicatorWriter;
    private readonly FeedHttp _http;
    private readonly IAppLog _log;
    private bool _shutDown;

    private AppServices(CrisisStore store, CrisisCache cache, IndicatorStore indicators, IndicatorCache indicatorCache, CountryResolver countries,
        CurrentBoundaries boundaries, IReadOnlyList<IFeed> feeds, FeedScheduler scheduler, MainViewModel main, bool cacheLoaded, FeedHttp http, IAppLog log)
    {
        Store = store;
        Cache = cache;
        Indicators = indicators;
        IndicatorCache = indicatorCache;
        Countries = countries;
        Boundaries = boundaries;
        Feeds = feeds;
        Scheduler = scheduler;
        Main = main;
        CacheLoaded = cacheLoaded;
        _http = http;
        _log = log;
        _cacheWriter = new CacheWriter(h => store.Changed += h, h => store.Changed -= h, cache.FilePath, () => cache.Save(store.Snapshot), log);
        _indicatorWriter = new CacheWriter(h => indicators.Changed += h, h => indicators.Changed -= h, indicatorCache.FilePath,
            () => indicatorCache.Save(indicators.Snapshot), log);
    }

    public CrisisStore Store { get; }
    public CrisisCache Cache { get; }

    /// <summary>The country-value series (Plan 3 shadings), shared with the view models and the scene.</summary>
    public IndicatorStore Indicators { get; }
    public IndicatorCache IndicatorCache { get; }
    public CountryResolver Countries { get; }

    /// <summary>The live boundary set (the bundled one, a stored newer one, or the one the Borders feed adopted).</summary>
    public CurrentBoundaries Boundaries { get; }
    public IReadOnlyList<IFeed> Feeds { get; }
    public FeedScheduler Scheduler { get; }
    public MainViewModel Main { get; }

    /// <summary>True when the last run's snapshot was loaded from the cache (shown at once, offline too).</summary>
    public bool CacheLoaded { get; }

    /// <param name="dispatcher">The UI thread's dispatcher (null in tests: view models update at once).</param>
    /// <param name="ipcAreaPause">The IPC area step's wait between countries (null: <see cref="IpcAreaFeed.DefaultPause"/>; zero for offline samples).</param>
    /// <param name="showDrafts">Also show the curated rows the owner has not approved yet (<c>--show-drafts</c>, P3-4).</param>
    public static AppServices Create(AppDataRoot root, FeedHttp http, IClock clock, IAppLog log, ISettingsStore<AppSettings> settings,
        Dispatcher? dispatcher, TimeSpan? ipcAreaPause = null, bool showDrafts = false)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(http);
        // The boundaries: the newest stored set that passes validation, else the bundled one, with the rules of data\boundary-rules.json.
        var (boundaries, boundaryStore, validator, rules, fingerprint) = BoundaryData.Open(BoundaryData.DataDir, root.CacheDir, log);
        var countries = new CountryResolver(boundaries, CountryResolver.ReadAliases(Path.Combine(BoundaryData.DataDir, CountryResolver.AliasesFile)));
        var store = new CrisisStore();
        var cache = new CrisisCache(root, log);
        var cached = cache.Load();
        if (cached is not null)
        {
            // An old cache is judged now: WHO reports past 90 days are dropped and IPC analyses past 12 months marked stale.
            // The view models and layers judge again at display time, so data that ages while the app runs is handled too.
            var seen = cached.AsSeenAt(clock.UtcNow);
            store.Load(seen);
            log.Info($"Crisis cache loaded: {cached.Crises.Count} crises ({cached.Crises.Count - seen.Crises.Count} WHO reports past "
                + $"{WhoOutbreakFeed.Window} left out), {cached.Displacement.Count} displacement figures, {cached.Unplaced.Count} unplaced reports");
        }

        // The country-value series of the last run, shown at once (offline too) with their own dates.
        var indicators = new IndicatorStore();
        var indicatorCache = new IndicatorCache(root, log);
        if (indicatorCache.Load() is { } cachedSeries)
        {
            indicators.Load(cachedSeries);
            log.Info($"Indicator cache loaded: {cachedSeries.Series.Count} series");
        }

        // Task 6: the bundled extracts (FAO, EIA, WRI) replace any cached copy of their series; one that is missing or
        // broken is logged, its dot turns amber and its layers say "Data not loaded yet" (the cached copy, if any, stays).
        var bundled = BundledDatasets.Select(d => BundledDataset.Load(d.Name, Path.Combine(BundledDataset.DatasetsDir, d.Folder),
            iso => countries.ByIso3(iso) is not null, log)).ToList();
        indicators.ReplaceAll([.. bundled.SelectMany(b => b.Series)]);
        foreach (var b in bundled)
            store.ReplaceUnplaced(b.Name, b.Loaded && b.Newest is { } cite
                ? [.. b.NotPlaced.Select(n => new UnplacedReport(b.Name, $"{b.Name} figures for {n.Code} ({n.Name})", n.Reason, cite))]
                : []);

        // P3-2: the IPC areas, an extra step of the IPC feed (its status dot): it needs the national records just stored, and
        // a country whose areas cannot be had keeps the 20% rule without failing the national figures. The areas of the last
        // run are on disk (cache\ipc-areas), so they show offline too, while they belong to the cached national records. They
        // are parsed on a pool thread (a few MB per country), not here on the UI thread: the map first composes with the 20%
        // rule, and the store's change redraws it by area when the load is done. The IPC feed's first run waits for the load.
        var ipcAreas = new IpcAreaFeed(http, store, clock, log, root.CacheDir, ipcAreaPause);
        var areasLoaded = ipcAreas.StartLoadingCached(store.Snapshot.Crises.OfType<FoodCrisis>());

        IFeed[] feeds =
        [
            new UcdpCandidateFeed(http, store, clock, log, countries),
            new IpcNationalFeed(http, store, clock, log, countries, ipcAreas),
            new WhoOutbreakFeed(http, store, clock, log, countries),
            new UnhcrDisplacementFeed(http, store, clock, log, countries),
            // Plan 3 needs: weekly, into the country-value store (their unplaced codes go to the crisis store's list).
            new GhoFeed(http, indicators, store, clock, log, countries),
            new HnoFeed(http, indicators, store, clock, log, countries),
            // Checks Natural Earth's releases once a day; a newer, valid set replaces the live one without a restart.
            // A rejected release is remembered with the fingerprint of the rules and probes; an app update that changes
            // them looks at that release again. After a failed download the feed waits 6 h (ruling R5).
            new BoundaryUpdateFeed(new NaturalEarthBoundarySource(http), rules, validator, boundaryStore, boundaries, log, fingerprint),
        ];
        if (!feeds.Select(f => f.Name).SequenceEqual(FeedNames)) throw new InvalidOperationException("Feed names and status dots differ.");

        // Plan 3: the curated designations and camp sites (data\curated), validated on load. A bad table is a packaging
        // fault the tests catch; should one ship anyway, the app runs without the curated layers and the log says why.
        CuratedTables curatedTables;
        try { curatedTables = CuratedTables.Load(Path.Combine(BoundaryData.DataDir, "curated")); }
        catch (Exception ex) when (ex is InvalidDataException or IOException or UnauthorizedAccessException)
        {
            log.Error("The curated tables could not be loaded; the atrocities and camps layers are empty", ex);
            curatedTables = CuratedTables.Empty;
        }
        var curated = new CuratedStore(curatedTables, showDrafts);
        if (showDrafts) log.Info("Curated drafts are shown (--show-drafts), each labelled \"" + DesignationText.DraftLabel + "\"");

        var resolver = countries;
        var main = new MainViewModel(store, settings, iso => resolver.ByIso3(iso)?.Name, dispatcher, () => clock.UtcNow, boundaries, indicators,
            curated: curated);
        var scheduler = new FeedScheduler(feeds, clock, log, AppInfo.FeedIdleNote);
        foreach (var status in scheduler.Statuses) main.Status.Update(status);
        foreach (var b in bundled) main.Status.Update(BundledDataset.Status(b, c => CrisisText.DataDate(c, clock.UtcNow)));
        scheduler.StatusChanged += (_, status) => main.Status.Update(status);   // feed threads: Update uses BeginInvoke
        return new AppServices(store, cache, indicators, indicatorCache, countries, boundaries, feeds, scheduler, main, cached is not null, http, log)
        {
            IpcAreasLoaded = areasLoaded,
        };
    }

    /// <summary>The start-up load of the cached IPC areas (off the UI thread); completes when they are in the store. Never faults.</summary>
    public Task IpcAreasLoaded { get; private init; } = Task.CompletedTask;

    /// <summary>Starts the feeds (each runs at once, then on its interval).</summary>
    public void Start() => Scheduler.Start();

    /// <summary>Stops the feeds and saves the cache once. Each step is guarded, so one failure does not skip the next.</summary>
    public void Shutdown()
    {
        if (_shutDown) return;
        _shutDown = true;
        Step("stop the feed scheduler", Scheduler.Stop);
        Step("stop the cache writer", _cacheWriter.Dispose);
        Step("save the crisis cache", _cacheWriter.SaveNow);
        Step("stop the indicator cache writer", _indicatorWriter.Dispose);
        Step("save the indicator cache", _indicatorWriter.SaveNow);
    }

    public void Dispose()
    {
        Shutdown();
        Step("dispose the HTTP client", _http.Dispose);
    }

    private void Step(string what, Action action)
    {
        try { action(); }
        catch (Exception ex) { _log.Error("Shutdown: could not " + what, ex); }
    }
}

/// <summary>
/// Saves a store to its cache file a short while after it changes (changes in a burst save once), and on demand: the
/// crisis store and the country-value store each have one.
/// </summary>
public sealed class CacheWriter : IDisposable
{
    public static readonly TimeSpan Delay = TimeSpan.FromSeconds(2);

    private readonly Action<EventHandler> _unsubscribe;
    private readonly string _filePath;
    private readonly Action _save;
    private readonly IAppLog _log;
    private readonly Timer _timer;
    private readonly object _gate = new();
    private readonly object _saveGate = new();   // the timer's save and SaveNow never write the file at once
    private bool _dirty, _disposed;

    /// <param name="subscribe">Adds a handler to the store's Changed event.</param>
    /// <param name="unsubscribe">Removes it.</param>
    /// <param name="filePath">The cache file, whose folder is made before a save.</param>
    /// <param name="save">Saves the store's current snapshot (the cache logs its own failures).</param>
    public CacheWriter(Action<EventHandler> subscribe, Action<EventHandler> unsubscribe, string filePath, Action save, IAppLog log)
    {
        ArgumentNullException.ThrowIfNull(subscribe);
        _unsubscribe = unsubscribe ?? throw new ArgumentNullException(nameof(unsubscribe));
        _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        _save = save ?? throw new ArgumentNullException(nameof(save));
        _log = log ?? throw new ArgumentNullException(nameof(log));
        _timer = new Timer(_ => SaveIfDirty());
        subscribe(OnChanged);
    }

    private void OnChanged(object? sender, EventArgs e)
    {
        lock (_gate)
        {
            _dirty = true;
            if (!_disposed) _timer.Change(Delay, Timeout.InfiniteTimeSpan);
        }
    }

    private void SaveIfDirty()
    {
        lock (_gate)
        {
            if (!_dirty) return;
            _dirty = false;
        }
        lock (_saveGate) Save();
    }

    /// <summary>Saves now if anything changed since the last save.</summary>
    public void SaveNow() => SaveIfDirty();

    private void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
            _save();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _log.Warn("Could not create the cache folder", ex);
        }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed) return;
            _disposed = true;
            _unsubscribe(OnChanged);
        }
        _timer.Dispose();
    }
}
