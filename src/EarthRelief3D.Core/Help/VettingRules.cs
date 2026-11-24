using System.Globalization;

namespace EarthRelief3D.Core.Help;

/// <summary>Whether a charity shows for a country, and if not why (each missing check is named).</summary>
public sealed record VettingResult(bool Shown, IReadOnlyList<string> Reasons);

/// <summary>
/// The checks a charity passes to be listed (spec §9.2; P4-8, P4-9): approved by the owner; a Publication 78 check; a confirmed
/// rating from an evaluator the country's profile lists; evidence it works in the country; and its review and evidence no more
/// than 12 months old. Any missing check, or a check that has expired, hides the entry: never shown out of date (Review Focus 3).
/// </summary>
public static class VettingRules
{
    /// <summary>A review, and the evidence of working in a country, are good for this long.</summary>
    public const int ReviewValidMonths = 12;

    /// <summary>The first day a review (or evidence) made on <paramref name="checkedOn"/> no longer counts.</summary>
    public static DateOnly ExpiresOn(DateOnly checkedOn) => checkedOn.AddMonths(ReviewValidMonths);

    /// <param name="evaluators">The evaluators the country's profile lists: a rating from another evaluator does not count.</param>
    /// <param name="showDrafts">A draft (not yet approved) entry passes the draft check; every other check still applies.</param>
    public static VettingResult Check(Charity c, string iso3, IReadOnlyList<Evaluator> evaluators, DateOnly today, bool showDrafts)
    {
        ArgumentNullException.ThrowIfNull(c);
        ArgumentNullException.ThrowIfNull(evaluators);
        var reasons = new List<string>();
        if (!c.Review.IsApproved && !showDrafts) reasons.Add("draft: not yet reviewed by the owner");

        if (c.Legal is null) reasons.Add("no IRS Publication 78 check");
        else if (string.IsNullOrWhiteSpace(c.Legal.DeductibilityCode)) reasons.Add("the IRS Publication 78 check gives no deductibility code");

        var listed = (c.Ratings ?? []).Any(r => evaluators.Any(e => string.Equals(e.Name, r.Evaluator, StringComparison.OrdinalIgnoreCase)));
        if (!listed) reasons.Add($"no confirmed rating from a listed evaluator ({string.Join(", ", evaluators.Select(e => e.Name))})");

        var work = (c.Countries ?? []).FirstOrDefault(w => string.Equals(w.Iso3, iso3, StringComparison.OrdinalIgnoreCase));
        if (work is null) reasons.Add($"no evidence that it works in {iso3}");
        else if (today >= ExpiresOn(work.CheckedOn))
            reasons.Add($"the evidence for {iso3} (checked {Date(work.CheckedOn)}) is older than {ReviewValidMonths} months");

        if (today >= ExpiresOn(c.ReviewedOn))
            reasons.Add($"the review of {Date(c.ReviewedOn)} is older than {ReviewValidMonths} months");
        return new VettingResult(reasons.Count == 0, reasons);
    }

    private static string Date(DateOnly d) => d.ToString("d MMM yyyy", CultureInfo.InvariantCulture);
}
