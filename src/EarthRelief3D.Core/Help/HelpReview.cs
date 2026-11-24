using System.Globalization;
using System.Text;
using EarthRelief3D.Core.Curated;

namespace EarthRelief3D.Core.Help;

/// <summary>
/// Writes <c>docs/help-review.md</c> from the tables (P4-13): one block per pooled fund, official contact and charity with its
/// facts, its citation links, what the app needs from the owner, and "approve / correct". The text depends only on the tables
/// (never on today's date), so a test can keep the committed file in step with them.
/// </summary>
public static class HelpReview
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public static string Render(HelpTables t)
    {
        ArgumentNullException.ThrowIfNull(t);
        var sb = new StringBuilder();
        void L(string line = "") => sb.Append(line).Append('\n');

        L("# Help catalog: UN funds, official contacts and charities, for the owner's review");
        L();
        L("Generated from `data/help/pooled-funds.json` and `data/countries/<ISO2>/*.json` by `HelpTablesTests.ReviewDocumentIsCurrent`");
        L("(run the tests with `ER3D_WRITE_REVIEW=1` to rewrite it). Do not edit by hand: correct the JSON, then regenerate.");
        L();
        L("Every row starts as a **draft** (ruling P4-13). The app shows drafts only with the diagnostic `--show-drafts`, labelled");
        L("\"Draft: not yet reviewed\". To approve a row, set its `review` to `{ \"status\": \"approved\", \"approvedOn\": \"<date>\" }` in the");
        L("JSON. To correct it, change the JSON (or tell Claude what to change).");
        L();
        L("How the panel uses them (\"Your one small step\"):");
        L("- A country with an approved UN pooled fund shows it first, with OCHA's page and OCHA's \"how to give\" page; every other country,");
        L("  and every country while the funds are drafts, shows CERF (P4-11). The app never takes a gift: every line links to an official page.");
        L("- Charities follow, alphabetically by name, never ranked. A charity shows only when every check holds (P4-8, P4-9): approved by you;");
        L("  an IRS Publication 78 check; a confirmed rating from a listed evaluator; evidence it works in that country; and its review and");
        L("  evidence no more than 12 months old. The app shows \"Rated by <evaluators>\" with links to their reports, never stars or scores.");
        L("  With none, the panel says \"No vetted charities listed yet\".");
        L("- Official contacts need your approval too. The panel links to the House's own finder (the app never asks for an address or ZIP");
        L("  code), and to the UN Blue Book for each country's mission (one link: the Blue Book's terms forbid compiling its list).");
        L();
        L($"Totals: {t.Funds.Count} pooled funds (including CERF), {t.Profile?.Contacts.Count ?? 0} official contacts, {t.Charities.Count} charities, "
          + $"{t.Senators.Count} senators (a list copied from the Senate's own file{(t.Senators.Count > 0 ? ", as of " + Date(t.SenatorsAsOf) : "")}; not reviewed row by row).");
        L();

        L("## UN pooled funds and CERF");
        L();
        L("Each fund's page is the row's citation. OCHA has no per-fund donation page: its \"how to give\" page is shared by all its funds. A regional");
        L("fund covers the countries OCHA's CBPF data lists as its sub-funds.");
        foreach (var f in t.Funds.OrderBy(f => f.Id == HelpTables.CerfId ? 1 : 0).ThenBy(f => f.Name, StringComparer.Ordinal).ThenBy(f => f.Id, StringComparer.Ordinal))
        {
            L();
            L($"### `{f.Id}`: {Md(f.Name)}");
            L();
            L("- Covers: " + (f.Iso3.Count == 0 ? "every country with no pooled fund of its own" : string.Join(", ", f.Iso3.Order(StringComparer.Ordinal))));
            L($"- Page: [{Md(f.Name)}]({f.Page})");
            L($"- How to give: [{f.GiveUrl}]({f.GiveUrl}): \"{Md(f.GiveNote)}\"");
            L($"- Checked on: {Date(f.CheckedOn)}");
            Status(L, f.Review);
        }
        L();

        L("## Official contacts");
        if (t.Profile is not { } profile) { L(); L("No country profile is loaded."); }
        else
        {
            L();
            L($"{Md(profile.Name)} (`{profile.Iso2}`): charities are checked in the {Md(profile.Registry)} ([search page]({profile.RegistryUrl})); evaluators accepted:");
            foreach (var e in profile.Evaluators) L($"- [{Md(e.Name)}]({e.Site}): qualifies at: {Md(e.Qualifies)}");
            foreach (var c in profile.Contacts)
            {
                L();
                L($"### `{c.Id}`: {Md(c.Title)}");
                L();
                L($"- Kind: `{c.Kind}`");
                L(c.Url is { } url ? $"- Link: [{url}]({url})" : "- Link: none (the senators are listed from senators.json by the state chosen in Settings)");
                L($"- Note: {Md(c.Note)}");
                L($"- Source: [{Md(c.Source.Title)}]({c.Source.Url}) ({Md(c.Source.Publisher)}, {Date(c.Source.Date)})");
                L($"- Checked on: {Date(c.CheckedOn)}");
                Status(L, c.Review);
            }
        }
        L();

        L("## Charities");
        if (t.Charities.Count == 0)
        {
            L();
            L("No charities are listed yet. The first batch follows P4-9 (organisations in OCHA's 3W data present in several countries that are US");
            L("public charities in IRS Publication 78); each is added as a draft, and you confirm its ratings by opening its evaluator pages.");
        }
        foreach (var c in t.Charities.OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase).ThenBy(c => c.Id, StringComparer.Ordinal))
        {
            L();
            L($"### `{c.Id}`: {Md(c.Name)}");
            L();
            L($"- EIN: {c.Ein}; website: [{c.Website}]({c.Website}); donate page: [{c.DonateUrl}]({c.DonateUrl})");
            L(c.Legal is { } legal
                ? $"- IRS Publication 78: deductibility code `{legal.DeductibilityCode}`, from `{legal.Pub78File}`, checked {Date(legal.CheckedOn)}"
                : "- IRS Publication 78: not checked yet");
            L(c.Programme is { } p
                ? $"- Programme spending: {(p.Share * 100).ToString("0.0", Inv)}% (Form 990, tax year {p.TaxYear.ToString(Inv)}, object {p.ObjectId}, worked out {Date(p.ComputedOn)})"
                : "- Programme spending: not worked out yet");
            if (c.Ratings.Count == 0) L("- Ratings: none confirmed yet (you confirm them by opening the evaluators' pages; no script or agent reads them)");
            foreach (var r in c.Ratings) L($"- Rating confirmed: {Md(r.Evaluator)}: [report]({r.ReportUrl}), read {Date(r.CheckedOn)} by {Md(r.CheckedBy)}");
            foreach (var link in c.ProfileLinks) L($"- Evaluator page to open: [{link}]({link})");
            foreach (var w in c.Countries)
                L($"- Works in {w.Iso3}: [{Md(w.EvidenceTitle)}]({w.Evidence}) ({Md(w.EvidenceKind)}, checked {Date(w.CheckedOn)}; expires {Date(VettingRules.ExpiresOn(w.CheckedOn))})");
            if (c.Note is { Length: > 0 } note) L($"- Note: {Md(note)}");
            L($"- Reviewed {Date(c.ReviewedOn)} by {Md(c.Reviewer)}; the review expires {Date(VettingRules.ExpiresOn(c.ReviewedOn))}");
            if (c.OwnerAction is { Length: > 0 } action) L($"- **For the owner:** {Md(action)}");
            // What would still hide it, independent of today: judged on the review day, drafts counted as shown.
            var reasons = t.Profile is null ? ["no country profile to check it against"]
                : (c.Countries.Count == 0 ? [VettingRules.Check(c, "(no country)", t.Profile.Evaluators, c.ReviewedOn, true).Reasons.FirstOrDefault(r => r.Contains("evidence", StringComparison.Ordinal)) ?? "no evidence it works anywhere"]
                    : c.Countries.SelectMany(w => VettingRules.Check(c, w.Iso3, t.Profile.Evaluators, c.ReviewedOn, true).Reasons).Distinct().ToList());
            L(reasons.Count == 0 ? "- Checks: every check holds once it is approved" : "- Hidden until fixed: " + string.Join("; ", reasons));
            Status(L, c.Review);
        }
        return sb.ToString();
    }

    private static void Status(Action<string> l, Review r)
    {
        l($"- Status: **{r.Status}**" + (r.ApprovedOn is { } a ? $" on {Date(a)}" : ""));
        if (r.Status == Review.Draft) l("- Owner: approve / correct: ");
    }

    private static string Date(DateOnly d) => DesignationText.Date(d);

    // Square brackets in a title would break the Markdown link.
    private static string Md(string s) => s.Replace("[", "(", StringComparison.Ordinal).Replace("]", ")", StringComparison.Ordinal);
}
