using System.IO;
using System.Globalization;
using EarthRelief3D.Core.Feeds;
using EarthRelief3D.Core.Model;
using EarthRelief3D.Core.Store;
using GlobeCore.Feeds;

namespace EarthRelief3D.Tests;

public class IpcFeedTests
{
    private static readonly DateTime Now = new(2026, 11, 22, 0, 0, 0, DateTimeKind.Utc);

    private static (IpcNationalFeed Feed, CrisisStore Store, ListLog Log) Make(FeedHttp http)
    {
        var store = new CrisisStore();
        var log = new ListLog();
        return (new IpcNationalFeed(http, store, new FixedClock(Now), log), store, log);
    }

    private static Dictionary<string, string> SampleRow(string iso3)
    {
        var rows = CsvReader.Read(new StringReader(Samples.Text("ipc_national_wide_latest.csv"))).ToList();
        var header = rows[0];
        var row = rows.Skip(1).Single(r => r[Array.IndexOf(header, "Country")] == iso3);
        return header.Zip(row).ToDictionary(p => p.First, p => p.Second);
    }

    private static long L(string s) => long.Parse(s, CultureInfo.InvariantCulture);

    [Fact]
    public async Task IpcHighestPhaseAndShare()
    {
        var (http, _) = Samples.IpcHttp();
        var (feed, store, _) = Make(http);
        Assert.Equal(FeedRunResult.Ok, await feed.RunOnceAsync(default));

        var sdn = SampleRow("SDN");
        var food = Assert.Single(store.Snapshot.Crises.OfType<FoodCrisis>(), c => c.CountryIso3 == "SDN");
        int expectedPhase = Enumerable.Range(1, 5).Where(n => L(sdn[$"Phase {n} number current"]) > 0).Max();
        Assert.Equal(expectedPhase, food.HighestPhase);
        Assert.Equal(expectedPhase, food.Band);
        Assert.Equal(L(sdn["Phase 3+ number current"]), food.Phase3PlusPeople);
        Assert.Equal(double.Parse(sdn["Phase 3+ percentage current"], CultureInfo.InvariantCulture), food.Phase3PlusShare);
        Assert.Equal(DateOnly.Parse(sdn["Current from"], CultureInfo.InvariantCulture), food.PeriodFrom);
        Assert.Equal(DateOnly.Parse(sdn["Current to"], CultureInfo.InvariantCulture), food.PeriodTo);
        Assert.False(food.IsStale);
        Assert.Equal(CrisisKind.FoodCrisis, food.Kind);
    }

    [Fact]
    public async Task BlankCurrentPeriodIsSkippedAndLoggedOnce()
    {
        var (http, _) = Samples.IpcHttp();
        var (feed, store, log) = Make(http);
        await feed.RunOnceAsync(default);
        foreach (var iso in new[] { "CAF", "GTM", "HND", "HTI", "NGA", "SSD" })
            Assert.DoesNotContain(store.Snapshot.Crises, c => c.CountryIso3 == iso);
        Assert.Single(log.Lines, l => l.Contains("IPC") && l.Contains("no current period"));
        Assert.DoesNotContain(store.Snapshot.Crises, c => c.CountryIso3 is "UKR" or "MMR");
    }

    [Fact]
    public async Task StaleIpcIsKeptButMarkedStale()
    {
        var (http, _) = Samples.IpcHttp();
        var (feed, store, _) = Make(http);
        await feed.RunOnceAsync(default);
        var ago = SampleRow("AGO");   // current period ended in 2021
        Assert.True(L(ago["Phase 3+ number current"]) > 0);
        var food = Assert.Single(store.Snapshot.Crises.OfType<FoodCrisis>(), c => c.CountryIso3 == "AGO");
        Assert.True(food.IsStale);
        Assert.False(store.Snapshot.Crises.OfType<FoodCrisis>().Single(c => c.CountryIso3 == "SDN").IsStale);
    }

    [Fact]
    public async Task IpcCitationAsOfIsPeriodEnd()
    {
        var (http, _) = Samples.IpcHttp();
        var (feed, store, _) = Make(http);
        await feed.RunOnceAsync(default);
        var sdn = SampleRow("SDN");
        var food = store.Snapshot.Crises.OfType<FoodCrisis>().Single(c => c.CountryIso3 == "SDN");
        var c = Assert.Single(food.Citations);
        Assert.Equal(DateOnly.Parse(sdn["Current to"], CultureInfo.InvariantCulture), DateOnly.FromDateTime(c.AsOfUtc));
        Assert.Equal(sdn["Date of analysis"], c.Version);
        Assert.Equal("CC0", c.Licence);
        Assert.Contains("HDX", c.Publisher);
        Assert.Equal(Now, c.RetrievedUtc);
    }

    [Fact]
    public async Task MissingResourceIsFormatChanged()
    {
        var h = new SampleHttpHandler();
        h.MapText(Samples.HdxPackage, "{\"success\":true,\"result\":{\"resources\":[{\"name\":\"other.csv\",\"url\":\"https://x/other.csv\"}]}}");
        var (feed, store, _) = Make(new FeedHttp("test", h));
        var e = await Assert.ThrowsAsync<FeedFormatException>(() => feed.RunOnceAsync(default));
        Assert.StartsWith("format changed:", e.Message);
        Assert.Empty(store.Snapshot.Crises);
    }

    [Fact]
    public async Task MissingColumnIsFormatChangedAndKeepsPreviousData()
    {
        var (http, h) = Samples.IpcHttp();
        var (feed, store, _) = Make(http);
        await feed.RunOnceAsync(default);
        var before = store.Snapshot.Crises.Count;
        Assert.True(before > 0);
        h.MapText(Samples.IpcCsv, "Date of analysis,Country\nSep 2026,SDN\n");
        var e = await Assert.ThrowsAsync<FeedFormatException>(() => feed.RunOnceAsync(default));
        Assert.StartsWith("format changed:", e.Message);
        Assert.Equal(before, store.Snapshot.Crises.Count);
    }
}
