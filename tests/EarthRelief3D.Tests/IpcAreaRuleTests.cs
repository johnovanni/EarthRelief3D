using EarthRelief3D.Core.Feeds;
using EarthRelief3D.Core.Model;
using EarthRelief3D.Core.Store;

namespace EarthRelief3D.Tests;

/// <summary>R17: IPC's 20% rule applied to the whole country, the outline for a worse phase, and the shares for the bar.</summary>
public class IpcAreaRuleTests
{
    private static FoodCrisis F(long analysed, long p1, long p2, long p3, long p4, long p5, int highest = 3) => TestData.Food() with
    {
        HighestPhase = highest,
        Phase3PlusPeople = p3 + p4 + p5,
        PopulationAnalysed = analysed,
        Phase1People = p1,
        Phase2People = p2,
        Phase3People = p3,
        Phase4People = p4,
        Phase5People = p5,
    };

    [Fact]
    public void SudanIsShadedCrisisWithAPhase5Outline()
    {
        var c = IpcAreaRule.Classify(F(47_535_794, 12_069_469, 15_999_822, 14_316_494, 5_015_231, 134_808, highest: 5));
        Assert.Equal(3, c.ShadedPhase);
        Assert.Equal(5, c.OutlinePhase);
        Assert.Equal(3, c.LinePhase);
        Assert.True(c.ShareKnown);
        Assert.Equal(47_535_794, c.Denominator);
        Assert.Equal(14_316_494 / 47_535_794.0, c.Share(3), 12);
        Assert.Equal(134_808 / 47_535_794.0, c.Share(5), 12);
        Assert.Equal((14_316_494 + 5_015_231 + 134_808) / 47_535_794.0, c.ShareAtOrAbove(3), 12);
    }

    [Fact]
    public void QuarterInPhase4OrHigherWithNoPhase5IsShadedEmergencyWithNoOutline()
    {
        var c = IpcAreaRule.Classify(F(1000, 400, 200, 150, 250, 0, highest: 4));
        Assert.Equal(4, c.ShadedPhase);
        Assert.Null(c.OutlinePhase);
    }

    [Fact]
    public void ExactlyTwentyPercentIsShaded()
    {
        Assert.Equal(3, IpcAreaRule.Classify(F(1000, 500, 300, 200, 0, 0)).ShadedPhase);
        // The same boundary on Phase 4, and with a large population (no rounding of a fraction).
        Assert.Equal(4, IpcAreaRule.Classify(F(50_000_005, 30_000_004, 0, 0, 10_000_001, 0, highest: 4)).ShadedPhase);
        // One person short of 20% is not.
        Assert.Null(IpcAreaRule.Classify(F(1000, 501, 300, 199, 0, 0)).ShadedPhase);
    }

    [Fact]
    public void UnderTwentyPercentWithSomeInPhase4IsOutlinedOnly()
    {
        var c = IpcAreaRule.Classify(F(1000, 600, 250, 100, 50, 0, highest: 4));
        Assert.Null(c.ShadedPhase);
        Assert.Equal(4, c.OutlinePhase);
        Assert.Equal(2, c.LinePhase);
    }

    [Fact]
    public void TwentyPercentInPhase5IsShadedPhase5WithNoOutline()
    {
        var c = IpcAreaRule.Classify(F(1000, 300, 200, 100, 100, 300, highest: 5));
        Assert.Equal(5, c.ShadedPhase);
        Assert.Null(c.OutlinePhase);
    }

    [Fact]
    public void BlankPopulationAnalysedUsesThePhaseSum()
    {
        var c = IpcAreaRule.Classify(F(0, 600, 150, 200, 50, 0, highest: 4));
        Assert.True(c.ShareKnown);
        Assert.Equal(1000, c.Denominator);
        Assert.Equal(3, c.ShadedPhase);
        Assert.Equal(4, c.OutlinePhase);
        Assert.Equal(0.6, c.Share(1), 12);
    }

    [Fact]
    public void EveryCountBlankMeansTheShareIsUnknown()
    {
        var c = IpcAreaRule.Classify(F(0, 0, 0, 0, 0, 0) with { Phase3PlusPeople = 1_200_000 });
        Assert.False(c.ShareKnown);
        Assert.Equal(0, c.Denominator);
        Assert.Null(c.ShadedPhase);
        Assert.Null(c.LinePhase);
        // The worst phase with people: the reader's highest phase (3 when only the Phase 3+ number is given).
        Assert.Equal(3, c.OutlinePhase);
        Assert.All(Enumerable.Range(1, 5), p => Assert.Equal(0, c.Share(p)));
    }

    [Fact]
    public void ExactCountsDecideNotTheRoundedPercentage()
    {
        // Pakistan in the sample: the file rounds 10,553,872 of 53,441,149 (19.7%) to 0.2.
        var c = IpcAreaRule.Classify(F(53_441_149, 24_712_280, 18_174_997, 9_168_262, 1_385_610, 0, highest: 4) with { Phase3PlusShare = 0.2 });
        Assert.Null(c.ShadedPhase);
        Assert.Equal(4, c.OutlinePhase);
    }

    [Fact]
    public void StaleAndProjectionOnlyRecordsAreNeitherShadedNorOutlinedOnTheMap()
    {
        var sudan = F(47_535_794, 12_069_469, 15_999_822, 14_316_494, 5_015_231, 134_808, highest: 5);
        Assert.Equal((3, 5), IpcAreaRule.MapStyle(sudan));
        Assert.Equal((null, null), IpcAreaRule.MapStyle(sudan with { IsStale = true }));
        Assert.Equal((null, null), IpcAreaRule.MapStyle(sudan with { ProjectionOnly = true, ProjectionName = "first projection", HighestPhase = 0 }));
        // The stale record still has its classification (the panel's bar for the past analysis).
        Assert.Equal(3, IpcAreaRule.Classify(sudan with { IsStale = true }).ShadedPhase);
    }

    [Fact]
    public void PeopleInPhaseReadsEachCount()
    {
        var f = F(15, 1, 2, 3, 4, 5);
        Assert.Equal([1L, 2, 3, 4, 5], Enumerable.Range(1, 5).Select(p => IpcAreaRule.People(f, p)));
        Assert.Throws<ArgumentOutOfRangeException>(() => IpcAreaRule.People(f, 6));
    }

    [Fact]
    public void ReportedFollowsTheBlankCells()
    {
        var f = F(15, 1, 2, 3, 4, 5) with { UnreportedPhases = 0b00011 };   // Phases 1 and 2 left blank
        Assert.False(f.IsReported(1));
        Assert.False(f.IsReported(2));
        Assert.True(f.IsReported(3));
    }

    [Fact]
    public void NewCountsTakePartInEquality()
    {
        var a = F(15, 1, 2, 3, 4, 5);
        Assert.Equal(a, a with { });
        Assert.NotEqual(a, a with { PopulationAnalysed = 16 });
        Assert.NotEqual(a, a with { Phase1People = 9 });
        Assert.NotEqual(a, a with { Phase2People = 9 });
        Assert.NotEqual(a, a with { UnreportedPhases = 1 });
        Assert.Equal(a.GetHashCode(), (a with { }).GetHashCode());
    }

    [Fact]
    public void ReaderReadsPopulationAnalysedAndPhases1And2()
    {
        var csv = System.Text.Encoding.UTF8.GetBytes(Samples.Text("ipc_national_wide_latest.csv"));
        var r = IpcNationalReader.Read(csv, CountryResolver.LoadDefault(), new DateTime(2026, 11, 22, 0, 0, 0, DateTimeKind.Utc));
        var sdn = r.Crises.Single(c => c.CountryIso3 == "SDN");
        Assert.Equal(47_535_794, sdn.PopulationAnalysed);
        Assert.Equal(12_069_469, sdn.Phase1People);
        Assert.Equal(15_999_822, sdn.Phase2People);
        Assert.Equal(14_316_494, sdn.Phase3People);
        Assert.Equal(5_015_231, sdn.Phase4People);
        Assert.Equal(134_808, sdn.Phase5People);
        Assert.Equal(0, sdn.UnreportedPhases);
        Assert.Equal((3, 5), IpcAreaRule.MapStyle(sdn));
    }

    [Fact]
    public void ReaderMarksBlankPhaseCellsAndABlankAnalysedCell()
    {
        var lines = Samples.Text("ipc_national_wide_latest.csv").Replace("\r", "").Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var header = lines[0].Split(',');
        var sdn = lines.Single(l => l.Contains(",SDN,")).Split(',');
        foreach (var name in new[] { "Population analyzed current", "Phase 1 number current", "Phase 2 number current" })
            sdn[Array.IndexOf(header, name)] = "";
        var csv = System.Text.Encoding.UTF8.GetBytes(lines[0] + "\n" + string.Join(",", sdn) + "\n");
        var r = IpcNationalReader.Read(csv, CountryResolver.LoadDefault(), new DateTime(2026, 11, 22, 0, 0, 0, DateTimeKind.Utc));
        var f = r.Crises.Single();
        Assert.Equal(0, f.PopulationAnalysed);
        Assert.Equal(0, f.Phase1People);
        Assert.False(f.IsReported(1));
        Assert.False(f.IsReported(2));
        Assert.True(f.IsReported(3));
        // The denominator falls back to the phase sum (Phases 3 to 5 only here).
        var c = IpcAreaRule.Classify(f);
        Assert.Equal(14_316_494 + 5_015_231 + 134_808, c.Denominator);
        Assert.Equal(4, c.ShadedPhase);   // 26.5% of that sum is in Phase 4 or higher
    }
}
