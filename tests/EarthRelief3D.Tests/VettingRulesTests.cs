using EarthRelief3D.Core.Help;
using static EarthRelief3D.Tests.HelpFixtures;

namespace EarthRelief3D.Tests;

/// <summary>The charity checks (spec §9.2; P4-8, P4-9): hidden when any check is missing, never shown out of date (Review Focus 3).</summary>
public class VettingRulesTests
{
    private static readonly IReadOnlyList<Evaluator> Evaluators = [CharityNavigator, Bbb];

    private static VettingResult Check(Charity c, DateOnly? today = null, bool drafts = false, string iso3 = "SDN") =>
        VettingRules.Check(c, iso3, Evaluators, today ?? Today, drafts);

    [Fact]
    public void ShownWhenAllChecksPass()
    {
        var r = Check(Charity("Alpha"));
        Assert.True(r.Shown);
        Assert.Empty(r.Reasons);
        Assert.Equal(12, VettingRules.ReviewValidMonths);
    }

    [Fact]
    public void ExpiresTwelveMonthsAfterReview()
    {
        var c = Charity("Alpha", reviewedOn: new DateOnly(2025, 11, 24), evidenceOn: new DateOnly(2026, 11, 1));
        Assert.True(Check(c, new DateOnly(2026, 11, 23)).Shown);
        var hidden = Check(c, new DateOnly(2026, 11, 24));
        Assert.False(hidden.Shown);
        Assert.Contains(hidden.Reasons, x => x.Contains("review") && x.Contains("12 months"));
    }

    [Fact]
    public void DraftHidden()
    {
        var c = Charity("Alpha", approved: false);
        var hidden = Check(c);
        Assert.False(hidden.Shown);
        Assert.Contains(hidden.Reasons, x => x.Contains("draft"));
        Assert.True(Check(c, drafts: true).Shown);
    }

    [Fact]
    public void MissingLegalCheckHidden()
    {
        var r = Check(Charity("Alpha", legalMissing: true));
        Assert.False(r.Shown);
        Assert.Contains(r.Reasons, x => x.Contains("Publication 78"));
        // Even with drafts: a missing check hides the entry.
        Assert.False(Check(Charity("Alpha", legalMissing: true, approved: false), drafts: true).Shown);
    }

    [Fact]
    public void NoRatingHidden()
    {
        var r = Check(Charity("Alpha", ratings: []));
        Assert.False(r.Shown);
        Assert.Contains(r.Reasons, x => x.Contains("rating"));
    }

    [Fact]
    public void UnlistedEvaluatorHidden()
    {
        var other = new CharityRating("Some Other Rater", new Uri("https://example.org/rating"), new DateOnly(2026, 11, 2), "owner");
        var r = Check(Charity("Alpha", ratings: [other]));
        Assert.False(r.Shown);
        Assert.Contains(r.Reasons, x => x.Contains("listed evaluator"));
        // One listed rating is enough; the unlisted one is simply not counted.
        var mixed = Charity("Alpha").Ratings.Take(1).Append(other).ToList();
        Assert.True(Check(Charity("Alpha", ratings: mixed)).Shown);
    }

    [Fact]
    public void NotWorkingHereHidden()
    {
        var c = Charity("Alpha", iso3: "SDN");
        var r = Check(c, iso3: "YEM");
        Assert.False(r.Shown);
        Assert.Contains(r.Reasons, x => x.Contains("YEM") && x.Contains("works"));
        Assert.False(Check(Charity("Alpha", noEvidence: true)).Shown);
    }

    [Fact]
    public void EvidenceOlderThanTwelveMonthsHidden()
    {
        var c = Charity("Alpha", evidenceOn: new DateOnly(2025, 11, 24));
        Assert.True(Check(c, new DateOnly(2026, 11, 23)).Shown);
        var r = Check(c, new DateOnly(2026, 11, 24));
        Assert.False(r.Shown);
        Assert.Contains(r.Reasons, x => x.Contains("evidence") && x.Contains("12 months"));
    }

    [Fact]
    public void EveryMissingCheckIsNamed()
    {
        var r = Check(Charity("Alpha", approved: false, legalMissing: true, ratings: [], noEvidence: true));
        Assert.False(r.Shown);
        Assert.Equal(4, r.Reasons.Count);
    }
}
