using EarthRelief3D.App.Presentation;
using EarthRelief3D.Core.Feeds;
using EarthRelief3D.Core.Store;
using GlobeCore.Feeds;

namespace EarthRelief3D.Tests;

public class GhoFeedTests
{
    private static readonly DateTime Now = new(2026, 11, 23, 12, 0, 0, DateTimeKind.Utc);

    private static GhoFeed Make(FeedHttp http, IndicatorStore ind, CrisisStore? crises = null) =>
        new(http, ind, crises ?? new CrisisStore(), new FixedClock(Now), new ListLog());

    private static async Task<IndicatorStore> Loaded()
    {
        var ind = new IndicatorStore();
        Assert.Equal(FeedRunResult.Ok, await Make(Samples.GhoHttp().Http, ind).RunOnceAsync(default));
        return ind;
    }

    private static void MapAll(SampleHttpHandler h, string json)
    {
        foreach (var code in new[] { GhoFeed.WaterCode, GhoFeed.SanitationCode, GhoFeed.UhcCode })
            h.MapText(GhoFeed.UrlFor(code).ToString(), json);
    }

    [Fact]
    public async Task ReadsLatestTotalPerCountry()
    {
        var ind = await Loaded();

        // Sudan's water: the newest RESIDENCEAREATYPE_TOTL row is 2024 (22.08 with access), shown as the share without.
        // The _URB (30.86) and _RUR (20.01) rows of the same year must not be taken.
        var water = ind.Snapshot.Get("gho.water")!;
        var sdn = water.For("SDN")!;
        Assert.Equal(2024, sdn.Year);
        Assert.Equal(100 - 22.08, sdn.Value, 6);
        Assert.Equal("Share of people without at least basic drinking water", water.Measures);
        Assert.Equal("%", water.Unit);
        Assert.Equal(new DateTime(2024, 12, 31, 0, 0, 0, DateTimeKind.Utc), sdn.Citation.AsOfUtc);
        Assert.Equal(Now, sdn.Citation.RetrievedUtc);
        Assert.Equal("CC BY 4.0", sdn.Citation.Licence);
        Assert.Contains("World Health Organization", sdn.Citation.Publisher);
        Assert.Contains("Global Health Observatory", sdn.Citation.Publisher + sdn.Citation.Dataset);
        Assert.Contains("2024", sdn.Citation.Attribution);

        // The USA has no 2023 row (a gap in the live data too): its newest is 2024.
        Assert.Equal(2024, water.For("USA")!.Year);
        Assert.Equal(100 - 94.75, water.For("USA")!.Value, 6);

        var san = ind.Snapshot.Get("gho.sanitation")!;
        Assert.Equal("Share of people without at least basic sanitation", san.Measures);
        Assert.Equal(100 - 93.41, san.For("SDN")!.Value, 6);

        // UHC has no Dim1 at all, and its latest year is 2023.
        var uhc = ind.Snapshot.Get("gho.uhc")!;
        Assert.Equal(2023, uhc.For("SDN")!.Year);
        Assert.Equal(100 - 43.0, uhc.For("SDN")!.Value, 6);
        Assert.Contains("100 minus", uhc.Measures);
        Assert.Equal(15, water.Values.Select(v => v.Iso3).Distinct().Count());
    }

    [Fact]
    public async Task TheThreeSeriesArriveInOneWrite()
    {
        var ind = new IndicatorStore();
        var changes = 0;
        ind.Changed += (_, _) => changes++;
        await Make(Samples.GhoHttp().Http, ind).RunOnceAsync(default);
        Assert.Equal(1, changes);
        Assert.Equal(3, ind.Snapshot.Series.Count);
        // Year-only data: the citation says so, and the day is never shown.
        Assert.Equal(EarthRelief3D.Core.Model.CitationDate.EstimateYear, ind.Snapshot.Get("gho.water")!.For("SDN")!.Citation.DateMeaning);
    }

    [Fact]
    public async Task AValueAbove100IsClampedAtZeroWithoutAccess()
    {
        var h = new SampleHttpHandler();
        MapAll(h, """
            {"value":[{"SpatialDimType":"COUNTRY","SpatialDim":"ISL","Dim1":"RESIDENCEAREATYPE_TOTL","TimeDim":2024,"NumericValue":100.3},
                      {"SpatialDimType":"REGION","SpatialDim":"EMR","Dim1":"RESIDENCEAREATYPE_TOTL","TimeDim":2024,"NumericValue":50.0}]}
            """);
        var ind = new IndicatorStore();
        await Make(new FeedHttp("test", h), ind).RunOnceAsync(default);
        var v = Assert.Single(ind.Snapshot.Get("gho.water")!.Values);   // the REGION row is not a country
        Assert.Equal(0, v.Value);
    }

    [Fact]
    public async Task ReshapedAnswerIsFormatChanged()
    {
        var h = new SampleHttpHandler();
        h.Map(GhoFeed.UrlFor(GhoFeed.WaterCode).ToString(), Samples.Path("gho_wsh_water_basic.json"));
        h.Map(GhoFeed.UrlFor(GhoFeed.SanitationCode).ToString(), Samples.Path("gho_wsh_sanitation_basic.json"));
        h.MapText(GhoFeed.UrlFor(GhoFeed.UhcCode).ToString(), """{"data":[{"SpatialDim":"SDN"}]}""");   // no "value"
        var ind = new IndicatorStore();
        var feed = Make(new FeedHttp("test", h), ind);

        var ex = await Assert.ThrowsAsync<FeedFormatException>(() => feed.RunOnceAsync(default));
        Assert.StartsWith("format changed:", ex.Message);
        Assert.Contains(GhoFeed.UhcCode, ex.Message);
        Assert.Empty(ind.Snapshot.Series);   // nothing written, not even the two good indicators

        // The scheduler turns that into a failing state carrying the message the status dot shows.
        var sched = new FeedScheduler([feed], new FixedClock(Now), new ListLog(), "idle");
        await sched.TickAsync(default);
        var status = Assert.Single(sched.Statuses);
        Assert.Equal("WHO GHO", status.Name);
        Assert.NotEqual(FeedState.Ok, status.State);
        Assert.StartsWith("format changed:", status.LastError);
    }

    [Fact]
    public async Task AnAnswerWithNoCountryValuesIsFormatChanged()
    {
        var h = new SampleHttpHandler();
        MapAll(h, """{"value":[{"SpatialDimType":"COUNTRY","SpatialDim":"ISL","TimeDim":2024}]}""");
        var feed = Make(new FeedHttp("test", h), new IndicatorStore());
        var ex = await Assert.ThrowsAsync<FeedFormatException>(() => feed.RunOnceAsync(default));
        Assert.StartsWith("format changed:", ex.Message);
    }

    [Fact]
    public async Task FailureKeepsCache()
    {
        var ind = await Loaded();
        var before = ind.Snapshot;
        var changes = 0;
        ind.Changed += (_, _) => changes++;

        // A network failure (404 for everything) on the next run: the series stay as they were.
        var feed = Make(new FeedHttp("test", new SampleHttpHandler()), ind);
        await Assert.ThrowsAnyAsync<Exception>(() => feed.RunOnceAsync(default));

        Assert.Same(before, ind.Snapshot);
        Assert.Equal(0, changes);
        Assert.NotNull(ind.Snapshot.Get("gho.water")!.For("SDN"));
    }

    [Fact]
    public async Task CodesWithNoBoundaryUnitAreListedAsNotPlaced()
    {
        var h = new SampleHttpHandler();
        MapAll(h, """
            {"value":[{"SpatialDimType":"COUNTRY","SpatialDim":"ISL","Dim1":"RESIDENCEAREATYPE_TOTL","TimeDim":2024,"NumericValue":99.0},
                      {"SpatialDimType":"COUNTRY","SpatialDim":"ZZZ","Dim1":"RESIDENCEAREATYPE_TOTL","TimeDim":2024,"NumericValue":50.0}]}
            """);
        var ind = new IndicatorStore();
        var crises = new CrisisStore();
        var log = new ListLog();
        await new GhoFeed(new FeedHttp("test", h), ind, crises, new FixedClock(Now), log).RunOnceAsync(default);

        Assert.Equal(["ISL"], ind.Snapshot.Get("gho.water")!.Values.Select(v => v.Iso3));
        var u = Assert.Single(crises.Snapshot.Unplaced);   // one line for the code, not one per indicator
        Assert.Equal("WHO GHO", u.Source);
        Assert.Contains("ZZZ", u.Title);
        Assert.Contains(log.Lines, l => l.Contains("ZZZ"));
        Assert.True(crises.Snapshot.CheckedUtc.ContainsKey("WHO GHO"));
    }

    [Fact]
    public void TheSeriesWordingIsTheLayersDefaultWording()
    {
        // The map's legend says the same words before and after the data arrives (§7).
        Assert.Equal(GhoFeed.WaterMeasures, LayerCatalog.DefaultMeasures(ShadingLayer.Water));
        Assert.Equal(GhoFeed.SanitationMeasures, LayerCatalog.DefaultMeasures(ShadingLayer.Sanitation));
        Assert.Equal(GhoFeed.UhcMeasures, LayerCatalog.DefaultMeasures(ShadingLayer.Health));
        Assert.Equal(TimeSpan.FromDays(7), Make(Samples.GhoHttp().Http, new IndicatorStore()).Interval);
        Assert.Equal("https://ghoapi.azureedge.net/api/", GhoFeed.BaseUrl);
    }
}
