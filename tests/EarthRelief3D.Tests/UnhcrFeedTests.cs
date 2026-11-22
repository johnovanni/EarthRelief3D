using System.Globalization;
using System.Text.Json;
using EarthRelief3D.Core.Feeds;
using EarthRelief3D.Core.Model;
using EarthRelief3D.Core.Store;
using GlobeCore.Feeds;

namespace EarthRelief3D.Tests;

public class UnhcrFeedTests
{
    private static readonly DateTime Now = new(2026, 11, 22, 0, 0, 0, DateTimeKind.Utc);

    private static (UnhcrDisplacementFeed Feed, CrisisStore Store, SampleHttpHandler H) Make(Action<SampleHttpHandler> map)
    {
        var h = new SampleHttpHandler();
        map(h);
        var store = new CrisisStore();
        return (new UnhcrDisplacementFeed(new FeedHttp("test", h), store, new FixedClock(Now), new ListLog()), store, h);
    }

    private static long Val(JsonElement e, string n) => e.GetProperty(n).ValueKind == JsonValueKind.Number ? e.GetProperty(n).GetInt64()
        : long.TryParse(e.GetProperty(n).GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : 0;

    [Fact]
    public async Task UnhcrSumsByOrigin()
    {
        var (feed, store, h) = Make(m => m.Map(UnhcrDisplacementFeed.UrlFor(2026).ToString(), Samples.Path("unhcr_population_coo.json")));
        Assert.Equal(FeedRunResult.Ok, await feed.RunOnceAsync(default));
        var items = JsonDocument.Parse(Samples.Text("unhcr_population_coo.json")).RootElement.GetProperty("items").EnumerateArray().ToList();
        var afg = items.Where(i => i.GetProperty("coo_iso").GetString() == "AFG").ToList();
        Assert.NotEmpty(afg);

        var f = Assert.Single(store.Snapshot.Displacement, d => d.CountryIso3 == "AFG");
        Assert.Equal(afg.Sum(i => Val(i, "refugees")), f.Refugees);
        Assert.Equal(afg.Sum(i => Val(i, "asylum_seekers")), f.AsylumSeekers);
        Assert.Equal(afg.Sum(i => Val(i, "idps")), f.Idps);
        Assert.Equal(2026, f.Year);
        Assert.Equal("CC BY 4.0", f.Citation.Licence);
        Assert.Equal("UNHCR Refugee Population Statistics Database", f.Citation.Attribution);
        Assert.Single(h.Requests);
        // "-" and "0" count as zero: Aruba has no refugees or IDPs but 7 asylum-seekers.
        var abw = Assert.Single(store.Snapshot.Displacement, d => d.CountryIso3 == "ABW");
        Assert.Equal((0L, 7L, 0L), (abw.Refugees, abw.AsylumSeekers, abw.Idps));
        // UNHCR spells Algeria "ALG" but its ISO code is DZA; the ISO field is used.
        Assert.Contains(store.Snapshot.Displacement, d => d.CountryIso3 == "DZA");
    }

    [Fact]
    public async Task UnhcrFallsBackToEarlierYear()
    {
        var (feed, store, h) = Make(m =>
        {
            m.MapText(UnhcrDisplacementFeed.UrlFor(2026).ToString(), "{\"page\":1,\"maxPages\":1,\"total\":[],\"items\":[]}");
            m.Map(UnhcrDisplacementFeed.UrlFor(2025).ToString(), Samples.Path("unhcr_population_coo.json"));
        });
        Assert.Equal(FeedRunResult.Ok, await feed.RunOnceAsync(default));
        Assert.Equal(2, h.Requests.Count);
        Assert.Contains("year=2026", h.Requests[0]);
        Assert.Contains("year=2025", h.Requests[1]);
        Assert.All(store.Snapshot.Displacement, d => Assert.Equal(2025, d.Year));
        Assert.NotEmpty(store.Snapshot.Displacement);
    }

    [Fact]
    public async Task UnhcrGivesUpAfterThreeYearsAndKeepsTheStore()
    {
        var (feed, store, h) = Make(m => m.MapText("https://api.unhcr.org/population/v1/population/", "{\"items\":[]}"));
        var ex = await Assert.ThrowsAsync<FeedFormatException>(() => feed.RunOnceAsync(default));
        Assert.StartsWith("format changed:", ex.Message);
        Assert.Equal(3, h.Requests.Count);
        Assert.Empty(store.Snapshot.Displacement);
    }

    [Theory]
    [InlineData(2026, "2026-11-22", 2026, 6, 30, "Mid-year 2026 figures")]
    [InlineData(2025, "2026-03-01", 2025, 6, 30, "Mid-year 2025 figures")]
    [InlineData(2025, "2026-08-01", 2025, 12, 31, "End-year 2025 figures")]
    public async Task AsOfFollowsTheReleaseRuleNotTheClock(int dataYear, string clock, int y, int m, int d, string label)
    {
        var now = DateTime.SpecifyKind(DateTime.Parse(clock, CultureInfo.InvariantCulture), DateTimeKind.Utc);
        var h = new SampleHttpHandler();
        h.MapText(UnhcrDisplacementFeed.UrlFor(now.Year).ToString(), "{\"items\":[]}");
        h.Map(UnhcrDisplacementFeed.UrlFor(dataYear).ToString(), Samples.Path("unhcr_population_coo.json"));
        var store = new CrisisStore();
        await new UnhcrDisplacementFeed(new FeedHttp("test", h), store, new FixedClock(now), new ListLog()).RunOnceAsync(default);
        var c = store.Snapshot.Displacement[0].Citation;
        Assert.Equal(new DateTime(y, m, d, 0, 0, 0, DateTimeKind.Utc), c.AsOfUtc);
        Assert.Equal(label, c.Dataset);
    }
}
