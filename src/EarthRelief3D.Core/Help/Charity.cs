using EarthRelief3D.Core.Curated;

namespace EarthRelief3D.Core.Help;

/// <summary>The IRS Publication 78 check (P4-9; made by the vetting tool, Task 7): the day, the file it read and the deductibility code (PC: public charity).</summary>
public sealed record LegalCheck(DateOnly CheckedOn, string Pub78File, string DeductibilityCode);

/// <summary>
/// A rating an evaluator gives, confirmed by a person (P4-8: scripts and agents never read the evaluators' pages): the
/// evaluator's name, the link to its report, the day it was read and by whom. The rating itself (stars, score) is never
/// stored or shown (Q1): the report link is the fact.
/// </summary>
public sealed record CharityRating(string Evaluator, Uri ReportUrl, DateOnly CheckedOn, string CheckedBy);

/// <summary>The share of expenses spent on programmes, worked out by the vetting tool from a Form 990 (Part IX line 25): shown as a whole percent with its tax year.</summary>
public sealed record ProgrammeShare(double Share, int TaxYear, string ObjectId, DateOnly ComputedOn);

/// <summary>
/// Evidence that the charity works in a country: its own programme page, or an OCHA 3W (operational presence) record
/// (<paramref name="EvidenceKind"/> is "charity's own page" or "OCHA 3W"), with the title of the page and the day it was read.
/// </summary>
public sealed record CountryWork(string Iso3, Uri Evidence, string EvidenceTitle, string EvidenceKind, DateOnly CheckedOn);

/// <summary>
/// A US charity the panel may list (spec §9.2). It shows only when every check holds (<see cref="VettingRules"/>): a draft, a
/// missing IRS check, no confirmed rating from a listed evaluator, no evidence for the country, evidence or review older than
/// 12 months: each hides it. The app never ranks charities: the UN fund first, then alphabetically by name.
/// </summary>
/// <param name="Ein">"12-3456789".</param>
/// <param name="Website">The charity's own site.</param>
/// <param name="DonateUrl">The charity's own donate page: the app links, it never takes a gift.</param>
/// <param name="ReviewedOn">The day the owner reviewed the entry (it expires 12 months later).</param>
/// <param name="Reviewer">Who reviewed it.</param>
public sealed record Charity(string Id, string Name, string Ein, Uri Website, Uri DonateUrl, LegalCheck? Legal, IReadOnlyList<CharityRating> Ratings,
    ProgrammeShare? Programme, IReadOnlyList<CountryWork> Countries, Review Review, DateOnly ReviewedOn, string Reviewer)
{
    /// <summary>The evaluators' profile pages for the owner to open when confirming the ratings (docs/help-review.md only; never shown in the app).</summary>
    public IReadOnlyList<Uri> ProfileLinks { get; init; } = [];

    /// <summary>What the owner is asked to do for this entry (docs/help-review.md only).</summary>
    public string? OwnerAction { get; init; }

    /// <summary>The reviewer's disclosures for the owner (R16): how a fact was established or what is weaker than usual (docs/help-review.md only; never shown in the app).</summary>
    public string? Note { get; init; }
}
