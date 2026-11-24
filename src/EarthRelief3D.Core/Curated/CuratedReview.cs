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
        L("- A country gets a triangle only from a court judgment, UN body finding or government determination whose");
        L("  continuing statement is dated within the last 12 months. Provisional measures, warrants and pending cases never draw");
        L("  one on their own; when the country has a triangle, those with a recent statement are listed with it, and when it");
        L("  has none, under \"interim steps: not on the map\". The rest is listed under the panel's history.");
        L("- A court judgment whose outcome is \"not established\" is never drawn and never counts as a crisis; the panel lists it");
        L("  under \"Cases decided: not established\".");
        L("- A camp keeps its marker unless its newest cited report says it closed (`reportedClosed`, ruling R14b); a closed camp");
        L("  is listed as history. Where sources differ on a camp's status, each statement is shown with its source (`statusNotes`).");
        L();
        L($"Totals: {t.Designations.Count} designations, {t.Camps.Count} camp sites, {t.Depots.Count} UN depots, {t.Explainers.Count} explainers, {t.Governments.Count} government(s), "
          + $"{t.NotEntered.Count} candidates awaiting confirmation or not entered (last section).");
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
                if (d.OwnerQuestion is { Length: > 0 } q) L($"- **Question for the owner:** {Md(q)}");
                L($"- Status: **{d.Review.Status}**" + (d.Review.ApprovedOn is { } a ? $" on {Date(a)}" : ""));
                if (d.Review.Status == Review.Draft) L("- Owner: approve / correct: ");
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
            if (c.ReportedClosed is { } closed)
                L($"- **Reported closed** by its newest report (ruling R14b; no marker, panel history): \"{Md(closed.Statement)}\" ([{Md(closed.Source.Title)}]({closed.Source.Url}),"
                  + $" {closed.Source.Publisher}, {Date(closed.Source.Date)})");
            else L("- Status on the map: a marker (its newest cited report does not say it closed)");
            foreach (var n in c.StatusNotes)
                L($"- Status note: {Md(n.Text)} ([{Md(n.Source.Title)}]({n.Source.Url}), {n.Source.Publisher}, {Date(n.Source.Date)})");
            L($"- Checked on: {Date(c.CheckedOn)}");
            L($"- What was checked and where: {Md(c.Verification ?? "(not recorded)")}");
            if (c.OwnerQuestion is { Length: > 0 } q) L($"- **Question for the owner:** {Md(q)}");
            L($"- Status: **{c.Review.Status}**" + (c.Review.ApprovedOn is { } a ? $" on {Date(a)}" : ""));
            if (c.Review.Status == Review.Draft) L("- Owner: approve / correct: ");
        }
        L();

        L("## UN depots");
        L();
        L("UNHRD (the UN Humanitarian Response Depot network, run by the World Food Programme) has five hubs; each address is as UNHRD's");
        L("contacts page writes it (no staff names, emails or phone numbers are kept). UNHRD publishes **no coordinates**, so each depot is");
        L("marked at the airport its own address names or sits beside, with the airport's position from the bundled OurAirports data (The");
        L("Unlicense; ruling R21). Each row says which airport, quotes the part of the address that ties the depot to it, and is approximate:");
        L("the marker is at the airport, not at the depot's building. The nearest-depot distances and arcs use these points. Approve a row,");
        L("or correct its position.");
        foreach (var d in t.Depots.OrderBy(d => d.Iso3, StringComparer.Ordinal).ThenBy(d => d.Id, StringComparer.Ordinal))
        {
            L();
            L($"### `{d.Id}`: {d.Name}");
            L();
            L($"- Country: {d.Iso3}; city: {Md(d.City)}");
            L($"- Address (as UNHRD writes it): {Md(d.Address)}");
            L($"- Position: {LatLon(d.Position)}, at the airport beside the depot (approximate):"
              + $" [{Md(d.PositionSource.Title)}]({d.PositionSource.Url}) ({d.PositionSource.Publisher}, {Date(d.PositionSource.Date)})");
            L($"- Source: [{Md(d.Source.Title)}]({d.Source.Url}) ({d.Source.Publisher}, {Date(d.Source.Date)})");
            L($"- Checked on: {Date(d.CheckedOn)}");
            L($"- What was checked and where: {Md(d.Verification ?? "(not recorded)")}");
            L($"- Status: **{d.Review.Status}**" + (d.Review.ApprovedOn is { } a ? $" on {Date(a)}" : ""));
            if (d.Review.Status == Review.Draft) L("- Owner: approve / correct: ");
        }
        L();

        L("## Explainers");
        L();
        L("The plain-language explainers the panel opens (\"What is this, and why does it happen?\"; \"Why these?\" for Your one small step).");
        L($"Each is {Explainer.MaxWords} words or fewer, paraphrased from at least {Explainer.MinSources} pages on the authority's own site (no quotation");
        L($"longer than {Explainer.MaxQuoteWords} words), and gives no figures that go stale: the panel's own sourced numbers say how many. Read each");
        L("one against its sources, then approve it or say what to change.");
        foreach (var e in t.Explainers.OrderBy(e => Explainer.Ids.ToList().IndexOf(e.Id)))
        {
            L();
            L($"### `{e.Id}`: {Md(e.Title)}");
            L();
            L($"Text ({e.WordCount.ToString(Inv)} words):");
            L();
            for (var i = 0; i < e.Paragraphs.Count; i++)
            {
                if (i > 0) L(">");
                L("> " + e.Paragraphs[i]);
            }
            L();
            L("- Sources:");
            foreach (var s in e.Sources) L($"  - [{Md(s.Title)}]({s.Url}) ({s.Publisher}, {Date(s.Date)})");
            L($"- Text checked against its sources on: {Date(e.ReviewedOn)}");
            L($"- What was opened, and when: {Md(e.Verification ?? "(not recorded)")}");
            L($"- Status: **{e.Review.Status}**" + (e.Review.ApprovedOn is { } a ? $" on {Date(a)}" : ""));
            if (e.Review.Status == Review.Draft) L("- Owner: approve / correct: ");
        }
        L();

        L("## Awaiting confirmation / not entered");
        L();
        L("Researched for this table but not entered. Links marked *lead* (news, studies) are never citations: a row is entered only");
        L("from the authority's own site.");
        foreach (var n in t.NotEntered)
        {
            L();
            L($"### `{n.Id}`: {Md(n.What)}");
            L();
            L($"- Status: **{n.Status}**");
            L($"- Why: {Md(n.Reason)}");
            if (n.OwnerAction is { Length: > 0 } action) L($"- Owner: {Md(action)}");
            if (n.Links is { Count: > 0 })
            {
                L("- Links:");
                foreach (var l in n.Links) L($"  - [{Md(l.Title)}]({l.Url})" + (l.Kind == "lead" ? " (*lead*, not a citation)" : " (authority)"));
            }
        }
        return sb.ToString();
    }

    private static string Date(DateOnly d) => DesignationText.Date(d);

    /// <summary>"5.60519° N, 0.16679° W": hemispheres named, never a minus sign before "E" (m20).</summary>
    private static string LatLon(AstroCore.Earth.GeoPosition p) =>
        $"{Math.Abs(p.LatitudeDeg).ToString("0.00000", Inv)}° {(p.LatitudeDeg < 0 ? "S" : "N")}, {Math.Abs(p.LongitudeDeg).ToString("0.00000", Inv)}° {(p.LongitudeDeg < 0 ? "W" : "E")}";

    // Square brackets in a title would break the Markdown link.
    private static string Md(string s) => s.Replace("[", "(", StringComparison.Ordinal).Replace("]", ")", StringComparison.Ordinal);
}
