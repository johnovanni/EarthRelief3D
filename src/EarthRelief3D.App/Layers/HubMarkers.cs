using System.Globalization;
using AstroCore.Earth;
using EarthRelief3D.App.Curated;
using EarthRelief3D.App.Presentation;
using EarthRelief3D.Core.Curated;
using EarthRelief3D.Core.Feeds;
using EarthRelief3D.Core.Logic;
using EarthRelief3D.Core.Model;
using GlobeCore.Bodies;
using GlobeCore.Feeds;

namespace EarthRelief3D.App.Layers;

/// <summary>
/// The Getting help in group's three hub marker layers (Plan 4, P4-5): UN depots (a star each, from the curated rows,
/// <see cref="ForDepots"/>), airports (a hexagon outline) and ports (a diamond outline), the last two from the bundled
/// extracts in <c>data\datasets\ourairports</c> and <c>wpi-ports</c>. Each marker's tooltip says what it is and where the
/// data is from, with its as-of day; the legend line gives the filter and the credit. One status dot, "Hubs", names both
/// extracts (as the GEM dot does for its four).
/// </summary>
public static class HubMarkers
{
    /// <summary>The status dot shared by the airport and port extracts.</summary>
    public const string DotName = "Hubs";

    /// <summary>The legend's note when no UN depot is shown (the rows are drafts until the owner approves them, P4-13).</summary>
    public const string NoneApprovedNote = "None approved yet: each depot is shown after the owner's review";

    /// <summary>The legend's note in a <c>--show-drafts</c> run.</summary>
    public const string DraftsNote = "Drafts shown: not yet reviewed (diagnostic run). " + DepotPositionNote;

    /// <summary>The note under the UN depots line (ruling R21): UNHRD publishes no coordinates, so each depot is marked at the airport beside it.</summary>
    public const string DepotPositionNote = "UNHRD publishes no coordinates: each depot is marked at the airport its address names or sits beside (OurAirports, The Unlicense), so its position is approximate";

    public const string DepotPublisher = "UNHRD (WFP)";
    public static readonly Uri DepotPage = new("https://unhrd.org/contacts");

    /// <summary>The layers drawn from bundled extracts and their folders under <c>data\datasets</c>.</summary>
    public static IReadOnlyList<(MarkerLayer Layer, string Folder)> Datasets { get; } =
        [(MarkerLayer.Airports, "ourairports"), (MarkerLayer.Ports, "wpi-ports")];

    // ---------------------------------------------------------------- sizes and tooltips

    /// <summary>The marker's radius in pixels for its class ("large" or "medium"); the star is a little bigger as there are only five.</summary>
    public static double SizeOf(MarkerLayer layer, string type) => layer switch
    {
        MarkerLayer.UnDepots => 5,
        MarkerLayer.Airports or MarkerLayer.Ports => type == "large" ? 4.5 : 3.5,
        _ => throw new ArgumentOutOfRangeException(nameof(layer), layer, "Only the hub layers are sized here."),
    };

    /// <summary>
    /// "Port Sudan · port (large harbour) · NGA World Port Index, as of 5 Nov 2026"; an airport reads "Keflavik International
    /// Airport · airport (large, scheduled flights) · IATA KEF · OurAirports, as of 24 Nov 2026". Long lines are wrapped (<see cref="TooltipText.Wrap"/>).
    /// </summary>
    public static string Tooltip(MarkerLayer layer, MarkerPoint p, SourceCitation c)
    {
        ArgumentNullException.ThrowIfNull(p);
        ArgumentNullException.ThrowIfNull(c);
        var when = CrisisText.DataDate(c, DateTime.MaxValue);
        return TooltipText.Wrap(layer switch
        {
            MarkerLayer.Airports => $"{p.Name} · airport ({p.Type}, scheduled flights){(p.Detail.Length > 0 ? $" · IATA {p.Detail}" : "")} · OurAirports, {when}",
            MarkerLayer.Ports => $"{p.Name} · port ({p.Type} harbour) · NGA World Port Index, {when}",
            _ => throw new ArgumentOutOfRangeException(nameof(layer), layer, "Only airports and ports have these tooltips."),
        });
    }

    /// <summary>
    /// "UNHRD Accra · UN Humanitarian Response Depot (WFP) · marked at Kotoka International Airport (DGAA), beside the depot, so
    /// approximate · position: OurAirports (The Unlicense), 24 Nov 2026 · UNHRD contacts page, checked 24 Nov 2026" (ruling R21);
    /// a draft is marked first.
    /// </summary>
    public static string DepotTooltip(Depot d)
    {
        ArgumentNullException.ThrowIfNull(d);
        const string prefix = "OurAirports: ";
        var at = d.PositionSource.Title.StartsWith(prefix, StringComparison.Ordinal) ? d.PositionSource.Title[prefix.Length..] : d.PositionSource.Title;
        return TooltipText.Wrap((d.Review.IsApproved ? "" : DesignationText.DraftLabel + " · ")
            + $"{d.Name} · UN Humanitarian Response Depot (WFP) · marked at {at}, beside the depot, so approximate"
            + $" · position: {d.PositionSource.Publisher}, {DesignationText.Date(d.PositionSource.Date)}"
            + $" · UNHRD contacts page, checked {DesignationText.Date(d.CheckedOn)}");
    }

    // ---------------------------------------------------------------- the layers' points

    /// <summary>
    /// The legend's line for the airport layer (ruling R20): only the large airports are drawn; the panel's nearest-airport search
    /// (and its arc) still takes the medium ones too (P4-5).
    /// </summary>
    public const string AirportsDrawn = "large airports with scheduled flights (the panel's nearest airport may be a medium one)";

    /// <summary>
    /// The airport or port layer's points from its loaded extract, or null when it did not load (the legend then says "Data not
    /// loaded yet", R2). Airports: the large ones only (ruling R20: some 3,250 hexagons smothered the globe and hid the arcs).
    /// </summary>
    public static PointSet? For(MarkerLayer layer, BundledPointsLoad load)
    {
        ArgumentNullException.ThrowIfNull(load);
        if (!load.Loaded) return null;
        var c = load.Citation!;
        bool airports = layer == MarkerLayer.Airports;
        var items = load.Points.Where(p => !airports || p.Type == "large")
            .Select(p => new PointItem(new GeoPosition(p.Lat, p.Lon), SizeOf(layer, p.Type), Tooltip(layer, p, c), p.Iso3)).ToList();
        return new PointSet(items, c, Credit(load.Manifest!))
        {
            Measures = $"{LayerCatalog.Label(layer)}: {(airports ? AirportsDrawn : load.Manifest!.Filter)}",
        };
    }

    /// <summary>The legend's note: the source's credit, its licence, and what EarthRelief3D changed.</summary>
    public static string Credit(DatasetManifest m)
    {
        ArgumentNullException.ThrowIfNull(m);
        return $"{m.Attribution} Licence: {m.Licence}." + (m.Changes is { } ch ? $" Changed: {ch}." : "");
    }

    /// <summary>
    /// The UN depots layer's points: a star at each depot the user sees (approved, or drafts with <c>--show-drafts</c>), or
    /// null when there are no depot rows at all (the legend then says "Data not loaded yet", R2).
    /// </summary>
    public static PointSet? ForDepots(CuratedStore store)
    {
        ArgumentNullException.ThrowIfNull(store);
        var all = store.Tables.Depots;
        if (all.Count == 0) return null;
        var items = store.VisibleDepots.Select(d => new PointItem(d.Position, SizeOf(MarkerLayer.UnDepots, ""), DepotTooltip(d), d.Iso3)).ToList();
        var at = all.Max(d => d.CheckedOn).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        // As the camps' line: the date is when the list was checked ("checked 24 Nov 2026").
        var citation = new SourceCitation(DepotPublisher, "UN Humanitarian Response Depots (curated)", null, DepotPage, "Facts with citations", DepotPublisher,
            at, at, CitationDate.Checked);
        string note = store.VisibleDepots.Count == 0 ? NoneApprovedNote
            : store.ShowDrafts && store.VisibleDepots.Any(d => !d.Review.IsApproved) ? DraftsNote : DepotPositionNote;
        return new PointSet(items, citation, note);
    }

    // ---------------------------------------------------------------- the hubs list (nearest hubs and arcs, Task 5)

    /// <summary>
    /// Every hub the user can be measured to (P4-5): each airport and port of the loaded extracts (an extract that did not
    /// load adds none), and each UN depot in <paramref name="visibleDepots"/> (approved ones; drafts in a diagnostic run).
    /// Each carries its own source: the extract's citation, or the depot row's source checked on the row's day.
    /// </summary>
    public static IReadOnlyList<Hub> Hubs(IReadOnlyList<(MarkerLayer Layer, BundledPointsLoad Load)> loads, IReadOnlyList<Depot> visibleDepots)
    {
        ArgumentNullException.ThrowIfNull(loads);
        ArgumentNullException.ThrowIfNull(visibleDepots);
        var hubs = new List<Hub>();
        foreach (var (layer, load) in loads)
        {
            if (!load.Loaded) continue;
            var kind = layer switch
            {
                MarkerLayer.Airports => HubKind.Airport,
                MarkerLayer.Ports => HubKind.Port,
                _ => throw new ArgumentOutOfRangeException(nameof(loads), layer, "Only the airport and port extracts hold hubs."),
            };
            hubs.AddRange(load.Points.Select(p => new Hub(kind, p.Name, p.Iso3, new SurfacePoint(p.Lat, p.Lon), p.Detail, load.Citation!)));
        }
        hubs.AddRange(visibleDepots.Select(d => new Hub(HubKind.UnDepot, d.Name, d.Iso3, d.Position, "", DepotCitation(d)) { IsDraft = !d.Review.IsApproved }));
        return hubs;
    }

    /// <summary>A depot's source: UNHRD's contacts page, "checked" on the day the row was (as the curated lists are, C10).</summary>
    public static SourceCitation DepotCitation(Depot d)
    {
        ArgumentNullException.ThrowIfNull(d);
        var at = d.CheckedOn.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        return new SourceCitation(d.Source.Publisher, d.Source.Title, null, d.Source.Url, "Facts with citations", d.Source.Publisher, at, at, CitationDate.Checked);
    }

    /// <summary>The name of a hub kind's source, as the panel and the legend say it.</summary>
    public static string SourceName(HubKind kind) => kind switch
    {
        HubKind.UnDepot => "UNHRD contacts page",
        HubKind.Airport => "OurAirports",
        HubKind.Port => "NGA World Port Index",
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    /// <summary>"UN depots", "Airports", "Ports".</summary>
    public static string KindLabel(HubKind kind) => kind switch
    {
        HubKind.UnDepot => LayerCatalog.Label(MarkerLayer.UnDepots),
        HubKind.Airport => LayerCatalog.Label(MarkerLayer.Airports),
        HubKind.Port => LayerCatalog.Label(MarkerLayer.Ports),
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    /// <summary>
    /// The datasets the hubs come from and their days, for the kinds present:
    /// "UN depots: UNHRD contacts page, checked 24 Nov 2026 · Airports: OurAirports, as of 24 Nov 2026 · Ports: NGA World Port Index, as of 5 Nov 2026".
    /// Null when there are no hubs (nothing is loaded).
    /// </summary>
    public static string? SourceText(IReadOnlyList<Hub> hubs, DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(hubs);
        var parts = Enum.GetValues<HubKind>().Select(k => (Kind: k, Cite: hubs.Where(h => h.Kind == k).Select(h => h.Citation).MaxBy(c => c.AsOfUtc)))
            .Where(x => x.Cite is not null)
            .Select(x => $"{KindLabel(x.Kind)}: {SourceName(x.Kind)}, {CrisisText.DataDate(x.Cite!, nowUtc)}").ToList();
        return parts.Count == 0 ? null : string.Join(" · ", parts);
    }

    // ---------------------------------------------------------------- the status dot

    /// <summary>
    /// The one "Hubs" status dot: green with each extract's as-of day when both loaded, otherwise amber naming the one that
    /// did not and why.
    /// </summary>
    public static FeedStatus Status(IReadOnlyList<(MarkerLayer Layer, BundledPointsLoad Load)> loads)
    {
        ArgumentNullException.ThrowIfNull(loads);
        var parts = loads.Select(l => $"{ShortName(l.Layer)} " + (l.Load.Loaded ? CrisisText.DataDate(l.Load.Citation!, DateTime.MaxValue) : $"not loaded ({l.Load.Problem})"));
        var note = string.Join("; ", parts) + " · " + BundledDataset.StatusNote;
        bool ok = loads.Count > 0 && loads.All(l => l.Load.Loaded);
        return new FeedStatus(DotName, ok ? FeedState.Ok : FeedState.Attention, null, ok ? null : "not loaded", 0, default, note);
    }

    private static string ShortName(MarkerLayer layer) => layer switch
    {
        MarkerLayer.Airports => "airports",
        MarkerLayer.Ports => "ports",
        _ => LayerCatalog.Label(layer),
    };
}
