using System.IO;
using System.Windows.Media;
using EarthRelief3D.App.Layers;
using EarthRelief3D.App.Presentation;
using EarthRelief3D.App.Services;
using EarthRelief3D.App.Settings;
using EarthRelief3D.App.ViewModels;
using EarthRelief3D.Core.Feeds;
using EarthRelief3D.Core.Model;
using EarthRelief3D.Core.Store;
using GlobeCore.Feeds;

namespace EarthRelief3D.Tests;

/// <summary>Task 7: the GEM power, oil and gas, and steel marker layers (Resources): tooltips, colours, sizes, legend, loading.</summary>
public class ResourceMarkersTests
{
    private static readonly DateTime Now = new(2026, 11, 24, 12, 0, 0, DateTimeKind.Utc);

    private static SourceCitation Release(string dataset, string release) =>
        new("Global Energy Monitor", dataset, release, new Uri("https://globalenergymonitor.org/"), "CC BY 4.0", $"Global Energy Monitor, {dataset}, {release}.",
            new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 11, 24, 0, 0, 0, DateTimeKind.Utc), CitationDate.Release) { DateText = release };

    private static readonly SourceCitation PowerCite = Release("Global Integrated Power Tracker", "September 2026 release (file V2)");
    private static readonly SourceCitation OilCite = Release("Global Oil and Gas Extraction Tracker", "March 2026 release");
    private static readonly SourceCitation SteelCite = Release("Global Iron and Steel Tracker", "September 2026 (V1) release");

    /// <summary>The tooltip as one line (long ones are wrapped for the screen).</summary>
    private static string OneLine(string tip) => tip.Replace(Environment.NewLine + "    ", " ", StringComparison.Ordinal);

    /// <summary>The committed extract of a layer, loaded as the app loads it.</summary>
    private static BundledPointsLoad Committed(MarkerLayer layer)
    {
        var folder = ResourceMarkers.Datasets.Single(d => d.Layer == layer).Folder;
        var load = BundledPoints.Load(ResourceMarkers.DotName, Path.Combine(BundledDataset.DatasetsDir, folder), iso => CountryResolver.LoadDefault().ByIso3(iso) is not null, new ListLog());
        Assert.True(load.Loaded, load.Problem);
        return load;
    }

    [Fact]
    public void TooltipNamesPlantCapacityStatusAndRelease()
    {
        var plant = new MarkerPoint("Taichung power station", "TWN", 24.2131, 120.485, 5780, "MW", "operating", "coal");
        Assert.Equal("Taichung power station · coal · 5,780 MW · operating · GEM Global Integrated Power Tracker, September 2026 release (file V2)",
            OneLine(ResourceMarkers.Tooltip(MarkerLayer.PowerPlants, plant, PowerCite)));
        var small = plant with { Name = "Kosovo B power station", Capacity = 680 };
        Assert.Contains(" · 680 MW · ", ResourceMarkers.Tooltip(MarkerLayer.PowerPlants, small, PowerCite));

        var field = new MarkerPoint("Rumaila Oil Field (Iraq)", "IRQ", 30.59, 47.35, 518.3, "million boe/y", "operating", "oil", "oil 518 million bbl/y (2022)");
        Assert.Equal("Rumaila Oil Field (Iraq) · oil · production: oil 518 million bbl/y (2022) · operating · GEM Global Oil and Gas Extraction Tracker, March 2026 release",
            OneLine(ResourceMarkers.Tooltip(MarkerLayer.OilGasFields, field, OilCite)));
        var unmeasured = field with { Capacity = null, Unit = "", Detail = "" };
        Assert.Contains(" · no production figure in GEM's field data · ", OneLine(ResourceMarkers.Tooltip(MarkerLayer.OilGasFields, unmeasured, OilCite)));

        var steel = new MarkerPoint("POSCO Gwangyang steel plant", "KOR", 34.92, 127.75, 22.999, "Mt/yr", "operating", "BF; BOF");
        Assert.Equal("POSCO Gwangyang steel plant · blast furnace, basic oxygen furnace · 23 million tonnes of crude steel a year · operating · "
            + "GEM Global Iron and Steel Tracker, September 2026 (V1) release", OneLine(ResourceMarkers.Tooltip(MarkerLayer.SteelPlants, steel, SteelCite)));
        var taranto = steel with { Name = "Acciaierie d'Italia Taranto steel plant", Capacity = 7.8, Status = "operating pre-retirement" };
        Assert.Contains(" · 7.8 million tonnes of crude steel a year · operating pre-retirement · ", OneLine(ResourceMarkers.Tooltip(MarkerLayer.SteelPlants, taranto, SteelCite)));
    }

    [Fact]
    public void AMultiFuelPlantListsEachFuelAndTakesItsLargestFuelsColour()
    {
        // Ruling R24: the tooltip names every fuel's MW, as the extract's types column has them; the colour is the largest's.
        var synthetic = new MarkerPoint("Taichung power station", "TWN", 24.2131, 120.485, 5780, "MW", "operating", "coal", Types: "coal 5500 MW; oil/gas 280 MW");
        Assert.Equal("Taichung power station · coal 5,500 MW, oil/gas 280 MW · 5,780 MW · operating · GEM Global Integrated Power Tracker, "
            + "September 2026 release (file V2)", OneLine(ResourceMarkers.Tooltip(MarkerLayer.PowerPlants, synthetic, PowerCite)));
        Assert.Equal("bioenergy 2,580 MW, oil/gas 96.5 MW", ResourceMarkers.FuelsText("bioenergy 2580 MW; oil/gas 96.5 MW"));

        // The committed extract: Taichung is read with its types column (checked against the raw file: coal 5,500 MW and
        // oil/gas 280 MW, 14 units) and drawn in the fossil colour.
        var load = Committed(MarkerLayer.PowerPlants);
        var taichung = Assert.Single(load.Points, p => p.Name == "Taichung power station");
        Assert.Equal(("coal", 5780.0, "coal 5500 MW; oil/gas 280 MW"), (taichung.Type, taichung.Capacity!.Value, taichung.Types));
        var item = Assert.Single(ResourceMarkers.For(MarkerLayer.PowerPlants, load)!.Items, i => i.Tooltip.StartsWith("Taichung power station ·", StringComparison.Ordinal));
        Assert.StartsWith("Taichung power station · coal 5,500 MW, oil/gas 280 MW · 5,780 MW · operating · ", OneLine(item.Tooltip));
        Assert.Equal(ResourceMarkers.FuelGroupColor(FuelGroup.Fossil), item.Color);
        // A one-fuel plant names its fuel alone.
        Assert.Equal("", Assert.Single(load.Points, p => p.Name == "Bruce nuclear power plant").Types);
    }

    [Fact]
    public void PowerPlantsAreColouredByFuelGroup()
    {
        Assert.Equal(FuelGroup.Fossil, ResourceMarkers.FuelGroupOf("coal"));
        Assert.Equal(FuelGroup.Fossil, ResourceMarkers.FuelGroupOf("oil/gas"));
        Assert.Equal(FuelGroup.Nuclear, ResourceMarkers.FuelGroupOf("nuclear"));
        Assert.Equal(FuelGroup.Hydro, ResourceMarkers.FuelGroupOf("hydropower"));
        Assert.Equal(FuelGroup.WindOrSolar, ResourceMarkers.FuelGroupOf("wind"));
        Assert.Equal(FuelGroup.WindOrSolar, ResourceMarkers.FuelGroupOf("utility-scale solar"));
        Assert.Equal(FuelGroup.Other, ResourceMarkers.FuelGroupOf("bioenergy"));
        Assert.Equal(FuelGroup.Other, ResourceMarkers.FuelGroupOf("geothermal"));
        var groups = Enum.GetValues<FuelGroup>();
        Assert.Equal(groups.Length, groups.Select(ResourceMarkers.FuelGroupColor).Distinct().Count());
        // Each colour differs from the dark edge and from the other Resources layers' colours (C8: colour plus shape).
        Assert.DoesNotContain(groups, g => ResourceMarkers.FuelGroupColor(g) == LayerStyle.MarkerEdgeColor
            || ResourceMarkers.FuelGroupColor(g) == LayerStyle.OilGasColor || ResourceMarkers.FuelGroupColor(g) == LayerStyle.SteelColor);

        var set = ResourceMarkers.For(MarkerLayer.PowerPlants, Committed(MarkerLayer.PowerPlants))!;
        var gorges = Assert.Single(set.Items, i => i.Tooltip.StartsWith("Three Gorges Dam hydroelectric plant ·", StringComparison.Ordinal));
        Assert.Equal(ResourceMarkers.FuelGroupColor(FuelGroup.Hydro), gorges.Color);
        var bruce = Assert.Single(set.Items, i => i.Tooltip.StartsWith("Bruce nuclear power plant ·", StringComparison.Ordinal));
        Assert.Equal(ResourceMarkers.FuelGroupColor(FuelGroup.Nuclear), bruce.Color);
        Assert.Equal("CAN", bruce.CountryIso3);
        // The other layers use their layer colour.
        Assert.All(ResourceMarkers.For(MarkerLayer.SteelPlants, Committed(MarkerLayer.SteelPlants))!.Items, i => Assert.Null(i.Color));
    }

    [Fact]
    public void SizeGrowsWithCapacityClass()
    {
        double Power(double mw) => ResourceMarkers.SizeOf(MarkerLayer.PowerPlants, mw);
        Assert.True(Power(680) < Power(1356));
        Assert.True(Power(1356) < Power(2676));
        Assert.True(Power(2676) < Power(22500));
        Assert.Equal(Power(5000), Power(22500));   // the top class
        double Steel(double mt) => ResourceMarkers.SizeOf(MarkerLayer.SteelPlants, mt);
        Assert.True(Steel(1) < Steel(3) && Steel(3) < Steel(23));
        // Oil and gas fields are one size: their production years and units differ, and many have no figure.
        Assert.Equal(ResourceMarkers.SizeOf(MarkerLayer.OilGasFields, null), ResourceMarkers.SizeOf(MarkerLayer.OilGasFields, 518));
        Assert.Empty(ResourceMarkers.SizeClasses(MarkerLayer.OilGasFields));
        Assert.Equal(["under 1,000 MW", "1,000–2,499 MW", "2,500–4,999 MW", "5,000 MW or more"], ResourceMarkers.SizeClasses(MarkerLayer.PowerPlants).Select(c => c.Label));
    }

    [Fact]
    public void LegendNamesFilterFuelsSizesAndCreditsGem()
    {
        var markers = new MarkerDataStore();
        var layers = new LayerStripViewModel(new MemorySettingsStore());
        var legend = new LegendViewModel(new CrisisStore(), layers, nowUtc: () => Now, markerData: markers);
        foreach (var (layer, _) in ResourceMarkers.Datasets)
        {
            markers.Replace(layer, ResourceMarkers.For(layer, Committed(layer))!);
            layers.Show(layer, true);
        }
        Assert.Equal(["power", "renewables", "oilgas", "steel"], legend.Entries.Select(e => e.Id));
        var power = legend.Entries[0];
        Assert.Equal("Power plants: operating plants of 500 MW or more (units summed per plant), coloured by its largest fuel", power.Measures);
        Assert.Equal("Global Energy Monitor, Global Integrated Power Tracker · September 2026 release (file V2)", power.SourceLine);
        // One key per fuel group, then the size classes.
        Assert.Equal(["fossil (coal, oil, gas)", "nuclear", "hydro", "wind or solar", "other (bioenergy, geothermal)",
            "under 1,000 MW", "1,000–2,499 MW", "2,500–4,999 MW", "5,000 MW or more"], power.Keys.Select(k => k.Label));
        Assert.True(power.Keys[5].Swatch < power.Keys[8].Swatch);
        // CC BY 4.0: GEM's citation, the licence's link, and what was changed.
        Assert.StartsWith("Global Energy Monitor, Global Integrated Power Tracker, September 2026 release. Licence:", power.Note);
        Assert.Contains("CC BY 4.0, https://creativecommons.org/licenses/by/4.0/", power.Note);
        Assert.Contains("Changed: filtered to operating plants of 500 MW or more", power.Note);

        var oil = legend.Entries[2];
        Assert.StartsWith("Oil and gas fields: operating fields with coordinates", oil.Measures);
        Assert.StartsWith("Global Energy Monitor, Global Oil and Gas Extraction Tracker, March 2026 release. Licence:", oil.Note);
        Assert.Empty(oil.Keys);
        var steel = legend.Entries[3];
        Assert.Equal("Global Energy Monitor, Global Iron and Steel Tracker · September 2026 (V1) release", steel.SourceLine);
        Assert.Equal(["1–2.9 million tonnes a year", "3–9.9 million tonnes a year", "10 million tonnes a year or more"], steel.Keys.Select(k => k.Label));
        Assert.All(steel.Keys, k => Assert.True(k.IsOutline));   // drawn as the layer's square outline
    }

    [Fact]
    public void CommittedExtractsAreUnderTheCapWithHandCheckedSites()
    {
        var power = Committed(MarkerLayer.PowerPlants);
        Assert.InRange(power.Points.Count, 1000, PointMarkerLayer.MaxMarkers);
        Assert.Equal("operating plants of 500 MW or more (units summed per plant)", power.Manifest!.Filter);
        // Checked against the raw file (Task 7 report): Three Gorges 22,500 MW, Bruce 6,944 MW (8 units), Kosovo B 680 MW (2 × 340).
        Assert.Equal((22500.0, "CHN", "hydropower"), Pick(power, "Three Gorges Dam hydroelectric plant"));
        Assert.Equal((6944.0, "CAN", "nuclear"), Pick(power, "Bruce nuclear power plant"));
        Assert.Equal((680.0, "KOS", "coal"), Pick(power, "Kosovo B power station"));

        var oil = Committed(MarkerLayer.OilGasFields);
        Assert.InRange(oil.Points.Count, 1000, PointMarkerLayer.MaxMarkers);
        var rumaila = Assert.Single(oil.Points, p => p.Name == "Rumaila Oil Field (Iraq)");
        Assert.Equal(("IRQ", "oil 518 million bbl/y (2022)"), (rumaila.Iso3, rumaila.Detail));

        var steel = Committed(MarkerLayer.SteelPlants);
        Assert.InRange(steel.Points.Count, 100, PointMarkerLayer.MaxMarkers);
        Assert.Equal((22.999, "KOR", "BF; BOF"), Pick(steel, "POSCO Gwangyang steel plant"));
        Assert.Equal("operating pre-retirement", Assert.Single(steel.Points, p => p.Name == "Acciaierie d'Italia Taranto steel plant").Status);

        foreach (var load in new[] { power, oil, steel })
        {
            Assert.Equal("CC BY 4.0", load.Manifest!.Licence);
            Assert.Equal(CitationDate.Release, load.Citation!.DateMeaning);
            Assert.False(string.IsNullOrWhiteSpace(load.Manifest.Changes));
            Assert.All(load.Points, p => Assert.Equal(p.Iso3, CountryResolver.LoadDefault().ByIso3(p.Iso3)?.Iso3));
        }
    }

    private static (double, string, string) Pick(BundledPointsLoad load, string name)
    {
        var p = Assert.Single(load.Points, p => p.Name == name);
        return (p.Capacity!.Value, p.Iso3, p.Type);
    }

    [Fact]
    public void TheAppLoadsTheThreeLayersWithOneStatusDot()
    {
        var log = new ListLog();
        var loads = ResourceMarkers.Datasets.Select(d => (d.Layer, BundledPoints.Load(ResourceMarkers.DotName,
            Path.Combine(BundledDataset.DatasetsDir, d.Folder), null, log))).ToList();
        var markers = new MarkerDataStore();
        var store = new CrisisStore();
        AppServices.ApplyBundledPoints(markers, store, loads);
        Assert.All(ResourceMarkers.Datasets, d => Assert.NotEmpty(markers.Items(d.Layer)));
        var dot = ResourceMarkers.Status(loads);
        Assert.Equal((ResourceMarkers.DotName, FeedState.Ok), (dot.Name, dot.State));
        Assert.Equal("power September 2026 release (file V2); renewables September 2026 release (file V2); oil and gas March 2026 release; steel September 2026 (V1) release · " + BundledDataset.StatusNote, dot.Note);
        Assert.Contains(ResourceMarkers.DotName, AppServices.StatusDotNames);

        // A missing extract leaves its layer "Data not loaded yet" (R2) and turns the dot amber, saying which.
        using var dir = new TempDir();
        var broken = loads.Select(l => l.Layer == MarkerLayer.SteelPlants ? (l.Layer, BundledPoints.Load(ResourceMarkers.DotName, dir.File("gem-steel"), null, log)) : l).ToList();
        var fresh = new MarkerDataStore();
        AppServices.ApplyBundledPoints(fresh, store, broken);
        Assert.Null(fresh.Get(MarkerLayer.SteelPlants));
        Assert.NotNull(fresh.Get(MarkerLayer.PowerPlants));
        var amber = ResourceMarkers.Status(broken);
        Assert.Equal(FeedState.Attention, amber.State);
        Assert.Contains("steel not loaded (not found)", amber.Note);
    }

    // ---------------------------------------------------------------- renewables (ruling R25)

    [Fact]
    public void ColourIsTheLargestFuelsEvenWhenAnotherIsListedFirst()
    {
        // R24 re-review: the colour must follow the plant's largest fuel (the extract's type), not the first fuel listed.
        var mixed = new MarkerPoint("Test station", "MLT", 35.8, 14.6, 547.8, "MW", "operating", "oil/gas", Types: "hydropower 75 MW; oil/gas 472.8 MW");
        Assert.Equal(ResourceMarkers.FuelGroupColor(FuelGroup.Fossil), ResourceMarkers.ColorOf(MarkerLayer.PowerPlants, mixed));
        Assert.NotEqual(ResourceMarkers.FuelGroupOf("hydropower"), ResourceMarkers.FuelGroupOf("oil/gas"));
        Assert.Equal(ResourceMarkers.FuelGroupColor(FuelGroup.Fossil), ResourceMarkers.ColorOf(MarkerLayer.Renewables, mixed with { Type = "coal" }));
        Assert.Equal(ResourceMarkers.FuelGroupColor(FuelGroup.Hydro), ResourceMarkers.ColorOf(MarkerLayer.Renewables, mixed with { Type = "hydropower" }));
        Assert.Null(ResourceMarkers.ColorOf(MarkerLayer.SteelPlants, mixed));
    }

    [Fact]
    public void RenewablesHaveTheirOwnHexagonGlyph() => StaAsync.Sta(() =>
    {
        Assert.Equal(MarkerShape.Hexagon, LayerStyle.Shape(MarkerLayer.Renewables));
        Assert.Equal("⬢", LayerStyle.Symbol(MarkerLayer.Renewables));
        // C8: no other layer has this shape or symbol (LayerStyleTests checks every layer's are distinct).
        Assert.Single(Enum.GetValues<MarkerLayer>(), l => LayerStyle.Shape(l) == MarkerShape.Hexagon);
        Assert.Equal("renewables", LayerCatalog.Id(MarkerLayer.Renewables));
        Assert.Equal([MarkerLayer.PowerPlants, MarkerLayer.Renewables, MarkerLayer.OilGasFields, MarkerLayer.SteelPlants], LayerCatalog.ResourceMarkers);
        Assert.Contains(MarkerLayer.Renewables, PointMarkerLayer.Layers);

        // Drawn as a filled hexagon in the fuel's colour inside a dark hexagonal edge.
        var item = new PointItem(new AstroCore.Earth.GeoPosition(0, 0), 5, "tip", null, ResourceMarkers.FuelGroupColor(FuelGroup.Hydro));
        var layer = new PointMarkerLayer(MarkerLayer.Renewables, () => [item], new ListLog());
        var camera = new System.Windows.Media.Media3D.PerspectiveCamera(new System.Windows.Media.Media3D.Point3D(3, 0, 0),
            new System.Windows.Media.Media3D.Vector3D(-3, 0, 0), new System.Windows.Media.Media3D.Vector3D(0, 0, 1), 45);
        layer.Update(new GlobeCore.Wpf.Layers.SceneContext(camera, new GlobeCore.Wpf.Rendering.Projector(camera, 800, 600), TestData.T));
        var group = Assert.IsType<System.Windows.Media.Media3D.Model3DGroup>(layer.Root.Content);
        Assert.Equal(2, group.Children.Count);   // the edge, then the fill
        var fill = (System.Windows.Media.Media3D.MeshGeometry3D)((System.Windows.Media.Media3D.GeometryModel3D)group.Children[1]).Geometry;
        Assert.Equal(7, fill.Positions.Count);   // a centre and six corners
        Assert.Equal(18, fill.TriangleIndices.Count);
    });

    [Fact]
    public void RenewablesTooltipFollowsThePowerLayer()
    {
        var farm = new MarkerPoint("Bajgora wind farm", "KOS", 42.98, 21.02, 103, "MW", "operating", "wind");
        Assert.Equal("Bajgora wind farm · wind · 103 MW · operating · GEM Global Integrated Power Tracker, September 2026 release (file V2)",
            OneLine(ResourceMarkers.Tooltip(MarkerLayer.Renewables, farm, PowerCite)));
        var mixed = farm with { Name = "Mixed park", Capacity = 450, Type = "utility-scale solar", Types = "utility-scale solar 300 MW; wind 150 MW" };
        Assert.StartsWith("Mixed park · utility-scale solar 300 MW, wind 150 MW · 450 MW · operating · ",
            OneLine(ResourceMarkers.Tooltip(MarkerLayer.Renewables, mixed, PowerCite)));
    }

    [Fact]
    public void RenewablesLegendNamesThresholdFuelsPresentOverlapAndCredit()
    {
        var markers = new MarkerDataStore();
        var layers = new LayerStripViewModel(new MemorySettingsStore());
        var legend = new LegendViewModel(new CrisisStore(), layers, nowUtc: () => Now, markerData: markers);
        var load = Committed(MarkerLayer.Renewables);
        markers.Replace(MarkerLayer.Renewables, ResourceMarkers.For(MarkerLayer.Renewables, load)!);
        layers.Show(MarkerLayer.Renewables, true);
        var e = Assert.Single(legend.Entries);
        Assert.Equal("renewables", e.Id);
        Assert.Equal($"Renewable power plants: {load.Manifest!.Filter}, coloured by its largest fuel", e.Measures);
        Assert.Matches(@"^operating renewable plants of \d+ MW or more \(renewable units summed per plant\)$", load.Manifest.Filter);
        // Only the fuel groups present (no fossil, no nuclear), then the size classes.
        Assert.Equal(["hydro", "wind or solar", "other (bioenergy, geothermal)", "under 500 MW", "500–999 MW", "1,000–4,999 MW", "5,000 MW or more"],
            e.Keys.Select(k => k.Label));
        Assert.StartsWith(ResourceMarkers.OverlapNote + " Global Energy Monitor, Global Integrated Power Tracker, September 2026 release. Licence: CC BY 4.0, ", e.Note);
        Assert.Contains("Changed: filtered to operating renewable plants of", e.Note);
        Assert.Equal("Global Energy Monitor, Global Integrated Power Tracker · September 2026 release (file V2)", e.SourceLine);
    }

    [Fact]
    public void CommittedRenewablesExtractIsUnderTheCapWithHandCheckedPlants()
    {
        var load = Committed(MarkerLayer.Renewables);
        Assert.InRange(load.Points.Count, 1000, PointMarkerLayer.MaxMarkers);
        Assert.All(load.Points, p => Assert.Contains(p.Type, new[] { "wind", "utility-scale solar", "hydropower", "geothermal", "bioenergy" }));
        Assert.Equal((22500.0, "CHN", "hydropower"), Pick(load, "Three Gorges Dam hydroelectric plant"));
        Assert.DoesNotContain(load.Points, p => p.Name == "Bruce nuclear power plant" || p.Name == "Taichung power station");
        // The thresholds tried, in order, with their counts.
        Assert.Equal("100 MW", load.Manifest!.Thresholds!.Keys.First());
        Assert.True(load.Manifest.Thresholds.Values.Last() <= PointMarkerLayer.MaxMarkers);
    }
}