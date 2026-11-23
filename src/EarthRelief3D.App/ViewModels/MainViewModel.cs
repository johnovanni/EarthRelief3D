using System.Windows.Threading;
using EarthRelief3D.App.Layers;
using EarthRelief3D.App.Services;
using EarthRelief3D.App.Settings;
using EarthRelief3D.Core.Store;
using GlobeCore.Regions;
using GlobeCore.Storage;
using GlobeCore.Wpf.Status;

namespace EarthRelief3D.App.ViewModels;

/// <summary>The window's view models (the layer strip, the legend, the country panel and the status dots) and the Plan 3 data stores they share with the scene.</summary>
public sealed class MainViewModel
{
    /// <param name="countryName">The country's name for an ISO3 code.</param>
    /// <param name="nowUtc">The clock for date wording (the system clock by default).</param>
    /// <param name="boundaries">The live boundary set: the legend names its edition and rules, and follows a swap.</param>
    /// <param name="dispatcher">The UI thread's: store and feed-status changes from other threads are queued onto it (null in tests: at once).</param>
    /// <param name="indicators">The country-value series (Plan 3 shadings); a new empty store when not given.</param>
    /// <param name="markerData">The point sets of the list-drawn marker layers; a new empty store when not given.</param>
    public MainViewModel(CrisisStore store, ISettingsStore<AppSettings> settings, Func<string, string?> countryName, Dispatcher? dispatcher,
        Func<DateTime>? nowUtc = null, CurrentBoundaries? boundaries = null, IndicatorStore? indicators = null, MarkerDataStore? markerData = null)
    {
        ArgumentNullException.ThrowIfNull(store);
        Indicators = indicators ?? new IndicatorStore();
        MarkerData = markerData ?? new MarkerDataStore();
        Layers = new LayerStripViewModel(settings);
        Legend = new LegendViewModel(store, Layers, dispatcher, nowUtc, boundaries, Indicators, MarkerData);
        Panel = new CountryPanelViewModel(store, countryName, dispatcher, nowUtc);
        Status = new StatusDotsViewModel(AppServices.FeedNames, dispatcher);
    }

    /// <summary>The country-value series the Plan 3 shadings draw (ruling R2: a missing series says "Data not loaded yet").</summary>
    public IndicatorStore Indicators { get; }

    /// <summary>The points of the marker layers drawn from lists (camps, power plants, oil and gas fields, steel plants).</summary>
    public MarkerDataStore MarkerData { get; }

    public LayerStripViewModel Layers { get; }
    public LegendViewModel Legend { get; }
    public CountryPanelViewModel Panel { get; }
    public StatusDotsViewModel Status { get; }
}
