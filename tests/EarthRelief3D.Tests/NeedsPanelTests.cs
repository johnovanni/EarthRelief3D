using EarthRelief3D.App.ViewModels;
using EarthRelief3D.Core.Feeds;
using EarthRelief3D.Core.Model;
using EarthRelief3D.Core.Store;

namespace EarthRelief3D.Tests;

public class NeedsPanelTests
{
    private static readonly DateTime Now = new(2026, 11, 23, 12, 0, 0, DateTimeKind.Utc);

    private static CountryPanelViewModel Panel(IndicatorStore? ind) =>
        new(new CrisisStore(), iso => CountryResolver.LoadDefault().ByIso3(iso)?.Name, nowUtc: () => Now, indicators: ind);

    private static async Task<IndicatorStore> Loaded()
    {
        var ind = new IndicatorStore();
        var crises = new CrisisStore();
        var clock = new FixedClock(Now);
        await new GhoFeed(Samples.GhoHttp().Http, ind, crises, clock, new ListLog()).RunOnceAsync(default);
        await new HnoFeed(Samples.HnoHttp().Http, ind, crises, clock, new ListLog()).RunOnceAsync(default);
        return ind;
    }

    private static PanelSection Section(CountryPanelViewModel p, string heading) => Assert.Single(p.Sections, s => s.Heading == heading);

    [Fact]
    public async Task SudanListsPlanRows()
    {
        var panel = Panel(await Loaded());
        panel.Show("SDN");

        var basic = Section(panel, CountryPanelViewModel.BasicNeedsHeading);
        Assert.Equal("Basic needs", basic.Heading);
        // The three GHO values as the map shows them (the share without), each with its year and source.
        Assert.Equal(
            [("No basic drinking water", "77.9% of people (2024, WHO GHO)"),
             ("No basic sanitation", "6.6% of people (2024, WHO GHO)"),
             ("Health services not covered", "57 points (2023, WHO GHO; 100 minus the UHC service coverage index)")],
            basic.Rows);
        Assert.StartsWith("Source: World Health Organization, Global Health Observatory", basic.SourceLine);
        Assert.Contains("as of 31 Dec 2024", basic.SourceLine);
        Assert.NotNull(basic.Link);

        // Sudan has the ALL row only (R7): the total, and no invented cluster rows.
        var plan = Section(panel, "People in need (UN humanitarian plan 2026)");
        Assert.Equal(
            [("Total (ALL)", "33,699,770 people, 65% of 51,700,000; 20,419,163 targeted"),
             ("Figures are", "Final")],
            plan.Rows);
        Assert.Equal("Source: OCHA Humanitarian Programme Cycle Tools (HPC Tools), via HDX, Global HPC HNO 2026 · file loaded 13 Feb 2026", plan.SourceLine);
        Assert.NotNull(plan.Link);
    }

    [Fact]
    public async Task ACountryWithClusterRowsListsThemInOrderAndYemenSaysGhoEstimate()
    {
        var panel = Panel(await Loaded());
        panel.Show("AFG");
        var plan = Section(panel, "People in need (UN humanitarian plan 2026)");
        Assert.Equal(
            ["Total (ALL)", "Food security (FSC)", "Water, sanitation and hygiene (WSH)", "Shelter (SHL)", "Health (HEA)", "Figures are"],
            plan.Rows.Select(r => r.Label));
        Assert.Equal("17,412,653 people in need; 11,911,619 targeted", plan.Rows[1].Value);
        Assert.Equal("Plan caseload", plan.Rows[^1].Value);

        panel.Show("YEM");
        var yem = Section(panel, "People in need (UN humanitarian plan 2026)");
        Assert.Equal("GHO Estimates: not from this country's own needs overview", yem.Rows[^1].Value);
    }

    [Fact]
    public async Task NonHnoCountrySaysNoPlan()
    {
        var panel = Panel(await Loaded());
        panel.Show("ISL");

        var none = Section(panel, "No UN humanitarian needs plan for 2026 lists this country");
        Assert.Empty(none.Rows);
        Assert.Contains("Global HPC HNO 2026", none.SourceLine);
        Assert.DoesNotContain(panel.Sections, s => s.Heading.StartsWith("People in need", StringComparison.Ordinal));
        // The GHO values are still there.
        Assert.Equal(3, Section(panel, CountryPanelViewModel.BasicNeedsHeading).Rows.Count);
    }

    [Fact]
    public async Task ACountryWithoutAGhoFigureSaysSo()
    {
        var ind = await Loaded();
        var panel = Panel(ind);
        panel.Show("GRL");   // Greenland: in no sample
        var basic = Section(panel, CountryPanelViewModel.BasicNeedsHeading);
        Assert.All(basic.Rows, r => Assert.Equal("No figure from WHO GHO", r.Value));
    }

    [Fact]
    public async Task AValueWithoutAClassIsNotAFigureInThePanelEither()
    {
        // The map shades a value only when it falls in a class (HasFigure). A negative "without" share is none.
        var ind = await Loaded();
        var water = ind.Snapshot.Get("gho.water")!;
        var bad = water.Values[0] with { Iso3 = "ISL", Value = -0.2 };
        ind.Replace("gho.water", water with { Values = [bad] });
        var panel = Panel(ind);
        panel.Show("ISL");
        Assert.Equal("No figure from WHO GHO", Section(panel, CountryPanelViewModel.BasicNeedsHeading).Rows[0].Value);
    }

    [Fact]
    public void WithNothingLoadedThePanelSaysSo()
    {
        var panel = Panel(new IndicatorStore());
        panel.Show("SDN");
        var basic = Section(panel, CountryPanelViewModel.BasicNeedsHeading);
        Assert.All(basic.Rows, r => Assert.Equal("Data not loaded yet", r.Value));
        Assert.Contains("Data not loaded yet", basic.SourceLine);
        var plan = Assert.Single(panel.Sections, s => s.Heading.StartsWith("People in need", StringComparison.Ordinal));
        Assert.Equal("Data not loaded yet", plan.Rows[0].Value);
    }

    [Fact]
    public void WithoutAnIndicatorStoreThereIsNoNeedsSection()
    {
        var panel = Panel(null);
        panel.Show("SDN");
        Assert.Empty(panel.Sections);
    }

    [Fact]
    public async Task ThePanelFollowsTheStoreWhileOpen()
    {
        var ind = new IndicatorStore();
        var panel = Panel(ind);
        panel.Show("SDN");
        Assert.All(Section(panel, CountryPanelViewModel.BasicNeedsHeading).Rows, r => Assert.Equal("Data not loaded yet", r.Value));

        var loaded = await Loaded();
        ind.Load(loaded.Snapshot);
        Assert.Equal("77.9% of people (2024, WHO GHO)", Section(panel, CountryPanelViewModel.BasicNeedsHeading).Rows[0].Value);
    }

    [Fact]
    public async Task NeedsSectionsDoNotMakeACrisis()
    {
        var panel = Panel(await Loaded());
        panel.Show("SDN");
        Assert.True(panel.HasNoCrisis);   // no crisis feed ran: the crisis rule is untouched by the needs sections
    }
}
