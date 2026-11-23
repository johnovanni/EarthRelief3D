using System.Windows.Media;
using AstroCore.Earth;
using EarthRelief3D.App.Layers;
using EarthRelief3D.App.Presentation;
using EarthRelief3D.Core.Feeds;
using EarthRelief3D.Core.Model;
using GlobeCore.Geo;

namespace EarthRelief3D.Tests;

/// <summary>R17 on the map: the fill by the 20% rule, the outline in a worse phase's colour, and no seam at the dateline.</summary>
public class FoodMapTests
{
    internal static FoodCrisis Counts(string iso3, long analysed, long p1, long p2, long p3, long p4, long p5) => TestData.Food() with
    {
        Id = "ipc-" + iso3,
        CountryIso3 = iso3,
        HighestPhase = p5 > 0 ? 5 : p4 > 0 ? 4 : 3,
        Phase3PlusPeople = p3 + p4 + p5,
        PopulationAnalysed = analysed,
        Phase1People = p1,
        Phase2People = p2,
        Phase3People = p3,
        Phase4People = p4,
        Phase5People = p5,
    };

    internal static FoodCrisis Sudan() => Counts("SDN", 47_535_794, 12_069_469, 15_999_822, 14_316_494, 5_015_231, 134_808);

    [Fact]
    public void PlanShadesByTheTwentyPercentRuleAndOutlinesAWorsePhase()
    {
        var sdn = Sudan();
        var som = Counts("SOM", 1000, 400, 200, 150, 250, 0);   // 25% in Phase 4 or higher
        var pak = Counts("PAK", 1000, 600, 250, 100, 50, 0);    // 15% in Phase 3 or higher: outline only
        var stale = Counts("AGO", 1000, 100, 100, 800, 0, 0) with { IsStale = true };
        var projected = Counts("NGA", 1000, 100, 100, 800, 0, 0) with { ProjectionOnly = true, HighestPhase = 0 };
        var plan = FoodImageLayer.Plan([sdn, som, pak, stale, projected, TestData.Conflict()]);
        Assert.Equal(
        [
            new FoodArea("PAK", null, LayerStyle.IpcPhaseColor(4)),
            new FoodArea("SDN", LayerStyle.IpcPhaseColor(3), LayerStyle.IpcPhaseColor(5)),
            new FoodArea("SOM", LayerStyle.IpcPhaseColor(4), null),
        ], plan);
    }

    [Fact]
    public void OneRecordPerCountryTheLatestAsInThePanel()
    {
        // Two analyses of one country: the newer is shaded 3 with a Phase 5 outline, the older shaded 4 with none.
        // The newer one decides both colours together (not the worst fill of one and the worst outline of the other).
        var newer = Counts("SOM", 1000, 300, 200, 400, 50, 50) with { PeriodTo = new DateOnly(2027, 1, 31) };
        var older = Counts("SOM", 1000, 400, 200, 150, 250, 0) with { PeriodTo = new DateOnly(2026, 6, 30) };
        Assert.Equal((3, 5), IpcAreaRule.MapStyle(newer));
        Assert.Equal((4, null), IpcAreaRule.MapStyle(older));
        var expected = new FoodArea("SOM", LayerStyle.IpcPhaseColor(3), LayerStyle.IpcPhaseColor(5));
        Assert.Equal([expected], FoodImageLayer.Plan([older, newer]));
        Assert.Equal([expected], FoodImageLayer.Plan([newer, older]));
        Assert.Same(newer, FoodCrisis.Latest([older, newer]));
        // A newer record that is stale or projection only hides the country, as the panel shows that record.
        var projection = newer with { ProjectionOnly = true, HighestPhase = 0, PeriodTo = new DateOnly(2027, 6, 30) };
        Assert.Empty(FoodImageLayer.Plan([older, projection]));
    }

    [Fact]
    public void ASmallCountryKeepsSomeFillInsideItsOutline() => StaAsync.Sta(() =>
    {
        const int w = 720, h = 360;   // 2 px per degree
        var fill = LayerStyle.IpcPhaseColor(3);
        var outline = LayerStyle.IpcPhaseColor(5);
        // A square 3 degrees (6 px) wide: a 2.5 px band from each side would leave 1 px; the band is narrowed to 1.5 px.
        var tiny = new GeoPolygon([[new GeoPosition(3, 0), new GeoPosition(3, 3), new GeoPosition(0, 3), new GeoPosition(0, 0)]]);
        Assert.Equal(1.5, FoodImageLayer.OutlineBandPx(tiny, w, h), 9);
        Assert.Equal(FoodImageLayer.OutlineWidthPx, FoodImageLayer.OutlineBandPx(SplitSquare(), w, h), 9);
        var tinier = new GeoPolygon([[new GeoPosition(1, 0), new GeoPosition(1, 1), new GeoPosition(0, 1), new GeoPosition(0, 0)]]);
        Assert.Equal(1.0, FoodImageLayer.OutlineBandPx(tinier, w, h), 9);   // the floor

        var px = FoodImageLayer.RasterizeAreas([(tiny, fill, outline)], w, h).Pixels;
        int filled = 0;
        for (int y = 174; y < 180; y++)
            for (int x = 360; x < 366; x++)
            {
                int i = (y * w + x) * 4;
                if (Math.Abs(px[i + 2] - fill.R) <= 2 && Math.Abs(px[i + 1] - fill.G) <= 2 && px[i + 3] < 200) filled++;
            }
        Assert.True(filled >= 4, $"{filled} pixels of fill left inside the outline");
    });

    [Fact]
    public async Task SampleSudanIsShadedCrisisWithAPhase5Outline()
    {
        var s = (await SampleStore.LoadAsync()).Snapshot.AsSeenAt(SampleStore.Now);
        var plan = FoodImageLayer.Plan(s.Crises);
        Assert.Contains(new FoodArea("SDN", LayerStyle.IpcPhaseColor(3), LayerStyle.IpcPhaseColor(5)), plan);
        // Pakistan: 19.7% in Phase 3 or higher (the file rounds it to 0.2): an outline only.
        Assert.Contains(new FoodArea("PAK", null, LayerStyle.IpcPhaseColor(4)), plan);
        // Every current analysis in the sample is either filled or outlined.
        Assert.All(plan, a => Assert.True(a.Fill is not null || a.Outline is not null));
    }

    [Fact]
    public void BuildKeyChangesWithTheOutline()
    {
        var a = new FoodArea("SDN", LayerStyle.IpcPhaseColor(3), LayerStyle.IpcPhaseColor(5));
        Assert.NotEqual(FoodImageLayer.BuildKey(1, [a]), FoodImageLayer.BuildKey(1, [a with { Outline = LayerStyle.IpcPhaseColor(4) }]));
        Assert.NotEqual(FoodImageLayer.BuildKey(1, [a]), FoodImageLayer.BuildKey(1, [a with { Outline = null }]));
        Assert.NotEqual(FoodImageLayer.BuildKey(1, [a]), FoodImageLayer.BuildKey(2, [a]));
        Assert.Equal(FoodImageLayer.BuildKey(1, [a]), FoodImageLayer.BuildKey(1, [a with { }]));
    }

    // A square split at +180 degrees, as Natural Earth splits Fiji and Russia: the edge along the meridian is not a border.
    private static GeoPolygon SplitSquare() => new(
    [
        [new GeoPosition(10, 170), new GeoPosition(10, 180), new GeoPosition(-10, 180), new GeoPosition(-10, 170), new GeoPosition(10, 170)],
    ]);

    private static bool OnSeam(GeoPosition a, GeoPosition b) => Math.Abs(a.LongitudeDeg) == 180 && Math.Abs(b.LongitudeDeg) == 180;

    [Fact]
    public void OutlineSkipsTheSeamAtTheDateline()
    {
        var lines = FoodImageLayer.OutlineLines(SplitSquare());
        var segments = lines.SelectMany(l => l.Zip(l.Skip(1))).ToList();
        Assert.Equal(3, segments.Count);   // top, left and bottom; not the edge along 180
        Assert.DoesNotContain(segments, s => OnSeam(s.First, s.Second));
        // A ring with no seam is drawn closed, last point back to the first.
        var closed = FoodImageLayer.OutlineLines(new GeoPolygon([[new GeoPosition(0, 0), new GeoPosition(0, 10), new GeoPosition(10, 10)]]));
        Assert.Equal(3, closed.Sum(l => l.Count - 1));
    }

    [Fact]
    public void RealSplitCountriesHaveNoSeamInTheirOutline()
    {
        var countries = CountryResolver.LoadDefault().Index;
        int seamEdges = 0;
        foreach (var iso in new[] { "RUS", "FJI" })
        {
            var c = countries.ByIso3(iso);
            Assert.NotNull(c);
            foreach (var poly in c.Polygons)
            {
                foreach (var ring in poly.Rings)
                    for (int i = 0; i < ring.Count; i++)
                        if (OnSeam(ring[i], ring[(i + 1) % ring.Count])) seamEdges++;
                foreach (var line in FoodImageLayer.OutlineLines(poly))
                    Assert.DoesNotContain(line.Zip(line.Skip(1)), s => OnSeam(s.First, s.Second));
            }
        }
        Assert.True(seamEdges > 0, "the test data should have edges along the dateline");
    }

    [Fact]
    public void OutlineIsDrawnAfterTheFillOpaqueAndWithoutTheSeam() => StaAsync.Sta(() =>
    {
        const int w = 720, h = 360;
        int X(double lon) => (int)((lon + 180) / 360 * w);
        int Y(double lat) => (int)((90 - lat) / 180 * h);
        var outline = LayerStyle.IpcPhaseColor(5);
        var fill = LayerStyle.IpcPhaseColor(3);
        byte[] Pixel(Bgra32 image, int x, int y) => image.Pixels.AsSpan((y * w + x) * 4, 4).ToArray();

        // Outline only: a band just inside the edge, in the outline colour, fully opaque; nothing further inside or outside.
        var only = new Bgra32(FoodImageLayer.RasterizeAreas([(SplitSquare(), null, outline)], w, h));
        byte[] solid = [outline.B, outline.G, outline.R, 255];
        Assert.Equal(solid, Pixel(only, X(175), Y(10)));        // the first row inside the top edge
        Assert.Equal(solid, Pixel(only, X(175), Y(10) + 1));
        Assert.Equal(0, Pixel(only, X(175), Y(10) - 1)[3]);     // the row outside it
        Assert.Equal(solid, Pixel(only, X(170), Y(0)));         // the first column inside the left edge
        Assert.Equal(0, Pixel(only, X(170) - 1, Y(0))[3]);      // the column outside it
        Assert.Equal(0, Pixel(only, X(175), Y(0))[3]);
        // No line along the dateline (the last column, between the top and bottom edges).
        for (int y = Y(8); y <= Y(-8); y++) Assert.Equal(0, Pixel(only, w - 1, y)[3]);

        // Two neighbours outlined in different phases: each colour stays on its own side of the shared border.
        GeoPolygon Square(double west) => new(
            [[new GeoPosition(10, west), new GeoPosition(10, west + 10), new GeoPosition(-10, west + 10), new GeoPosition(-10, west)]]);
        var four = LayerStyle.IpcPhaseColor(4);
        var pair = new Bgra32(FoodImageLayer.RasterizeAreas([(Square(0), null, outline), (Square(10), null, four)], w, h));
        Assert.Equal(solid, Pixel(pair, X(10) - 1, Y(0)));
        Assert.Equal(new byte[] { four.B, four.G, four.R, 255 }, Pixel(pair, X(10), Y(0)));

        // Fill and outline: the fill inside, the outline over the fill's own edge.
        var both = new Bgra32(FoodImageLayer.RasterizeAreas([(SplitSquare(), fill, outline)], w, h));
        var inside = Pixel(both, X(175), Y(0));
        Assert.InRange(inside[0], fill.B - 2, fill.B + 2);   // premultiplied rendering rounds by a step or two
        Assert.InRange(inside[1], fill.G - 2, fill.G + 2);
        Assert.InRange(inside[2], fill.R - 2, fill.R + 2);
        Assert.InRange(inside[3], 140, 170);   // the boosted 35% fill, about 60%
        Assert.Equal(new byte[] { outline.B, outline.G, outline.R, 255 }, Pixel(both, X(175), Y(10)));
        Assert.Equal(new byte[] { outline.B, outline.G, outline.R, 255 }, Pixel(both, X(170), Y(0)));
    });

    private sealed record Bgra32(byte[] Pixels)
    {
        public Bgra32(GlobeCore.Imaging.Bgra32Image image) : this(image.Pixels) { }
    }
}
