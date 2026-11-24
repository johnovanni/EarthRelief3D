using System.IO;
using EarthRelief3D.App.Layers;
using EarthRelief3D.App.Presentation;
using EarthRelief3D.App.ViewModels;
using EarthRelief3D.Core.Feeds;
using EarthRelief3D.Core.Help;
using EarthRelief3D.Core.Model;
using EarthRelief3D.Core.Store;

namespace EarthRelief3D.Tests;

/// <summary>The Getting help in section's access rows (Plan 4 Task 3; rulings R5, R8, P4-2).</summary>
public class AccessPanelTests
{
    internal static readonly DateTime Now = new(2026, 11, 24, 12, 0, 0, DateTimeKind.Utc);

    internal static LogisticsClusterLinks Logistics { get; } = LogisticsClusterLinks.Load(Path.Combine(Repo.Root(), "data", "help"));

    internal static CountryPanelViewModel Panel(IndicatorStore? ind) =>
        new(new CrisisStore(), iso => CountryResolver.LoadDefault().ByIso3(iso)?.Name, nowUtc: () => Now, indicators: ind, logistics: Logistics);

    internal static async Task<IndicatorStore> Loaded()
    {
        var ind = new IndicatorStore();
        Assert.Equal(GlobeCore.Feeds.FeedRunResult.Ok,
            await new AcapsAccessFeed(Samples.AcapsHttp().Http, ind, new CrisisStore(), new FixedClock(Now), new ListLog()).RunOnceAsync(default));
        return ind;
    }

    internal static PanelSection Section(CountryPanelViewModel p, string heading) => Assert.Single(p.Sections, s => s.Heading == heading);

    private static SourceCitation Cite() => new(AcapsAccessFeed.Publisher, AcapsAccessFeed.Dataset, "December 2025", AcapsAccessFeed.PageUrl,
        AcapsAccessFeed.Licence, "ACAPS", new DateTime(2025, 12, 31, 0, 0, 0, DateTimeKind.Utc), Now, CitationDate.Release)
    { DateText = "December 2025 edition" };

    /// <summary>A store holding one assessment (and its score series), made by hand.</summary>
    private static IndicatorStore With(int score, params (int Number, int Score)[] indicators)
    {
        var c = Cite();
        var a = new AccessAssessment("MMR", score, [score, score, score],
            [.. indicators.Select(i => new AccessIndicator(i.Number, "x", i.Score))], "Stable", 0.1, "December 2025 edition", c);
        var ind = new IndicatorStore();
        ind.ReplaceAccess([a], new IndicatorSeries(AcapsAccessFeed.ScoreSeriesId, AcapsAccessFeed.ScoreMeasures, "score (0 to 5)",
            [new CountryValue("MMR", score, 2025, c)]));
        return ind;
    }

    [Fact]
    public async Task ScoreUsesAcapsWords()
    {
        var panel = Panel(await Loaded());
        panel.Show("SDN");
        var s = Section(panel, CountryPanelViewModel.GettingHelpHeading);

        Assert.Equal(("Access constraints", "5 of 5: Extreme access constraints"), s.Rows[0]);
        Assert.Contains(("Trend", "Deteriorating"), s.Rows);
        // Nine indicator rows, labelled with the website's wording, in number order, valued "<n> of 3" (SDN: 3,3,3,3,3,3,3,2,2).
        var indicators = s.Rows.Where(r => AccessPanel.IndicatorWording.Values.Contains(r.Label)).ToList();
        Assert.Equal(AccessPanel.IndicatorWording.OrderBy(w => w.Key).Select(w => w.Value), indicators.Select(r => r.Label));
        Assert.Equal(["3 of 3", "3 of 3", "3 of 3", "3 of 3", "3 of 3", "3 of 3", "3 of 3", "2 of 3", "2 of 3"], indicators.Select(r => r.Value));
        Assert.Equal("Source: ACAPS, via HDX, Humanitarian Access dataset · December 2025 edition", s.SourceLine);
        Assert.NotNull(s.Link);
    }

    [Fact]
    public void TheNineIndicatorTextsAreAcapsWebsiteWording()
    {
        Assert.Equal(Enumerable.Range(1, 9), AccessPanel.IndicatorWording.Keys.Order());
        Assert.Equal("Denial of existence of humanitarian needs or entitlements to assistance", AccessPanel.IndicatorWording[1]);
        Assert.Equal("Physical constraints in the environment", AccessPanel.IndicatorWording[9]);
        Assert.Equal(["No constraints", "Low access constraints", "Moderate access constraints", "High access constraints",
            "Very high access constraints", "Extreme access constraints"], AccessPanel.LevelNames);
    }

    [Fact]
    public void AnIndicatorTheAssessmentLeavesOutIsAnInformationGap()
    {
        var panel = Panel(With(3, (1, 2), (2, 3), (4, 0), (5, 1), (6, 1), (7, 2), (8, 0), (9, 1)));   // no indicator 3 (R8)
        panel.Show("MMR");
        var s = Section(panel, CountryPanelViewModel.GettingHelpHeading);
        Assert.Equal(("Impediments to enter the country", "No score (information gap)"),
            s.Rows.Single(r => r.Label == AccessPanel.IndicatorWording[3]));
        // 0 of 3 is a score, not a gap.
        Assert.Equal("0 of 3", s.Rows.Single(r => r.Label == AccessPanel.IndicatorWording[4]).Value);
    }

    [Fact]
    public async Task NoAssessmentSaysSo()
    {
        var panel = Panel(await Loaded());
        panel.Show("ISL");
        var s = Section(panel, CountryPanelViewModel.GettingHelpHeading);
        Assert.Equal([("Access constraints", "No ACAPS assessment for Iceland in the December 2025 edition")], s.Rows);
        Assert.Equal("Source: ACAPS, via HDX, Humanitarian Access dataset · December 2025 edition", s.SourceLine);

        // Before the feed has run: never a blank that looks fine.
        var before = Panel(new IndicatorStore());
        before.Show("ISL");
        Assert.Equal([("Access constraints", "Data not loaded yet")], Section(before, CountryPanelViewModel.GettingHelpHeading).Rows);
    }

    [Fact]
    public void ZeroIsNoConstraints()
    {
        var ind = With(0);
        var scale = ShadingScales.For(ShadingLayer.Access);
        Assert.Equal(ShadingScales.AccessColors[0], scale.ColorOf(0));   // shaded the lightest class (R5)
        Assert.True(CountryValueLayer.HasFigure(ind.Snapshot.Get(AcapsAccessFeed.ScoreSeriesId)!.For("MMR")!, scale));
        Assert.Equal("Myanmar · Humanitarian access constraints (ACAPS score 0–5): 0 of 5: No constraints · ACAPS, December 2025 edition",
            CountryValueLayer.Tooltip(ShadingLayer.Access, "Myanmar", "MMR", ind.Snapshot.Get(AcapsAccessFeed.ScoreSeriesId), scale));

        var panel = Panel(ind);
        panel.Show("MMR");
        Assert.Equal(("Access constraints", "0 of 5: No constraints"), Section(panel, CountryPanelViewModel.GettingHelpHeading).Rows[0]);
    }

    [Fact]
    public async Task EveryCountryHasTheSectionAfterTheNeedsSections()
    {
        var panel = Panel(await Loaded());
        panel.Show("ISL");
        var headings = panel.Sections.Select(s => s.Heading).ToList();
        Assert.Equal(CountryPanelViewModel.GettingHelpHeading, headings[^2]);   // then the funding section
        Assert.True(headings.IndexOf(CountryPanelViewModel.ResourcesHeading) < headings.IndexOf(CountryPanelViewModel.GettingHelpHeading));
        // Neither section is a crisis: Iceland still says it has none.
        Assert.True(panel.HasNoCrisis);
    }

    [Fact]
    public async Task TheAccessFocusOpensAtTheSection()
    {
        var panel = Panel(await Loaded());
        panel.Show("SDN", section: PanelFocus.Access);
        Assert.Equal(CountryPanelViewModel.GettingHelpHeading, panel.Sections[panel.FocusedSectionIndex].Heading);
        panel.Show("SDN", section: PanelFocus.Help);   // this panel has no help tables (Task 6): no step section, nothing to focus
        Assert.Equal(-1, panel.FocusedSectionIndex);
    }
}
