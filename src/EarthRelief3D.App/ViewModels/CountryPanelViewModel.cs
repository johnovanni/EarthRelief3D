using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EarthRelief3D.App.Layers;
using EarthRelief3D.App.Presentation;
using EarthRelief3D.Core.Feeds;
using EarthRelief3D.Core.Model;
using EarthRelief3D.Core.Store;

namespace EarthRelief3D.App.ViewModels;

/// <summary>One part of the country panel: a heading, label/value rows, the source line and a link to the source.</summary>
/// <param name="SourceLine">"Source: &lt;publisher&gt;, &lt;dataset&gt; · as of &lt;date&gt;" (food: the IPC analysis period instead).</param>
/// <param name="Bar">The IPC phase bar (R17): food sections with current-period figures, current or past; null otherwise.</param>
public sealed record PanelSection(string Heading, IReadOnlyList<(string Label, string Value)> Rows, string SourceLine, Uri? Link,
    PhaseBar? Bar = null);

/// <summary>
/// The country panel (C1: per country, a section per crisis), the "What is happening" part of spec §9.1. Sections come in
/// this order: armed conflict, food crisis, disease outbreaks, people forced from home; a kind with nothing for the country
/// has no section. A country with no crisis says <see cref="NoCrisisText"/> and lists the sources checked, with their
/// dates. While open it follows the store (marshalled to the dispatcher).
/// </summary>
public sealed partial class CountryPanelViewModel : ObservableObject
{
    public const string ConflictHeading = "Armed conflict";
    public const string FoodHeading = "Food crisis";
    /// <summary>An IPC analysis whose period ended more than 12 months ago: listed, not a crisis (P2-4).</summary>
    public const string PastFoodHeading = "Food security: last IPC analysis";
    /// <summary>An IPC analysis with no current-period figures, only a projection: listed, not a crisis and not "no crisis".</summary>
    public const string ProjectionFoodHeading = "Food security: IPC projection only";
    public const string OutbreakHeading = "Disease outbreaks";
    public const string DisplacementHeading = "People forced from home";
    /// <summary>The needs part of the panel (Plan 3): the WHO GHO values, then the UN humanitarian plan's rows.</summary>
    public const string BasicNeedsHeading = "Basic needs";
    public const string NoFigureText = "No figure from WHO GHO";
    public const string NoCrisisText = "No crisis recorded by these sources";
    /// <summary>The headline when the only IPC record is a projection and no other source records a crisis.</summary>
    public const string ProjectionOnlyText = "IPC: projection only, no current-period figures; no crisis recorded by the other sources";

    private static readonly IReadOnlyList<PanelSection> NoSections = [];
    private readonly CrisisStore _store;
    private readonly IndicatorStore? _indicators;
    private readonly Func<string, string?> _nameOf;
    private readonly Dispatcher? _dispatcher;
    private readonly Func<DateTime> _nowUtc;
    private int _refreshPending;

    [ObservableProperty] private bool _isOpen;
    [ObservableProperty] private string _iso3 = "";
    [ObservableProperty] private string _countryName = "";
    [ObservableProperty] private IReadOnlyList<PanelSection> _sections = NoSections;
    [ObservableProperty] private bool _hasNoCrisis;
    /// <summary>The list of sources checked is shown: no crisis, or only an IPC projection (which is not "no crisis").</summary>
    [ObservableProperty] private bool _showsSourcesChecked;
    /// <summary>
    /// The line at the top (ruling R12): <see cref="NoCrisisText"/> when there is no crisis and every source has been
    /// checked (<see cref="ProjectionOnlyText"/> when IPC has only a projection); "Not yet checked: &lt;sources&gt;" while
    /// any source has never run (with or without a crisis); otherwise empty.
    /// </summary>
    [ObservableProperty] private string _headline = "";
    [ObservableProperty] private bool _hasHeadline;
    [ObservableProperty] private IReadOnlyList<string> _sourcesChecked = [];
    /// <summary>The section the panel was opened at (a marker click, C1), or -1.</summary>
    [ObservableProperty] private int _focusedSectionIndex = -1;

    private CrisisLayer? _focus;

    /// <param name="countryName">The country's name for an ISO3 code (null: the code is shown).</param>
    /// <param name="dispatcher">The UI thread's; store changes from other threads are queued onto it. Null: handled at once.</param>
    /// <param name="nowUtc">The clock (a future as-of date is not shown as "as of"); the system clock by default.</param>
    /// <param name="indicators">The country-value series and needs rows (Plan 3); null: no "Basic needs" sections.</param>
    public CountryPanelViewModel(CrisisStore store, Func<string, string?> countryName, Dispatcher? dispatcher = null,
        Func<DateTime>? nowUtc = null, IndicatorStore? indicators = null)
    {
        _nowUtc = nowUtc ?? (() => DateTime.UtcNow);
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _nameOf = countryName ?? throw new ArgumentNullException(nameof(countryName));
        _dispatcher = dispatcher;
        _store.Changed += OnStoreChanged;
        _indicators = indicators;
        if (_indicators is not null) _indicators.Changed += OnStoreChanged;
    }

    /// <summary>Opens the panel for <paramref name="iso3"/>, at the <paramref name="focus"/> layer's section if given.</summary>
    public void Show(string iso3, CrisisLayer? focus = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(iso3);
        _focus = focus;
        Build(iso3.Trim().ToUpperInvariant());
        IsOpen = true;
    }

    [RelayCommand]
    private void Close()
    {
        IsOpen = false;
        Iso3 = "";
        CountryName = "";
        Sections = NoSections;
        SourcesChecked = [];
        HasNoCrisis = false;
        ShowsSourcesChecked = false;
        Headline = "";
        HasHeadline = false;
        FocusedSectionIndex = -1;
        _focus = null;
    }

    private void Build(string iso3)
    {
        var now = _nowUtc();
        var s = _store.Snapshot.AsSeenAt(now);   // WHO's 90 days and IPC's 12 months judged now, even for cached data
        var crises = s.ByCountry(iso3);
        var sections = new List<PanelSection>();
        var focusIndex = -1;
        void Add(PanelSection? section, CrisisLayer layer)
        {
            if (section is null) return;
            if (_focus == layer) focusIndex = sections.Count;
            sections.Add(section);
        }

        var conflicts = crises.OfType<ConflictCrisis>().OrderByDescending(c => c.Severity.Value).ToList();
        Add(ConflictSection(conflicts, now), CrisisLayer.Conflict);
        var food = FoodCrisis.Latest(crises.OfType<FoodCrisis>());   // the record the map and the hover use too
        Add(food is null ? null : FoodSection(food, _nameOf(iso3) ?? iso3), CrisisLayer.Food);
        var outbreaks = crises.OfType<OutbreakCrisis>().OrderByDescending(o => o.PublishedUtc).ToList();
        Add(OutbreakSection(outbreaks, now), CrisisLayer.Outbreaks);
        var displaced = s.Displacement.Where(d => string.Equals(d.CountryIso3, iso3, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(d => d.Year).FirstOrDefault();
        Add(displaced is null ? null : DisplacementSection(displaced, now), CrisisLayer.Displacement);

        // The needs sections follow the crisis ones. They are not crises: the headline rules below ignore them.
        if (_indicators is not null) sections.AddRange(NeedsSections(_indicators.Snapshot, _store.Snapshot.CheckedUtc, iso3, now));

        var currentCrisis = conflicts.Count > 0 || outbreaks.Count > 0 || food is { IsStale: false, ProjectionOnly: false };
        var projectionOnly = !currentCrisis && food is { ProjectionOnly: true };
        var noCrisis = !currentCrisis && !projectionOnly;
        Iso3 = iso3;
        CountryName = _nameOf(iso3) ?? iso3;
        Sections = sections;
        HasNoCrisis = noCrisis;
        ShowsSourcesChecked = noCrisis || projectionOnly;
        var notChecked = SourceDates.Unchecked(s);
        Headline = notChecked.Count > 0 ? SourceDates.NotCheckedHeadline(notChecked)
            : noCrisis ? NoCrisisText : projectionOnly ? ProjectionOnlyText : "";
        HasHeadline = Headline.Length > 0;
        SourcesChecked = ShowsSourcesChecked ? [.. SourceDates.All(s, now).Select(x => SourceCheckedLine(x, food, displaced, now))] : [];
        FocusedSectionIndex = focusIndex;
    }

    /// <summary>
    /// One source in a no-crisis country's list (ruling R12): "UCDP (armed conflict) · none recorded · checked 22 Nov 2026",
    /// "WHO (disease outbreaks) · no reports in the last 90 days · checked ...", what the source does have for the country
    /// (a stale IPC analysis, an IPC projection with no current figures, UNHCR figures), or "· not yet checked".
    /// </summary>
    public static string SourceCheckedLine(SourceSummary s, FoodCrisis? food, DisplacementFigure? displaced, DateTime nowUtc)
    {
        var head = $"{s.ShortName} ({s.What})";
        if (s.CheckedUtc is not { } checkedUtc) return $"{head} · {SourceDates.NotChecked}";
        var found = s.Layer switch
        {
            CrisisLayer.Food when food is { ProjectionOnly: true } => CrisisText.ProjectionOnlyNote(food),
            CrisisLayer.Food when food is { IsStale: true } => $"no current crisis (last analysis {CrisisText.Period(food.PeriodFrom, food.PeriodTo)})",
            CrisisLayer.Displacement when displaced is not null => CrisisText.IsFuture(displaced.Citation, nowUtc)
                ? displaced.Citation.Dataset
                : $"{displaced.Citation.Dataset}, {CrisisText.AsOf(displaced.Citation.AsOfUtc)}",
            _ => SourceDates.NothingFound(s.ShortName),
        };
        return $"{head} · {found} · {SourceDates.Checked(checkedUtc)}";
    }

    // ---------------------------------------------------------------- sections

    private static PanelSection? ConflictSection(IReadOnlyList<ConflictCrisis> conflicts, DateTime now)
    {
        if (conflicts.Count == 0) return null;
        var rows = new List<(string, string)>();
        foreach (var c in conflicts)
        {
            rows.Add(("Conflict", ConflictName(c)));
            if (!string.IsNullOrWhiteSpace(c.SideA) || !string.IsNullOrWhiteSpace(c.SideB)) rows.Add(("Sides", $"{c.SideA} vs {c.SideB}"));
            rows.Add(("Deaths", c.Severity.Text));   // UCDP's own wording, shown as is
            if (c.Trend is { } t) rows.Add(("Trend", $"{CrisisText.Number(t.Recent)} deaths in the last 3 months, {CrisisText.Number(t.Previous)} in the 3 before"));
        }
        var newest = conflicts.SelectMany(c => c.Citations).MaxBy(x => x.AsOfUtc)!;
        return new PanelSection(ConflictHeading, rows, CrisisText.SourceLine(newest, CrisisText.DataDate(newest, now)), newest.Url);
    }

    // The title is "<country>: <UCDP conflict name>"; the panel already names the country.
    private static string ConflictName(ConflictCrisis c)
    {
        var colon = c.Title.IndexOf(": ", StringComparison.Ordinal);
        return colon >= 0 ? c.Title[(colon + 2)..] : c.Title;
    }

    private static PanelSection FoodSection(FoodCrisis f, string country)
    {
        var cite = f.Citations.Count > 0 ? f.Citations[0]
            : new SourceCitation(IpcNationalReader.Publisher, IpcNationalReader.Dataset, null, IpcNationalReader.PageUrl, "CC0", IpcNationalReader.Attribution, default, default);
        var period = CrisisText.Period(f.PeriodFrom, f.PeriodTo);
        if (f.ProjectionOnly)
        {
            List<(string, string)> projected =
            [
                ("IPC analysis", CrisisText.ProjectionOnlyLine(f)),
                ("Projected in Phase 3 or higher", $"{CrisisText.Number(f.Phase3PlusPeople)} ({CrisisText.Percent(f.Phase3PlusShare)} of the people analysed; {f.ProjectionName ?? "projection"})"),
                ("Note", "A projection is not current-period figures, so it is not shown as a current crisis"),
            ];
            if (f.IsStale) projected.Add(("Ended", "More than 12 months ago"));
            return new PanelSection(ProjectionFoodHeading, projected, CrisisText.SourceLine(cite, CrisisText.FoodDate(f)), cite.Url);
        }
        // R17: the 20% rule and the phase bar, for a past analysis too. The bar's rows carry the people in every phase (R16:
        // a small Phase 5 count is not read as a whole country in famine).
        var c = IpcAreaRule.Classify(f);
        var bar = PhaseBar.For(country, f);
        List<(string, string)> rows;
        if (f.IsStale)
            rows = [("Last IPC analysis", period), ("Note", "Ended more than 12 months ago, so it is not shown as a current crisis")];
        else
        {
            rows = [("Country shading", CrisisText.FoodShading(c))];
            // A shaded country's worse-phase outline; an outline-only country's tint and outline are in the shading row.
            if (c.ShadedPhase is not null && c.OutlinePhase is { } o) rows.Add(("Outline", $"{LayerStyle.IpcPhaseLabel(o)}: some people are in this phase"));
            rows.Add(("People in Phase 3 or higher", CrisisText.Number(c.Phase3PlusPeople)
                + (c.ShareKnown ? $" ({CrisisText.ShareOf(f, c, c.Phase3PlusShare)})" : " (share unknown)")));
            rows.Add(("Analysis period", period));
        }
        return new PanelSection(f.IsStale ? PastFoodHeading : FoodHeading, rows, CrisisText.SourceLine(cite, CrisisText.FoodDate(f)), cite.Url, bar);
    }

    private static PanelSection? OutbreakSection(IReadOnlyList<OutbreakCrisis> outbreaks, DateTime now)
    {
        if (outbreaks.Count == 0) return null;
        var rows = new List<(string, string)>();
        foreach (var o in outbreaks)
        {
            rows.Add(("Disease", o.Disease));
            rows.Add(("Reported", CrisisText.Date(o.PublishedUtc)));
        }
        rows.Add(("Window", $"WHO Outbreak News reports in the last {WhoOutbreakFeed.Window}"));
        var newest = outbreaks[0];
        var cite = newest.Citations.MaxBy(x => x.AsOfUtc)!;
        return new PanelSection(OutbreakHeading, rows, CrisisText.SourceLine(cite, CrisisText.DataDate(cite, now)), newest.ReportUrl);
    }

    private static PanelSection DisplacementSection(DisplacementFigure d, DateTime now) => new(DisplacementHeading,
        [
            ("Refugees", CrisisText.Number(d.Refugees)),
            ("Asylum-seekers", CrisisText.Number(d.AsylumSeekers)),
            ("Internally displaced", CrisisText.Number(d.Idps)),
            ("Total", CrisisText.Number(CrisisText.Total(d))),
            ("Counted", "by country of origin"),
        ],
        CrisisText.SourceLine(d.Citation, CrisisText.DataDate(d.Citation, now)), d.Citation.Url);

    // ---------------------------------------------------------------- needs (Plan 3)

    private static readonly (string Code, string Label)[] ClusterRows =
    [
        ("ALL", "Total (ALL)"), ("FSC", "Food security (FSC)"), ("WSH", "Water, sanitation and hygiene (WSH)"),
        ("SHL", "Shelter (SHL)"), ("HEA", "Health (HEA)"),
    ];

    /// <summary>
    /// "Basic needs": the country's three WHO GHO values as the map shows them (each with its year and source), then the UN
    /// humanitarian plan's rows (or the statement that no plan lists the country). A series not loaded says "Data not loaded
    /// yet"; a value the map would not shade (no class) is "No figure", the same <see cref="CountryValueLayer.HasFigure"/> rule.
    /// </summary>
    private static IEnumerable<PanelSection> NeedsSections(IndicatorSnapshot snap, IReadOnlyDictionary<string, DateTime> checkedUtc, string iso3, DateTime now)
    {
        var rows = new List<(string, string)>();
        var cites = new List<SourceCitation>();
        foreach (var (layer, label) in new[]
                 { (ShadingLayer.Water, "Without at least basic drinking water"), (ShadingLayer.Sanitation, "Without at least basic sanitation"), (ShadingLayer.Health, "Health services not covered") })
        {
            if (snap.Get(LayerCatalog.SeriesId(layer)) is not { } series) { rows.Add((label, LayerCatalog.NotLoaded)); continue; }
            var scale = ShadingScales.For(layer);
            if (series.For(iso3) is not { } v || !CountryValueLayer.HasFigure(v, scale)) { rows.Add((label, NoFigureText)); continue; }
            cites.Add(v.Citation);
            var note = layer == ShadingLayer.Health ? "; 100 minus the UHC service coverage index" : "";
            var unit = layer == ShadingLayer.Health ? CountryValueLayer.ValueText(v, series.Unit, scale) : CountryValueLayer.ValueText(v, series.Unit, scale) + " of people";
            rows.Add((label, $"{unit} ({v.Year}, {LayerCatalog.SourceName(layer)}{note})"));
        }
        const string ghoPublisher = "World Health Organization, Global Health Observatory";
        var newest = cites.MaxBy(c => c.AsOfUtc);
        var ghoLine = newest is null
            ? $"Source: {ghoPublisher} · {(rows.Any(r => r.Item2 == LayerCatalog.NotLoaded) ? LayerCatalog.NotLoaded : "no figure for this country")} · {CheckedText(checkedUtc, GhoFeed.FeedName)}"
            : $"Source: {newest.Publisher}, Global Health Observatory · {CrisisText.DataDate(newest, now)}, newest (each figure shows its own year)";
        yield return new PanelSection(BasicNeedsHeading, rows, ghoLine, newest?.Url ?? GhoFeed.PageUrl);

        yield return PlanSection(snap, checkedUtc, iso3, now);
    }

    private static PanelSection PlanSection(IndicatorSnapshot snap, IReadOnlyDictionary<string, DateTime> checkedUtc, string iso3, DateTime now)
    {
        if (snap.Needs.Count == 0)
            return new PanelSection("People in need (UN humanitarian plan)", [("People in need", LayerCatalog.NotLoaded)],
                $"Source: {HnoFeed.Publisher} · {LayerCatalog.NotLoaded} · {CheckedText(checkedUtc, HnoFeed.FeedName)}", HnoFeed.PageUrl);
        var any = snap.Needs[0];
        var source = $"Source: {any.Citation.Publisher}, {any.Citation.Dataset} · {CrisisText.DataDate(any.Citation, now)}";
        var mine = snap.NeedsOf(iso3);
        if (mine.Count == 0)
            return new PanelSection($"No UN humanitarian needs plan for {any.PlanYear} lists this country", [], source, any.Citation.Url);

        var rows = new List<(string, string)>();
        foreach (var (code, label) in ClusterRows)
        {
            if (mine.FirstOrDefault(n => n.Cluster == code) is not { } n) continue;   // not reported in this file: no row, never zero
            rows.Add((label, NeedValue(n)));
        }
        if (mine.FirstOrDefault(n => n.Cluster == "ALL")?.Basis is { } basis)
            rows.Add(("Figures are", basis.Contains("GHO", StringComparison.OrdinalIgnoreCase)
                ? basis + ": not from this country's own needs overview" : basis));
        return new PanelSection($"People in need (UN humanitarian plan {any.PlanYear})", rows, source, any.Citation.Url);
    }

    /// <summary>"checked 23 Nov 2026" from the feed's last successful run, or "not yet checked".</summary>
    private static string CheckedText(IReadOnlyDictionary<string, DateTime> checkedUtc, string feed) =>
        checkedUtc.TryGetValue(feed, out var t) ? SourceDates.Checked(t) : SourceDates.NotChecked;

    private static string NeedValue(HumanitarianNeed n)
    {
        if (n.InNeed is not { } need) return "not reported" + (n.Targeted is { } t0 ? $"; {CrisisText.Number(t0)} targeted" : "");
        var text = $"{CrisisText.Number(need)} " + (n.Cluster == "ALL" ? "people" : "people in need");
        if (n.Cluster == "ALL" && n.Population is > 0 and var pop) text += $", {CrisisText.Percent((double)need / pop)} of {CrisisText.Number(pop)}";
        if (n.Targeted is { } targeted) text += $"; {CrisisText.Number(targeted)} targeted";
        return text;
    }

    // ---------------------------------------------------------------- store

    private void OnStoreChanged(object? sender, EventArgs e)
    {
        if (_dispatcher is null || _dispatcher.CheckAccess()) { RefreshIfOpen(); return; }
        if (Interlocked.Exchange(ref _refreshPending, 1) == 1) return;
        _dispatcher.BeginInvoke(() =>
        {
            Volatile.Write(ref _refreshPending, 0);
            RefreshIfOpen();
        });
    }

    private void RefreshIfOpen()
    {
        if (IsOpen && Iso3.Length > 0) Build(Iso3);
    }
}
