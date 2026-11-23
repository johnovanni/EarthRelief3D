using EarthRelief3D.App.Presentation;
using EarthRelief3D.App.ViewModels;
using EarthRelief3D.Core.Feeds;
using EarthRelief3D.Core.Model;
using EarthRelief3D.Core.Store;

namespace EarthRelief3D.Tests;

/// <summary>R17 in words: the tooltip, the panel's "Country shading" row, the phase bar and the legend.</summary>
public class FoodPanelTests
{
    private static readonly DateTime Now = SampleStore.Now;

    private static FoodCrisis Dated(FoodCrisis f) => f with { Citations = [f.Citations[0] with { Version = "Sep 2026" }] };

    private static CountryPanelViewModel Panel(CrisisStore store) =>
        new(store, iso => CountryResolver.LoadDefault().ByIso3(iso)?.Name, nowUtc: () => Now);

    private static PanelSection FoodSectionOf(params FoodCrisis[] food)
    {
        var store = new CrisisStore();
        store.Load(new CrisisSnapshot([.. food], [], []));
        var panel = Panel(store);
        panel.Show(food[0].CountryIso3);
        return panel.Sections.Single(s => s.Heading.StartsWith("Food", StringComparison.Ordinal));
    }

    // ---------------------------------------------------------------- tooltip (GlobeCore's text-only tooltip)

    [Fact]
    public void TooltipNamesTheShadedPhaseThenTheWorstPhaseWithPeople()
    {
        Assert.Equal("Sudan · food crisis · shaded Phase 3 Crisis (20% rule) · worst phase with people: Phase 5 Catastrophe/Famine · "
                     + "people in Phase 3 Crisis 14,316,494, Phase 4 Emergency 5,015,231, Phase 5 Catastrophe/Famine 134,808 · "
                     + "IPC analysis period 1 Apr 2026–31 Dec 2026 (analysed Sep 2026)",
            CrisisText.FoodHover("Sudan", Dated(FoodMapTests.Sudan())));
        var somalia = Dated(FoodMapTests.Counts("SOM", 1000, 400, 200, 150, 250, 0));
        Assert.StartsWith("SOM · food crisis · shaded Phase 4 Emergency (20% rule) · people in Phase 3 Crisis 150, ", CrisisText.FoodHover("SOM", somalia));
        var outlined = FoodMapTests.Counts("PAK", 1000, 600, 250, 100, 50, 0);
        Assert.StartsWith("Pakistan · food crisis · not shaded (under 20%) · worst phase with people: Phase 4 Emergency · people in ",
            CrisisText.FoodHover("Pakistan", outlined));
        var unknown = TestData.Food() with { HighestPhase = 3 };
        Assert.StartsWith("Somalia · food crisis · not shaded (share unknown) · worst phase with people: Phase 3 Crisis · ",
            CrisisText.FoodHover("Somalia", unknown));
    }

    // ---------------------------------------------------------------- the panel

    [Fact]
    public async Task SudanPanelShowsTheShadingRowAndThePhaseBar()
    {
        var store = await SampleStore.LoadAsync();
        var panel = Panel(store);
        panel.Show("SDN");
        var food = panel.Sections.Single(s => s.Heading == CountryPanelViewModel.FoodHeading);
        Assert.Contains(("Country shading", "Phase 3 Crisis (IPC's 20% rule)"), food.Rows);
        Assert.Contains(("Outline", "Phase 5 Catastrophe/Famine: some people are in this phase"), food.Rows);
        Assert.DoesNotContain(food.Rows, r => r.Label == "Highest phase");
        Assert.Contains(("People in Phase 3 or higher", "19,466,533 (41.0% of the people analysed)"), food.Rows);

        var bar = Assert.IsType<PhaseBar>(food.Bar);
        Assert.True(bar.HasBar);
        Assert.Equal("of 47,535,794 people analysed", bar.OfAnalysed);
        Assert.Equal("The line falls in Phase 3, so Sudan is shaded Crisis, with a Phase 5 outline.", bar.Caption);
        Assert.Equal(
        [
            ("Phase 1 Minimal", "12,069,469", "25.4%"),
            ("Phase 2 Stressed", "15,999,822", "33.7%"),
            ("Phase 3 Crisis", "14,316,494", "30.1%"),
            ("Phase 4 Emergency", "5,015,231", "10.6%"),
            ("Phase 5 Catastrophe/Famine", "134,808", "0.3%"),
        ], bar.Rows.Select(r => (r.Name, r.People, r.Percent)));
        Assert.Equal([1, 2, 3, 4, 5], bar.Rows.Select(r => r.Phase));
        Assert.Equal(LayerStyle.IpcPhaseColor(5), bar.Rows[4].Color);
        // The same facts as text for screen readers: colour is never the only carrier.
        Assert.Equal("IPC phase bar for Sudan, of 47,535,794 people analysed: Phase 1 Minimal 25.4%, Phase 2 Stressed 33.7%, "
                     + "Phase 3 Crisis 30.1%, Phase 4 Emergency 10.6%, Phase 5 Catastrophe/Famine 0.3%. A dashed line marks the worst 20%. "
                     + "The line falls in Phase 3, so Sudan is shaded Crisis, with a Phase 5 outline.", bar.AutomationName);
    }

    [Fact]
    public void NotShadedCountrySaysSoAndNamesTheOutline()
    {
        var food = FoodSectionOf(FoodMapTests.Counts("PAK", 1000, 600, 250, 100, 50, 0));
        Assert.Contains(("Country shading", "Not shaded: no phase covers 20% of the people analysed"), food.Rows);
        Assert.Equal("The line falls in Phase 2, so Pakistan is not shaded, with a Phase 4 outline.", food.Bar!.Caption);
        var shadedNoOutline = FoodSectionOf(FoodMapTests.Counts("SOM", 1000, 400, 200, 150, 250, 0));
        Assert.Equal("The line falls in Phase 4, so Somalia is shaded Emergency.", shadedNoOutline.Bar!.Caption);
        Assert.DoesNotContain(shadedNoOutline.Rows, r => r.Label == "Outline");
    }

    [Fact]
    public void BlankCellsAreNotReportedAndPercentagesKeepSmallPhasesVisible()
    {
        var f = FoodMapTests.Counts("SOM", 2_000_000, 0, 1_000_000, 899_900, 100_000, 100) with { UnreportedPhases = 0b00001 };
        var bar = FoodSectionOf(f).Bar!;
        Assert.Equal(("not reported", "not reported"), (bar.Rows[0].People, bar.Rows[0].Percent));
        Assert.Equal(("100", "under 0.1%"), (bar.Rows[4].People, bar.Rows[4].Percent));
        Assert.Equal("50.0%", bar.Rows[1].Percent);
        Assert.Contains("Phase 1 Minimal not reported", bar.AutomationName);
    }

    [Fact]
    public void AnalysedBlankUsesThePhaseSumAndSaysSo()
    {
        var bar = FoodSectionOf(FoodMapTests.Counts("SOM", 0, 600, 150, 200, 50, 0)).Bar!;
        Assert.Equal("of 1,000 people analysed (the sum of the five phases; the file gives no total)", bar.OfAnalysed);
    }

    [Fact]
    public void NoCountsMeansShareUnknown()
    {
        var food = FoodSectionOf(TestData.Food() with { HighestPhase = 3 });
        Assert.Contains(("Country shading", "Not shaded: share unknown (the file gives no number of people analysed)"), food.Rows);
        var bar = food.Bar!;
        Assert.False(bar.HasBar);
        Assert.Equal("share unknown", bar.OfAnalysed);
        Assert.Equal("Share unknown, so Somalia is not shaded, with a Phase 3 outline.", bar.Caption);
        Assert.All(bar.Rows, r => Assert.Equal("", r.Percent));
    }

    [Fact]
    public void StaleAnalysisShowsTheBarButIsNotShaded()
    {
        var stale = FoodMapTests.Sudan() with { IsStale = true };
        var food = FoodSectionOf(stale);
        Assert.Equal(CountryPanelViewModel.PastFoodHeading, food.Heading);
        Assert.Equal("The line falls in Phase 3, but this analysis ended more than 12 months ago, so Sudan is not shaded.", food.Bar!.Caption);
    }

    [Fact]
    public void ProjectionOnlyHasNoBar()
    {
        var food = FoodSectionOf(TestData.Food() with { ProjectionOnly = true, ProjectionName = "first projection", HighestPhase = 0 });
        Assert.Equal(CountryPanelViewModel.ProjectionFoodHeading, food.Heading);
        Assert.Null(food.Bar);
    }

    // ---------------------------------------------------------------- the bar's layout

    [Fact]
    public void BarWidthsFillTheBarAndKeepEveryPhaseWithPeopleVisible()
    {
        long[] sudan = [12_069_469, 15_999_822, 14_316_494, 5_015_231, 134_808];
        var w = PhaseBarLayout.Widths(sudan, 352);
        Assert.Equal(352, w.Sum(), 6);
        Assert.Equal(PhaseBarLayout.MinPx, w[4], 6);   // 0.3% would be 1 px
        Assert.True(w[0] > w[3] && w[1] > w[2]);
        // A phase with nobody in it has no width; the rest share the bar in proportion.
        var two = PhaseBarLayout.Widths([300, 0, 100, 0, 0], 400);
        Assert.Equal([300.0, 0, 100, 0, 0], two);
        Assert.All(PhaseBarLayout.Widths([0, 0, 0, 0, 0], 300), x => Assert.Equal(0, x));
    }

    [Fact]
    public async Task TheMarkerFallsInTheShadedPhaseForEverySampleCountry()
    {
        // The bar is 100% stacked over the five counts and the 20% rule uses the people analysed: in the sample they agree.
        var s = (await SampleStore.LoadAsync()).Snapshot;
        int checkedRows = 0;
        foreach (var f in s.Crises.OfType<FoodCrisis>().Where(f => !f.ProjectionOnly))
        {
            var c = IpcAreaRule.Classify(f);
            var bar = PhaseBar.For(f.CountryIso3, f);
            if (!bar.HasBar) continue;
            double cumulative = 0;
            int markerPhase = 0;
            foreach (var seg in bar.Segments)
            {
                cumulative += seg.Fraction;
                if (cumulative > 0.8 + 1e-12) { markerPhase = seg.Phase; break; }
            }
            Assert.True(markerPhase == c.LinePhase, $"{f.CountryIso3}: marker in Phase {markerPhase}, rule says {c.LinePhase}");
            checkedRows++;
        }
        Assert.True(checkedRows > 30);
    }

    // ---------------------------------------------------------------- the legend

    [Fact]
    public void LayerStripTooltipUsesTheLegendsWording()
    {
        var xaml = System.IO.File.ReadAllText(System.IO.Path.Combine(Repo.Root(), "src", "EarthRelief3D.App", "Views", "LayerStrip.xaml"));
        Assert.Contains("ToolTip=\"{x:Static vm:LegendViewModel.FoodTooltip}\"", xaml);
        Assert.DoesNotContain("highest phase with people", xaml);
        Assert.Equal("Food crisis (IPC): " + LegendViewModel.FoodRule, LegendViewModel.FoodTooltip);
        Assert.Equal("Food crisis: " + LegendViewModel.FoodRule, LegendViewModel.Measures(CrisisLayer.Food));
    }

    [Fact]
    public void LegendDescribesTheRuleTheOutlineAndTheFamineNote()
    {
        Assert.Equal("Food crisis: the country is shaded by the worst IPC phase that covers at least 20% of the people analysed "
                     + "(IPC's 20% rule, applied to the whole country). An outline in a worse phase's colour means some people are in that phase.",
            LegendViewModel.Measures(CrisisLayer.Food));
        var store = new CrisisStore();
        store.Load(new CrisisSnapshot([Dated(FoodMapTests.Sudan())], [], []));
        var legend = new LegendViewModel(store, new LayerStripViewModel(new MemorySettingsStore()), nowUtc: () => Now);
        var food = legend.Lines.Single(l => l.Layer == CrisisLayer.Food);
        Assert.Equal(["Phase 3 Crisis", "Phase 4 Emergency", "Phase 5 Catastrophe/Famine", LegendViewModel.OutlineKeyLabel],
            food.Keys.Select(k => k.Label));
        Assert.True(food.Keys[^1].IsOutline);
        Assert.DoesNotContain(food.Keys.SkipLast(1), k => k.IsOutline);
        Assert.Equal("A country shaded Phase 5 is not an IPC famine classification, which also needs malnutrition and death-rate data.",
            food.Note);
        Assert.All(legend.Lines.Where(l => l.Layer != CrisisLayer.Food), l => Assert.Null(l.Note));
    }
}
