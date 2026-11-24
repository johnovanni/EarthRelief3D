using System.Globalization;
using EarthRelief3D.Core.Curated;

namespace EarthRelief3D.Core.Help;

/// <summary>
/// One entry under "Your one small step": a <paramref name="Group"/> ("Give" or "Contact"), a title, text lines and links. Every
/// link is https. <see cref="ReviewedOn"/> is the day the owner approved or reviewed the row (null for a draft, and for the built-in CERF line).
/// </summary>
public sealed record HelpEntry(string Group, string Title, IReadOnlyList<string> Lines, IReadOnlyList<PanelLink> Links)
{
    public DateOnly? ReviewedOn { get; init; }
}

/// <summary>
/// "Your one small step" for a country (spec §9.2; P4-8, P4-10 to P4-13): what to give (the UN pooled fund, or CERF where the
/// country has none, then the vetted charities in alphabetical order, never ranked) and whom to contact (the user's senators,
/// representative, the State Department, the country's mission to the UN). The app never takes a gift and never asks for an
/// address or ZIP code: every entry links to the official page. Ratings are never copied: "Rated by &lt;evaluators&gt;" with links.
/// </summary>
public static class HelpCatalog
{
    public const string GiveGroup = "Give";
    public const string ContactGroup = "Contact";
    public const string NoCharitiesText = "No vetted charities listed yet";
    public const string ChooseStateText = "Choose your state in Settings to list your two senators";
    public const string SenatorsTitle = "Your two senators";
    /// <summary>The link text of the Blue Book page (P4-10).</summary>
    public const string BlueBookText = "UN Blue Book";

    /// <summary>
    /// CERF as P4-11 words it: used when the table has no visible <c>cerf</c> row, so a country with no pooled fund of its own (and
    /// every country, while the funds are drafts) still gets a way to give through the UN. The facts are the ruling's, from cerf.un.org.
    /// </summary>
    public static PooledFund BuiltInCerf { get; } = new(HelpTables.CerfId, "Central Emergency Response Fund (CERF)", [], new Uri("https://cerf.un.org"),
        new Uri("https://cerf.un.org/donate"), "US tax-deductible gifts go through the UN Foundation; CERF's page explains how",
        new Review(Review.Approved, null), new DateOnly(2026, 11, 24));

    /// <param name="state">The user's state by two-letter code (Settings), or null when not chosen.</param>
    /// <param name="showDrafts">Drafts the owner has not approved are included, each labelled "Draft: not yet reviewed" (<c>--show-drafts</c>).</param>
    public static IReadOnlyList<HelpEntry> Step(HelpTables t, string iso3, string countryName, DateOnly today, string? state, bool showDrafts)
    {
        ArgumentNullException.ThrowIfNull(t);
        var entries = new List<HelpEntry>();
        GiveEntries(t, iso3, countryName, today, showDrafts, entries);
        if (t.Profile is { } profile) ContactEntries(t, profile, countryName, state, showDrafts, entries);
        return entries;
    }

    // ---------------------------------------------------------------- give

    private static void GiveEntries(HelpTables t, string iso3, string countryName, DateOnly today, bool showDrafts, List<HelpEntry> entries)
    {
        bool Visible(Review r) => r.IsApproved || showDrafts;
        var funds = t.Funds.Where(f => f.Id != HelpTables.CerfId && Visible(f.Review)
                && f.Iso3.Contains(iso3, StringComparer.OrdinalIgnoreCase))
            .OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase).ToList();
        if (funds.Count > 0)
        {
            foreach (var f in funds)
                entries.Add(new HelpEntry(GiveGroup, $"UN pooled fund for {countryName} (OCHA)", [.. Draft(f.Review), f.Name, f.GiveNote, $"Checked {Date(f.CheckedOn)}"],
                    [new PanelLink($"{f.Name}: OCHA page", f.Page), new PanelLink("How to give", f.GiveUrl)]) { ReviewedOn = f.Review.ApprovedOn });
        }
        else
        {
            var cerf = t.Funds.FirstOrDefault(f => f.Id == HelpTables.CerfId && Visible(f.Review)) ?? BuiltInCerf;
            entries.Add(new HelpEntry(GiveGroup, cerf.Name, [.. Draft(cerf.Review), cerf.GiveNote, $"Checked {Date(cerf.CheckedOn)}"],
                [new PanelLink("About CERF", cerf.Page), new PanelLink("CERF donate page", cerf.GiveUrl)]) { ReviewedOn = cerf.Review.ApprovedOn });
        }

        // The charities, alphabetically by name and never ranked (spec §9.2); each shows only when every check holds.
        var shown = t.Profile is null ? []
            : t.Charities.Where(c => VettingRules.Check(c, iso3, t.Profile.Evaluators, today, showDrafts).Shown)
                .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase).ToList();
        if (shown.Count == 0) entries.Add(new HelpEntry(GiveGroup, "Charities", [NoCharitiesText], []));
        foreach (var c in shown) entries.Add(CharityEntry(c, iso3, t.Profile!));
    }

    private static HelpEntry CharityEntry(Charity c, string iso3, CountryProfile profile)
    {
        var lines = new List<string>(Draft(c.Review));
        if (c.Legal is { } legal) lines.Add($"Tax-deductible (IRS Publication 78, checked {Date(legal.CheckedOn)})");
        if (c.Programme is { } p)
            lines.Add($"{Math.Round(p.Share * 100, MidpointRounding.AwayFromZero).ToString("0", CultureInfo.InvariantCulture)}% spent on programmes (Form 990, tax year {p.TaxYear.ToString(CultureInfo.InvariantCulture)})");
        // Rated by: the profile's evaluators that have a confirmed report, in the profile's order, linked and never scored (Q1).
        var rated = profile.Evaluators.Select(e => (Evaluator: e, Ratings: c.Ratings.Where(r => string.Equals(r.Evaluator, e.Name, StringComparison.OrdinalIgnoreCase)).ToList()))
            .Where(x => x.Ratings.Count > 0).ToList();
        lines.Add("Rated by " + string.Join(", ", rated.Select(x => x.Evaluator.Name)));
        var work = c.Countries.First(w => string.Equals(w.Iso3, iso3, StringComparison.OrdinalIgnoreCase));
        lines.Add($"Working here: {work.EvidenceTitle} (checked {Date(work.CheckedOn)})");
        lines.Add($"Reviewed {Date(c.ReviewedOn)}");
        var links = rated.SelectMany(x => x.Ratings.Select(r => new PanelLink($"{x.Evaluator.Name} report", r.ReportUrl))).ToList();
        links.Add(new PanelLink($"Evidence: {work.EvidenceTitle}", work.Evidence));
        links.Add(new PanelLink("Donate page", c.DonateUrl));
        return new HelpEntry(GiveGroup, c.Name, lines, links) { ReviewedOn = c.Review.IsApproved ? c.ReviewedOn : null };
    }

    // ---------------------------------------------------------------- contact

    private static void ContactEntries(HelpTables t, CountryProfile profile, string countryName, string? state, bool showDrafts, List<HelpEntry> entries)
    {
        foreach (var role in profile.Contacts.Where(r => r.Review.IsApproved || showDrafts))
        {
            switch (role.Kind)
            {
                case ContactKind.Senators:
                    if (t.Senators.Count > 0) entries.Add(SenatorsEntry(t, role, state));
                    break;
                case ContactKind.Representative when role.Url is not null:
                    entries.Add(new HelpEntry(ContactGroup, role.Title, [.. Draft(role.Review), role.Note, $"Checked {Date(role.CheckedOn)}"],
                        [new PanelLink("House of Representatives: find your representative", role.Url)]) { ReviewedOn = role.Review.ApprovedOn });
                    break;
                case ContactKind.Link when role.Url is not null:
                    entries.Add(new HelpEntry(ContactGroup, role.Title, [.. Draft(role.Review), role.Note, $"Checked {Date(role.CheckedOn)}"],
                        [new PanelLink(role.Source.Title, role.Url)]) { ReviewedOn = role.Review.ApprovedOn });
                    break;
                case ContactKind.UnMission when role.Url is not null:
                    // P4-10: one link to the Blue Book, never a list of ours (its terms forbid compiling one).
                    entries.Add(new HelpEntry(ContactGroup, $"{countryName}'s mission to the UN", [.. Draft(role.Review), role.Note, $"Checked {Date(role.CheckedOn)}"],
                        [new PanelLink(BlueBookText, role.Url)]) { ReviewedOn = role.Review.ApprovedOn });
                    break;
            }
        }
    }

    private static HelpEntry SenatorsEntry(HelpTables t, ContactRole role, string? state)
    {
        var name = UsStates.Name(state);
        var mine = name is null ? [] : t.Senators.Where(s => string.Equals(s.State, state, StringComparison.Ordinal)).ToList();
        if (mine.Count == 0)
            return new HelpEntry(ContactGroup, SenatorsTitle, [.. Draft(role.Review), ChooseStateText], []) { ReviewedOn = role.Review.ApprovedOn };
        var lines = new List<string>(Draft(role.Review));
        lines.AddRange(mine.Select(s => $"{s.Name} ({s.Party})"));
        lines.Add($"{role.Note} (US Senate contact list, as of {Date(t.SenatorsAsOf)})");
        var links = new List<PanelLink>();
        links.AddRange(mine.Select(s => new PanelLink($"Contact {s.Name}", s.ContactForm)));
        links.AddRange(mine.Select(s => new PanelLink($"{s.Name}: official website", s.Website)));
        return new HelpEntry(ContactGroup, $"{SenatorsTitle}: {name}", lines, links) { ReviewedOn = role.Review.ApprovedOn };
    }

    // ---------------------------------------------------------------- shared

    private static IEnumerable<string> Draft(Review r) => r.IsApproved ? [] : [DesignationText.DraftLabel];

    private static string Date(DateOnly d) => DesignationText.Date(d);
}
