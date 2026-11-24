using EarthRelief3D.Core.Curated;
using EarthRelief3D.Core.Help;

namespace EarthRelief3D.Tests;

/// <summary>Hand-made help rows (Plan 4 Task 6): approved unless a test says otherwise, all checked on 2026-11-24.</summary>
internal static class HelpFixtures
{
    public static readonly DateOnly Today = new(2026, 11, 24);

    public static readonly Evaluator CharityNavigator = new("Charity Navigator", "3 stars or more", new Uri("https://www.charitynavigator.org/"));
    public static readonly Evaluator Bbb = new("BBB Wise Giving Alliance", "meets all 20 Standards for Charity Accountability", new Uri("https://give.org/"));

    public static Review Approved(DateOnly? on = null) => new(Review.Approved, on ?? Today);
    public static Review Draft() => new(Review.Draft, null);

    public static DocumentCitation Cite(string url = "https://example.org/page") => new("A page", "A publisher", new Uri(url), Today);

    public static ContactRole Role(string id, ContactKind kind, string? url, bool approved = true) =>
        new(id, "Role " + id, kind, url is null ? null : new Uri(url), "A note.", Cite(), approved ? Approved() : Draft(), Today);

    public static CountryProfile Profile(bool approved = true) => new("US", "USA", "United States", "IRS Tax Exempt Organization Search (Publication 78)",
        new Uri("https://apps.irs.gov/app/eos/"), [CharityNavigator, Bbb],
        [
            Role("senators", ContactKind.Senators, null, approved),
            Role("representative", ContactKind.Representative, "https://www.house.gov/representatives/find-your-representative", approved),
            Role("state-department", ContactKind.Link, "https://register.state.gov/contactus/contactusform", approved),
            Role("un-mission", ContactKind.UnMission, "https://www.un.org/dgacm/en/content/protocol/blue-book", approved),
        ], Today);

    public static Charity Charity(string name, string iso3 = "SDN", DateOnly? reviewedOn = null, DateOnly? evidenceOn = null, bool approved = true,
        IReadOnlyList<CharityRating>? ratings = null, bool legalMissing = false, bool noEvidence = false, string? id = null) =>
        new(id ?? name.ToLowerInvariant(), name, "12-3456789", new Uri("https://example.org/" + name.ToLowerInvariant()),
            new Uri("https://example.org/" + name.ToLowerInvariant() + "/donate"),
            legalMissing ? null : new LegalCheck(new DateOnly(2026, 11, 1), "data-download-pub78.zip", "PC"),
            ratings ??
            [
                new CharityRating("Charity Navigator", new Uri("https://www.charitynavigator.org/ein/123456789"), new DateOnly(2026, 11, 2), "owner"),
                new CharityRating("BBB Wise Giving Alliance", new Uri("https://give.org/charity-reviews/national/x"), new DateOnly(2026, 11, 2), "owner"),
            ],
            new ProgrammeShare(0.84, 2024, "202400000000000001", new DateOnly(2026, 11, 3)),
            noEvidence ? [] : [new CountryWork(iso3, new Uri("https://example.org/" + name.ToLowerInvariant() + "/work"), $"{name} in {iso3}", "charity's own page", evidenceOn ?? new DateOnly(2026, 11, 4))],
            approved ? Approved(reviewedOn ?? new DateOnly(2026, 11, 5)) : Draft(), reviewedOn ?? new DateOnly(2026, 11, 5), "owner");

    public static PooledFund Fund(string id, string name, string[] iso3, bool approved = true) =>
        new(id, name, iso3, new Uri("https://www.unocha.org/" + id), new Uri("https://www.unocha.org/our-funding"),
            "OCHA's page explains how to give to its humanitarian funds", approved ? Approved() : Draft(), Today);

    public static PooledFund Cerf(bool approved = true) =>
        new("cerf", "Central Emergency Response Fund (CERF)", [], new Uri("https://cerf.un.org"), new Uri("https://cerf.un.org/donate"),
            "US tax-deductible gifts go through the UN Foundation; CERF's page explains how", approved ? Approved() : Draft(), Today);

    public static Senator Senator(string state, string name, string party = "D")
    {
        var last = name.Split(' ')[^1].ToLowerInvariant();
        return new Senator(state, name, party, new Uri($"https://www.{last}.senate.gov/contact"), new Uri($"https://www.{last}.senate.gov/"));
    }

    public static IReadOnlyList<Senator> Senators { get; } =
    [
        Senator("MD", "Angela D. Alsobrooks"), Senator("MD", "Chris Van Hollen"), Senator("WI", "Tammy Baldwin"), Senator("WI", "Ron Johnson", "R"),
    ];

    public static HelpTables Tables(IReadOnlyList<Charity>? charities = null, IReadOnlyList<PooledFund>? funds = null, CountryProfile? profile = null,
        IReadOnlyList<Senator>? senators = null) =>
        new(profile ?? Profile(), charities ?? [], funds ?? [Cerf()], senators ?? Senators, new DateOnly(2026, 11, 18));
}
