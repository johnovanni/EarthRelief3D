using System.ComponentModel;
using System.Windows.Media;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using EarthRelief3D.App.Curated;
using EarthRelief3D.App.Layers;
using EarthRelief3D.App.Presentation;
using EarthRelief3D.Core.Store;
using GlobeCore.Regions;

namespace EarthRelief3D.App.ViewModels;

/// <summary>One colour key under a legend line ("Phase 4 Emergency", "last 3 months").</summary>
/// <param name="IsOutline">Drawn as an outline sample (a ring), not a filled swatch: the food layer's outline (R17).</param>
/// <param name="IsFaint">Drawn as a ring around a faint fill: the food layer's faint tint and outline (round 2).</param>
/// <param name="IsNone">Drawn as an empty box with a thin grey edge: "no figure: not shaded" under a country-value shading.</param>
/// <param name="Swatch">The swatch's width and height in pixels (a size class's key is about the marker's size, Task 7).</param>
/// <param name="IsRound">Drawn as a circle (the power plants' discs, Task 7).</param>
public sealed record LegendKey(string Label, Color Color, bool IsOutline = false, bool IsFaint = false, bool IsNone = false,
    double Swatch = 12, bool IsRound = false)
{
    /// <summary>For the view: one shared frozen brush per colour (computed, so it takes no part in the record's equality).</summary>
    public SolidColorBrush Brush => LayerStyle.Frozen(Color);

    /// <summary>The swatch's corner radius: a circle for round keys (the power plants' discs), else slightly rounded.</summary>
    public double Radius => IsRound ? Swatch / 2 : 2;

    /// <summary>The faint tint's brush: the colour at the map's faint opacity (computed, not part of equality).</summary>
    public SolidColorBrush FaintBrush => LayerStyle.Frozen(Color.FromArgb((byte)Layers.FoodImageLayer.FaintAlpha, Color.R, Color.G, Color.B));
}

/// <summary>One visible layer: its symbol, what it measures, the source and the as-of date.</summary>
/// <param name="Note">A short note under the keys (food: Phase 5 shading is not a famine classification); null for none.</param>
public sealed record LegendLine(CrisisLayer Layer, string Symbol, Color Color, string Measures, string Source, string AsOf,
    IReadOnlyList<LegendKey> Keys, string? Note = null)
{
    public bool HasNote => Note is not null;

    /// <summary>For the view: one shared frozen brush per colour (computed, so it takes no part in the record's equality).</summary>
    public SolidColorBrush Brush => LayerStyle.Frozen(Color);

    /// <summary>"&lt;publisher&gt;, &lt;dataset&gt; · &lt;as of&gt;", the line under the measure.</summary>
    public string SourceLine => $"{Source} · {AsOf}";
}

/// <summary>
/// One visible Plan 3 layer (a country-value shading, or a marker layer the crisis lines do not cover): its symbol, what it
/// measures (with the year), the source and the as-of date, or "Data not loaded yet" (ruling R2).
/// </summary>
/// <param name="Id">The layer's name ("water", "power").</param>
public sealed record LegendEntry(string Id, string Symbol, Color Color, string Measures, string Source, string AsOf,
    IReadOnlyList<LegendKey> Keys, string? Note = null)
{
    public bool HasNote => Note is not null;

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
    /// <summary>The food layer's outline key (R17), drawn as a ring in Phase 4's colour.</summary>
    public const string OutlineKeyLabel = "Outline: people in a worse phase";
    /// <summary>The food layer's faint tint key (owner's decision, round 2), drawn as a faint fill in a ring of Phase 4's colour.</summary>
    public const string FaintKeyLabel = "Faint tint and outline: some people are in this phase, but no phase covers 20%";
    /// <summary>Under the food keys (R17): the shading is not IPC's famine classification.</summary>
    public const string FamineNote = CrisisText.FamineNote;
    /// <summary>The food layer's rule (R17), one wording for the legend line and the layer strip's tooltip.</summary>
    public const string FoodRule = "the country is shaded by the worst IPC phase that covers at least 20% of the people analysed "
        + "(IPC's 20% rule, applied to the whole country). An outline in a worse phase's colour means some people are in that phase; "
        + "a faint tint with an outline alone means some people are in that phase, but no phase covers 20%.";
    /// <summary>Which countries the rule shades (P3-2): where IPC publishes areas, they are drawn instead. One wording for the legend and the strip.</summary>
    public const string FoodAreasRule = "Where IPC publishes areas, each area is shaded in IPC's own area classification; "
        + "elsewhere the country is shaded by the 20% rule";
    /// <summary>The food layer's whole description after a colon: the areas (lower-case "where"), then the rule.</summary>
    private static readonly string FoodDescription = char.ToLowerInvariant(FoodAreasRule[0]) + FoodAreasRule[1..] + ". By the 20% rule, " + FoodRule;
    /// <summary>The layer strip's tooltip for the food toggle.</summary>
    public static readonly string FoodTooltip = "Food crisis (IPC): " + FoodDescription;
    /// <summary>The last key under a country-value shading (P3-7): a country with no figure is not filled.</summary>
    public const string NoFigureKeyLabel = "no figure: not shaded";
    /// <summary>A layer whose data is not loaded yet (ruling R2).</summary>
    public const string NotLoaded = LayerCatalog.NotLoaded;

    private readonly CrisisStore _store;
    private readonly IndicatorStore? _indicators;
    private readonly MarkerDataStore? _markerData;
    private readonly CuratedStore? _curated;
    private readonly LayerStripViewModel _layers;
    private readonly Dispatcher? _dispatcher;
    private readonly Func<DateTime> _nowUtc;
    private int _refreshPending;

    [ObservableProperty] private IReadOnlyList<LegendLine> _lines = [];
    /// <summary>The visible Plan 3 layers, after the crisis lines: atrocities and camps, the shading (unless food), the resource markers.</summary>
    [ObservableProperty] private IReadOnlyList<LegendEntry> _entries = [];
    [ObservableProperty] private IReadOnlyList<UnplacedLine> _unplaced = [];
    [ObservableProperty] private string _unplacedHeader = UnplacedTitle + " (0)";
    [ObservableProperty] private bool _hasUnplaced;
    [ObservableProperty] private string _boundaryText = BoundaryNote;

    /// <param name="dispatcher">The UI thread's; store changes from other threads are queued onto it. Null: handled at once (tests).</param>
    /// <param name="nowUtc">The clock (a future as-of date is not shown as "as of"); the system clock by default.</param>
    /// <param name="indicators">The country-value series the shadings draw (none: every shading says "Data not loaded yet").</param>
    /// <param name="markerData">The point sets of the marker layers drawn from lists (none: those say "Data not loaded yet").</param>
    /// <param name="curated">The curated designations as the user sees them (the atrocities line); null: that line says "Data not loaded yet".</param>
    public LegendViewModel(CrisisStore store, LayerStripViewModel layers, Dispatcher? dispatcher = null, Func<DateTime>? nowUtc = null,
        CurrentBoundaries? boundaries = null, IndicatorStore? indicators = null, MarkerDataStore? markerData = null, CuratedStore? curated = null)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _layers = layers ?? throw new ArgumentNullException(nameof(layers));
        _dispatcher = dispatcher;
        _nowUtc = nowUtc ?? (() => DateTime.UtcNow);
        _indicators = indicators;
        _markerData = markerData;
        _curated = curated;
        _store.Changed += OnStoreChanged;
        if (_indicators is not null) _indicators.Changed += OnStoreChanged;
        if (_markerData is not null) _markerData.Changed += OnMarkerDataChanged;
        if (_curated is not null) _curated.Changed += OnStoreChanged;
        _layers.PropertyChanged += OnLayersChanged;
        // The set in use at start; after a swap the scene calls ShowBoundaries once the new borders are drawn.
        if (boundaries is not null) BoundaryText = NoteFor(boundaries.Set);
        Refresh();
    }

    public void Refresh()
    {
        var now = _nowUtc();
        var s = _store.Snapshot.AsSeenAt(now);   // WHO's 90 days and IPC's 12 months judged now, even for cached data
        var lines = new List<LegendLine>();
        foreach (var summary in SourceDates.All(s, now))
        {
            if (!_layers.IsShown(summary.Layer)) continue;
            lines.Add(new LegendLine(summary.Layer, LayerStyle.Symbol(summary.Layer), LayerStyle.SymbolColor(summary.Layer),
                Measures(summary.Layer), summary.Source, summary.LegendDate, Keys(summary.Layer),
                summary.Layer == CrisisLayer.Food ? FamineNote : null));
        }
        Lines = lines;
        Entries = BuildEntries(now);
        Unplaced = [.. s.Unplaced.OrderByDescending(u => u.Citation.AsOfUtc)
            .Select(u => new UnplacedLine(u.Title, $"{u.Source} · {u.Reason} · {CrisisText.DataDate(u.Citation, now)}"))];
        UnplacedHeader = $"{UnplacedTitle} ({Unplaced.Count})";
        HasUnplaced = Unplaced.Count > 0;
    }

    /// <summary>
    /// The note under the legend (B-6): the edition of the boundaries in use and the rules applied to them, then the UN
    /// sentence, e.g. "Borders: Natural Earth 5.1.2 (2022-05-13); Crimea shown as part of Ukraine (...). The boundaries ...".
    /// </summary>
    public static string NoteFor(BoundarySet set) => $"Borders: {set.Describe()}. {BoundaryNote}";

    /// <summary>
    /// Names <paramref name="set"/> in the note: called by the scene once the borders on the globe are drawn from it, so
    /// the legend and the lines never name different editions (a failed redraw keeps both as they were). Any thread:
    /// the text is computed here and set on the UI thread.
    /// </summary>
    public void ShowBoundaries(BoundarySet set)
    {
        ArgumentNullException.ThrowIfNull(set);
        var text = NoteFor(set);
        if (_dispatcher is null || _dispatcher.CheckAccess()) BoundaryText = text;
        else _dispatcher.BeginInvoke(() => BoundaryText = text);
    }

    public static string Measures(CrisisLayer layer) => layer switch
    {
        CrisisLayer.Conflict => "Armed conflict: UCDP events of conflicts with 25 or more deaths in a calendar year; dot size by the band of that conflict's deaths in the country over the data window, colour by the event's age",
        CrisisLayer.Food => "Food crisis: " + FoodDescription,
        CrisisLayer.Outbreaks => "Disease outbreaks: WHO Outbreak News reports in the last 90 days, at the country",
        CrisisLayer.Displacement => "People forced from home, by country of origin (refugees, asylum-seekers and internally displaced); ring area grows with the number",
        _ => throw new ArgumentOutOfRangeException(nameof(layer)),
    };

    private static IReadOnlyList<LegendKey> Keys(CrisisLayer layer) => layer switch
    {
        CrisisLayer.Conflict => [.. LayerStyle.ConflictAgeColors.Select((c, i) => new LegendKey(LayerStyle.ConflictAgeLabels[i], c))],
        CrisisLayer.Food =>
        [
            .. Enumerable.Range(3, 3).Select(p => new LegendKey(LayerStyle.IpcPhaseLabel(p), LayerStyle.IpcPhaseColor(p))),
            new LegendKey(OutlineKeyLabel, LayerStyle.IpcPhaseColor(4), IsOutline: true),
            new LegendKey(FaintKeyLabel, LayerStyle.IpcPhaseColor(4), IsFaint: true),
        ],
        _ => [],
    };

    private void OnLayersChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (LayerStripViewModel.IsLayerProperty(e.PropertyName)) Refresh();
    }

    // ---------------------------------------------------------------- Plan 3 entries

    private List<LegendEntry> BuildEntries(DateTime now)
    {
        var entries = new List<LegendEntry>();
        foreach (var m in LayerCatalog.CrisisMarkersAdded)
            if (_layers.IsShown(m)) entries.Add(m == MarkerLayer.Atrocities && _curated is not null ? AtrocityEntry(_curated, now) : MarkerEntry(m, now));
        if (_layers.Shading is not ShadingLayer.None and not ShadingLayer.Food) entries.Add(ShadingEntry(_layers.Shading, now));
        foreach (var m in LayerCatalog.ResourceMarkers)
            if (_layers.IsShown(m)) entries.Add(MarkerEntry(m, now));
        return entries;
    }

    /// <summary>
    /// A shading's entry: "&lt;measures&gt;, &lt;year&gt;", one key per class and "no figure: not shaded", and the newest
    /// citation's source and as-of date; or, with no series yet, the layer's own wording and "Data not loaded yet".
    /// </summary>
    private LegendEntry ShadingEntry(ShadingLayer layer, DateTime now)
    {
        string id = LayerCatalog.Id(layer), symbol = LayerStyle.Symbol(layer);
        var color = LayerStyle.SymbolColor(layer);
        if (_indicators?.Snapshot.Get(LayerCatalog.SeriesId(layer)) is not { } series)
            return new LegendEntry(id, symbol, color, LayerCatalog.DefaultMeasures(layer), LayerCatalog.SourceName(layer), NotLoaded, []);
        var scale = ShadingScales.For(layer);
        // Each country's newest figure, as the layer shows it (IndicatorSeries.For), and only figures it shows.
        var values = series.Values.Select(v => v.Iso3.ToUpperInvariant()).Distinct()
            .Select(iso => series.For(iso)!).Where(v => CountryValueLayer.HasFigure(v, scale)).ToList();
        var newest = values.Select(v => v.Citation).MaxBy(c => c.AsOfUtc);
        // 3-year averages (FAO) are shown as ranges, never as their last year (fix round 1).
        bool averages = values.Count > 0 && values.All(v => v.Citation.DateMeaning == Core.Model.CitationDate.ThreeYearAverage);
        // A modelled baseline (WRI) names its period in the measure and its date on the source line: no year appended (I3).
        bool baseline = values.Count > 0 && values.All(v => v.Citation.DateMeaning == Core.Model.CitationDate.Baseline);
        var measures = values.Count == 0 || baseline ? series.Measures
            : $"{series.Measures}, {(averages ? AverageText(values.Select(v => v.Year)) : YearText(values.Select(v => v.Year)))}";
        // Ruling R17b: the grey arid key only when some country in the data is in that category.
        IReadOnlyList<LegendKey> keys =
        [
            .. scale.ClassLabels.Select((label, i) => (label, i))
                .Where(x => scale.Colors[x.i] != ShadingScales.AridColor || values.Any(v => scale.ClassOf(v.Value) == x.i))
                .Select(x => new LegendKey(x.label, scale.Colors[x.i])),
            new LegendKey(NoFigureKeyLabel, NoFigureColor, IsNone: true),
        ];
        var note = layer == ShadingLayer.WaterStress ? WaterStressNote
            : averages ? AverageNote(values.Min(v => v.Year), values.Max(v => v.Year)) : null;
        return newest is null
            ? new LegendEntry(id, symbol, color, measures, LayerCatalog.SourceName(layer), "no figures in the data", keys, note)
            : new LegendEntry(id, symbol, color, measures, $"{newest.Publisher}, {newest.Dataset}", CrisisText.SeriesDate(newest, now), keys, note);
    }

    /// <summary>"2023–2025", or "2017–2019 to 2023–2025 (each country's latest average)" when the countries' latest averages differ.</summary>
    public static string AverageText(IEnumerable<int> lastYears)
    {
        var list = lastYears.ToList();
        int min = list.Min(), max = list.Max();
        return min == max ? CrisisText.AverageRange(max) : $"{CrisisText.AverageRange(min)} to {CrisisText.AverageRange(max)} (each country's latest average)";
    }

    /// <summary>The note under a legend of 3-year averages: some countries' latest is older; the hover shows each one's range.</summary>
    public static string AverageNote(int oldestLastYear, int newestLastYear) => oldestLastYear == newestLastYear
        ? "These figures are 3-year averages; hover over a country to see its range."
        : $"These figures are 3-year averages. Some countries' latest average is older (the oldest ends in {oldestLastYear}); "
          + "hover over a country to see its own range.";

    /// <summary>
    /// The water-stress legend's note: the cautions of the Aqueduct 4.0 workbook's Read Me (docs/data-sources.md §10).
    /// </summary>
    public const string WaterStressNote = "WRI's indicators are modelled: they cannot be measured directly and have not been validated, "
        + "and water moved between river basins is not modelled.";

    /// <summary>The edge of the "no figure" key.</summary>
    public static Color NoFigureColor { get; } = Color.FromRgb(0x9A, 0xA3, 0xB5);

    /// <summary>"2024", or "2019–2024 (each country's latest year)" when the countries' latest years differ.</summary>
    public static string YearText(IEnumerable<int> years)
    {
        var list = years.ToList();
        int min = list.Min(), max = list.Max();
        return min == max ? max.ToString(System.Globalization.CultureInfo.InvariantCulture) : $"{min}–{max} (each country's latest year)";
    }

    /// <summary>A Plan 3 marker layer's entry: what it shows, and its source and as-of date, or "Data not loaded yet".</summary>
    private LegendEntry MarkerEntry(MarkerLayer layer, DateTime now)
    {
        string id = LayerCatalog.Id(layer), symbol = LayerStyle.Symbol(layer), measures = LayerCatalog.Measures(layer);
        var color = LayerStyle.SymbolColor(layer);
        if (_markerData?.Get(layer) is not { } set)
            return new LegendEntry(id, symbol, color, measures, LayerCatalog.SourceName(layer), NotLoaded, []);
        var c = set.Citation;
        return new LegendEntry(id, symbol, color, set.Measures ?? measures, $"{c.Publisher}, {c.Dataset}", CrisisText.DataDate(c, now),
            ResourceKeys(layer, set), set.Filter);
    }

    /// <summary>The swatch for a size class: about the marker's own diameter (its radius in pixels, doubled).</summary>
    private static double SwatchFor(double sizePx) => Math.Round(2 * sizePx + 1);

    /// <summary>
    /// A GEM layer's keys (Task 7): for power plants one round key per fuel group, then the size classes as grey discs; for
    /// renewable plants (R25) the same, with only the fuel groups present in the layer; for steel plants the size classes as
    /// square outlines in the layer's colour; none for oil and gas fields (one size).
    /// </summary>
    private static IReadOnlyList<LegendKey> ResourceKeys(MarkerLayer layer, PointSet set)
    {
        if (!LayerCatalog.ResourceMarkers.Contains(layer)) return [];
        var sizes = ResourceMarkers.SizeClasses(layer);
        var colours = set.Items.Select(i => i.Color).ToHashSet();
        return layer switch
        {
            MarkerLayer.PowerPlants or MarkerLayer.Renewables =>
            [
                .. Enum.GetValues<FuelGroup>().Where(g => layer == MarkerLayer.PowerPlants || colours.Contains(ResourceMarkers.FuelGroupColor(g)))
                    .Select(g => new LegendKey(ResourceMarkers.FuelGroupLabel(g), ResourceMarkers.FuelGroupColor(g), IsRound: true)),
                .. sizes.Select(s => new LegendKey(s.Label, SizeKeyColor, Swatch: SwatchFor(s.SizePx), IsRound: true)),
            ],
            MarkerLayer.SteelPlants => [.. sizes.Select(s => new LegendKey(s.Label, LayerStyle.SteelColor, IsOutline: true, Swatch: SwatchFor(s.SizePx)))],
            _ => [],
        };
    }

    /// <summary>The power plants' size keys: a neutral light grey, as size says nothing about fuel.</summary>
    public static Color SizeKeyColor { get; } = Color.FromRgb(0xD5, 0xDC, 0xE8);

    /// <summary>The atrocities line's note when the user sees no designation at all (P3-4).</summary>
    public const string NoneApprovedNote = "None approved yet: each entry is shown after the owner's review";

    /// <summary>
    /// The atrocities line (C14): what a triangle means, how many countries have one, the date the tables were checked, and
    /// in a <c>--show-drafts</c> run that drafts are shown.
    /// </summary>
    private static LegendEntry AtrocityEntry(CuratedStore curated, DateTime now)
    {
        const MarkerLayer layer = MarkerLayer.Atrocities;
        string id = LayerCatalog.Id(layer), symbol = LayerStyle.Symbol(layer), measures = LayerCatalog.Measures(layer);
        var color = LayerStyle.SymbolColor(layer);
        var all = curated.Tables.Designations;
        if (all.Count == 0) return new LegendEntry(id, symbol, color, measures, LayerCatalog.SourceName(layer), NotLoaded, []);
        var asOf = "checked " + Core.Curated.DesignationText.Date(all.Max(d => d.CheckedOn));
        var visible = curated.Visible;
        if (visible.Count == 0) return new LegendEntry(id, symbol, color, measures, LayerCatalog.SourceName(layer), asOf, [], NoneApprovedNote);
        int countries = AtrocityLayer.CurrentByCountry(curated, now).Count;
        var note = $"A triangle: a court judgment, UN body finding or government determination, and a statement from the last 12 months "
                   + $"that the situation continues ({countries} {(countries == 1 ? "country" : "countries")}). Provisional measures, warrants and "
                   + "pending cases are listed with it but never draw a triangle alone. Past ones are in the country panel's history.";
        if (visible.Any(d => !d.Review.IsApproved)) note += " " + Curated.CampMarkers.DraftsNote + ".";
        return new LegendEntry(id, symbol, color, measures, LayerCatalog.SourceName(layer), asOf, [], note);
    }

    private void OnMarkerDataChanged(object? sender, MarkerLayer layer) => OnStoreChanged(sender, EventArgs.Empty);

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
