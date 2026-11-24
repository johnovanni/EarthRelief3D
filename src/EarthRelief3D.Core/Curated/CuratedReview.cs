using System.Globalization;
using System.Text;

namespace EarthRelief3D.Core.Curated;

/// <summary>
/// Writes <c>docs/curated-review.md</c> from the tables (P3-4): one block per row with its facts, its citation links, what
/// was checked and where, and "approve / correct" for the owner. The text depends only on the tables, so a test can keep
/// the committed file in step with them.
/// </summary>
public static class CuratedReview
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public static string Render(CuratedTables t)
    {
        ArgumentNullException.ThrowIfNull(t);
        var sb = new StringBuilder();
        void L(string line = "") => sb.Append(line).Append('\n');

        L("# Curated designations and camp sites: for the owner's review");
        L();
        L("Generated from `data/curated/*.json` by `CuratedTablesTests.ReviewDocumentIsCurrent` (run the tests with");
        L("`ER3D_WRITE_REVIEW=1` to rewrite it). Do not edit by hand: correct the JSON, then regenerate.");
        L();
        L("Every row starts as a **draft** (ruling P3-4). The app shows drafts only with the diagnostic `--show-drafts`,");
        L("labelled \"Draft: not yet reviewed\". To approve a row, set its `review` to");
        L("`{ \"status\": \"approved\", \"approvedOn\": \"<date>\" }` in the JSON. To correct it, change the JSON (or tell Claude what to change).");
        L();
        L("How the app uses a row (C14):");
        L("- The label always names the authority, what kind of authority it is, and the date.");
        L("- Provisional measures, arrest warrants and pending cases say they are not verdicts.");
        L("- A row is drawn on the map (a hollow triangle at the country) only while its continuing statement is dated within");
        L("  the last 12 months. Otherwise it is listed under the panel's history.");
        L("- A court judgment whose outcome is \"not established\" is never drawn and never counts as a crisis.");
        L();
        L($"Totals: {t.Designations.Count} designations, {t.Camps.Count} camp sites, {t.Governments.Count} government(s).");
        L("Candidates that were researched but not entered (and why) are listed in the Plan 3 Task 5 report,");
        L("`.superpowers/sdd/2026-11-23-plan-3-atrocities-needs-resources/task-5-report.md`.");
        L();

        L("## Governments offered in Settings (C15)");
        L();
        foreach (var g in t.Governments)
        {
            L($"- **{g.Name}** (`{g.Iso3}`): curated completely: {(g.CuratedComplete ? "yes" : "no")}; checked {Date(g.CheckedOn)}.");
            if (g.Scope is { Length: > 0 } scope) L($"  Scope: {scope}");
        }
        L();

        L("## Designations");
        foreach (var group in t.Designations.GroupBy(d => d.Iso3).OrderBy(g => g.Key, StringComparer.Ordinal))
        {
            L();
            L($"### {group.Key}");
            foreach (var d in group.OrderBy(d => d.Date))
            {
                L();
                L($"#### `{d.Id}`: {DesignationText.Label(d, t.Governments)} ({d.ShortName}, {d.Date.Year.ToString(Inv)})");
                L();
                L($"- Authority: {d.Authority} ({DesignationText.AuthorityKind(d, t.Governments)}); type `{d.Type}`"
                  + (d.Outcome == FindingOutcome.NotEstablished ? "; outcome **not established**" : ""));
                L($"- Term: {d.Term}");
                L($"- Group: {d.Group}");
                L($"- Situation: {d.Situation}");
                L($"- Date: {Date(d.Date)}");
                L($"- Document: [{Md(d.DocumentTitle)}]({d.DocumentUrl})");
                if (d.Continuing is { } c)
                {
                    L($"- Continuing: \"{Md(c.Statement)}\"");
                    L($"  Source: [{Md(c.Title)}]({c.Url}), {Date(c.Date)}. On the map until {Date(c.Date.AddMonths(Designation.ContinuingMonths))}"
                      + " unless a newer statement is added.");
                }
                else L("- Continuing: none, so it is panel history only (not on the map).");
                L($"- Checked on: {Date(d.CheckedOn)}");
                L($"- What was checked and where: {Md(d.Verification ?? "(not recorded)")}");
                L($"- Status: **{d.Review.Status}**" + (d.Review.ApprovedOn is { } a ? $", approved {Date(a)}" : ""));
                L("- Owner: approve / correct: ");
            }
        }
        L();

        L("## Camp sites");
        foreach (var c in t.Camps.OrderBy(c => c.Iso3, StringComparer.Ordinal).ThenBy(c => c.Id, StringComparer.Ordinal))
        {
            L();
            L($"### `{c.Id}`: {c.Name}" + (c.LocalName is { } local ? $" ({local})" : ""));
            L();
            L($"- Country: {c.Iso3}");
            L($"- Description: {Md(c.Description)}");
            L($"- Position: {c.Position.LatitudeDeg.ToString("0.000000", Inv)}° N, {c.Position.LongitudeDeg.ToString("0.000000", Inv)}° E,"
              + $" as published in [{Md(c.PositionSource.Title)}]({c.PositionSource.Url}) ({c.PositionSource.Publisher}, {Date(c.PositionSource.Date)})");
            L("- Documented by (newest first):");
            foreach (var r in c.DocumentedBy) L($"  - [{Md(r.Title)}]({r.Url}) ({r.Publisher}, {Date(r.Date)})");
            L($"- Checked on: {Date(c.CheckedOn)}");
            L($"- What was checked and where: {Md(c.Verification ?? "(not recorded)")}");
            L($"- Status: **{c.Review.Status}**" + (c.Review.ApprovedOn is { } a ? $", approved {Date(a)}" : ""));
            L("- Owner: approve / correct: ");
        }
        return sb.ToString();
    }

    private static string Date(DateOnly d) => DesignationText.Date(d);

    // Square brackets in a title would break the Markdown link.
    private static string Md(string s) => s.Replace("[", "(", StringComparison.Ordinal).Replace("]", ")", StringComparison.Ordinal);
}
