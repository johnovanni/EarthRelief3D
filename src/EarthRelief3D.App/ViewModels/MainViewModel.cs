using System.Windows.Threading;
using EarthRelief3D.App.Services;
using EarthRelief3D.App.Settings;
using EarthRelief3D.Core.Store;
using GlobeCore.Regions;
using GlobeCore.Storage;
using GlobeCore.Wpf.Status;

namespace EarthRelief3D.App.ViewModels;

/// <summary>The window's view models: the layer strip, the legend, the country panel and the status dots.</summary>
public sealed class MainViewModel
{
    /// <param name="countryName">The country's name for an ISO3 code.</param>
    /// <param name="nowUtc">The clock for date wording (the system clock by default).</param>
    /// <param name="boundaries">The live boundary set: the legend names its edition and rules, and follows a swap.</param>
    /// <param name="dispatcher">The UI thread's: store and feed-status changes from other threads are queued onto it (null in tests: at once).</param>
    public MainViewModel(CrisisStore store, ISettingsStore<AppSettings> settings, Func<string, string?> countryName, Dispatcher? dispatcher,
        Func<DateTime>? nowUtc = null, CurrentBoundaries? boundaries = null)
    {
        ArgumentNullException.ThrowIfNull(store);
        Layers = new LayerStripViewModel(settings);
        Legend = new LegendViewModel(store, Layers, dispatcher, nowUtc, boundaries);
        Panel = new CountryPanelViewModel(store, countryName, dispatcher, nowUtc);
        Status = new StatusDotsViewModel(AppServices.FeedNames, dispatcher);
    }

    public LayerStripViewModel Layers { get; }
    public LegendViewModel Legend { get; }
    public CountryPanelViewModel Panel { get; }
    public StatusDotsViewModel Status { get; }
}
