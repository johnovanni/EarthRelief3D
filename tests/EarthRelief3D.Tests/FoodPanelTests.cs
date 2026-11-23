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
        // Outline-only countries (round 2): the faint tint and outline, in the same words as the panel.
        Assert.StartsWith("Pakistan · food crisis · not shaded (no phase covers 20%) · faint Phase 4 tint and outline: some people are in Phase 4 · people in ",
            CrisisText.FoodHover("Pakistan", outlined));
        var unknown = TestData.Food() with { HighestPhase = 3 };
        Assert.StartsWith("Somalia · food crisis · not shaded (share unknown) · faint Phase 3 tint and outline: some people are in Phase 3 · ",
            CrisisText.FoodHover("Somalia", unknown));
    }

    [Fact]
    public void CountryHoverUsesTheLatestRecordLikeTheMapAndThePanel()
    {
        var current = Dated(FoodMapTests.Counts("SOM", 1000, 400, 200, 150, 250, 0)) with { PeriodTo = new DateOnly(2026, 9, 30) };
        var projection = current with { ProjectionOnly = true, ProjectionName = "first projection", HighestPhase = 0, PeriodTo = new DateOnly(2027, 1, 31) };
        var store = new CrisisStore();
        store.Load(new CrisisSnapshot([current, projection], [], []));
        var hover = CrisisText.CountryHover("Somalia", "SOM", store.Snapshot, Now);
        Assert.Contains("IPC: projection only", hover);
        Assert.DoesNotContain(CrisisText.FoodKind, hover);
        // The other way round: the current analysis is the latest.
        store.Load(new CrisisSnapshot([current with { PeriodTo = new DateOnly(2027, 3, 31) }, projection], [], []));
        hover = CrisisText.CountryHover("Somalia", "SOM", store.Snapshot, Now);
        Assert.Contains(CrisisText.FoodKind, hover);
        Assert.DoesNotContain("projection only", hover);
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
        // The sample's five phases add up to 30 more than the people analysed (rounding, under 1%): the sum is D for the
        // arithmetic, and the text names the people analysed and the small difference.
        Assert.Equal("of 47,535,794 people analysed (the phase numbers add up to 30 more)", bar.OfAnalysed);
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
        Assert.Equal([1, 2, 3, 4, 5], bar.Segments.Select(s => s.Phase));   // nothing outside the five phases
        Assert.Null(bar.Note);   // shaded Phase 3: no famine note
        // The same facts as text for screen readers, people as well as shares: colour is never the only carrier.
        Assert.Equal("IPC phase bar for Sudan, of 47,535,794 people analysed (the phase numbers add up to 30 more): "
                     + "Phase 1 Minimal 25.4%, 12,069,469 people; Phase 2 Stressed 33.7%, 15,999,822 people; Phase 3 Crisis 30.1%, 14,316,494 people; "
                     + "Phase 4 Emergency 10.6%, 5,015,231 people; Phase 5 Catastrophe/Famine 0.3%, 134,808 people. A dashed line marks the worst 20%. "
                     + "The line falls in Phase 3, so Sudan is shaded Crisis, with a Phase 5 outline.", bar.AutomationName);
    }

    [Fact]
    public void AnalysedLargerThanTheSumShowsTheRestAsNotInAPhaseAndTheMarkerAgrees()
    {
        var f = FoodMapTests.Counts("SOM", 100, 0, 20, 30, 20, 10) with { UnreportedPhases = 0b00001 };
        var food = FoodSectionOf(f);
        var bar = food.Bar!;
        Assert.Equal("of 100 people analysed", bar.OfAnalysed);
        // The people in no reported phase come first (left), so the right-hand 20% are always the worst-off.
        Assert.Equal([0, 1, 2, 3, 4, 5], bar.Segments.Select(s => s.Phase));
        Assert.Equal(0.2, bar.Segments[0].Fraction, 12);
        Assert.Equal(PhaseBar.NotInAPhaseColor, bar.Segments[0].Color);
        Assert.Equal(4, MarkerPhase(bar));
        Assert.Equal(4, IpcAreaRule.Classify(f).LinePhase);
        Assert.Equal(("Not in a reported phase", "20", "20.0%"), (bar.Rows[^1].Name, bar.Rows[^1].People, bar.Rows[^1].Percent));
        Assert.Equal("The line falls in Phase 4, so Somalia is shaded Emergency, with a Phase 5 outline.", bar.Caption);
        Assert.Contains("Not in a reported phase 20.0%, 20 people", bar.AutomationName);
        Assert.Contains(("People in Phase 3 or higher", "60 (60.0% of the people analysed)"), food.Rows);
    }

    [Fact]
    public void SumLargerThanAnalysedIsTheDenominatorAndTheMarkerAgrees()
    {
        var f = FoodMapTests.Counts("SOM", 900, 500, 200, 100, 150, 50);   // the phases add up to 1,000: 11% more, not rounding
        var food = FoodSectionOf(f);
        var bar = food.Bar!;
        Assert.Equal("of 1,000 people (the five phases add up to more than the 900 people analysed)", bar.OfAnalysed);
        // M4: the share names the total it is of.
        Assert.Contains(("People in Phase 3 or higher", "300 (30.0% of the 1,000 people in the five phases)"), food.Rows);
        Assert.Equal([1, 2, 3, 4, 5], bar.Segments.Select(s => s.Phase));
        Assert.Equal(4, MarkerPhase(bar));   // 20% in Phase 4 or higher, exactly
        Assert.Equal(4, IpcAreaRule.Classify(f).LinePhase);
        Assert.Equal("50.0%", bar.Rows[0].Percent);
    }

    [Fact]
    public void NoPhaseReachingTheWorst20PercentIsSaidSo()
    {
        // 15 of 100 analysed in a reported phase: the share is known, but even Phase 1 or higher is under 20%.
        var f = FoodMapTests.Counts("SOM", 100, 0, 0, 10, 5, 0) with { UnreportedPhases = 0b00011 };
        var bar = FoodSectionOf(f).Bar!;
        Assert.Equal("No phase reaches the worst 20%, so Somalia is not shaded, with a faint Phase 4 tint and outline.", bar.Caption);
        Assert.Null(MarkerPhase(bar));   // the marker is in the "not in a reported phase" piece
        Assert.Null(IpcAreaRule.Classify(f).LinePhase);
        // Without an outline.
        var none = FoodMapTests.Counts("SOM", 100, 0, 10, 0, 0, 0) with { UnreportedPhases = 0b00001 };   // nobody in Phase 3+
        Assert.Equal("No phase reaches the worst 20%, so Somalia is not shaded.", FoodSectionOf(none).Bar!.Caption);
    }

    [Fact]
    public void BlankPerPhaseCellsWithAPhase3PlusFigureHaveNoBarAndNoLine()
    {
        var f = TestData.Food() with { HighestPhase = 3, PopulationAnalysed = 1000, Phase3PlusPeople = 250, UnreportedPhases = 0b11111 };
        var food = FoodSectionOf(f);
        Assert.Contains(("People in Phase 3 or higher", "250 (25.0% of the people analysed)"), food.Rows);
        Assert.Contains(("Country shading", "Phase 3 Crisis (IPC's 20% rule)"), food.Rows);
        var bar = food.Bar!;
        Assert.False(bar.HasBar);
        Assert.Equal("The per-phase numbers are not reported, but 250 people (25.0% of the people analysed) are in Phase 3 or higher, "
                     + "so Somalia is shaded Crisis.", bar.Caption);
        Assert.DoesNotContain("line", bar.Caption, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("dashed", bar.AutomationName, StringComparison.Ordinal);
    }

    [Fact]
    public void APhase3PlusFigureAboveAllThePeopleAnalysedIsNotShownAsOver100Percent()
    {
        // M1: inconsistent data, 150 in Phase 3 or higher of 100 analysed.
        var f = TestData.Food() with { HighestPhase = 3, PopulationAnalysed = 100, Phase3PlusPeople = 150, UnreportedPhases = 0b11111 };
        var food = FoodSectionOf(f);
        Assert.Contains(("People in Phase 3 or higher", "150 (more than all the people analysed: the file's numbers disagree)"), food.Rows);
        Assert.Equal("The per-phase numbers are not reported, but 150 people (more than all the people analysed: the file's numbers disagree) "
                     + "are in Phase 3 or higher, so Somalia is shaded Crisis.", food.Bar!.Caption);
        Assert.DoesNotContain("150.0%", food.Bar!.AutomationName);
    }

    [Fact]
    public void ShadedPhase5CarriesTheFamineNoteUnderTheBar()
    {
        var bar = FoodSectionOf(FoodMapTests.Counts("SOM", 1000, 300, 200, 100, 100, 300)).Bar!;
        Assert.Equal(CrisisText.FamineNote, bar.Note);
        Assert.Equal(CrisisText.FamineNote, LegendViewModel.FamineNote);
        Assert.EndsWith(CrisisText.FamineNote, bar.AutomationName);
        // Not for a past analysis, which is not shaded.
        Assert.Null(FoodSectionOf(FoodMapTests.Counts("SOM", 1000, 300, 200, 100, 100, 300) with { IsStale = true }).Bar!.Note);
    }

    /// <summary>
    /// The phase of the piece the marker (80% from the left) falls in, worked out here in whole people from the segments;
    /// null for "not in a reported phase". Also checks that the bar's own MarkerIndex (what the view draws) agrees.
    /// </summary>
    private static int? MarkerPhase(PhaseBar bar)
    {
        long total = bar.Segments.Sum(s => s.People), cumulative = 0;
        int index = -1;
        for (int i = 0; i < bar.Segments.Count; i++)
        {
            cumulative += bar.Segments[i].People;
            if (cumulative * 5 > total * 4) { index = i; break; }
        }
        Assert.Equal(index, bar.MarkerIndex);
        return bar.Segments[index].Phase == 0 ? null : bar.Segments[index].Phase;
    }

    [Fact]
    public void NotShadedCountrySaysSoAndNamesTheOutline()
    {
        var food = FoodSectionOf(FoodMapTests.Counts("PAK", 1000, 600, 250, 100, 50, 0));
        Assert.Contains(("Country shading", "Not shaded (no phase covers 20% of the people); faint Phase 4 tint and outline: some people are in Phase 4"),
            food.Rows);
        Assert.DoesNotContain(food.Rows, r => r.Label == "Outline");   // said in the shading row
        Assert.Equal("The line falls in Phase 2, so Pakistan is not shaded, with a faint Phase 4 tint and outline.", food.Bar!.Caption);
        // Nobody in Phase 3 or higher (not a crisis record in practice): no tint, no outline.
        var none = FoodSectionOf(FoodMapTests.Counts("PAK", 1000, 600, 400, 0, 0, 0));
        Assert.Contains(("Country shading", "Not shaded (no phase covers 20% of the people)"), none.Rows);
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
        var food = FoodSectionOf(FoodMapTests.Counts("SOM", 0, 600, 150, 200, 50, 0));
        Assert.Equal("of 1,000 people analysed (the sum of the five phases; the file gives no total)", food.Bar!.OfAnalysed);
        Assert.Contains(("People in Phase 3 or higher", "250 (25.0% of the 1,000 people in the five phases)"), food.Rows);
    }

    [Fact]
    public void NoCountsMeansShareUnknown()
    {
        var food = FoodSectionOf(TestData.Food() with { HighestPhase = 3 });
        Assert.Contains(("Country shading", "Not shaded (share unknown: the file gives no number of people analysed); "
                                            + "faint Phase 3 tint and outline: some people are in Phase 3"), food.Rows);
        var bar = food.Bar!;
        Assert.False(bar.HasBar);
        Assert.Equal("share unknown", bar.OfAnalysed);
        // M3: the Phase 3+ figure is reported; say what is and is not.
        Assert.Equal("Share unknown: the file gives 1,200,000 people in Phase 3 or higher, but not the people analysed or the per-phase numbers, "
                     + "so Somalia is not shaded, with a faint Phase 3 tint and outline.", bar.Caption);
        // With no Phase 3+ figure either (not a crisis record in practice), neither is reported.
        var nothing = FoodSectionOf(TestData.Food() with { HighestPhase = 3, Phase3PlusPeople = 0 }).Bar!;
        Assert.StartsWith("Share unknown: neither the people analysed nor the per-phase numbers are reported, so Somalia is not shaded", nothing.Caption);
        Assert.All(bar.Rows, r => Assert.Equal("", r.Percent));
        Assert.Equal(5, bar.Rows.Count);
    }

    [Fact]
    public void StaleAnalysisShowsTheBarButIsNotShaded()
    {
        var stale = FoodMapTests.Sudan() with { IsStale = true };
        var food = FoodSectionOf(stale);
        Assert.Equal(CountryPanelViewModel.PastFoodHeading, food.Heading);
        Assert.Equal("The line falls in Phase 3. This analysis ended more than 12 months ago, so Sudan is not shaded.", food.Bar!.Caption);
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
        // The bar and the rule share one denominator, so the marker always falls in the rule's phase.
        var s = (await SampleStore.LoadAsync()).Snapshot;
        int checkedRows = 0;
        foreach (var f in s.Crises.OfType<FoodCrisis>().Where(f => !f.ProjectionOnly))
        {
            var c = IpcAreaRule.Classify(f);
            var bar = PhaseBar.For(f.CountryIso3, f);
            if (!bar.HasBar) continue;
            Assert.True(MarkerPhase(bar) == c.LinePhase, $"{f.CountryIso3}: marker in Phase {MarkerPhase(bar)}, rule says {c.LinePhase}");
            Assert.Equal(1.0, bar.Segments.Sum(x => x.Fraction), 9);
            checkedRows++;
        }
        Assert.True(checkedRows > 30);
    }

    [Fact]
    public void TheDrawnMarkerStaysInsideItsPieceWhenMinimumWidthsShiftTheEdges()
    {
        // 79%, 1%, 20%: the 1% piece is widened to 3 px, so 80% of the width would land in it; the marker belongs to the last piece.
        long[] people = [790, 10, 200];
        var widths = PhaseBarLayout.Widths(people, 100);
        double start = widths[0] + widths[1], end = start + widths[2];
        Assert.True(start > 80, "the setup: the edge has moved past 80%");
        Assert.InRange(PhaseBarLayout.MarkerX(widths, 2, 100), start, end);
        // Normally the marker is at 80% of the width.
        Assert.Equal(80, PhaseBarLayout.MarkerX(PhaseBarLayout.Widths([500, 500], 100), 1, 100), 9);
    }

    // ---------------------------------------------------------------- the legend

    [Fact]
    public void LayerStripTooltipUsesTheLegendsWording()
    {
        var xaml = System.IO.File.ReadAllText(System.IO.Path.Combine(Repo.Root(), "src", "EarthRelief3D.App", "Views", "LayerStrip.xaml"));
        // Plan 3: the strip's toggles are items of its view model (LayerToggle), each bound to its Tooltip; food's is the legend's wording.
        Assert.Contains("ToolTip=\"{Binding Tooltip}\"", xaml);
        var strip = new LayerStripViewModel(new MemorySettingsStore());
        Assert.Equal(LegendViewModel.FoodTooltip, strip.Groups.SelectMany(g => g.Toggles).Single(t => t.Id == "food").Tooltip);
        Assert.DoesNotContain("highest phase with people", xaml);
        Assert.Equal("Food crisis (IPC): " + LegendViewModel.FoodRule, LegendViewModel.FoodTooltip);
        Assert.Equal("Food crisis: " + LegendViewModel.FoodRule, LegendViewModel.Measures(CrisisLayer.Food));
    }

    [Fact]
    public void LegendDescribesTheRuleTheOutlineAndTheFamineNote()
    {
        Assert.Equal("Food crisis: the country is shaded by the worst IPC phase that covers at least 20% of the people analysed "
                     + "(IPC's 20% rule, applied to the whole country). An outline in a worse phase's colour means some people are in that phase; "
                     + "a faint tint with an outline alone means some people are in that phase, but no phase covers 20%.",
            LegendViewModel.Measures(CrisisLayer.Food));
        var store = new CrisisStore();
        store.Load(new CrisisSnapshot([Dated(FoodMapTests.Sudan())], [], []));
        var legend = new LegendViewModel(store, new LayerStripViewModel(new MemorySettingsStore()), nowUtc: () => Now);
        var food = legend.Lines.Single(l => l.Layer == CrisisLayer.Food);
        Assert.Equal(["Phase 3 Crisis", "Phase 4 Emergency", "Phase 5 Catastrophe/Famine", LegendViewModel.OutlineKeyLabel, LegendViewModel.FaintKeyLabel],
            food.Keys.Select(k => k.Label));
        Assert.Equal("Faint tint and outline: some people are in this phase, but no phase covers 20%", LegendViewModel.FaintKeyLabel);
        Assert.True(food.Keys[^2].IsOutline);
        Assert.True(food.Keys[^1].IsFaint);
        Assert.DoesNotContain(food.Keys.SkipLast(2), k => k.IsOutline || k.IsFaint);
        Assert.Equal("A country shaded Phase 5 is not an IPC famine classification, which also needs malnutrition and death-rate data.",
            food.Note);
        Assert.All(legend.Lines.Where(l => l.Layer != CrisisLayer.Food), l => Assert.Null(l.Note));
    }
}
