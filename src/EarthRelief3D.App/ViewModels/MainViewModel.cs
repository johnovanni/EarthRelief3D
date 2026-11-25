using System.Windows.Threading;
using CommunityToolkit.Mvvm.Input;
using AstroCore.Earth;
using EarthRelief3D.App.Curated;
using EarthRelief3D.App.Layers;
using EarthRelief3D.App.Presentation;
using EarthRelief3D.App.Services;
using EarthRelief3D.App.Settings;
using EarthRelief3D.Core.Help;
using EarthRelief3D.Core.Store;
using GlobeCore.Regions;
using GlobeCore.Storage;
using EarthRelief3D.Core.Model;
using EarthRelief3D.Core.Sources;
using GlobeCore.Wpf.Services;
using GlobeCore.Wpf.Status;

namespace EarthRelief3D.App.ViewModels;

/// <summary>
/// The window's view models (the layer strip with its Settings flyout, the legend, the country panel and the status dots)
/// and the Plan 3 data stores they share with the scene.
/// </summary>
public sealed class MainViewModel
{
    /// <param name="countryName">The country's name for an ISO3 code.</param>
    /// <param name="nowUtc">The clock for date wording (the system clock by default).</param>
    /// <param name="boundaries">The live boundary set: the legend names its edition and rules, and follows a swap.</param>
    /// <param name="dispatcher">The UI thread's: store and feed-status changes from other threads are queued onto it (null in tests: at once).</param>
    /// <param name="indicators">The country-value series (Plan 3 shadings); a new empty store when not given.</param>
    /// <param name="markerData">The point sets of the list-drawn marker layers; a new empty store when not given.</param>
    /// <param name="curated">The curated designations and camp sites (Plan 3); an empty store when not given.</param>
    /// <param name="logistics">The Logistics Cluster's country pages (Plan 4); none when not given.</param>
    /// <param name="crisisPlace">The place a country's crisis is taken to be, for the nearest hubs (P4-6): its label point; none when not given.</param>
    /// <param name="help">The help catalog behind "Your one small step" and the Settings country and state lists (Plan 4); none when not given.</param>
    /// <param name="registry">The list of data sources the Data card shows (Plan 5); empty when not given (the card says it could not be read).</param>
    /// <param name="datasets">The newest citation of each bundled extract that loaded, by its folder under <c>data\datasets</c> (the Data card's dates).</param>
    /// <param name="links">Opens the Data and About cards' links in the user's browser; the real shell when not given.</param>
    /// <param name="dataFolder">The folder that holds the app's settings, cache and logs (the About card names it and opens it).</param>
    public MainViewModel(CrisisStore store, ISettingsStore<AppSettings> settings, Func<string, string?> countryName, Dispatcher? dispatcher,
        Func<DateTime>? nowUtc = null, CurrentBoundaries? boundaries = null, IndicatorStore? indicators = null, MarkerDataStore? markerData = null,
        CuratedStore? curated = null, LogisticsClusterLinks? logistics = null, Func<string, GeoPosition?>? crisisPlace = null, HelpTables? help = null,
        SourceRegistry? registry = null, IReadOnlyDictionary<string, SourceCitation>? datasets = null, ILinkOpener? links = null, string? dataFolder = null)
    {
        ArgumentNullException.ThrowIfNull(store);
        Indicators = indicators ?? new IndicatorStore();
        MarkerData = markerData ?? new MarkerDataStore();
        Curated = curated ?? CuratedStore.Empty();
        Layers = new LayerStripViewModel(settings);
        // The Settings flyout applies the saved choice of governments to the curated store before anything reads it.
        Settings = new SettingsViewModel(settings, Curated, help);
        Layers.Settings = Settings;
        Hubs = new HubStore(Curated);
        Legend = new LegendViewModel(store, Layers, dispatcher, nowUtc, boundaries, Indicators, MarkerData, Curated, Hubs, settings);
        Panel = new CountryPanelViewModel(store, countryName, dispatcher, nowUtc, Indicators, Curated, logistics,
            Hubs, crisisPlace, () => Settings.Units, help, () => Settings.UserState);
        // A change of unit writes the open panel again at once (the scene redraws its arc labels, too).
        Settings.UnitsChanged += (_, _) => Panel.RefreshOpen();
        // Choosing a state writes the open panel's senators again at once.
        Settings.StateChanged += (_, _) => Panel.RefreshOpen();
        Status = new StatusDotsViewModel(AppServices.StatusDotNames, dispatcher);
        // Plan 5: the cards over the globe. The Data card lists every source with its date, read again when a store changes while it is open.
        Registry = registry ?? new SourceRegistry([]);
        var datasetCitations = datasets ?? new Dictionary<string, SourceCitation>();
        var helpTables = help ?? HelpTables.Empty;
        Cards = new CardHost();
        var opener = links ?? new ShellLinkOpener();
        Data = new DataViewModel(Registry, () => new SourceAsOfInputs(store.Snapshot, Indicators.Snapshot, datasetCitations, Curated, helpTables, boundaries?.Set),
            nowUtc ?? (() => DateTime.UtcNow), opener);
        Cards.Register(AppCard.Data, Data.Refresh);
        Data.CloseRequested += (_, _) => { if (Cards.Open == AppCard.Data) Cards.CloseTop(); };
        // The Welcome card (Task 6): first start, or "Show the welcome again" in About.
        Welcome = new WelcomeViewModel(settings, Settings, Cards);
        Cards.Register(AppCard.Welcome);
        // The About card (Task 5): the strip's "ⓘ About" opens it, or closes it when it is open.
        About = new AboutViewModel(Registry, opener, dataFolder ?? "", Cards);
        Cards.Register(AppCard.About);
        About.CloseRequested += (_, _) => { if (Cards.Open == AppCard.About) Cards.CloseTop(); };
        // The update banner (Plan 5 Task 10): at the top centre, not a card. The update service drives it in a normal run; a picture
        // run shows it from a scene's "update" field.
        Updates = new UpdateBannerViewModel(opener, AppInfo.VersionNumber);
        Layers.AboutCommand = new RelayCommand(() =>
        {
            if (Cards.Open == AppCard.About) Cards.CloseTop(); else Cards.Show(AppCard.About);
        });
        void RefreshDataCard()
        {
            if (Cards.Open == AppCard.Data) Data.Refresh();
        }
        int refreshPending = 0;
        void OnDataChanged(object? sender, EventArgs e)
        {
            // Raised on the feeds' threads: only a flag crosses, and the stores are read on the UI thread.
            if (dispatcher is null || dispatcher.CheckAccess()) { RefreshDataCard(); return; }
            if (Interlocked.Exchange(ref refreshPending, 1) == 1) return;
            dispatcher.BeginInvoke(() => { Volatile.Write(ref refreshPending, 0); RefreshDataCard(); });
        }
        store.Changed += OnDataChanged;
        Indicators.Changed += OnDataChanged;
        Curated.Changed += OnDataChanged;
        if (boundaries is not null) boundaries.Changed += (_, _) => OnDataChanged(null, EventArgs.Empty);
        // The prison camps layer draws from the marker store: its points follow the curated store (no tables: not loaded, R2).
        PublishCurated();
        Curated.Changed += (_, _) => PublishCurated();
    }

    // The prison camps and the UN depots (Plan 4) are drawn from the curated store: their points follow it.
    private void PublishCurated()
    {
        if (CampMarkers.For(Curated) is { } camps) MarkerData.Replace(MarkerLayer.Camps, camps);
        if (HubMarkers.ForDepots(Curated) is { } depots) MarkerData.Replace(MarkerLayer.UnDepots, depots);
    }

    /// <summary>The country-value series the Plan 3 shadings draw (ruling R2: a missing series says "Data not loaded yet").</summary>
    public IndicatorStore Indicators { get; }

    /// <summary>The points of the marker layers drawn from lists (camps, power plants, oil and gas fields, steel plants).</summary>
    public MarkerDataStore MarkerData { get; }

    /// <summary>The curated designations and camp sites, as the user sees them (drafts, chosen governments).</summary>
    public CuratedStore Curated { get; }

    /// <summary>The UN depots, airports and ports the nearest-hub rows and the arcs measure to (Plan 4); the extracts are set once loaded.</summary>
    public HubStore Hubs { get; }

    /// <summary>The list of data sources (<c>data\sources.json</c>) the Data card and the About card show.</summary>
    public SourceRegistry Registry { get; }

    /// <summary>Which card is open over the globe: Welcome, About or Data, one at a time; Esc closes it first.</summary>
    public CardHost Cards { get; }

    /// <summary>The Data card (Plan 5 Task 4): every source with its date, licence and link.</summary>
    public DataViewModel Data { get; }

    /// <summary>The Welcome card (Plan 5 Task 6): shown on the first start, and again from About.</summary>
    public WelcomeViewModel Welcome { get; }

    /// <summary>The About card (Plan 5 Task 5): version, licences, credits, severity bands and the data folder.</summary>
    public AboutViewModel About { get; }

    /// <summary>The update banner (Plan 5 Task 10, P5-19): a newer version's offer, its download and install, or the 30-day note.</summary>
    public UpdateBannerViewModel Updates { get; }

    public LayerStripViewModel Layers { get; }
    public SettingsViewModel Settings { get; }
    public LegendViewModel Legend { get; }
    public CountryPanelViewModel Panel { get; }

    /// <summary>The explainer card (Plan 4 Task 9), opened from the panel's links; to the left of the panel.</summary>
    public ExplainerViewModel Explainer => Panel.Explainer;
    public StatusDotsViewModel Status { get; }
}
