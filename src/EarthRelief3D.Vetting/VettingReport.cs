using System.Globalization;
using System.Text;
using EarthRelief3D.Core.Help;

namespace EarthRelief3D.Vetting;

/// <summary>A day by which a review or a piece of evidence must be renewed (VettingRules: 12 months after it was made).</summary>
public sealed record ExpiryItem(string CharityName, string What, DateOnly CheckedOn, DateOnly ExpiresOn);

/// <summary>One link of a charity's entry and what the check found. A null <see cref="Status"/> means the tool did not request it.</summary>
public sealed record LinkOutcome(string What, Uri Url, LinkStatus? Status);

/// <summary>The share of expenses spent on programmes, from the latest Form 990 (Part IX line 25).</summary>
public sealed record ProgrammeResult(double Share, long Programme, long Total, int TaxYear, string TaxPeriodBegin, string TaxPeriodEnd);

/// <summary>Everything the tool found out about one charity.</summary>
/// <param name="ProgrammeNote">Why there is no programme share: no 990 in the index, a 990-EZ, the file could not be read.</param>
public sealed record CharityCheck(Charity Charity, Pub78Entry? Pub78, bool Revoked, Form990Ref? Latest990, ProgrammeResult? Programme,
    string? ProgrammeNote, IReadOnlyList<LinkOutcome> Links);

/// <summary>Which IRS files a run read, worded for the report.</summary>
/// <param name="Pub78File">What legal.pub78File should say: the file and its date.</param>
public sealed record ReportSources(string Pub78, string Revocation, string Index, string Pub78File);

/// <summary>
/// The report the maintainer reviews (spec 9.3). It states facts and flags; the maintainer decides, and edits the list by hand.
/// </summary>
public static class VettingReport
{
    public const int ExpiryDays = 60;

    /// <summary>The reviews and evidence that run out by <paramref name="days"/> days from <paramref name="today"/>, or already have. Soonest first.</summary>
    public static IReadOnlyList<ExpiryItem> Expiring(IReadOnlyList<Charity> charities, DateOnly today, int days = ExpiryDays)
    {
        ArgumentNullException.ThrowIfNull(charities);
        var limit = today.AddDays(days);
        var items = new List<ExpiryItem>();
        foreach (var c in charities)
        {
            var review = VettingRules.ExpiresOn(c.ReviewedOn);
            if (review <= limit) items.Add(new ExpiryItem(c.Name, $"Owner review (by {c.Reviewer})", c.ReviewedOn, review));
            foreach (var w in c.Countries ?? [])
            {
                var evidence = VettingRules.ExpiresOn(w.CheckedOn);
                if (evidence <= limit) items.Add(new ExpiryItem(c.Name, $"Evidence it works in {w.Iso3} ({w.EvidenceTitle})", w.CheckedOn, evidence));
            }
        }
        return [.. items.OrderBy(i => i.ExpiresOn).ThenBy(i => i.CharityName, StringComparer.Ordinal).ThenBy(i => i.What, StringComparer.Ordinal)];
    }

    public static string Render(DateOnly today, IReadOnlyList<CharityCheck> checks, IReadOnlyList<ExpiryItem> expiring, ReportSources sources)
    {
        var sb = new StringBuilder();
        void L(string s = "") => sb.Append(s).Append('\n');

        L($"# EarthRelief3D vetting report, {Iso(today)}");
        L();
        L("Written by the vetting tool (spec 9.3). It reads the charity list and the IRS files and **never edits the list**: this report is for the maintainer to review, and any change to `data/countries/US/charities.json` is made by hand.");
        L();
        L("## Summary");
        L();
        L($"- **{Count(checks.Count, "charity", "charities")}** in the list.");
        L($"- Checked against: IRS Publication 78 ({sources.Pub78}); IRS auto-revocation list ({sources.Revocation}); IRS Form 990 e-file index ({sources.Index}).");
        L($"- Entries expiring within {ExpiryDays} days of {Iso(today)}, or already expired: **{expiring.Count}**.");
        var broken = checks.SelectMany(c => c.Links.Where(l => l.Status == LinkStatus.Broken).Select(l => (c, l))).ToList();
        var blocked = checks.SelectMany(c => c.Links.Where(l => l.Status == LinkStatus.Blocked).Select(l => (c, l))).ToList();
        L($"- Links that failed: **{broken.Count}** broken; {blocked.Count} blocked to scripts (check them in a browser).");
        L("- The share of expenses spent on programmes comes from each charity's own IRS Form 990 (Part IX, line 25), not from a third party.");
        L();

        if (checks.Count > 0)
        {
            L("| Charity | EIN | IRS Pub 78 | Revoked | Programme share (tax year) | Broken links | Review runs out |");
            L("|---|---|---|---|---|---|---|");
            foreach (var k in checks)
            {
                var c = k.Charity;
                var share = k.Programme is { } p ? $"{Pct(p.Share)} ({p.TaxYear})" : "none";
                L($"| {Cell(c.Name)} | {c.Ein} | {(k.Pub78 is null ? "**NOT LISTED**" : "listed, " + k.Pub78.DeductibilityCode)} | {(k.Revoked ? "**REVOKED**" : "no")} | {share} | {k.Links.Count(l => l.Status == LinkStatus.Broken)} | {Iso(VettingRules.ExpiresOn(c.ReviewedOn))} |");
            }
            L();
        }

        L($"## Entries expiring in the next {ExpiryDays} days");
        L();
        if (expiring.Count == 0) L($"None: every review and every piece of evidence is good for more than {ExpiryDays} days from {Iso(today)}.");
        else
        {
            L("An entry is hidden in the app on the day its review or evidence is 12 months old.");
            L();
            L("| Charity | What | Made on | Runs out | |");
            L("|---|---|---|---|---|");
            foreach (var e in expiring)
                L($"| {Cell(e.CharityName)} | {Cell(e.What)} | {Iso(e.CheckedOn)} | {Iso(e.ExpiresOn)} | {(e.ExpiresOn <= today ? "**already expired: hidden in the app**" : $"in {e.ExpiresOn.DayNumber - today.DayNumber} days")} |");
        }
        L();

        L("## Links that fail");
        L();
        if (broken.Count == 0) L("No link is broken.");
        foreach (var (c, l) in broken) L($"- **BROKEN** {Cell(c.Charity.Name)}: {l.What}: <{l.Url}>");
        if (blocked.Count > 0)
        {
            L();
            L("Blocked to scripts (HTTP 401, 403, 429 or a challenge page). The link is probably fine: open each in a browser.");
            L();
            foreach (var (c, l) in blocked) L($"- {Cell(c.Charity.Name)}: {l.What}: <{l.Url}>");
        }
        L();

        L("## Each charity");
        foreach (var k in checks) Charity(k, today, sources, L);
        return sb.ToString();
    }

    private static void Charity(CharityCheck k, DateOnly today, ReportSources sources, Action<string> L)
    {
        var c = k.Charity;
        L("");
        L($"### {Cell(c.Name)} (EIN {c.Ein}, {(c.Review.IsApproved ? "approved" : "draft")})");
        L("");
        if (k.Pub78 is { } p)
        {
            L($"- **IRS Publication 78:** listed. IRS name: \"{p.Name}\", {p.City}, {p.State}. Deductibility code: **{p.DeductibilityCode}**.");
            if (!string.Equals(Norm(p.Name), Norm(c.Name), StringComparison.Ordinal))
                L($"  - The list calls it \"{c.Name}\": check that it is the same organisation.");
            if (!string.Equals(p.DeductibilityCode, "PC", StringComparison.OrdinalIgnoreCase))
                L("  - The code is not PC (public charity): the entry should not be listed unless the code means gifts are deductible.");
        }
        else L("- **IRS Publication 78:** **NOT LISTED in Publication 78.** Gifts may not be deductible: do not list this charity until that is explained.");
        L(k.Revoked
            ? "- **IRS auto-revocation list:** **REVOKED**: the IRS revoked this organisation's tax exemption (and did not record a reinstatement). Do not list it."
            : "- **IRS auto-revocation list:** not on the list.");

        if (k.Programme is { } g && k.Latest990 is { } r)
        {
            L($"- **Programme share (Form 990, Part IX line 25):** **{Pct(g.Share)}** of expenses went to programmes (programme services ${g.Programme:N0} of total functional expenses ${g.Total:N0}). Tax year {g.TaxYear} (period {(g.TaxPeriodBegin.Length > 0 ? g.TaxPeriodBegin : "?")} to {(g.TaxPeriodEnd.Length > 0 ? g.TaxPeriodEnd : "?")}). IRS return {r.ReturnType}, object id {r.ObjectId} (zip {r.BatchId}).");
        }
        else
        {
            var what = k.Latest990 is { } l ? $"the latest return is a {l.ReturnType} for the period ending {Period(l.TaxPeriod)}, object id {l.ObjectId}" : "";
            L($"- **Programme share:** none. {k.ProgrammeNote}{(what.Length > 0 && k.ProgrammeNote is not null ? " (" + what + ")" : what)}".TrimEnd());
        }

        var expires = VettingRules.ExpiresOn(c.ReviewedOn);
        L($"- **Review:** by {c.Reviewer} on {Iso(c.ReviewedOn)}; runs out on {Iso(expires)} ({(expires <= today ? "already expired" : $"{expires.DayNumber - today.DayNumber} days from {Iso(today)}")}).");

        if (k.Pub78 is not null || k.Programme is not null)
        {
            L("- **To record these checks in the list** (by hand; the tool never edits it):");
            if (k.Pub78 is { } q) L($"  - `\"legal\": {{ \"checkedOn\": \"{Iso(today)}\", \"pub78File\": \"{sources.Pub78File}\", \"deductibilityCode\": \"{q.DeductibilityCode}\" }}`");
            if (k.Programme is { } s && k.Latest990 is { } f)
                L($"  - `\"programme\": {{ \"share\": {s.Share.ToString("0.0000", CultureInfo.InvariantCulture)}, \"taxYear\": {s.TaxYear}, \"objectId\": \"{f.ObjectId}\", \"computedOn\": \"{Iso(today)}\" }}`");
        }

        L("- **Links:**");
        if (k.Links.Count == 0) L("  - none");
        foreach (var l in k.Links)
        {
            var result = l.Status is null ? LinkChecker.NotCheckedEvaluator : LinkChecker.Describe(l.Status.Value);
            L($"  - {l.What}: <{l.Url}>: {(l.Status == LinkStatus.Broken ? "**BROKEN**" : result)}");
        }
    }

    private static string Iso(DateOnly d) => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static string Pct(double share) => (share * 100).ToString("0.#", CultureInfo.InvariantCulture) + "%";

    private static string Period(string yyyymm) => yyyymm.Length == 6 ? yyyymm[..4] + "-" + yyyymm[4..] : yyyymm;

    private static string Count(int n, string one, string many) => n == 1 ? $"1 {one}" : $"{n} {many}";

    private static string Cell(string s) => s.Replace("|", "/", StringComparison.Ordinal).Replace("\r", " ").Replace("\n", " ");

    private static string Norm(string s) => new string(s.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
}


