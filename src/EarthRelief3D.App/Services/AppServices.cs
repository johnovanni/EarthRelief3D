using System.IO;
using System.Windows.Threading;
using EarthRelief3D.App.Presentation;
using EarthRelief3D.App.Settings;
using EarthRelief3D.App.ViewModels;
using EarthRelief3D.Core.Feeds;
using EarthRelief3D.Core.Store;
using GlobeCore.Feeds;
using GlobeCore.Earth;
using GlobeCore.Logging;
using GlobeCore.Regions;
using GlobeCore.Storage;
using GlobeCore.Time;

namespace EarthRelief3D.App.Services;

/// <summary>
/// Everything below the window (the composition root's non-UI part, so tests can build it): the crisis store filled from
/// the cache, the cache writer, the four feeds and their scheduler, and the view models wired to them. Feed status
/// changes arrive on the scheduler's threads; the status dots queue them onto the dispatcher.
/// </summary>
public sealed class AppServices : IDisposable
{
    /// <summary>The status dots, in this order: the feeds' <see cref="IFeed.Name"/>s (the four sources, then Borders).</summary>
    public static IReadOnlyList<string> FeedNames { get; } = [.. SourceDates.SourceNames, SourceDates.BordersFeedName];

    private readonly CrisisCacheWriter _cacheWriter;
    private readonly FeedHttp _http;
    private readonly IAppLog _log;
    private bool _shutDown;

    private AppServices(CrisisStore store, CrisisCache cache, CountryResolver countries, CurrentBoundaries boundaries, IReadOnlyList<IFeed> feeds,
        FeedScheduler scheduler, MainViewModel main, bool cacheLoaded, FeedHttp http, IAppLog log)
    {
        Store = store;
        Cache = cache;
        Countries = countries;
        Boundaries = boundaries;
        Feeds = feeds;
        Scheduler = scheduler;
        Main = main;
        CacheLoaded = cacheLoaded;
        _http = http;
        _log = log;
        _cacheWriter = new CrisisCacheWriter(store, cache, log);
    }

    public CrisisStore Store { get; }
    public CrisisCache Cache { get; }
    public CountryResolver Countries { get; }

    /// <summary>The live boundary set (the bundled one, a stored newer one, or the one the Borders feed adopted).</summary>
    public CurrentBoundaries Boundaries { get; }
    public IReadOnlyList<IFeed> Feeds { get; }
    public FeedScheduler Scheduler { get; }
    public MainViewModel Main { get; }

    /// <summary>True when the last run's snapshot was loaded from the cache (shown at once, offline too).</summary>
    public bool CacheLoaded { get; }

    /// <param name="dispatcher">The UI thread's dispatcher (null in tests: view models update at once).</param>
    public static AppServices Create(AppDataRoot root, FeedHttp http, IClock clock, IAppLog log, ISettingsStore<AppSettings> settings,
        Dispatcher? dispatcher)
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

        IFeed[] feeds =
        [
            new UcdpCandidateFeed(http, store, clock, log, countries),
            new IpcNationalFeed(http, store, clock, log, countries),
            new WhoOutbreakFeed(http, store, clock, log, countries),
            new UnhcrDisplacementFeed(http, store, clock, log, countries),
            // Checks Natural Earth's releases once a day; a newer, valid set replaces the live one without a restart.
            // A rejected release is remembered with the fingerprint of the rules and probes; an app update that changes
            // them looks at that release again. After a failed download the feed waits 6 h (ruling R5).
            new BoundaryUpdateFeed(new NaturalEarthBoundarySource(http), rules, validator, boundaryStore, boundaries, log, fingerprint),
        ];
        if (!feeds.Select(f => f.Name).SequenceEqual(FeedNames)) throw new InvalidOperationException("Feed names and status dots differ.");

        var resolver = countries;
        var main = new MainViewModel(store, settings, iso => resolver.ByIso3(iso)?.Name, dispatcher, () => clock.UtcNow, boundaries);
        var scheduler = new FeedScheduler(feeds, clock, log, AppInfo.FeedIdleNote);
        foreach (var status in scheduler.Statuses) main.Status.Update(status);
        scheduler.StatusChanged += (_, status) => main.Status.Update(status);   // feed threads: Update uses BeginInvoke
        return new AppServices(store, cache, countries, boundaries, feeds, scheduler, main, cached is not null, http, log);
    }

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

/// <summary>Saves the store to the cache a short while after it changes (changes in a burst save once), and on demand.</summary>
public sealed class CrisisCacheWriter : IDisposable
{
    public static readonly TimeSpan Delay = TimeSpan.FromSeconds(2);

    private readonly CrisisStore _store;
    private readonly CrisisCache _cache;
    private readonly IAppLog _log;
    private readonly Timer _timer;
    private readonly object _gate = new();
    private readonly object _saveGate = new();   // the timer's save and SaveNow never write the file at once
    private bool _dirty, _disposed;

    public CrisisCacheWriter(CrisisStore store, CrisisCache cache, IAppLog log)
    {
        _store = store;
        _cache = cache;
        _log = log;
        _timer = new Timer(_ => SaveIfDirty());
        _store.Changed += OnChanged;
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
            Directory.CreateDirectory(Path.GetDirectoryName(_cache.FilePath)!);
            _cache.Save(_store.Snapshot);   // logs its own failures
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
            _store.Changed -= OnChanged;
        }
        _timer.Dispose();
    }
}
