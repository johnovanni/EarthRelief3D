using System.Windows.Media;
using System.Windows.Media.Media3D;
using AstroCore.Earth;
using EarthRelief3D.App.Layers;
using EarthRelief3D.App.Presentation;
using EarthRelief3D.App.Settings;
using EarthRelief3D.App.ViewModels;
using EarthRelief3D.Core.Feeds;
using EarthRelief3D.Core.Model;
using EarthRelief3D.Core.Store;
using GlobeCore.Geo;
using GlobeCore.Wpf.Rendering;

namespace EarthRelief3D.Tests;

/// <summary>P3-2 on the map and in words: a country with matching IPC areas is drawn by area, in IPC's own area classification.</summary>
public class FoodAreaMapTests
{
    private static readonly DateTime Now = IpcAreaFixture.Now;
    private static readonly DateOnly From = IpcAreaFixture.From, To = IpcAreaFixture.To;

    private static readonly SourceCitation AreaCite = new(IpcNationalReader.Publisher, IpcAreaReader.Dataset, "Mar 2026",
        new Uri("https://data.humdata.org/dataset/" + IpcAreaFixture.PackageName), "CC0", IpcAreaReader.Attribution,
        To.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc), Now);

    private static GeoPolygon Square(double south, double west, double size) => new(
        [[new GeoPosition(south + size, west), new GeoPosition(south + size, west + size), new GeoPosition(south, west + size), new GeoPosition(south, west)]]);

    private static IpcArea Area(string name, int? phase, double south, double west, double size = 2) =>
        new("NGA", name, phase, 100_000, [Square(south, west, size)], "1", From, To, AreaCite);

    // Synthetic areas inside Nigeria (4.3–13.9°N, 2.7–14.7°E), big enough to test pixels and picking.
    private static IpcAreaSet Set(params IpcArea[] areas) => new("NGA", "1", "Mar 2026", From, To, areas, AreaCite);

    private static IpcAreaSet Squares() => Set(
        Area("Alpha", 3, 8, 4), Area("Bravo", 4, 8, 6), Area("Charlie", 5, 8, 8), Area("Delta", 2, 10, 4), Area("Echo", null, 10, 6));

    private static CrisisSnapshot Snapshot(IpcAreaSet set, params Crisis[] others) =>
        new CrisisSnapshot([IpcAreaFixture.National(), .. others], [], []) { IpcAreas = [set] };

    [Fact]
    public void CountryWithAreasIsNotShadedByTheRule() => StaAsync.Sta(() =>
    {
        var set = Squares();
        var sudan = FoodMapTests.Sudan();
        var plan = FoodImageLayer.Plan(Snapshot(set, sudan));
        // Nigeria: no country fill, outline or tint; its areas instead. Sudan (no areas): R17 as before.
        Assert.Equal([new FoodArea("NGA", null, null, null, set), new FoodArea("SDN", LayerStyle.IpcPhaseColor(3), LayerStyle.IpcPhaseColor(5))], plan);
        // By the rule, Nigeria's national record alone would be drawn (an outline at least).
        Assert.False(IpcAreaRule.MapStyle(IpcAreaFixture.National()).IsEmpty);

        // Areas of another period, or a stale national record, are not used: the rule (or nothing) as before.
        var other = set with { From = new DateOnly(2026, 2, 1), To = new DateOnly(2026, 5, 31) };
        Assert.Equal(FoodImageLayer.Plan([IpcAreaFixture.National()]), FoodImageLayer.Plan(Snapshot(other)));
        Assert.Empty(FoodImageLayer.Plan(new CrisisSnapshot([IpcAreaFixture.National() with { IsStale = true }], [], []) { IpcAreas = [set] }));

        // Drawn: each Phase 3–5 area filled in its phase colour, with a 1 px edge in it; Phase 2 and unclassified areas not at all.
        const int w = 3600, h = 1800;   // 10 px per degree
        var image = FoodImageLayer.Rasterize(plan, CountryResolver.LoadDefault().Index, w, h);
        byte[] Px(double lat, double lon)
        {
            int x = (int)((lon + 180) / 360 * w), y = (int)((90 - lat) / 180 * h);
            return image.Pixels.AsSpan((y * w + x) * 4, 4).ToArray();
        }
        foreach (var (lon, phase) in new[] { (5.0, 3), (7.0, 4), (9.0, 5) })
        {
            var c = LayerStyle.IpcPhaseColor(phase);
            var inside = Px(9, lon);
            Assert.InRange(inside[0], c.B - 2, c.B + 2);
            Assert.InRange(inside[1], c.G - 2, c.G + 2);
            Assert.InRange(inside[2], c.R - 2, c.R + 2);
            Assert.InRange(inside[3], 140, 170);   // the boosted fill, as the countries'
        }
        var edge = Px(9, 4.02);   // the 1 px edge on Alpha's west side: Phase 3, (nearly) opaque, unlike the fill
        var p3 = LayerStyle.IpcPhaseColor(3);
        Assert.InRange(edge[0], p3.B - 2, p3.B + 2);
        Assert.InRange(edge[1], p3.G - 2, p3.G + 2);
        Assert.InRange(edge[2], p3.R - 2, p3.R + 2);
        Assert.True(edge[3] >= 240, $"edge alpha {edge[3]}");
        Assert.Equal(0, Px(9, 3.85)[3]);   // a pixel outside it: nothing (Nigeria is not shaded as a country)
        Assert.Equal(0, Px(11, 5)[3]);    // Delta, Phase 2
        Assert.Equal(0, Px(11, 7)[3]);    // Echo, not classified
        Assert.Equal(0, Px(6.5, 7.5)[3]); // Nigeria outside the areas: no country shading
    });

    [Fact]
    public void AreaPhase5IsLabelledFamineAreaClassification()
    {
        Assert.Equal("Phase 5 Famine (IPC area classification)", CrisisText.AreaFamineLabel);
        Assert.Equal(CrisisText.AreaFamineLabel, CrisisText.AreaPhaseText(5));
        Assert.Equal("IPC area classification Phase 4 Emergency", CrisisText.AreaPhaseText(4));
        Assert.Equal("IPC area classification Phase 2 Stressed", CrisisText.AreaPhaseText(2));
        Assert.Equal("IPC area classification: none (area not classified in this analysis)", CrisisText.AreaPhaseText(null));

        var set = Squares();
        Assert.Equal("Charlie, Nigeria · Phase 5 Famine (IPC area classification) · IPC analysis period 1 Jun 2026–31 Aug 2026 (analysed Mar 2026) · IPC via HDX",
            CrisisText.AreaHover(set.Areas[2], "Nigeria"));
        Assert.Equal("Bravo, Nigeria · IPC area classification Phase 4 Emergency · IPC analysis period 1 Jun 2026–31 Aug 2026 (analysed Mar 2026) · IPC via HDX",
            CrisisText.AreaHover(set.Areas[1], "Nigeria"));
        Assert.DoesNotContain("Catastrophe", CrisisText.AreaHover(set.Areas[2], "Nigeria"));

        // The panel's line under the food section, and the country-wide R17 rows and famine note are gone for this country.
        Assert.Equal("1 in Phase 3, 1 in Phase 4, 1 in Phase 5 Famine (IPC area classification)", CrisisText.AreaCounts(set));
        Assert.Equal("none in Phase 3 or higher (of 1 area)", CrisisText.AreaCounts(Set(Area("Delta", 2, 10, 4))));
        var national = IpcAreaFixture.National() with { Phase5People = 60_000_000 };   // the rule alone would shade it Phase 5
        Assert.Equal(5, IpcAreaRule.Classify(national).ShadedPhase);
        var store = new CrisisStore();
        store.Load(new CrisisSnapshot([national], [], []) { IpcAreas = [set] });
        var panel = new CountryPanelViewModel(store, iso => CountryResolver.LoadDefault().ByIso3(iso)?.Name, nowUtc: () => Now);
        panel.Show("NGA");
        var food = panel.Sections.Single(s => s.Heading == CountryPanelViewModel.FoodHeading);
        Assert.Contains(("IPC areas", "1 in Phase 3, 1 in Phase 4, 1 in Phase 5 Famine (IPC area classification)"), food.Rows);
        Assert.Contains(("Map shading", CrisisText.ByAreaShading), food.Rows);
        Assert.Contains(("IPC areas source", $"{IpcNationalReader.Publisher}, {IpcAreaReader.Dataset} (analysed Mar 2026)"), food.Rows);
        Assert.DoesNotContain(food.Rows, r => r.Label is "Country shading" or "Outline");
        Assert.Null(food.Bar!.Note);   // the R17 note is for countries shaded by the rule
        Assert.Contains("Nigeria is drawn on the map by IPC area (IPC area classification), not by the 20% rule", food.Bar.Caption);
        Assert.DoesNotContain(CrisisText.FamineNote, food.Bar.AutomationName);

        // Without matching areas the same record keeps R17's rows and note.
        store.Load(new CrisisSnapshot([national], [], []));
        panel.Show("NGA");
        food = panel.Sections.Single(s => s.Heading == CountryPanelViewModel.FoodHeading);
        Assert.Contains(food.Rows, r => r.Label == "Country shading");
        Assert.DoesNotContain(food.Rows, r => r.Label == "IPC areas");
        Assert.Equal(CrisisText.FamineNote, food.Bar!.Note);
    }

    [Fact]
    public void PickReturnsTheArea() => StaAsync.Sta(() =>
    {
        var store = new CrisisStore();
        store.Load(Snapshot(Squares(), FoodMapTests.Sudan()));
        using var sta = new StaWorker("food-area-test");
        var layer = new FoodImageLayer(4096, 2048, store, CountryResolver.LoadDefault(), sta, new ListLog(), () => false, () => Now);
        // Facing 0°N 0°E from 3 Earth radii, as the scene tests.
        var camera = new PerspectiveCamera(new Point3D(3, 0, 0), new Vector3D(-3, 0, 0), new Vector3D(0, 0, 1), 45);
        var projector = new Projector(camera, 800, 600);
        System.Windows.Point At(double lat, double lon)
        {
            Assert.True(projector.TryProject(MarkerMeshes.Above(new GeoPosition(lat, lon), 1.0), out var p));
            return p;
        }

        var hit = layer.HitTest(At(9, 9), projector, hover: true);
        Assert.NotNull(hit);
        Assert.Equal(FoodImageLayer.LayerId, hit.LayerId);
        Assert.Equal(new CountryPick("NGA", CrisisLayer.Food), hit.Item);
        Assert.Equal(CrisisText.AreaHover(Squares().Areas[2], "Nigeria"), hit.Tooltip);
        Assert.StartsWith("Bravo, Nigeria · IPC area classification Phase 4 Emergency", layer.HitTest(At(9, 7), projector, hover: true)!.Tooltip);
        // An unclassified area answers too: the tooltip says so.
        Assert.Contains("none (area not classified", layer.HitTest(At(11, 7), projector, hover: true)!.Tooltip);
        // Nigeria outside every area: the country's food line, which names the area classification, never the 20% rule.
        var outside = layer.HitTest(At(6.5, 7.5), projector, hover: true)!.Tooltip;
        Assert.StartsWith("Nigeria · food crisis · " + CrisisText.FoodShadingByArea, outside);
        Assert.DoesNotContain("20% rule", outside);
        layer.Close();
    });

    [Fact]
    public void BuildKeyChangesWithTheAreas()
    {
        var set = Squares();
        var a = new FoodArea("NGA", null, null, null, set);
        Assert.Equal(FoodImageLayer.BuildKey(1, [a]), FoodImageLayer.BuildKey(1, [a with { Areas = Squares() }]));
        Assert.NotEqual(FoodImageLayer.BuildKey(1, [a]), FoodImageLayer.BuildKey(1, [a with { Areas = set with { AnalysisId = "2" } }]));
        Assert.NotEqual(FoodImageLayer.BuildKey(1, [a]), FoodImageLayer.BuildKey(1, [a with { Areas = Set(Area("Alpha", 4, 8, 4)) }]));
        Assert.NotEqual(FoodImageLayer.BuildKey(1, [a]), FoodImageLayer.BuildKey(1, [new FoodArea("NGA", LayerStyle.IpcPhaseColor(3), null)]));
    }

    [Fact]
    public void LegendSaysWhereAreasAreUsed()
    {
        Assert.Equal("Where IPC publishes areas, each area is shaded in IPC's own area classification; elsewhere the country is shaded by the 20% rule",
            LegendViewModel.FoodAreasRule);
        // After a colon the sentence goes on in lower case.
        const string inline = "where IPC publishes areas, each area is shaded in IPC's own area classification; elsewhere the country is shaded by the 20% rule";
        Assert.Equal("Food crisis: " + inline + ". By the 20% rule, " + LegendViewModel.FoodRule, LegendViewModel.Measures(CrisisLayer.Food));
        Assert.Equal("Food crisis (IPC): " + inline + ". By the 20% rule, " + LegendViewModel.FoodRule, LegendViewModel.FoodTooltip);
        Assert.StartsWith("Food crisis: where IPC", LegendViewModel.Measures(CrisisLayer.Food));
        var store = new CrisisStore();
        store.Load(Snapshot(Squares()));
        var legend = new LegendViewModel(store, new LayerStripViewModel(new MemorySettingsStore()), nowUtc: () => Now);
        var food = legend.Lines.Single(l => l.Layer == CrisisLayer.Food);
        Assert.Equal(LegendViewModel.Measures(CrisisLayer.Food), food.Measures);
        Assert.Equal(CrisisText.FamineNote, food.Note);   // the country-wide note stays for countries shaded by the rule
    }

    [Fact]
    public async Task FixtureAreasMatchTheirNationalRecord()
    {
        // The feed's own output (the C-period fixture) joins the national record the map shows.
        var set = await IpcAreaFixture.SetAsync();
        var s = new CrisisSnapshot([IpcAreaFixture.National()], [], []) { IpcAreas = [set] };
        Assert.Same(set, s.AreasFor(FoodCrisis.Latest(s.Crises.OfType<FoodCrisis>())));
        Assert.Null(s.AreasFor(IpcAreaFixture.National() with { ProjectionOnly = true }));
        Assert.Null(s.AreasFor(null));
        Assert.Equal("13 in Phase 3, 8 in Phase 4", CrisisText.AreaCounts(set));
        // A Phase 4 area of the sample is picked at a point inside it.
        var bama = set.Areas.Single(a => a.Name == "Bama");
        Assert.Same(bama, set.AreaAt(IpcAreaFixture.Inside(set, bama)));
    }
}
