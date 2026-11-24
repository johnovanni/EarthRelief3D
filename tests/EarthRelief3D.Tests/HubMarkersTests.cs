using System.IO;
using System.Windows.Media;
using EarthRelief3D.App.Curated;
using EarthRelief3D.App.Layers;
using EarthRelief3D.App.Presentation;
using EarthRelief3D.App.Services;
using EarthRelief3D.App.ViewModels;
using EarthRelief3D.Core.Curated;
using EarthRelief3D.Core.Feeds;
using EarthRelief3D.Core.Model;
using EarthRelief3D.Core.Store;
using GlobeCore.Feeds;

namespace EarthRelief3D.Tests;

/// <summary>Plan 4 Task 4: the UN depot, airport and port marker layers of Getting help in (P4-5, P4-14).</summary>
public class HubMarkersTests
{
    private static readonly DateTime Now = new(2026, 11, 24, 12, 0, 0, DateTimeKind.Utc);

    private static BundledPointsLoad Committed(MarkerLayer layer)
    {
        var folder = HubMarkers.Datasets.Single(d => d.Layer == layer).Folder;
        var load = BundledPoints.Load(HubMarkers.DotName, Path.Combine(BundledDataset.DatasetsDir, folder), iso => CountryResolver.LoadDefault().ByIso3(iso) is not null, new ListLog());
        Assert.True(load.Loaded, load.Problem);
        return load;
    }

    private static string OneLine(string tip) => tip.Replace(Environment.NewLine + "    ", " ", StringComparison.Ordinal);

    private static List<(MarkerLayer Layer, BundledPointsLoad Load)> CommittedLoads() => [.. HubMarkers.Datasets.Select(d => (d.Layer, Committed(d.Layer)))];

    [Fact]
    public void OneDotNamesBothDatasets()
    {
        Assert.Equal("Hubs", HubMarkers.DotName);
        Assert.Equal([(MarkerLayer.Airports, "ourairports"), (MarkerLayer.Ports, "wpi-ports")], HubMarkers.Datasets);
        Assert.Equal("GEM", AppServices.StatusDotNames[^2]);   // after GEM
        Assert.Equal("Hubs", AppServices.StatusDotNames[^1]);
        Assert.Single(AppServices.StatusDotNames, n => n == "Hubs");
        var loads = CommittedLoads();
        var dot = HubMarkers.Status(loads);
        Assert.Equal(("Hubs", FeedState.Ok), (dot.Name, dot.State));
        // Both datasets, each with the day it is as of, and the bundled note.
        Assert.Equal("airports as of 24 Nov 2026; ports as of 5 Nov 2026 · " + BundledDataset.StatusNote, dot.Note);

        // A missing extract turns the dot amber and says which; the other layer still loads.
        using var dir = new TempDir();
        var broken = loads.Select(l => l.Layer == MarkerLayer.Ports ? (l.Layer, BundledPoints.Load(HubMarkers.DotName, dir.File("wpi-ports"), null, new ListLog())) : l).ToList();
        var amber = HubMarkers.Status(broken);
        Assert.Equal(FeedState.Attention, amber.State);
        Assert.Contains("ports not loaded (not found)", amber.Note);
        var markers = new MarkerDataStore();
        AppServices.ApplyHubPoints(markers, new CrisisStore(), broken);
        Assert.Null(markers.Get(MarkerLayer.Ports));
        Assert.NotEmpty(markers.Items(MarkerLayer.Airports));
    }

    [Fact]
    public void TooltipSaysWhatItIs()
    {
        var cite = new SourceCitation("US National Geospatial-Intelligence Agency (NGA)", "World Port Index (Pub 150)", null, new Uri("https://msi.nga.mil/Publications/WPI"),
            "Public domain (US government work, 17 U.S.C. 105)", "NGA.", new DateTime(2026, 11, 5, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 11, 24, 0, 0, 0, DateTimeKind.Utc));
        var port = new MarkerPoint("Port Sudan", "SDN", 19.6, 37.233333, null, "", "", "large", "SD PZU");
        Assert.Equal("Port Sudan · port (large harbour) · NGA World Port Index, as of 5 Nov 2026", OneLine(HubMarkers.Tooltip(MarkerLayer.Ports, port, cite)));
        Assert.StartsWith("Aden · port (medium harbour) · NGA World Port Index", HubMarkers.Tooltip(MarkerLayer.Ports, port with { Name = "Aden", Type = "medium" }, cite), StringComparison.Ordinal);

        var airportCite = cite with { Publisher = "OurAirports", Dataset = "OurAirports airport data", AsOfUtc = new DateTime(2026, 11, 24, 0, 0, 0, DateTimeKind.Utc) };
        var airport = new MarkerPoint("Port Sudan New International Airport", "SDN", 19.43, 37.23, null, "", "", "large", "PZU");
        Assert.Equal("Port Sudan New International Airport · airport (large, scheduled flights) · IATA PZU · OurAirports, as of 24 Nov 2026",
            OneLine(HubMarkers.Tooltip(MarkerLayer.Airports, airport, airportCite)));
        Assert.DoesNotContain("IATA", HubMarkers.Tooltip(MarkerLayer.Airports, airport with { Detail = "" }, airportCite), StringComparison.Ordinal);

        // The committed data: every marker's tooltip says what it is and where the data is from, with its as-of day.
        foreach (var (layer, load) in CommittedLoads())
        {
            var set = HubMarkers.For(layer, load)!;
            // R20: the airport layer draws the large airports only (the nearest-airport search keeps the medium ones).
            Assert.Equal(layer == MarkerLayer.Airports ? load.Points.Count(p => p.Type == "large") : load.Points.Count, set.Items.Count);
            Assert.All(set.Items, i => Assert.Matches(layer == MarkerLayer.Airports
                ? @"· airport \(large, scheduled flights\) · .*OurAirports, as of 24 Nov 2026$"
                : @"· port \((large|medium) harbour\) · NGA World Port Index, as of 5 Nov 2026$", OneLine(i.Tooltip)));
            Assert.All(set.Items, i => Assert.False(string.IsNullOrEmpty(i.CountryIso3)));
        }
        var sudan = HubMarkers.For(MarkerLayer.Ports, Committed(MarkerLayer.Ports))!.Items.Where(i => i.CountryIso3 == "SDN").ToList();
        Assert.Contains(sudan, i => OneLine(i.Tooltip).StartsWith("Port Sudan · port (medium harbour)", StringComparison.Ordinal));
    }

    [Fact]
    public void SizesFollowTheClass()
    {
        var loads = CommittedLoads();
        foreach (var (layer, load) in loads)
        {
            var set = HubMarkers.For(layer, load)!;
            var shown = layer == MarkerLayer.Airports ? load.Points.Where(p => p.Type == "large").ToList() : load.Points;
            var sizes = shown.Zip(set.Items).Select(z => (z.First.Type, z.Second.Size)).Distinct().OrderBy(x => x.Type, StringComparer.Ordinal).ToList();
            // R20: only large airports are drawn; ports keep both classes.
            Assert.Equal(layer == MarkerLayer.Airports ? ["large"] : ["large", "medium"], sizes.Select(x => x.Type));
            Assert.Equal(HubMarkers.SizeOf(layer, "large"), sizes[0].Size);
            Assert.True(HubMarkers.SizeOf(layer, "large") > HubMarkers.SizeOf(layer, "medium"), $"{layer}: large markers are bigger than medium ones");
        }
    }

    [Fact]
    public void LayersAreUnderTheMarkerCapAndKeepTheirNotPlacedCodes()
    {
        var loads = CommittedLoads();
        Assert.All(loads, l => Assert.InRange(l.Load.Points.Count, 400, PointMarkerLayer.MaxMarkers));
        Assert.InRange(loads.Single(l => l.Layer == MarkerLayer.Airports).Load.Points.Count, 3000, 3300);   // about 3,270 less the ones with no unit
        Assert.InRange(loads.Single(l => l.Layer == MarkerLayer.Ports).Load.Points.Count, 500, 550);
        var markers = new MarkerDataStore();
        var crises = new CrisisStore();
        AppServices.ApplyHubPoints(markers, crises, loads);
        Assert.NotEmpty(markers.Items(MarkerLayer.Airports));
        Assert.NotEmpty(markers.Items(MarkerLayer.Ports));
        // C16: a code with no unit in the boundary set is listed under "Not placed on the map", never dropped silently.
        var unplaced = crises.Snapshot.Unplaced.Where(u => u.Source == HubMarkers.DotName).ToList();
        Assert.NotEmpty(unplaced);
        Assert.Contains(unplaced, u => u.Title.StartsWith("Ports in Gibraltar", StringComparison.Ordinal));
        Assert.Contains(unplaced, u => u.Title.StartsWith("Airports in ", StringComparison.Ordinal));
    }

    // ---------------------------------------------------------------- UN depots

    private static CuratedStore Store(bool showDrafts, params Depot[] depots) =>
        new(CuratedTablesTests.Tables() with { Depots = depots }, showDrafts);

    [Fact]
    public void DepotsDrawOnlyVisibleRows()
    {
        var approved = CuratedTablesTests.DepotRow("unhrd-ok", "approved");
        var draft = CuratedTablesTests.DepotRow("unhrd-draft") with { Name = "UNHRD Draft", Position = new AstroCore.Earth.GeoPosition(8.9, -79.6) };

        var normal = HubMarkers.ForDepots(Store(false, approved, draft))!;
        var item = Assert.Single(normal.Items);
        Assert.Equal("GHA", item.CountryIso3);
        Assert.Equal(new AstroCore.Earth.GeoPosition(5.6, -0.17), item.Position);
        Assert.DoesNotContain("Draft", item.Tooltip, StringComparison.Ordinal);

        var drafts = HubMarkers.ForDepots(Store(true, approved, draft))!;
        Assert.Equal(2, drafts.Items.Count);
        Assert.Contains("Draft: not yet reviewed", Assert.Single(drafts.Items, i => i.Tooltip.Contains("UNHRD Draft", StringComparison.Ordinal)).Tooltip, StringComparison.Ordinal);
        Assert.Equal(HubMarkers.DraftsNote, drafts.Filter);

        // Nothing approved: no markers, and the legend says why. No tables at all: null (the legend says "Data not loaded yet", R2).
        var none = HubMarkers.ForDepots(Store(false, draft))!;
        Assert.Empty(none.Items);
        Assert.Equal(HubMarkers.NoneApprovedNote, none.Filter);
        Assert.Null(HubMarkers.ForDepots(CuratedStore.Empty()));
    }

    [Fact]
    public void DepotTooltipSaysWhatItIsAndWhereThePointCameFrom()
    {
        var d = CuratedTablesTests.DepotRow("unhrd-ok", "approved");
        Assert.Equal("UNHRD Test · UN Humanitarian Response Depot (WFP) · marked at Testville Airport (DXXX), beside the depot, so approximate · position: OurAirports (The Unlicense), 24 Nov 2026 · UNHRD contacts page, checked 24 Nov 2026",
            OneLine(HubMarkers.DepotTooltip(d)));
        Assert.StartsWith("Draft: not yet reviewed · UNHRD Test", HubMarkers.DepotTooltip(d with { Review = new Review(Review.Draft, null) }), StringComparison.Ordinal);
        // The real rows: all five, with their cited positions.
        var real = new CuratedStore(CuratedTables.Load(Path.Combine(AppContext.BaseDirectory, "data", "curated")), showDrafts: true);
        var set = HubMarkers.ForDepots(real)!;
        Assert.Equal(5, set.Items.Count);
        Assert.Equal(["GHA", "ITA", "ARE", "MYS", "PAN"], set.Items.Select(i => i.CountryIso3));
        Assert.All(set.Items, i => Assert.Contains("Draft: not yet reviewed", i.Tooltip, StringComparison.Ordinal));
        Assert.Equal("UNHRD (WFP)", set.Citation.Publisher);
        Assert.Equal(CitationDate.Checked, set.Citation.DateMeaning);
        Assert.Equal(new DateTime(2026, 11, 24, 0, 0, 0, DateTimeKind.Utc), set.Citation.AsOfUtc);
    }

    [Fact]
    public void LegendNamesTheSourceAndDateOrSaysNotLoaded()
    {
        var markers = new MarkerDataStore();
        var layers = new LayerStripViewModel(new MemorySettingsStore());
        layers.Show(EarthRelief3D.App.Presentation.MarkerLayer.HubLines, false);   // on by default (R3): its own legend line is tested in HubPanelTests
        var legend = new LegendViewModel(new CrisisStore(), layers, nowUtc: () => Now, markerData: markers);
        layers.Show(MarkerLayer.Airports, true);
        layers.Show(MarkerLayer.Ports, true);
        layers.Show(MarkerLayer.UnDepots, true);
        // Nothing loaded yet: each says so (R2).
        Assert.Equal(["un-depots", "airports", "ports"], legend.Entries.Select(e => e.Id));   // strip order
        Assert.All(legend.Entries, e => Assert.Equal(LayerCatalog.NotLoaded, e.AsOf));

        foreach (var (layer, load) in CommittedLoads()) markers.Replace(layer, HubMarkers.For(layer, load)!);
        markers.Replace(MarkerLayer.UnDepots, HubMarkers.ForDepots(Store(true, CuratedTablesTests.DepotRow("unhrd-ok", "approved")))!);
        var airports = Assert.Single(legend.Entries, e => e.Id == "airports");
        Assert.Equal("OurAirports, OurAirports airport data · as of 24 Nov 2026", airports.SourceLine);
        Assert.Equal("Airports: large airports with scheduled flights (the panel's nearest airport may be a medium one)", airports.Measures);
        // R20: about 1,150 large airports are drawn, not the 3,250 large and medium ones the nearest-airport search uses.
        Assert.InRange(markers.Items(MarkerLayer.Airports).Count, 1000, 1200);
        Assert.Contains("The Unlicense", airports.Note, StringComparison.Ordinal);
        var ports = Assert.Single(legend.Entries, e => e.Id == "ports");
        Assert.Equal("US National Geospatial-Intelligence Agency (NGA), World Port Index (Pub 150) · as of 5 Nov 2026", ports.SourceLine);
        Assert.Contains("Public domain (US government work, 17 U.S.C. 105)", ports.Note, StringComparison.Ordinal);
        var depots = Assert.Single(legend.Entries, e => e.Id == "un-depots");
        Assert.Contains("checked 24 Nov 2026", depots.SourceLine, StringComparison.Ordinal);
    }

    // ---------------------------------------------------------------- shapes and colours (P4-14, C8)

    [Fact]
    public void HubShapesAndColoursAreTheirOwn()
    {
        Assert.Equal(MarkerShape.Star, LayerStyle.Shape(MarkerLayer.UnDepots));
        Assert.Equal(MarkerShape.HexagonOutline, LayerStyle.Shape(MarkerLayer.Airports));
        Assert.Equal(MarkerShape.DiamondOutline, LayerStyle.Shape(MarkerLayer.Ports));
        Assert.Equal(("★", "⬡", "◇"), (LayerStyle.Symbol(MarkerLayer.UnDepots), LayerStyle.Symbol(MarkerLayer.Airports), LayerStyle.Symbol(MarkerLayer.Ports)));
        Assert.Equal(Color.FromRgb(0x4F, 0xC3, 0xF7), LayerStyle.SymbolColor(MarkerLayer.UnDepots));
        Assert.Equal(Color.FromRgb(0xE8, 0xE8, 0xE8), LayerStyle.SymbolColor(MarkerLayer.Airports));
        Assert.Equal(Color.FromRgb(0x80, 0xCB, 0xC4), LayerStyle.SymbolColor(MarkerLayer.Ports));
        Assert.Equal(["un-depots", "airports", "ports"], new[] { MarkerLayer.UnDepots, MarkerLayer.Airports, MarkerLayer.Ports }.Select(LayerCatalog.Id));
        Assert.Equal(["UN depots", "Airports", "Ports"], new[] { MarkerLayer.UnDepots, MarkerLayer.Airports, MarkerLayer.Ports }.Select(LayerCatalog.Label));
        Assert.Equal([MarkerLayer.UnDepots, MarkerLayer.Airports, MarkerLayer.Ports, MarkerLayer.HubLines], LayerCatalog.HelpMarkers);
        // The new members are appended at the end: saved settings serialise by name, and the scene order uses the number.
        Assert.Equal([MarkerLayer.UnDepots, MarkerLayer.Airports, MarkerLayer.Ports], Enum.GetValues<MarkerLayer>()[^4..^1]);   // then the hub lines (Task 5)
        Assert.Equal([MarkerShape.Star, MarkerShape.HexagonOutline, MarkerShape.DiamondOutline], Enum.GetValues<MarkerShape>()[^4..^1]);   // then the arc (Task 5)
        Assert.Equal(Enum.GetValues<MarkerLayer>().Length, Enum.GetValues<MarkerLayer>().Select(LayerCatalog.Id).Distinct().Count());
        // Round trip by name, as the settings file does.
        Assert.True(LayerCatalog.TryParse("un-depots", out _, out var m) && m == MarkerLayer.UnDepots);
        Assert.Contains("airports", LayerCatalog.AllNames);
        Assert.Contains("ports", LayerCatalog.AllNames);
    }
}
