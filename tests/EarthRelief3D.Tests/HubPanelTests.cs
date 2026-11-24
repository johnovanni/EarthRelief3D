using System.IO;
using System.Text.RegularExpressions;
using AstroCore.Earth;
using EarthRelief3D.App.Curated;
using EarthRelief3D.App.Layers;
using EarthRelief3D.App.Presentation;
using EarthRelief3D.App.Settings;
using EarthRelief3D.App.ViewModels;
using EarthRelief3D.Core.Curated;
using EarthRelief3D.Core.Feeds;
using EarthRelief3D.Core.Logic;
using EarthRelief3D.Core.Model;
using EarthRelief3D.Core.Store;

namespace EarthRelief3D.Tests;

/// <summary>Plan 4 Task 5: the nearest hubs in the Getting help in section, and the arcs that follow the panel (P4-6, P4-7, R3).</summary>
public class HubPanelTests
{
    private static readonly DateTime Now = AccessPanelTests.Now;
    private static readonly CountryResolver Countries = CountryResolver.LoadDefault();

    private static List<(MarkerLayer Layer, BundledPointsLoad Load)> Loads() =>
    [
        .. HubMarkers.Datasets.Select(d =>
        {
            var load = BundledPoints.Load(HubMarkers.DotName, Path.Combine(BundledDataset.DatasetsDir, d.Folder), iso => Countries.ByIso3(iso) is not null, new ListLog());
            Assert.True(load.Loaded, load.Problem);
            return (d.Layer, load);
        }),
    ];

    private static CuratedStore WithDepots(params Depot[] depots) => new(CuratedTablesTests.Tables() with { Depots = depots }, showDrafts: false);

    private static MainViewModel BuildMain(CuratedStore curated, MemorySettingsStore? settings = null, bool loadHubs = true, bool place = true)
    {
        var main = new MainViewModel(new CrisisStore(), settings ?? new MemorySettingsStore(), iso => Countries.ByIso3(iso)?.Name, null, () => Now,
            curated: curated, crisisPlace: place ? iso => Countries.ByIso3(iso)?.LabelPoint : null);
        if (loadHubs) main.Hubs.SetLoads(Loads());
        return main;
    }

    private static IReadOnlyList<(string Label, string Value)> HelpRows(MainViewModel main) =>
        Assert.Single(main.Panel.Sections, s => s.Heading == CountryPanelViewModel.GettingHelpHeading).Rows;

    private static string Row(IReadOnlyList<(string Label, string Value)> rows, string label) => Assert.Single(rows, r => r.Label == label).Value;

    [Fact]
    public void RowsSayStraightLine()
    {
        var main = BuildMain(WithDepots(CuratedTablesTests.DepotRow("unhrd-ok", "approved")));
        main.Panel.Show("SDN");
        var rows = HelpRows(main);

        // "<name> (<IATA>) · <d> km straight-line": an airport shows its IATA code, a port its UN/LOCODE, a depot no brackets.
        var airport = Row(rows, "Nearest airport with scheduled flights");
        Assert.Matches(@"^.+ \([A-Z]{3}\) · [\d,]+ km straight-line$", airport);
        Assert.Matches(@"^.+ \([A-Z]{2} ?[A-Z0-9]{3}\) · [\d,]+ km straight-line$", Row(rows, "Nearest large or medium port"));
        Assert.Matches(@"^UNHRD Test, Ghana · [\d,]+ km straight-line$", Row(rows, "Nearest UN depot"));   // a hub in another country names it

        // They are the nearest of the committed data, measured from the country's label point (P4-6), whole kilometres.
        var place = Countries.ByIso3("SDN")!.LabelPoint;
        var links = NearestHubFinder.Nearest(place, main.Hubs.Hubs);
        Assert.Equal([HubKind.UnDepot, HubKind.Airport, HubKind.Port], links.Select(l => l.Hub.Kind));
        var a = links.Single(l => l.Hub.Kind == HubKind.Airport);
        Assert.Equal($"{a.Hub.Name} ({a.Hub.Detail}) · {CrisisText.Distance(a.DistanceKm, DistanceUnit.Kilometres)} straight-line", airport);

        // The nearest airport and port of Sudan are Sudanese ones, in the order of the section (after the Logistics row).
        Assert.StartsWith("Dongola Airport (DOG) · ", airport);   // the committed data: Sudan's label point is in the north
        Assert.StartsWith("Port Sudan (SD PZU) · ", Row(rows, "Nearest large or medium port"));
        Assert.Equal("SDN", a.Hub.Iso3);
        Assert.Equal("SDN", links.Single(l => l.Hub.Kind == HubKind.Port).Hub.Iso3);
        var labels = rows.Select(r => r.Label).ToList();
        Assert.True(labels.IndexOf("Nearest UN depot") > labels.IndexOf("Logistics"));
        Assert.Equal(["Nearest UN depot", "Nearest airport with scheduled flights", "Nearest large or medium port"], labels.Where(l => l.StartsWith("Nearest", StringComparison.Ordinal)));
        // An approved depot's row and arc label carry no draft mark.
        Assert.DoesNotContain("Draft", Row(rows, "Nearest UN depot"), StringComparison.Ordinal);
        Assert.False(links.Single(l => l.Hub.Kind == HubKind.UnDepot).Hub.IsDraft);

        // Every fact shows its source and date: one row names the datasets and their days.
        var sources = Row(rows, "Hub data");
        Assert.Contains("UN depots: UNHRD contacts page, checked 24 Nov 2026", sources, StringComparison.Ordinal);
        Assert.Contains("Airports: OurAirports, as of 24 Nov 2026", sources, StringComparison.Ordinal);
        Assert.Contains("Ports: NGA World Port Index, as of 5 Nov 2026", sources, StringComparison.Ordinal);
    }

    [Fact]
    public void AHubInAnotherCountryNamesIt()
    {
        // Fiji has no large or medium port in the World Port Index: its nearest is Auckland, and the row says New Zealand.
        var main = BuildMain(WithDepots());
        main.Panel.Show("FJI");
        Assert.Matches(@"^Auckland, New Zealand \(NZ AKL\) · [\d,]+ km straight-line$", Row(HelpRows(main), "Nearest large or medium port"));
        // A hub in the panel's own country does not repeat it.
        Assert.DoesNotContain(", Fiji", Row(HelpRows(main), "Nearest airport with scheduled flights"), StringComparison.Ordinal);
    }

    [Fact]
    public void NoApprovedDepotSaysSo()
    {
        // All the real depots are drafts, so none is visible in a normal run (P4-13).
        var main = BuildMain(new CuratedStore(CuratedTables.Load(Path.Combine(AppContext.BaseDirectory, "data", "curated")), showDrafts: false));
        main.Panel.Show("SDN");
        var rows = HelpRows(main);
        Assert.Contains(("Nearest UN depot", "No UN depot confirmed yet"), rows);
        Assert.Matches(@"straight-line$", Row(rows, "Nearest airport with scheduled flights"));
        Assert.DoesNotContain("UN depots:", Row(rows, "Hub data"), StringComparison.Ordinal);
    }

    [Fact]
    public void WhenHubsAreNotLoadedOrThereIsNoPoint()
    {
        var notLoaded = BuildMain(WithDepots(), loadHubs: false);
        notLoaded.Panel.Show("SDN");
        var rows = HelpRows(notLoaded);
        Assert.Contains(("Nearest airport with scheduled flights", LayerCatalog.NotLoaded), rows);
        Assert.Contains(("Nearest large or medium port", LayerCatalog.NotLoaded), rows);

        // No label point for the country (P4-6): no distances, and the panel says so in place of the three rows.
        var nowhere = BuildMain(WithDepots(), place: false);
        nowhere.Panel.Show("SDN");
        rows = HelpRows(nowhere);
        Assert.Equal("No point to measure from", Row(rows, "Nearest hubs"));
        Assert.DoesNotContain(rows, r => r.Label.StartsWith("Nearest ", StringComparison.Ordinal) && r.Label != "Nearest hubs");
    }

    [Fact]
    public void UnitsSettingChangesRows()
    {
        var main = BuildMain(WithDepots(CuratedTablesTests.DepotRow("unhrd-ok", "approved")));
        main.Panel.Show("SDN");
        Assert.EndsWith(" km straight-line", Row(HelpRows(main), "Nearest airport with scheduled flights"));

        // Changing the unit rebuilds the open panel at once.
        main.Settings.Units = DistanceUnit.Miles;
        var miles = HelpRows(main);
        Assert.Matches(@"^.+ \([A-Z]{3}\) · [\d,]+ mi straight-line$", Row(miles, "Nearest airport with scheduled flights"));
        Assert.EndsWith(" mi straight-line", Row(miles, "Nearest UN depot"));
        Assert.EndsWith(" mi straight-line", Row(miles, "Nearest large or medium port"));
        main.Settings.Units = DistanceUnit.Kilometres;
        Assert.EndsWith(" km straight-line", Row(HelpRows(main), "Nearest airport with scheduled flights"));
    }

    [Fact]
    public void HubsFollowTheCuratedStore()
    {
        var curated = WithDepots(CuratedTablesTests.DepotRow("unhrd-draft"));
        var main = BuildMain(curated);
        main.Panel.Show("SDN");
        Assert.Equal("No UN depot confirmed yet", Row(HelpRows(main), "Nearest UN depot"));
        Assert.DoesNotContain(main.Hubs.Hubs, h => h.Kind == HubKind.UnDepot);

        // The owner approves the depot (here: the drafts are shown): the hubs list and the open panel follow.
        curated.SetShowDrafts(true);
        var depot = Assert.Single(main.Hubs.Hubs, h => h.Kind == HubKind.UnDepot);
        Assert.Equal("UNHRD Test", depot.Name);
        Assert.Equal(new GeoPosition(5.6, -0.17), (GeoPosition)depot.Position);
        Assert.Equal("", depot.Detail);
        Assert.Equal(CitationDate.Checked, depot.Citation.DateMeaning);
        // A draft shown in a --show-drafts run says so on its row and its arc label.
        Assert.Matches(@"^Draft: not yet reviewed · UNHRD Test, Ghana · [\d,]+ km straight-line$", Row(HelpRows(main), "Nearest UN depot"));
        Assert.True(depot.IsDraft);
        var arc = NearestHubFinder.Nearest(Countries.ByIso3("SDN")!.LabelPoint, main.Hubs.Hubs).Single(l => l.Hub.Kind == HubKind.UnDepot);
        Assert.Matches(@"^UNHRD Test \(draft\) · [\d,]+ km straight-line$", HubLabelsLayer.LabelText(arc, DistanceUnit.Kilometres));
    }

    [Fact]
    public void EveryHubCarriesItsSource()
    {
        var hubs = HubMarkers.Hubs(Loads(), [CuratedTablesTests.DepotRow("unhrd-ok", "approved")]);
        Assert.Equal(1, hubs.Count(h => h.Kind == HubKind.UnDepot));
        Assert.True(hubs.Count(h => h.Kind == HubKind.Airport) > 1000);
        Assert.True(hubs.Count(h => h.Kind == HubKind.Port) > 300);
        Assert.All(hubs, h => Assert.False(string.IsNullOrWhiteSpace(h.Name)));
        Assert.All(hubs, h => Assert.StartsWith("https://", h.Citation.Url.ToString(), StringComparison.Ordinal));
        Assert.Equal("OurAirports", hubs.First(h => h.Kind == HubKind.Airport).Citation.Publisher);
        Assert.Contains("World Port Index", hubs.First(h => h.Kind == HubKind.Port).Citation.Dataset, StringComparison.Ordinal);
        // A load that did not load adds none.
        var broken = Loads().Select(l => l.Layer == MarkerLayer.Ports ? (l.Layer, new BundledPointsLoad("Hubs", "x", null, null, [], [], "not found")) : l).ToList();
        Assert.DoesNotContain(HubMarkers.Hubs(broken, []), h => h.Kind == HubKind.Port);
    }

    // ---------------------------------------------------------------- arcs follow the panel

    [Fact]
    public void ArcsOnlyWhilePanelIsOpenAndTheToggleIsOn()
    {
        var main = BuildMain(WithDepots(CuratedTablesTests.DepotRow("unhrd-ok", "approved")));
        Func<string, GeoPosition?> place = iso => Countries.ByIso3(iso)?.LabelPoint;
        IReadOnlyList<HubArcSpec> Arcs() => HubArcLayer.SpecsFor(main.Panel.IsOpen, main.Layers.IsShown(MarkerLayer.HubLines), main.Panel.Iso3, place, main.Hubs.Hubs);

        Assert.True(main.Layers.IsShown(MarkerLayer.HubLines));   // on by default (R3)
        Assert.Empty(Arcs());                                      // no panel yet
        main.Panel.Show("SDN");
        var arcs = Arcs();
        Assert.Equal([HubKind.UnDepot, HubKind.Airport, HubKind.Port], arcs.Select(a => a.Link.Hub.Kind));
        Assert.All(arcs, a => Assert.Equal((SurfacePointComparer)Countries.ByIso3("SDN")!.LabelPoint, (SurfacePointComparer)a.From));
        main.Panel.Show("FJI");
        Assert.Equal(3, Arcs().Count);                             // follows the country
        Assert.NotEqual(arcs[1].Link.Hub.Name, Arcs()[1].Link.Hub.Name);
        main.Layers.Show(MarkerLayer.HubLines, false);
        Assert.Empty(Arcs());
        main.Layers.Show(MarkerLayer.HubLines, true);
        Assert.Equal(3, Arcs().Count);
        main.Panel.CloseCommand.Execute(null);
        Assert.Empty(Arcs());
        // No point to measure from: no arcs.
        Assert.Empty(HubArcLayer.SpecsFor(true, true, "XXX", _ => null, main.Hubs.Hubs));
    }

    [Fact]
    public void ArcLabelsSayStraightLine()
    {
        var hub = new Hub(HubKind.Airport, "Port Sudan New International Airport", "SDN", new GlobeCore.Bodies.SurfacePoint(19.43, 37.23), "PZU",
            new SourceCitation("OurAirports", "x", null, new Uri("https://ourairports.com"), "Unlicense", "x", Now, Now));
        Assert.Equal("Port Sudan New International Airport · 812 km straight-line", HubLabelsLayer.LabelText(new HubLink(hub, 812.4), DistanceUnit.Kilometres));
        Assert.Equal("Port Sudan New International Airport · 505 mi straight-line", HubLabelsLayer.LabelText(new HubLink(hub, 812.4), DistanceUnit.Miles));
    }

    [Fact]
    public void ToggleIsDefaultOnAndInTheGroup()
    {
        Assert.Equal(MarkerShape.Arc, LayerStyle.Shape(MarkerLayer.HubLines));
        Assert.Equal("⌒", LayerStyle.Symbol(MarkerLayer.HubLines));
        Assert.Equal("hub-lines", LayerCatalog.Id(MarkerLayer.HubLines));
        Assert.Equal("Lines to nearest hubs", LayerCatalog.Label(MarkerLayer.HubLines));
        Assert.Contains("Straight-line arcs from the selected country to its nearest UN depot, airport and port", LayerCatalog.Measures(MarkerLayer.HubLines), StringComparison.OrdinalIgnoreCase);
        // A toggle only: no point layer draws it.
        Assert.DoesNotContain(MarkerLayer.HubLines, PointMarkerLayer.Layers);
        // Appended at the end of both enums, and after Ports in the group.
        Assert.Equal(MarkerLayer.HubLines, Enum.GetValues<MarkerLayer>()[^1]);
        Assert.Equal(MarkerShape.Arc, Enum.GetValues<MarkerShape>()[^1]);
        Assert.Equal(["un-depots", "airports", "ports", "hub-lines"], LayerCatalog.HelpMarkers.Select(LayerCatalog.Id));
        // On in a fresh settings file; a file that lists markers keeps its own list; a Plan 2 file (flags, no list) gets it on.
        Assert.Contains(MarkerLayer.HubLines, new AppSettings().EffectiveMarkers);
        Assert.Contains(MarkerLayer.HubLines, LayerCatalog.DefaultMarkers);
        Assert.DoesNotContain(MarkerLayer.HubLines, new AppSettings { Markers = [MarkerLayer.Conflict] }.EffectiveMarkers);
        Assert.Contains(MarkerLayer.HubLines, new AppSettings { Markers = [MarkerLayer.Conflict, MarkerLayer.HubLines] }.EffectiveMarkers);
    }

    private readonly record struct SurfacePointComparer(double Lat, double Lon)
    {
        public static implicit operator SurfacePointComparer(GeoPosition p) => new(p.LatitudeDeg, p.LongitudeDeg);
        public static implicit operator SurfacePointComparer(GlobeCore.Bodies.SurfacePoint p) => new(p.LatitudeDeg, p.LongitudeDeg);
    }
}
