using System.IO;
using EarthRelief3D.Core.Feeds;
using EarthRelief3D.Core.Model;
using EarthRelief3D.Core.Store;
using GlobeCore.Feeds;

namespace EarthRelief3D.Tests;

public class UcdpFeedFixTests
{
    private static readonly DateTime Now = new(2026, 11, 22, 0, 0, 0, DateTimeKind.Utc);
    private static readonly string[] Header = CsvReader.Read(new StringReader(Samples.Text("ucdp_candidate_monthly.csv"))).First();

    private static string Row(int id, int best, string conflict, string country)
    {
        var f = new string[Header.Length];
        Array.Fill(f, "");
        void Set(string col, string v) => f[Array.IndexOf(Header, col)] = v;
        Set("id", id.ToString()); Set("type_of_violence", "1"); Set("conflict_new_id", conflict); Set("conflict_name", "Test: A vs B");
        Set("side_a", "A"); Set("side_b", "B"); Set("country", country); Set("latitude", "15.5"); Set("longitude", "32.5");
        Set("date_start", "2026-10-01 00:00:00.000"); Set("date_end", "2026-10-01 00:00:00.000"); Set("best", best.ToString());
        return string.Join(",", f);
    }

    private static (UcdpCandidateFeed Feed, CrisisStore Store, SampleHttpHandler H, FixedClock Clock) Make(string monthlyCsv)
    {
        var head = string.Join(",", Header);
        var h = new SampleHttpHandler();
        h.Map(Samples.UcdpPage, Samples.Path("ucdp_downloads.html"));
        h.MapText(Samples.UcdpMonthly, monthlyCsv);
        h.MapText(Samples.UcdpQuarterly, head + "\n");
        var store = new CrisisStore();
        var clock = new FixedClock(Now);
        return (new UcdpCandidateFeed(new FeedHttp("test", h), store, clock, new ListLog()), store, h, clock);
    }

    private static string Csv(params string[] rows) => string.Join(",", Header) + "\n" + string.Join("\n", rows) + "\n";

    [Fact]
    public async Task ThresholdIsOnTheWholeConflictAcrossCountries()
    {
        // 20 + 20 deaths in two countries: 40 in the conflict, so a crisis in each country
        var (feed, store, _, _) = Make(Csv(Row(1, 20, "9100", "Sudan"), Row(2, 20, "9100", "South Sudan"),
            Row(3, 500, "9200", "Sudan"), Row(4, 3, "9200", "South Sudan"),
            Row(5, 10, "9300", "Sudan"), Row(6, 10, "9300", "South Sudan")));   // 20 in total: no crisis
        await feed.RunOnceAsync(default);
        var c = store.Snapshot.Crises.OfType<ConflictCrisis>().Select(x => (x.UcdpConflictId, x.CountryIso3)).ToHashSet();
        Assert.Contains((9100, "SDN"), c);
        Assert.Contains((9100, "SSD"), c);
        Assert.Contains((9200, "SDN"), c);
        Assert.Contains((9200, "SSD"), c);
        Assert.DoesNotContain(c, x => x.UcdpConflictId == 9300);
        var ssd = store.Snapshot.Crises.OfType<ConflictCrisis>().Single(x => x.UcdpConflictId == 9200 && x.CountryIso3 == "SSD");
        Assert.Equal(3, ssd.Events.Sum(e => e.BestDeaths));   // each crisis holds only its own country's events
        Assert.Null(ssd.Began);
        Assert.Contains("since Oct 2026", ssd.Severity.Text);
    }

    [Fact]
    public async Task NewestMonthlyAndQuarterlyFilesExpireAfter24Hours()
    {
        var (feed, store, h, clock) = Make(Csv(Row(1, 30, "9100", "Sudan")));
        await feed.RunOnceAsync(default);
        Assert.Equal(2, h.Requests.Count(u => u.EndsWith(".csv")));
        clock.UtcNow = Now.AddHours(23);
        await feed.RunOnceAsync(default);
        Assert.Equal(2, h.Requests.Count(u => u.EndsWith(".csv")));

        h.MapText(Samples.UcdpMonthly, Csv(Row(1, 30, "9100", "Sudan"), Row(2, 40, "9100", "Sudan")));
        clock.UtcNow = Now.AddHours(25);
        await feed.RunOnceAsync(default);
        Assert.Equal(4, h.Requests.Count(u => u.EndsWith(".csv")));
        Assert.Equal(70, store.Snapshot.Crises.OfType<ConflictCrisis>().Single().Events.Sum(e => e.BestDeaths));
    }

    [Fact]
    public async Task ZeroEventsIsFormatChangedAndKeepsData()
    {
        var (feed2, store2, _, _) = Make(Csv());
        var e = await Assert.ThrowsAsync<FeedFormatException>(() => feed2.RunOnceAsync(default));
        Assert.StartsWith("format changed: no events", e.Message);
        Assert.Empty(store2.Snapshot.Crises);
    }

    [Fact]
    public async Task LatestCompleteMonthComesFromTheFileName()
    {
        // events run into November, but the monthly file is the October release: the trend must end at October
        var (feed, store, _, _) = Make(Csv(Row(1, 30, "9100", "Sudan")));
        await feed.RunOnceAsync(default);
        var files = UcdpReader.FindFiles(Samples.Text("ucdp_downloads.html"), new Uri(Samples.UcdpPage));
        Assert.Equal(new DateOnly(2026, 10, 1), files.LatestCompleteMonth);
        Assert.Single(store.Snapshot.Crises);
    }
}
