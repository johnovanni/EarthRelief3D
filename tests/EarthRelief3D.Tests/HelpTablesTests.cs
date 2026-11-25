using System.IO;
using EarthRelief3D.Core.Curated;
using EarthRelief3D.Core.Feeds;
using EarthRelief3D.Core.Help;
using static EarthRelief3D.Tests.HelpFixtures;

namespace EarthRelief3D.Tests;

/// <summary>
/// The help catalog's files (spec §9.4, §10; P4-8 to P4-13): every row cited, the review document kept in step, the
/// OCHA list complete. Run on the real <c>data/countries</c> and <c>data/help</c> files, so a bad row fails the build.
/// </summary>
public class HelpTablesTests
{
    private static string DataDir => Path.Combine(AppContext.BaseDirectory, "data");

    private static readonly string[] OchaPages =
    [
        "https://www.unocha.org/afghanistan-humanitarian-fund",
        "https://www.unocha.org/lebanon-humanitarian-fund",
        "https://www.unocha.org/somalia-humanitarian-fund",
        "https://www.unocha.org/syria-cross-border-humanitarian-fund",
        "https://www.unocha.org/regional-humanitarian-pooled-fund-asia-pacific",
        "https://www.unocha.org/central-african-repulic/about-central-african-repulic-humanitarian-fund",
        "https://www.unocha.org/myanmar-humanitarian-fund",
        "https://www.unocha.org/south-sudan-humanitarian-fund",
        "https://www.unocha.org/ukraine-humanitarian-fund",
        "https://www.unocha.org/regional-humanitarian-pooled-fund-west-and-central-africa",
        "https://www.unocha.org/democratic-republic-congo-humanitarian-fund",
        "https://www.unocha.org/nigeria-humanitarian-fund",
        "https://www.unocha.org/sudan-humanitarian-fund",
        "https://www.unocha.org/venezuela-humanitarian-fund",
        "https://www.unocha.org/regional-humanitarian-fund-latin-america-and-caribbean",
        "https://www.unocha.org/ethiopia-humanitarian-fund",
        "https://www.unocha.org/occupied-palestinian-territory-humanitarian-fund",
        "https://www.unocha.org/syria-humanitarian-fund",
        "https://www.unocha.org/yemen-humanitarian-fund",
        "https://www.unocha.org/regional-humanitarian-pooled-fund-eastern-and-southern-africa",
    ];

    [Fact]
    public void EveryRowIsCited()
    {
        var t = HelpTables.Load(DataDir, "US");   // Load validates; it throws on any problem
        Assert.Empty(HelpTables.Validate(t));
        Assert.NotNull(t.Profile);
        var profile = t.Profile!;

        Assert.Equal("USA", profile.Iso3);
        Assert.Equal("IRS Tax Exempt Organization Search (Publication 78)", profile.Registry);
        Assert.Equal("https://apps.irs.gov/app/eos/", profile.RegistryUrl.AbsoluteUri);
        Assert.Equal(["Charity Navigator", "BBB Wise Giving Alliance"], profile.Evaluators.Select(e => e.Name));
        Assert.Equal(["3 stars or more", "meets all 20 Standards for Charity Accountability"], profile.Evaluators.Select(e => e.Qualifies));
        Assert.Equal(["senators", "representative", "state-department", "un-mission"], profile.Contacts.Select(c => c.Id));
        Assert.Equal([ContactKind.Senators, ContactKind.Representative, ContactKind.Link, ContactKind.UnMission], profile.Contacts.Select(c => c.Kind));
        Assert.Equal("https://register.state.gov/contactus/contactusform", profile.Contacts.Single(c => c.Id == "state-department").Url!.AbsoluteUri);
        Assert.Equal("https://www.un.org/dgacm/en/content/protocol/blue-book", profile.Contacts.Single(c => c.Id == "un-mission").Url!.AbsoluteUri);
        Assert.Null(profile.Contacts.Single(c => c.Id == "senators").Url);
        foreach (var c in profile.Contacts)
        {
            // Approved by the owner on 2026-11-25; a new row starts as a draft again (P4-13).
            Assert.Equal(new Review(Review.Approved, new DateOnly(2026, 11, 25)), c.Review);
            Assert.Equal(Uri.UriSchemeHttps, c.Source.Url.Scheme);
            Assert.False(string.IsNullOrWhiteSpace(c.Source.Title), c.Id);
            Assert.True(c.CheckedOn > new DateOnly(2026, 1, 1), c.Id);
        }
        foreach (var f in t.Funds)
        {
            Assert.Equal(new Review(Review.Approved, new DateOnly(2026, 11, 25)), f.Review);   // a new row starts as a draft again
            Assert.Equal(new DateOnly(2026, 11, 24), f.CheckedOn);
            Assert.Equal(Uri.UriSchemeHttps, f.Page.Scheme);
            Assert.Equal(Uri.UriSchemeHttps, f.GiveUrl.Scheme);
            Assert.False(string.IsNullOrWhiteSpace(f.Name), f.Id);
        }
        // Task 8's first batch (P4-9): drafts with no rating (P4-8: the owner confirms ratings), the IRS check filled in, the
        // evaluators' profile links for the owner, and two pieces of evidence for every country listed: the charity's own page and 3W.
        // The second batch (OR-4, 2026-11-25) follows the same rule; the charities the owner named (OR-5) need only their own
        // page in each country, with a 3W row where the 3W file has one.
        string[] ownerNamed = ["american-red-cross", "doctors-without-borders-usa"];
        Assert.Equal(16, t.Charities.Count);
        Assert.All(ownerNamed, id => Assert.Contains(t.Charities, c => c.Id == id));
        foreach (var c in t.Charities)
        {
            Assert.Equal(Review.Draft, c.Review.Status);
            Assert.Equal("Claude (draft)", c.Reviewer);
            Assert.Empty(c.Ratings);
            Assert.Equal("PC", c.Legal?.DeductibilityCode);
            Assert.Equal(new Uri($"https://www.charitynavigator.org/ein/{c.Ein.Replace("-", "", StringComparison.Ordinal)}"), c.ProfileLinks[0]);
            Assert.StartsWith("https://www.give.org/search?term=", c.ProfileLinks[1].AbsoluteUri, StringComparison.Ordinal);
            Assert.Equal("Open both profile links; say whether it is rated 3★ or more by Charity Navigator or meets all 20 BBB standards", c.OwnerAction);
            Assert.NotEmpty(c.Countries);
            foreach (var country in c.Countries.GroupBy(w => w.Iso3))
            {
                var kinds = country.Select(w => w.EvidenceKind).Order(StringComparer.Ordinal).ToArray();
                if (ownerNamed.Contains(c.Id) && kinds.Length == 1) Assert.Equal(["charity's own page"], kinds);
                else Assert.Equal(["OCHA 3W", "charity's own page"], kinds);
            }
        }
    }

    [Fact]
    public void FundsCoverOchaList()
    {
        var t = HelpTables.Load(DataDir, "US");
        Assert.Equal(21, t.Funds.Count);   // OCHA's 20 pooled funds plus CERF
        var cerf = Assert.Single(t.Funds, f => f.Id == "cerf");
        Assert.Equal("https://cerf.un.org/donate", cerf.GiveUrl.AbsoluteUri);
        Assert.Equal("https://cerf.un.org/", cerf.Page.AbsoluteUri);
        Assert.Equal("US tax-deductible gifts go through the UN Foundation; CERF's page explains how", cerf.GiveNote);
        Assert.Empty(cerf.Iso3);

        var ocha = t.Funds.Where(f => f.Id != "cerf").ToList();
        Assert.Equal(OchaPages.Order(StringComparer.Ordinal), ocha.Select(f => f.Page.AbsoluteUri).Order(StringComparer.Ordinal));
        Assert.All(ocha, f =>
        {
            Assert.Equal("https://www.unocha.org/our-funding", f.GiveUrl.AbsoluteUri);
            Assert.Equal("OCHA's page explains how to give to its humanitarian funds", f.GiveNote);
            Assert.NotEmpty(f.Iso3);
        });

        // The regional funds cover the countries the CBPF API lists as their sub-funds.
        string[] Of(string slug) => [.. ocha.Single(f => f.Page.AbsoluteUri.EndsWith(slug, StringComparison.Ordinal)).Iso3.Order(StringComparer.Ordinal)];
        Assert.Equal(["BGD", "FJI", "LAO", "LKA", "NPL", "PAK", "PHL", "PNG", "SLB", "TON", "VUT"], Of("regional-humanitarian-pooled-fund-asia-pacific"));
        Assert.Equal(["BFA", "MLI", "NER", "TCD"], Of("regional-humanitarian-pooled-fund-west-and-central-africa"));
        Assert.Equal(["COL", "GTM", "HND", "HTI", "SLV"], Of("regional-humanitarian-fund-latin-america-and-caribbean"));
        Assert.Equal(["KEN", "UGA"], Of("regional-humanitarian-pooled-fund-eastern-and-southern-africa"));
        // Two funds for Syria; the cross-border one maps to SYR.
        Assert.Equal(["SYR"], Of("syria-cross-border-humanitarian-fund"));
        Assert.Equal(2, ocha.Count(f => f.Iso3.Contains("SYR")));
        // Every fund country is a country on the map (C16).
        var countries = CountryResolver.LoadDefault();
        foreach (var iso in ocha.SelectMany(f => f.Iso3)) Assert.NotNull(countries.ByIso3(iso));
    }

    [Fact]
    public void RealFilesLoad()
    {
        var t = HelpTables.Load(DataDir, "US");
        Assert.Equal(100, t.Senators.Count);
        var states = t.Senators.GroupBy(s => s.State).ToList();
        Assert.Equal(50, states.Count);
        Assert.All(states, g => Assert.Equal(2, g.Count()));
        Assert.All(t.Senators, s =>
        {
            Assert.Equal(Uri.UriSchemeHttps, s.ContactForm.Scheme);
            Assert.Equal(Uri.UriSchemeHttps, s.Website.Scheme);
            Assert.False(string.IsNullOrWhiteSpace(s.Name));
        });
        Assert.True(t.SenatorsAsOf > new DateOnly(2026, 1, 1));
        Assert.Equal([new AvailableCountry("US", "United States")], t.AvailableCountries);

        // A country with no profile.json has no profile; the funds still load (they are keyed by country).
        var other = HelpTables.Load(DataDir, "FR");
        Assert.Null(other.Profile);
        Assert.Empty(other.Senators);
        Assert.Equal(21, other.Funds.Count);
        Assert.Equal(t.AvailableCountries, other.AvailableCountries);
    }

    [Fact]
    public void ABadFileIsAnInvalidDataException()
    {
        using var dir = new TempDir();
        Directory.CreateDirectory(Path.Combine(dir.Path, "help"));
        File.WriteAllText(Path.Combine(dir.Path, "help", "pooled-funds.json"), "[ not json");
        Assert.Throws<InvalidDataException>(() => HelpTables.Load(dir.Path, "US"));
        Assert.Throws<InvalidDataException>(() => HelpTables.Load(Path.Combine(dir.Path, "nowhere"), "US"));
    }

    [Fact]
    public void ValidateNamesEachProblem()
    {
        Assert.Empty(HelpTables.Validate(Tables(charities: [Charity("Alpha")])));
        Assert.Empty(HelpTables.Validate(HelpTables.Empty));

        // A link that is not https, a missing citation, a duplicate id, a third senator.
        var http = Fund("a", "A", ["SDN"]) with { Page = new Uri("http://www.unocha.org/a") };
        Assert.Contains(HelpTables.Validate(Tables(funds: [http])), p => p.StartsWith("a:") && p.Contains("https"));
        Assert.Contains(HelpTables.Validate(Tables(funds: [Fund("a", "A", ["SDN"]), Fund("a", "B", ["SDN"])])), p => p.Contains("used twice"));
        Assert.Contains(HelpTables.Validate(Tables(funds: [Fund("a", "A", [])])), p => p.Contains("country"));
        Assert.Contains(HelpTables.Validate(Tables(funds: [Fund("a", "A", ["sdn"])])), p => p.Contains("iso3"));
        var noCite = Profile().Contacts[0] with { Source = Cite() with { Title = "" } };
        Assert.Contains(HelpTables.Validate(Tables(profile: Profile() with { Contacts = [noCite] })), p => p.StartsWith("senators:") && p.Contains("title"));
        var badLink = Profile().Contacts[1] with { Url = new Uri("http://www.house.gov/") };
        Assert.Contains(HelpTables.Validate(Tables(profile: Profile() with { Contacts = [badLink] })), p => p.Contains("https"));
        var noUrl = Profile().Contacts[1] with { Url = null };
        Assert.Contains(HelpTables.Validate(Tables(profile: Profile() with { Contacts = [noUrl] })), p => p.Contains("url"));
        Assert.Contains(HelpTables.Validate(Tables(profile: Profile() with { Evaluators = [] })), p => p.Contains("evaluator"));
        Assert.Contains(HelpTables.Validate(Tables(senators: [.. Senators, Senator("MD", "Third Person")])), p => p.Contains("MD") && p.Contains("two"));
        Assert.Contains(HelpTables.Validate(Tables(senators: [Senator("XX", "Nobody")])), p => p.Contains("XX"));
        var draftWithDate = Fund("a", "A", ["SDN"]) with { Review = new Review(Review.Draft, Today) };
        Assert.Contains(HelpTables.Validate(Tables(funds: [draftWithDate])), p => p.Contains("approvedOn"));

        // A charity: an approved row that the rules would hide is a problem for the owner to see; a draft may be incomplete (P4-8).
        Assert.Empty(HelpTables.Validate(Tables(charities: [Charity("Alpha", approved: false, ratings: [], legalMissing: true)])));
        Assert.Contains(HelpTables.Validate(Tables(charities: [Charity("Alpha", ratings: [])])), p => p.Contains("approved") && p.Contains("rating"));
        Assert.Contains(HelpTables.Validate(Tables(charities: [Charity("Alpha") with { Ein = "123" }])), p => p.Contains("EIN"));
        Assert.Contains(HelpTables.Validate(Tables(charities: [Charity("Alpha"), Charity("Alpha")])), p => p.Contains("used twice"));
        var httpDonate = Charity("Alpha") with { DonateUrl = new Uri("http://example.org/d") };
        Assert.Contains(HelpTables.Validate(Tables(charities: [httpDonate])), p => p.Contains("https"));
    }

    [Fact]
    public void ReviewDocumentIsCurrent()
    {
        // docs/help-review.md is generated from the tables, for the owner. Set ER3D_WRITE_REVIEW=1 to rewrite it.
        var t = HelpTables.Load(DataDir, "US");
        var text = HelpReview.Render(t);
        var path = Path.Combine(Repo.Root(), "docs", "help-review.md");
        if (Environment.GetEnvironmentVariable("ER3D_WRITE_REVIEW") == "1") File.WriteAllText(path, text);
        Assert.True(File.Exists(path), "docs/help-review.md is missing: run the tests with ER3D_WRITE_REVIEW=1");
        Assert.Equal(text.ReplaceLineEndings("\n"), File.ReadAllText(path).ReplaceLineEndings("\n"));

        foreach (var f in t.Funds) { Assert.Contains(f.Id, text); Assert.Contains(f.Page.AbsoluteUri, text); }
        foreach (var c in t.Profile!.Contacts) Assert.Contains(c.Id, text);
        Assert.Contains("https://register.state.gov/contactus/contactusform", text);
        Assert.Contains("## Charities", text);
        foreach (var c in t.Charities) Assert.Contains($"`{c.Id}`", text);
        Assert.DoesNotContain("No charities are listed yet", text);
        // Only drafts carry the owner's line.
        Assert.Equal(t.Funds.Count(f => !f.Review.IsApproved) + t.Profile.Contacts.Count(c => !c.Review.IsApproved) + t.Charities.Count(c => !c.Review.IsApproved),
            text.Split('\n').Count(l => l.StartsWith("- Owner: approve / correct", StringComparison.Ordinal)));
        Assert.DoesNotContain(".superpowers", text);
    }

    [Fact]
    public void ReviewDocumentShowsAChargesChecks()
    {
        // A draft charity: its ratings, links and what is still missing for it to show, independent of today's date.
        var draft = Charity("Alpha", approved: false, ratings: []) with { ProfileLinks = [new Uri("https://www.charitynavigator.org/ein/123456789")], OwnerAction = "Open its profile pages." };
        var text = HelpReview.Render(Tables(charities: [draft, Charity("Beta")]));
        Assert.Contains("### `alpha`: Alpha", text);
        Assert.Contains("https://www.charitynavigator.org/ein/123456789", text);
        Assert.Contains("Open its profile pages.", text);
        Assert.Contains("no confirmed rating", text);
        Assert.Contains("### `beta`: Beta", text);
        Assert.Contains("the review expires 5 Nov 2027", text);
        Assert.DoesNotContain("No charities are listed yet", text);
    }

    [Fact]
    public void ReviewDocumentShowsACharitysNote()
    {
        // R16: the reviewer's note reaches the owner in the review document; a charity without one has no note line.
        var noted = Charity("Alpha", approved: false, ratings: []) with { Note = "EIN matched by name in Publication 78 only." };
        var text = HelpReview.Render(Tables(charities: [noted, Charity("Beta")]));
        Assert.Contains("- Note: EIN matched by name in Publication 78 only.", text);
        var charities = text[text.IndexOf("## Charities", StringComparison.Ordinal)..];   // contacts have notes of their own above
        Assert.Single(charities.Split('\n'), l => l.StartsWith("- Note:", StringComparison.Ordinal));

        // The real list: read from the JSON's "note".
        var real = HelpTables.Load(DataDir, "US");
        Assert.Contains("EIN matched by name in Publication 78 only", real.Charities.Single(c => c.Id == "norwegian-refugee-council-usa").Note, StringComparison.Ordinal);
        foreach (var c in real.Charities.Where(c => c.Note is { Length: > 0 })) Assert.Contains($"- Note: {c.Note}", HelpReview.Render(real));
    }
}
