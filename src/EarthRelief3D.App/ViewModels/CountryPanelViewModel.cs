using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EarthRelief3D.App.Curated;
using EarthRelief3D.App.Layers;
using EarthRelief3D.App.Presentation;
using EarthRelief3D.Core.Curated;
using EarthRelief3D.Core.Feeds;
using EarthRelief3D.Core.Model;
using EarthRelief3D.Core.Store;

namespace EarthRelief3D.App.ViewModels;

/// <summary>One part of the country panel: a heading, label/value rows, the source line and a link to the source.</summary>
/// <param name="SourceLine">"Source: &lt;publisher&gt;, &lt;dataset&gt; · as of &lt;date&gt;" (food: the IPC analysis period instead).</param>
/// <param name="Bar">The IPC phase bar (R17): food sections with current-period figures, current or past; null otherwise.</param>
/// <param name="Links">The section's documents, one link each (curated sections: every designation's and camp report's own document).</param>
public sealed record PanelSection(string Heading, IReadOnlyList<(string Label, string Value)> Rows, string SourceLine, Uri? Link,
    PhaseBar? Bar = null, IReadOnlyList<PanelLink>? Links = null)
{
    public bool HasLinks => Links is { Count: > 0 };
}

/// <summary>A link under a panel section: the document's short description and its address.</summary>
public sealed record PanelLink(string Text, Uri Url);

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
    /// <summary>The needs part of the panel (Plan 3): the WHO GHO values, then the UN humanitarian needs overview's rows.</summary>
    public const string BasicNeedsHeading = "Basic needs";
    public const string NoFigureText = "No figure from WHO GHO";
    /// <summary>Designations on the map now (C14: a statement within 12 months says the situation continues).</summary>
    public const string AtrocityHeading = "Genocide and mass atrocities";
    /// <summary>The country's other designations the user sees: past, or "not established" (C14).</summary>
    public const string AtrocityHistoryHeading = "Genocide and mass atrocities: history";
    /// <summary>Court judgments that found the crime alleged not established (ICJ, Croatia v. Serbia): never on the map, never a crisis.</summary>
    public const string NotEstablishedHeading = "Cases decided: not established";
    public const string CampsHeading = "Prison camps";
    /// <summary>Camps whose newest cited report says they have closed (ruling R14): no marker, no crisis.</summary>
    public const string CampsHistoryHeading = "Prison camps: reported closed";
    /// <summary>The note at the top of the history section.</summary>
    public const string HistoryNote = "Not on the map. A triangle needs a court judgment, UN body finding or government determination, "
        + "and a statement from the last 12 months, by the designating body or in a UN report, that the situation continues.";
    /// <summary>The note at the top of the not-established section.</summary>
    public const string NotEstablishedNote = "The court did not find the crime alleged established. This is not a finding of genocide or of another atrocity crime.";
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
    private MarkerLayer? _markerFocus;
    private readonly CuratedStore? _curated;

    /// <param name="countryName">The country's name for an ISO3 code (null: the code is shown).</param>
    /// <param name="dispatcher">The UI thread's; store changes from other threads are queued onto it. Null: handled at once.</param>
    /// <param name="nowUtc">The clock (a future as-of date is not shown as "as of"); the system clock by default.</param>
    /// <param name="indicators">The country-value series and needs rows (Plan 3); null: no "Basic needs" sections.</param>
    /// <param name="curated">The curated designations and camp sites as the user sees them (Plan 3); null: no such sections.</param>
    public CountryPanelViewModel(CrisisStore store, Func<string, string?> countryName, Dispatcher? dispatcher = null,
        Func<DateTime>? nowUtc = null, IndicatorStore? indicators = null, CuratedStore? curated = null)
    {
        _nowUtc = nowUtc ?? (() => DateTime.UtcNow);
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _nameOf = countryName ?? throw new ArgumentNullException(nameof(countryName));
        _dispatcher = dispatcher;
        _store.Changed += OnStoreChanged;
        _indicators = indicators;
        if (_indicators is not null) _indicators.Changed += OnStoreChanged;
        _curated = curated;
        if (_curated is not null) _curated.Changed += OnStoreChanged;
    }

    /// <summary>
    /// Opens the panel for <paramref name="iso3"/>, at the <paramref name="focus"/> layer's section if given, or at a Plan 3
    /// marker layer's (<paramref name="marker"/>: atrocities, camps).
    /// </summary>
    public void Show(string iso3, CrisisLayer? focus = null, MarkerLayer? marker = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(iso3);
        _focus = focus;
        _markerFocus = marker;
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
        _markerFocus = null;
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
        Add(food is null ? null : FoodSection(food, _nameOf(iso3) ?? iso3, s.AreasFor(food)), CrisisLayer.Food);
        var outbreaks = crises.OfType<OutbreakCrisis>().OrderByDescending(o => o.PublishedUtc).ToList();
        Add(OutbreakSection(outbreaks, now), CrisisLayer.Outbreaks);
        var displaced = s.Displacement.Where(d => string.Equals(d.CountryIso3, iso3, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(d => d.Year).FirstOrDefault();
        Add(displaced is null ? null : DisplacementSection(displaced, now), CrisisLayer.Displacement);

        // Plan 3's curated crises (C11: a current designation or a documented camp is a crisis; history is not).
        var currentAtrocity = false;
        var anyCamp = false;
        if (_curated is not null)
        {
            void AddMarker(PanelSection? section, MarkerLayer layer)
            {
                if (section is null) return;
                if (_markerFocus == layer && focusIndex < 0) focusIndex = sections.Count;
                sections.Add(section);
            }
            var governments = _curated.Tables.Governments;
            var split = CountryDesignations.Of(_curated.Visible.Where(d => string.Equals(d.Iso3, iso3, StringComparison.OrdinalIgnoreCase)), now);
            currentAtrocity = split.Current.Count > 0;
            AddMarker(split.Current.Count == 0 ? null : AtrocitySection(AtrocityHeading, split.Current, governments, now, HistoryKind.None), MarkerLayer.Atrocities);
            AddMarker(split.History.Count == 0 ? null : AtrocitySection(AtrocityHistoryHeading, split.History, governments, now, HistoryKind.Past), MarkerLayer.Atrocities);
            AddMarker(split.NotEstablished.Count == 0 ? null
                : AtrocitySection(NotEstablishedHeading, split.NotEstablished, governments, now, HistoryKind.NotEstablished), MarkerLayer.Atrocities);
            // R14: a camp whose newest cited report says it closed is listed as history, without a marker, and is no crisis.
            var camps = _curated.VisibleCamps.Where(c => string.Equals(c.Iso3, iso3, StringComparison.OrdinalIgnoreCase)).ToList();
            var open = camps.Where(c => c.IsActive).ToList();
            var closed = camps.Where(c => !c.IsActive).ToList();
            anyCamp = open.Count > 0;
            AddMarker(open.Count > 0 ? CampsSection(CampsHeading, open) : null, MarkerLayer.Camps);
            AddMarker(closed.Count > 0 ? CampsSection(CampsHistoryHeading, closed) : null, MarkerLayer.Camps);
        }

        // The needs sections follow the crisis ones. They are not crises: the headline rules below ignore them.
        if (_indicators is not null) sections.AddRange(NeedsSections(_indicators.Snapshot, _store.Snapshot.CheckedUtc, iso3, now));

        var currentCrisis = conflicts.Count > 0 || outbreaks.Count > 0 || food is { IsStale: false, ProjectionOnly: false }
            || currentAtrocity || anyCamp;
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

    /// <param name="areas">The country's IPC areas when they belong to <paramref name="f"/> (P3-2): the map draws them, so the
    /// rows say so and count them, and R17's shading rows and famine note are not given.</param>
    private static PanelSection FoodSection(FoodCrisis f, string country, IpcAreaSet? areas = null)
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
        var byArea = areas is not null && areas.Matches(f);
        var bar = PhaseBar.For(country, f, byArea);
        List<(string, string)> rows;
        if (f.IsStale)
            rows = [("Last IPC analysis", period), ("Note", "Ended more than 12 months ago, so it is not shown as a current crisis")];
        else if (byArea)
        {
            // P3-2: the map shows IPC's own area classification; the national figures follow, as for any country.
            var a = areas!;
            rows =
            [
                ("Map shading", CrisisText.ByAreaShading),
                ("IPC areas", CrisisText.AreaCounts(a)),
                ("IPC areas source", $"{a.Citation.Publisher}, {a.Citation.Dataset}"
                    + (string.IsNullOrWhiteSpace(a.AnalysisDate) ? "" : $" (analysed {a.AnalysisDate})")),
                ("People in Phase 3 or higher", CrisisText.Number(c.Phase3PlusPeople)
                    + (c.ShareKnown ? $" ({CrisisText.ShareOf(f, c, c.Phase3PlusShare)})" : " (share unknown)")),
                ("Analysis period", period),
            ];
        }
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

    // ---------------------------------------------------------------- curated (Plan 3)

    /// <summary>
    /// The country's designations, newest first: each with its label (type and term; drafts marked), the authority and its
    /// kind, the date, the situation and group, and (when current) the continuing statement; a link to every document.
    /// </summary>
    private enum HistoryKind { None, Past, NotEstablished }

    private PanelSection AtrocitySection(string heading, IReadOnlyList<Designation> list, IReadOnlyList<Government> governments,
        DateTime now, HistoryKind kind)
    {
        var rows = new List<(string, string)>();
        if (kind == HistoryKind.Past) rows.Add(("Note", HistoryNote));
        if (kind == HistoryKind.NotEstablished) rows.Add(("Note", NotEstablishedNote));
        var links = new List<PanelLink>();
        foreach (var d in list)
        {
            var value = $"{d.Authority} ({DesignationText.AuthorityKind(d, governments)}), {DesignationText.Date(d.Date)}. {d.Situation}. Group: {d.Group}.";
            if (d.Continuing is { } c)
                value += d.ContinuesAt(now)
                    ? $" Continuing: \"{c.Statement}\" ({c.Title}, {DesignationText.Date(c.Date)})"
                    : $" Last statement that it continues: {DesignationText.Date(c.Date)}, more than 12 months ago.";
            rows.Add((DesignationText.DisplayLabel(d, governments), value));
            links.Add(new PanelLink($"{d.ShortName}, {DesignationText.Date(d.Date)}: {d.DocumentTitle}", d.DocumentUrl));
            if (d.Continuing is { } cont && d.ContinuesAt(now)) links.Add(new PanelLink($"Continuing: {cont.Title}, {DesignationText.Date(cont.Date)}", cont.Url));
        }
        var checkedOn = list.Max(d => d.CheckedOn);
        var drafts = list.Any(d => !d.Review.IsApproved) ? " · drafts shown: not yet reviewed" : "";
        return new PanelSection(heading, rows,
            $"Source: each entry's own document (courts, UN bodies, governments) · checked {DesignationText.Date(checkedOn)}{drafts}", null,
            Links: links);
    }

    /// <summary>The country's documented camps: name, description and where the position was published; a link to every report.</summary>
    private static PanelSection CampsSection(string heading, IReadOnlyList<CampSite> camps)
    {
        var rows = new List<(string, string)>();
        var links = new List<PanelLink>();
        foreach (var c in camps)
        {
            var ps = c.PositionSource;
            var label = (c.Review.IsApproved ? "" : DesignationText.DraftLabel + " · ") + c.Name + (c.LocalName is { } l ? $" ({l})" : "");
            var value = $"{c.Description} Position as published by {ps.Publisher} in \"{ps.Title}\" ({DesignationText.Date(ps.Date)}).";
            if (c.ReportedClosed is { } closed)
            {
                label += " · " + CampMarkers.ClosedLabel(closed);
                value = $"{closed.Statement} ({closed.Source.Publisher}, \"{closed.Source.Title}\", {DesignationText.Date(closed.Source.Date)}). " + value;
            }
            // R14b: where sources differ on its status, each statement is shown with its source and year.
            if (c.StatusNotes.Count > 0) value += " Status: " + string.Join("; ", c.StatusNotes.Select(n => n.Text)) + ".";
            rows.Add((label, value));
            links.AddRange(c.DocumentedBy.Select(r => new PanelLink($"{r.Publisher}, {DesignationText.Date(r.Date)}: {r.Title}", r.Url)));
        }
        var newest = camps.SelectMany(c => c.DocumentedBy).Max(r => r.Date);
        var drafts = camps.Any(c => !c.Review.IsApproved) ? " · drafts shown: not yet reviewed" : "";
        return new PanelSection(heading, rows,
            $"Source: {CampMarkers.Publisher} · newest report {DesignationText.Date(newest)}{drafts}", null, Links: links);
    }

    // ---------------------------------------------------------------- needs (Plan 3)

    private static readonly (string Code, string Label)[] ClusterRows =
    [
        ("ALL", "Total (ALL)"), ("FSC", "Food security (FSC)"), ("WSH", "Water, sanitation and hygiene (WSH)"),
        ("SHL", "Shelter (SHL)"), ("HEA", "Health (HEA)"),
    ];

    /// <summary>
    /// "Basic needs": the country's three WHO GHO values as the map shows them (each with its year and source), then the UN
    /// humanitarian needs overview's rows (or the statement that the country is not in the HNO file). A series not loaded says "Data not loaded
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
        rows.Add((FoodSupplyLabel, FoodSupply(snap, iso3)));   // FAO, bundled (Task 6): the row names its source and years
        yield return new PanelSection(BasicNeedsHeading, rows, ghoLine, newest?.Url ?? GhoFeed.PageUrl);

        yield return PlanSection(snap, checkedUtc, iso3, now);
        yield return ResourcesSection(snap, iso3, now);
    }

    public const string FoodSupplyLabel = "Food supply";

    /// <summary>
    /// "dietary energy adequacy 84%, 2023–2025, FAO": FAO's 3-year average (stored with the last of its years), or "No
    /// figure from FAO", or "Data not loaded yet".
    /// </summary>
    private static string FoodSupply(IndicatorSnapshot snap, string iso3)
    {
        if (snap.Get(EnergyAdequacySeries) is not { } series) return LayerCatalog.NotLoaded;
        if (series.For(iso3) is not { } v || double.IsNaN(v.Value) || v.Value < 0) return "No figure from FAO";
        var value = v.Qualifier is { Length: > 0 } q ? q : v.Value.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) + "%";
        var years = v.Citation.DateMeaning == CitationDate.ThreeYearAverage ? CrisisText.AverageRange(v.Year) : v.Year.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return $"dietary energy adequacy {value}, {years}, FAO";
    }

    /// <summary>The store id of FAO's dietary energy supply adequacy (bundled, Task 6): shown in the panel, not shaded.</summary>
    public const string EnergyAdequacySeries = "fao.energyAdequacy";

    /// <summary>The Resources part of the panel (Task 6): oil production and water stress, from the bundled extracts.</summary>
    public const string ResourcesHeading = "Resources";

    /// <summary>
    /// "Resources": crude oil production (EIA) and baseline water stress (WRI), each as the map shows it with its year and
    /// source; the source line names both publishers with their dates and that the data ship with the app.
    /// </summary>
    private static PanelSection ResourcesSection(IndicatorSnapshot snap, string iso3, DateTime now)
    {
        var rows = new List<(string, string)>();
        var sources = new List<(string Text, bool Loaded)>();
        var links = new List<PanelLink>();
        foreach (var (layer, label) in new[] { (ShadingLayer.OilProduction, "Crude oil production"), (ShadingLayer.WaterStress, "Water stress") })
        {
            if (snap.Get(LayerCatalog.SeriesId(layer)) is not { } series)
            {
                rows.Add((label, LayerCatalog.NotLoaded));
                sources.Add((LayerCatalog.SourceName(layer), false));
                continue;
            }
            var scale = ShadingScales.For(layer);
            var any = series.Values.Select(v => v.Citation).MaxBy(c => c.AsOfUtc);
            if (any is not null)
            {
                sources.Add(($"{any.Publisher}, {CrisisText.SeriesDate(any, now)}", true));
                links.Add(new PanelLink(any.Publisher, any.Url));
            }
            else sources.Add(($"{LayerCatalog.SourceName(layer)}, no figures in the data", true));
            if (series.For(iso3) is not { } v || !CountryValueLayer.HasFigure(v, scale)) { rows.Add((label, $"No figure from {LayerCatalog.SourceName(layer)}")); continue; }
            var year = scale.Categorical ? $"{v.Year} baseline" : v.Year.ToString(System.Globalization.CultureInfo.InvariantCulture);
            rows.Add((label, $"{CountryValueLayer.ValueText(v, series.Unit, scale)} ({year}, {LayerCatalog.SourceName(layer)})"));
        }
        var line = sources.All(x => !x.Loaded)
            ? $"Source: {string.Join("; ", sources.Select(x => x.Text))} · {LayerCatalog.NotLoaded} · {BundledDataset.StatusNote}"
            : $"Source: {string.Join("; ", sources.Select(x => x.Loaded ? x.Text : $"{x.Text} ({LayerCatalog.NotLoaded})"))} · {BundledDataset.StatusNote}";
        return new PanelSection(ResourcesHeading, rows, line, links.FirstOrDefault()?.Url, Links: links);
    }

    private static PanelSection PlanSection(IndicatorSnapshot snap, IReadOnlyDictionary<string, DateTime> checkedUtc, string iso3, DateTime now)
    {
        if (snap.Needs.Count == 0)
            return new PanelSection("People in need (UN humanitarian needs overview)", [("People in need", LayerCatalog.NotLoaded)],
                $"Source: {HnoFeed.Publisher} · {LayerCatalog.NotLoaded} · {CheckedText(checkedUtc, HnoFeed.FeedName)}", HnoFeed.PageUrl);
        var any = snap.Needs[0];
        var source = $"Source: {any.Citation.Publisher}, {any.Citation.Dataset} · {CrisisText.DataDate(any.Citation, now)}";
        var mine = snap.NeedsOf(iso3);
        if (mine.Count == 0)
            return new PanelSection(NotInHnoFile(any.PlanYear), [], source, any.Citation.Url);

        var rows = new List<(string, string)>();
        foreach (var (code, label) in ClusterRows)
        {
            if (mine.FirstOrDefault(n => n.Cluster == code) is not { } n) continue;   // not reported in this file: no row, never zero
            rows.Add((label, NeedValue(n)));
        }
        if (mine.FirstOrDefault(n => n.Cluster == "ALL")?.Basis is { } basis)
            rows.Add(("Figures are", basis.Contains("GHO", StringComparison.OrdinalIgnoreCase)
                ? basis + ": not from this country's own needs overview" : basis));
        return new PanelSection($"People in need (UN humanitarian needs overview {any.PlanYear})", rows, source, any.Citation.Url);
    }

    /// <summary>
    /// The line for a country with no rows in the HNO file (I2): "Not in OCHA's 2026 needs-overview file on HDX". The source
    /// is one HDX file of about 20 countries, so it says only that the country is not in that file, never that no UN plan
    /// lists it; and the data is a Humanitarian Needs Overview, not a Response Plan.
    /// </summary>
    public static string NotInHnoFile(int year) => $"Not in OCHA's {year.ToString(System.Globalization.CultureInfo.InvariantCulture)} needs-overview file on HDX";

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
