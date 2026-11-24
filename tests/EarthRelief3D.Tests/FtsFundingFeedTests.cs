using EarthRelief3D.Core.Feeds;
using EarthRelief3D.Core.Model;
using EarthRelief3D.Core.Store;
using GlobeCore.Feeds;

namespace EarthRelief3D.Tests;

/// <summary>The FTS appeal funding feed (Plan 4 Task 2, ruling P4-4) over the real, trimmed sample.</summary>
public class FtsFundingFeedTests
{
    private static readonly DateTime Now = new(2026, 11, 24, 12, 0, 0, DateTimeKind.Utc);
    private const string Header = "countryCode,id,name,code,typeId,typeName,startDate,endDate,year,requirements,funding,percentFunded\n";

    private static FtsFundingFeed Make(FeedHttp http, IndicatorStore ind, CrisisStore? crises = null, DateTime? now = null) =>
        new(http, ind, crises ?? new CrisisStore(), new FixedClock(now ?? Now), new ListLog());

    private static async Task<IndicatorStore> Loaded(DateTime? now = null)
    {
        var ind = new IndicatorStore();
        Assert.Equal(FeedRunResult.Ok, await Make(Samples.FtsHttp().Http, ind, now: now).RunOnceAsync(default));
        return ind;
    }

    private static FeedHttp HttpWithCsv(string csv)
    {
        var h = new SampleHttpHandler();
        h.Map(FtsFundingFeed.PackageUrl.ToString(), Samples.Path("hdx_fts_package.json"));
        h.MapText(Samples.FtsCsv, csv);
        return new FeedHttp("test", h);
    }

    [Fact]
    public async Task CurrentYearOnly()
    {
        var ind = await Loaded();
        Assert.NotEmpty(ind.Snapshot.Appeals);
        Assert.All(ind.Snapshot.Appeals, a => Assert.Equal(2026, a.Year));
        Assert.Contains(ind.Snapshot.Appeals, a => a.Iso3 == "AFG");
    }

    [Fact]
    public async Task EarlyJanuaryFallsBackToLastYear()
    {
        var ind = await Loaded(new DateTime(2027, 1, 2, 9, 0, 0, DateTimeKind.Utc));   // the sample has no plan rows for 2027
        Assert.NotEmpty(ind.Snapshot.Appeals);
        Assert.All(ind.Snapshot.Appeals, a => Assert.Equal(2026, a.Year));
    }

    [Fact]
    public async Task NoPlanRowsForEitherYearIsFormatChanged()
    {
        var ind = new IndicatorStore();
        var feed = Make(HttpWithCsv(Header + "AFG,1,x,C,1,Flash appeal,,,2020,100,50,50\n"), ind);
        var ex = await Assert.ThrowsAsync<FeedFormatException>(() => feed.RunOnceAsync(default));
        Assert.StartsWith("format changed:", ex.Message);
        Assert.Empty(ind.Snapshot.Appeals);
    }

    [Fact]
    public async Task PercentAsPublished()
    {
        var ind = await Loaded();
        var afg = Assert.Single(ind.Snapshot.AppealsOf("AFG"), a => a.PlanId == 1502);
        Assert.Equal(1_714_181_496L, afg.RequirementsUsd);
        Assert.Equal(580_680_803L, afg.FundingUsd);
        Assert.Equal(34, afg.PercentFunded);
        Assert.Equal("Afghanistan Humanitarian Needs and Response Plan 2026", afg.PlanName);
        Assert.Equal("Humanitarian needs and response plan", afg.PlanType);
        Assert.False(afg.Regional);
    }

    [Fact]
    public async Task BlankRequirementsIsNull()
    {
        var csv = Header + "ISL,77,Plan without requirements,P77,1,Flash appeal,2026-01-01,2026-12-31,2026,,1000,\n";
        var ind = new IndicatorStore();
        await Make(HttpWithCsv(csv), ind).RunOnceAsync(default);
        var a = Assert.Single(ind.Snapshot.Appeals);
        Assert.Null(a.RequirementsUsd);   // never 0
        Assert.Null(a.PercentFunded);
        Assert.Equal(1000L, a.FundingUsd);
    }

    [Fact]
    public async Task RowsWithoutPlanIdAreSkipped()
    {
        var ind = await Loaded();
        Assert.DoesNotContain(ind.Snapshot.Appeals, a => a.PlanName == "Not specified");
        Assert.All(ind.Snapshot.Appeals, a => Assert.True(a.PlanId > 0));
    }

    [Fact]
    public async Task RegionalPlanFlagged()
    {
        var csv = Header
            + "ISL,900,Regional plan,R1,1,Flash appeal,,,2026,3000,1000,33\n"
            + "FJI,900,Regional plan,R1,1,Flash appeal,,,2026,3000,1000,33\n"
            + "TON,900,Regional plan,R1,1,Flash appeal,,,2026,3000,1000,33\n"
            + "AFG,901,Afghanistan plan,A1,1,Regional response plan,,,2026,100,10,10\n"   // the type alone sets it
            + "HTI,902,Haiti plan,H1,1,Flash appeal,,,2026,100,10,10\n";
        var ind = new IndicatorStore();
        await Make(HttpWithCsv(csv), ind).RunOnceAsync(default);
        var snap = ind.Snapshot;
        Assert.All(snap.Appeals.Where(a => a.PlanId == 900), a => Assert.True(a.Regional));
        Assert.Equal(["FJI", "ISL", "TON"], snap.Appeals.Where(a => a.PlanId == 900).Select(a => a.Iso3).Order(StringComparer.Ordinal));
        Assert.True(Assert.Single(snap.AppealsOf("AFG")).Regional);
        Assert.False(Assert.Single(snap.AppealsOf("HTI")).Regional);
    }

    [Fact]
    public async Task UnresolvedCodesAreListedNotDropped()
    {
        var csv = Header + "ISL,5,Plan,P,1,Flash appeal,,,2026,100,10,10\nZZZ,6,Plan,P,1,Flash appeal,,,2026,100,10,10\n";
        var ind = new IndicatorStore();
        var crises = new CrisisStore();
        await Make(HttpWithCsv(csv), ind, crises).RunOnceAsync(default);
        Assert.DoesNotContain(ind.Snapshot.Appeals, a => a.Iso3 == "ZZZ");
        var u = Assert.Single(crises.Snapshot.Unplaced);
        Assert.Equal("FTS", u.Source);
        Assert.Contains("ZZZ", u.Title);
        Assert.True(crises.Snapshot.CheckedUtc.ContainsKey("FTS"));
    }

    [Fact]
    public async Task CreditFollowsFtsTerms()
    {
        var ind = await Loaded();
        var c = ind.Snapshot.Appeals[0].Citation;
        // The resource's last_modified in the sample package is 2026-11-24.
        Assert.Equal("Financial Tracking Service (FTS), 24 Nov 2026, https://fts.unocha.org", c.Attribution);
        Assert.Equal(new DateTime(2026, 11, 24), c.AsOfUtc.Date);
        Assert.Contains("CC BY", c.Licence);
        Assert.Equal(CitationDate.AsOf, c.DateMeaning);
    }

    [Fact]
    public async Task AMissingColumnIsFormatChangedAndKeepsTheRows()
    {
        var ind = await Loaded();
        var before = ind.Snapshot;
        var feed = Make(HttpWithCsv("countryCode,id,name\nAFG,1,x\n"), ind);
        var ex = await Assert.ThrowsAsync<FeedFormatException>(() => feed.RunOnceAsync(default));
        Assert.StartsWith("format changed:", ex.Message);
        Assert.Same(before, ind.Snapshot);
        Assert.Equal(TimeSpan.FromDays(1), feed.Interval);
        Assert.Equal("FTS", feed.Name);
    }
}
