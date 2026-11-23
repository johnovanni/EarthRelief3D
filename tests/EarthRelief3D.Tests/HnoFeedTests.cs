using EarthRelief3D.Core.Feeds;
using EarthRelief3D.Core.Model;
using EarthRelief3D.Core.Store;
using GlobeCore.Feeds;

namespace EarthRelief3D.Tests;

public class HnoFeedTests
{
    private static readonly DateTime Now = new(2026, 11, 23, 12, 0, 0, DateTimeKind.Utc);
    private const string Header = "Country ISO3,Description,Cluster,Category,Population,In Need,Targeted,Affected,Reached,Info\n";

    private static HnoFeed Make(FeedHttp http, IndicatorStore ind, CrisisStore? crises = null, DateTime? now = null, ListLog? log = null) =>
        new(http, ind, crises ?? new CrisisStore(), new FixedClock(now ?? Now), log ?? new ListLog());

    private static async Task<IndicatorStore> Loaded()
    {
        var ind = new IndicatorStore();
        Assert.Equal(FeedRunResult.Ok, await Make(Samples.HnoHttp().Http, ind).RunOnceAsync(default));
        return ind;
    }

    private static HumanitarianNeed Row(IndicatorStore ind, string iso3, string cluster) =>
        Assert.Single(ind.Snapshot.Needs, n => n.Iso3 == iso3 && n.Cluster == cluster);

    private static FeedHttp HttpWithCsv(string csv)
    {
        var h = new SampleHttpHandler();
        h.Map(HnoFeed.PackageUrl.ToString(), Samples.Path("hdx_hno_package.json"));
        h.MapText(Samples.HnoCsv2026, csv);
        return new FeedHttp("test", h);
    }

    [Fact]
    public async Task TotalsUseBlankCategoryAndClusterCodes()
    {
        var ind = await Loaded();

        var all = Row(ind, "AFG", "ALL");
        Assert.Equal((48595632L, 21889283L, 17479657L, 2026), (all.Population, all.InNeed, all.Targeted, all.PlanYear));
        Assert.Equal("Plan caseload", all.Basis);
        Assert.Equal(17412653L, Row(ind, "AFG", "FSC").InNeed);
        Assert.Equal(15909907L, Row(ind, "AFG", "WSH").InNeed);
        Assert.Equal(4243627L, Row(ind, "AFG", "SHL").InNeed);
        Assert.Equal(14423290L, Row(ind, "AFG", "HEA").InNeed);
        Assert.Null(Row(ind, "AFG", "FSC").Population);   // the population is only on the ALL row

        // Only the five codes the panel reads: education, nutrition, protection and the rest are not kept.
        Assert.All(ind.Snapshot.Needs, n => Assert.Contains(n.Cluster, new[] { "ALL", "FSC", "WSH", "SHL", "HEA" }));
        // A cluster with no row means "not reported": Sudan and Yemen have only the total.
        Assert.Equal(["ALL"], ind.Snapshot.NeedsOf("SDN").Select(n => n.Cluster));
        Assert.Equal(["ALL"], ind.Snapshot.NeedsOf("YEM").Select(n => n.Cluster));
        Assert.Equal("GHO Estimates", Row(ind, "YEM", "ALL").Basis);
        Assert.Equal(20, ind.Snapshot.Needs.Select(n => n.Iso3).Distinct().Count());
    }

    [Fact]
    public async Task OnlyBlankCategoryRowsAreUsed()
    {
        var csv = Header
            + "ISL,Final,ALL,,1000,100,50,,,\n"
            + "ISL,Final,ALL,Women,1000,999,999,,,\n"
            + "ISL,Shelter,SHL,,,10,5,,,\n"
            + "ISL,Shelter,SHL,Children,,888,5,,,\n";
        var ind = new IndicatorStore();
        await Make(HttpWithCsv(csv), ind).RunOnceAsync(default);
        Assert.Equal(100, Row(ind, "ISL", "ALL").InNeed);
        Assert.Equal(10, Row(ind, "ISL", "SHL").InNeed);
    }

    [Fact]
    public async Task ARepeatedClusterRowIsRejectedNotAdded()
    {
        var csv = Header + "ISL,Final,ALL,,1000,100,50,,,\nISL,Health,HEA,,,10,5,,,\nISL,Health again,HEA,,,77,5,,,\n";
        var ind = new IndicatorStore();
        var log = new ListLog();
        await Make(HttpWithCsv(csv), ind, log: log).RunOnceAsync(default);
        Assert.Equal(10, Row(ind, "ISL", "HEA").InNeed);
        Assert.Contains(log.Lines, l => l.StartsWith("WARN") && l.Contains("ISL") && l.Contains("HEA"));
    }

    [Fact]
    public async Task FrenchDescriptionsStillMatchByCode()
    {
        var ind = await Loaded();
        // CAF's descriptions are French ("Sécurité alimentaire", "Eau, hygiène et assainissement", "Santé", "ABRIS&NFI").
        Assert.Equal(1765978L, Row(ind, "CAF", "FSC").InNeed);
        Assert.Equal(1096042L, Row(ind, "CAF", "WSH").InNeed);
        Assert.Equal(300750L, Row(ind, "CAF", "SHL").InNeed);
        Assert.Equal(955008L, Row(ind, "CAF", "HEA").InNeed);
        Assert.Equal(2286959L, Row(ind, "CAF", "ALL").InNeed);
        // The PRO sub-sector rows repeat a cluster code; they are not among the five and are not read.
        Assert.DoesNotContain(ind.Snapshot.Needs, n => n.Cluster.StartsWith("PRO", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ShelterShareIsInNeedOverPopulation()
    {
        var ind = await Loaded();
        var series = ind.Snapshot.Get("hno.shelter")!;
        var afg = series.For("AFG")!;
        Assert.Equal(4243627.0 / 48595632.0 * 100, afg.Value, 2);
        Assert.Equal(2026, afg.Year);
        Assert.Equal("%", series.Unit);
        Assert.Equal("People in need of shelter, as a share of the population (UN humanitarian needs overviews)", series.Measures);
        Assert.Equal(3138086.0 / 19700000.0 * 100, series.For("SOM")!.Value, 2);

        // Only countries with an SHL row get a figure (ruling R7): Sudan and Yemen have the total only, so they are not shaded.
        Assert.Null(series.For("SDN"));
        Assert.Null(series.For("YEM"));
        Assert.Equal(11, series.Values.Count);
        Assert.Equal(["AFG", "CAF", "COD", "HTI", "MMR", "MOZ", "NGA", "SOM", "SSD", "TCD", "UKR"], series.Values.Select(v => v.Iso3).Order());
    }

    [Fact]
    public async Task CitationNamesTheFileAndNeverTodaysDate()
    {
        var ind = await Loaded();
        var c = Row(ind, "AFG", "ALL").Citation;
        Assert.Contains("OCHA Humanitarian Programme Cycle Tools", c.Publisher);
        Assert.Contains("2026", c.Dataset);
        Assert.Equal("CC BY-IGO", c.Licence);
        // The file's last_modified (2026-02-13), not the clock.
        Assert.Equal(new DateTime(2026, 2, 13), c.AsOfUtc.Date);
        Assert.Equal(Now, c.RetrievedUtc);
        Assert.Contains("CC BY-IGO", c.Attribution);
    }

    [Fact]
    public async Task AResourceWithoutLastModifiedIsFormatChanged()
    {
        // The package's own date is not the file's load date, so it is not used in its place.
        var node = System.Text.Json.Nodes.JsonNode.Parse(Samples.Text("hdx_hno_package.json"))!;
        foreach (var r in node["result"]!["resources"]!.AsArray()) r!.AsObject().Remove("last_modified");
        var h = new SampleHttpHandler();
        h.MapText(HnoFeed.PackageUrl.ToString(), node.ToJsonString());
        h.Map(Samples.HnoCsv2026, Samples.Path("hpc_hno_2026.csv"));
        var ind = new IndicatorStore();
        var ex = await Assert.ThrowsAsync<FeedFormatException>(() => Make(new FeedHttp("test", h), ind).RunOnceAsync(default));
        Assert.StartsWith("format changed:", ex.Message);
        Assert.Contains("last_modified", ex.Message);
        Assert.Empty(ind.Snapshot.Needs);
    }

    [Fact]
    public async Task TheCitationSaysTheDateIsTheFilesLoadDate()
    {
        var ind = await Loaded();
        Assert.Equal(CitationDate.FileLoaded, Row(ind, "AFG", "ALL").Citation.DateMeaning);
    }

    [Fact]
    public async Task FallsBackToThePreviousYearWhenTheNewOneIsNotThereYet()
    {
        var (http, h) = Samples.HnoHttp();
        var ind = new IndicatorStore();
        // 2027: the package has no hpc_hno_2027.csv, so the 2026 file is read, and the plan year says 2026.
        await Make(http, ind, now: new DateTime(2027, 1, 5, 0, 0, 0, DateTimeKind.Utc)).RunOnceAsync(default);
        Assert.All(ind.Snapshot.Needs, n => Assert.Equal(2026, n.PlanYear));
        Assert.Contains(Samples.HnoCsv2026, h.Requests);

        // 2028: neither 2028 nor 2027 is in the package.
        var feed = Make(Samples.HnoHttp().Http, new IndicatorStore(), now: new DateTime(2028, 1, 5, 0, 0, 0, DateTimeKind.Utc));
        var ex = await Assert.ThrowsAsync<FeedFormatException>(() => feed.RunOnceAsync(default));
        Assert.StartsWith("format changed:", ex.Message);
    }

    [Fact]
    public async Task AMissingColumnIsFormatChangedAndKeepsTheRows()
    {
        var ind = await Loaded();
        var before = ind.Snapshot;
        var bad = "Country ISO3,Description,Cluster,Population,In Need,Targeted\nAFG,x,ALL,1,2,3\n";   // no Category column
        var feed = Make(HttpWithCsv(bad), ind);
        var ex = await Assert.ThrowsAsync<FeedFormatException>(() => feed.RunOnceAsync(default));
        Assert.StartsWith("format changed:", ex.Message);
        Assert.Same(before, ind.Snapshot);
        Assert.Equal(TimeSpan.FromDays(7), feed.Interval);
        Assert.Equal("UN HNO", feed.Name);
    }

    [Fact]
    public async Task UnresolvedCodesAreListedNotDropped()
    {
        var csv = Header + "ISL,Final,ALL,,1000,100,50,,,\nZZZ,Final,ALL,,1000,100,50,,,\n";
        var ind = new IndicatorStore();
        var crises = new CrisisStore();
        await Make(HttpWithCsv(csv), ind, crises).RunOnceAsync(default);
        Assert.DoesNotContain(ind.Snapshot.Needs, n => n.Iso3 == "ZZZ");
        var u = Assert.Single(crises.Snapshot.Unplaced);
        Assert.Equal("UN HNO", u.Source);
        Assert.Contains("ZZZ", u.Title);
    }
}
