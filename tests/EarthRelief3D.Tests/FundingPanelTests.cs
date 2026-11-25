using EarthRelief3D.App.ViewModels;
using EarthRelief3D.Core.Feeds;
using EarthRelief3D.Core.Model;
using EarthRelief3D.Core.Store;
using GlobeCore.Feeds;

namespace EarthRelief3D.Tests;

/// <summary>The appeal-funding section and the Logistics Cluster link (Plan 4 Task 3; rulings P4-4, R7).</summary>
public class FundingPanelTests
{
    private static async Task<IndicatorStore> Loaded()
    {
        var ind = new IndicatorStore();
        Assert.Equal(GlobeCore.Feeds.FeedRunResult.Ok,
            await new FtsFundingFeed(Samples.FtsHttp().Http, ind, new CrisisStore(), new FixedClock(AccessPanelTests.Now), new ListLog()).RunOnceAsync(default));
        return ind;
    }

    private static PanelSection Funding(CountryPanelViewModel p, int year = 2026) => AccessPanelTests.Section(p, $"Humanitarian appeals {year}");

    [Fact]
    public async Task PlanRow()
    {
        var panel = AccessPanelTests.Panel(await Loaded());
        panel.Show("AFG");
        var s = Funding(panel);
        Assert.Equal([("Afghanistan Humanitarian Needs and Response Plan 2026", "34% funded: $580,680,803 of $1,714,181,496")], s.Rows);
        Assert.StartsWith("Source: ", s.SourceLine);
        Assert.Contains("24 Nov 2026", s.SourceLine);
        Assert.NotNull(s.Link);
        // The funding section follows the Getting help in section.
        Assert.Equal(CountryPanelViewModel.GettingHelpHeading, panel.Sections[^2].Heading);
        Assert.Equal(s, panel.Sections[^1]);
    }

    private static AppealFunding Plan(string iso3, long? req, long funding, int? pct, bool regional)
    {
        var cite = new SourceCitation(FtsFundingFeed.Publisher, "Requirements and funding", "2026", FtsFundingFeed.CreditUrl, "CC BY-IGO",
            "FTS", AccessPanelTests.Now, AccessPanelTests.Now, CitationDate.AsOf);
        return new AppealFunding(iso3, 7, "A plan", "Flash appeal", 2026, req, funding, pct, regional, cite);
    }

    [Fact]
    public void RegionalPlanSaysWholePlan()
    {
        Assert.Equal("34% funded: $1,000 of $3,000 (regional plan: figures for the whole plan, all its countries)",
            CountryPanelViewModel.FundingText(Plan("FJI", 3000, 1000, 34, regional: true)));
        var ind = new IndicatorStore();
        ind.ReplaceAppeals([Plan("FJI", 3000, 1000, 34, true)]);
        var panel = AccessPanelTests.Panel(ind);
        panel.Show("FJI");
        Assert.EndsWith(" (regional plan: figures for the whole plan, all its countries)", Funding(panel).Rows[0].Value);
    }

    [Fact]
    public void NullsAreNeverFilledIn()
    {
        Assert.Equal("$1,000 received; requirement not published", CountryPanelViewModel.FundingText(Plan("FJI", null, 1000, null, false)));
        // FTS gave no percentage: ours is not worked out (P4-4).
        Assert.Equal("$1,000 of $3,000 (FTS gives no percentage)", CountryPanelViewModel.FundingText(Plan("FJI", 3000, 1000, null, false)));
    }

    [Fact]
    public async Task LastYearShownSaysNoAppealYet()
    {
        var h = new SampleHttpHandler();
        h.Map(FtsFundingFeed.PackageUrl.ToString(), Samples.Path("hdx_fts_package.json"));
        h.MapText(Samples.FtsCsv, "countryCode,id,name,code,typeId,typeName,startDate,endDate,year,requirements,funding,percentFunded\n"
            + "AFG,1,Afghanistan 2027,A27,1,Flash appeal,,,2027,500,50,10\n"
            + "SDN,3,Sudan 2026,S26,1,Flash appeal,,,2026,900,450,50\n");
        var ind = new IndicatorStore();
        var now = new DateTime(2027, 1, 15, 12, 0, 0, DateTimeKind.Utc);
        await new FtsFundingFeed(new GlobeCore.Feeds.FeedHttp("test", h), ind, new CrisisStore(), new FixedClock(now), new ListLog()).RunOnceAsync(default);
        var panel = new CountryPanelViewModel(new CrisisStore(), iso => CountryResolver.LoadDefault().ByIso3(iso)?.Name, nowUtc: () => now,
            indicators: ind, logistics: AccessPanelTests.Logistics);
        panel.Show("SDN");
        var s = Funding(panel, 2026);
        Assert.Equal("Humanitarian appeals 2026", s.Heading);
        Assert.Equal(("Appeal", "No appeal in FTS for 2027 yet"), s.Rows[0]);
        Assert.Equal("Sudan 2026", s.Rows[1].Label);
        Assert.Equal("No appeal in FTS for 2027 yet", CountryPanelViewModel.NoAppealYet(2027));
        panel.Show("AFG");
        Assert.DoesNotContain(Funding(panel, 2027).Rows, r => r.Value.Contains("yet"));
    }

    [Fact]
    public async Task NoAppealSaysSo()
    {
        var panel = AccessPanelTests.Panel(await Loaded());
        panel.Show("ISL");
        Assert.Equal([("Appeal", "No appeal in FTS for 2026")], Funding(panel).Rows);

        var before = AccessPanelTests.Panel(new IndicatorStore());
        before.Show("ISL");
        Assert.Equal([("Appeals", "Data not loaded yet")], AccessPanelTests.Section(before, CountryPanelViewModel.FundingNotLoadedHeading).Rows);
    }

    [Fact]
    public async Task LogisticsClusterLinkWhenListed()
    {
        var panel = AccessPanelTests.Panel(await AccessPanelTests.Loaded());
        panel.Show("SDN");
        var link = Assert.Single(panel.Sections.SelectMany(s => s.Links ?? []), l => l.Text.StartsWith("Logistics Cluster"));
        Assert.Equal("Logistics Cluster: Sudan operation", link.Text);
        Assert.Equal("https://logcluster.org/en/where-we-work/sdn/sudan", link.Url.ToString());
        Assert.Contains(AccessPanelTests.Section(panel, CountryPanelViewModel.GettingHelpHeading).Rows, r => r.Label == "Logistics" && r.Value.Contains("checked 24 Nov 2026"));

        panel.Show("ISL");
        Assert.DoesNotContain(panel.Sections.SelectMany(s => s.Links ?? []), l => l.Text.StartsWith("Logistics Cluster"));
    }
}
