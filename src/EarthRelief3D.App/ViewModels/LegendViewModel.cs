using System.ComponentModel;
using System.Windows.Media;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using EarthRelief3D.App.Presentation;
using EarthRelief3D.Core.Store;

namespace EarthRelief3D.App.ViewModels;

/// <summary>One colour key under a legend line ("Phase 4 Emergency", "last 3 months").</summary>
public sealed record LegendKey(string Label, Color Color)
{
    /// <summary>For the view: one shared frozen brush per colour (computed, so it takes no part in the record's equality).</summary>
    public SolidColorBrush Brush => LayerStyle.Frozen(Color);
}

/// <summary>One visible layer: its symbol, what it measures, the source and the as-of date.</summary>
public sealed record LegendLine(CrisisLayer Layer, string Symbol, Color Color, string Measures, string Source, string AsOf,
    IReadOnlyList<LegendKey> Keys)
{
    /// <summary>For the view: one shared frozen brush per colour (computed, so it takes no part in the record's equality).</summary>
    public SolidColorBrush Brush => LayerStyle.Frozen(Color);

    /// <summary>"&lt;publisher&gt;, &lt;dataset&gt; · &lt;as of&gt;", the line under the measure.</summary>
    public string SourceLine => $"{Source} · {AsOf}";
}

/// <summary>A report the source published that could not be put on the map.</summary>
public sealed record UnplacedLine(string Title, string Detail);

/// <summary>
/// The legend: one line per visible layer, in strip order, each with the source and the newest as-of date among that
/// layer's citations (for food the newest IPC analysis date, never a future period end), and the flyout's "Not placed on
/// the map" list. Follows the store (whose Changed comes on feed threads: marshalled to the dispatcher here) and the strip.
/// </summary>
public sealed partial class LegendViewModel : ObservableObject
{
    /// <summary>A source that has never run successfully (ruling R12).</summary>
    public const string NotChecked = SourceDates.NotChecked;
    public const string UnplacedTitle = "Not placed on the map";
    /// <summary>The standard UN note (spec Q3, §11), shown under the legend.</summary>
    public const string BoundaryNote = "The boundaries and names shown on this map do not imply official endorsement or acceptance by the United Nations.";

    private readonly CrisisStore _store;
    private readonly LayerStripViewModel _layers;
    private readonly Dispatcher? _dispatcher;
    private readonly Func<DateTime> _nowUtc;
    private int _refreshPending;

    [ObservableProperty] private IReadOnlyList<LegendLine> _lines = [];
    [ObservableProperty] private IReadOnlyList<UnplacedLine> _unplaced = [];
    [ObservableProperty] private string _unplacedHeader = UnplacedTitle + " (0)";
    [ObservableProperty] private bool _hasUnplaced;

    /// <param name="dispatcher">The UI thread's; store changes from other threads are queued onto it. Null: handled at once (tests).</param>
    /// <param name="nowUtc">The clock (a future as-of date is not shown as "as of"); the system clock by default.</param>
    public LegendViewModel(CrisisStore store, LayerStripViewModel layers, Dispatcher? dispatcher = null, Func<DateTime>? nowUtc = null)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _layers = layers ?? throw new ArgumentNullException(nameof(layers));
        _dispatcher = dispatcher;
        _nowUtc = nowUtc ?? (() => DateTime.UtcNow);
        _store.Changed += OnStoreChanged;
        _layers.PropertyChanged += OnLayersChanged;
        Refresh();
    }

    public void Refresh()
    {
        var s = _store.Snapshot;
        var lines = new List<LegendLine>();
        foreach (var summary in SourceDates.All(s, _nowUtc()))
        {
            if (!_layers.IsShown(summary.Layer)) continue;
            lines.Add(new LegendLine(summary.Layer, LayerStyle.Symbol(summary.Layer), LayerStyle.SymbolColor(summary.Layer),
                Measures(summary.Layer), summary.Source, summary.LegendDate, Keys(summary.Layer)));
        }
        Lines = lines;
        Unplaced = [.. s.Unplaced.OrderByDescending(u => u.Citation.AsOfUtc)
            .Select(u => new UnplacedLine(u.Title, $"{u.Source} · {u.Reason} · {CrisisText.Date(u.Citation.AsOfUtc)}"))];
        UnplacedHeader = $"{UnplacedTitle} ({Unplaced.Count})";
        HasUnplaced = Unplaced.Count > 0;
    }

    public static string Measures(CrisisLayer layer) => layer switch
    {
        CrisisLayer.Conflict => "Armed conflict: UCDP events with 25 or more deaths in a year in their conflict; dot size by the conflict's deaths band, colour by the event's age",
        CrisisLayer.Food => "Food crisis: the highest IPC phase that has people in it (country shading)",
        CrisisLayer.Outbreaks => "Disease outbreaks: WHO Outbreak News reports in the last 90 days, at the country",
        CrisisLayer.Displacement => "People forced from home, by country of origin (refugees, asylum-seekers and internally displaced); ring area grows with the number",
        _ => throw new ArgumentOutOfRangeException(nameof(layer)),
    };

    private static IReadOnlyList<LegendKey> Keys(CrisisLayer layer) => layer switch
    {
        CrisisLayer.Conflict => [.. LayerStyle.ConflictAgeColors.Select((c, i) => new LegendKey(LayerStyle.ConflictAgeLabels[i], c))],
        CrisisLayer.Food => [.. Enumerable.Range(3, 3).Select(p => new LegendKey(LayerStyle.IpcPhaseLabel(p), LayerStyle.IpcPhaseColor(p)))],
        _ => [],
    };

    private void OnLayersChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(LayerStripViewModel.ShowConflict) or nameof(LayerStripViewModel.ShowFood)
            or nameof(LayerStripViewModel.ShowOutbreaks) or nameof(LayerStripViewModel.ShowDisplacement)) Refresh();
    }

    // Raised on the writing (feed) thread: only a flag crosses, and the store is read on the UI thread.
    private void OnStoreChanged(object? sender, EventArgs e)
    {
        if (_dispatcher is null || _dispatcher.CheckAccess()) { Refresh(); return; }
        if (Interlocked.Exchange(ref _refreshPending, 1) == 1) return;
        _dispatcher.BeginInvoke(() =>
        {
            Volatile.Write(ref _refreshPending, 0);
            Refresh();
        });
    }
}
