using System.IO;
using EarthRelief3D.App.Curated;
using EarthRelief3D.App.Presentation;
using EarthRelief3D.App.Settings;
using EarthRelief3D.App.ViewModels;
using EarthRelief3D.Core.Curated;
using EarthRelief3D.Core.Feeds;
using EarthRelief3D.Core.Help;
using EarthRelief3D.Core.Model;
using EarthRelief3D.Core.Store;
using static EarthRelief3D.Tests.HelpFixtures;

namespace EarthRelief3D.Tests;

/// <summary>"Your one small step" (Plan 4 Task 6; P4-8, P4-10 to P4-13, P4-15; Review Focus 1, 3 and 5).</summary>
public class HelpPanelTests
{
    private static readonly DateTime Now = AccessPanelTests.Now;
    private static readonly CountryResolver Countries = CountryResolver.LoadDefault();

    private static IReadOnlyList<HelpEntry> Step(HelpTables t, string iso3 = "SDN", string? name = null, string? state = null, bool drafts = false) =>
        HelpCatalog.Step(t, iso3, name ?? Countries.ByIso3(iso3)?.Name ?? iso3, Today, state, drafts);

    private static HelpEntry Single(IReadOnlyList<HelpEntry> entries, string title) => Assert.Single(entries, e => e.Title == title);

    private static CrisisStore WithConflict(string iso3)
    {
        var store = new CrisisStore();
        store.Load(new CrisisSnapshot([TestData.Conflict() with { CountryIso3 = iso3 }], [], []));
        return store;
    }

    private static CountryPanelViewModel Panel(HelpTables? t, CrisisStore? store = null, string? state = null, bool drafts = false, IndicatorStore? ind = null,
        CuratedTables? curated = null) =>
        new(store ?? new CrisisStore(), iso => Countries.ByIso3(iso)?.Name, nowUtc: () => Now, indicators: ind,
            curated: new CuratedStore(curated ?? CuratedTables.Empty, drafts), help: t, userState: () => state);

    private static PanelSection StepSection(CountryPanelViewModel p) => Assert.Single(p.Sections, s => s.Heading == CountryPanelViewModel.StepHeading);

    [Fact]
    public void FundFirstThenAlphabetical()
    {
        var t = Tables(charities: [Charity("Gamma"), Charity("Alpha"), Charity("beta")],
            funds: [Fund("sdn", "Sudan Humanitarian Fund", ["SDN"]), Cerf()]);
        var give = Step(t).Where(e => e.Group == "Give").ToList();
        Assert.Equal(["UN pooled fund for Sudan (OCHA)", "Alpha", "beta", "Gamma"], give.Select(e => e.Title));
        // The fund's OCHA page, then OCHA's "how to give" page (P4-11).
        var fund = give[0];
        Assert.Contains(fund.Links, l => l.Url.AbsoluteUri == "https://www.unocha.org/sdn");
        Assert.Contains(fund.Links, l => l.Url.AbsoluteUri == "https://www.unocha.org/our-funding");
        Assert.Contains("OCHA's page explains how to give to its humanitarian funds", fund.Lines);
        // The pooled fund replaces CERF for a country that has one.
        Assert.DoesNotContain(give, e => e.Title.Contains("CERF"));
        // Contacts follow the Give group.
        Assert.Equal(["Give", "Give", "Give", "Give", "Contact", "Contact", "Contact", "Contact"], Step(t).Select(e => e.Group));
    }

    [Fact]
    public void CerfWhenNoFund()
    {
        // Iceland with a crisis fixture: no fund of its own, so the panel shows CERF (Review Focus 1).
        var panel = Panel(Tables(), WithConflict("ISL"));
        panel.Show("ISL");
        var give = Assert.Single(StepSection(panel).Entries!, e => e.Group == "Give" && e.Title.Contains("CERF"));
        Assert.Contains(give.Links, l => l.Url.AbsoluteUri == "https://cerf.un.org/donate");
        Assert.Contains("US tax-deductible gifts go through the UN Foundation; CERF's page explains how", give.Lines);

        // With no tables at all the CERF line is still there, and so is the charity line.
        var empty = Step(HelpTables.Empty, "ISL");
        var cerf = Assert.Single(empty, e => e.Title.Contains("CERF"));
        Assert.Contains(cerf.Links, l => l.Url.AbsoluteUri == "https://cerf.un.org/donate");
        Assert.Contains(empty, e => e.Lines.Contains("No vetted charities listed yet"));
        Assert.DoesNotContain(empty, e => e.Group == "Contact");
    }

    [Fact]
    public void DraftFundsFallBackToCerf()
    {
        // Pooled funds are drafts until the owner approves them (P4-13): a normal run shows CERF; --show-drafts shows the fund, labelled.
        var t = Tables(funds: [Fund("sdn", "Sudan Humanitarian Fund", ["SDN"], approved: false), Cerf(approved: false)]);
        Assert.Contains(Step(t), e => e.Title.Contains("CERF"));
        Assert.DoesNotContain(Step(t), e => e.Title.StartsWith("UN pooled fund", StringComparison.Ordinal));
        var shown = Single(Step(t, drafts: true), "UN pooled fund for Sudan (OCHA)");
        Assert.Contains(DesignationText.DraftLabel, shown.Lines);
    }

    [Fact]
    public void RegionalFundCoversItsCountries()
    {
        var east = Fund("regional-east", "Regional Humanitarian Pooled Fund for Eastern and Southern Africa", ["KEN", "UGA"]);
        var t = Tables(funds: [east, Cerf()]);
        var kenya = Single(Step(t, "KEN"), "UN pooled fund for Kenya (OCHA)");
        Assert.Contains(east.Name, kenya.Lines);
        Assert.Contains(kenya.Links, l => l.Url.AbsoluteUri == "https://www.unocha.org/regional-east");
        Assert.DoesNotContain(Step(t, "TZA"), e => e.Title.StartsWith("UN pooled fund", StringComparison.Ordinal));

        // The real list (drafts shown): Kenya gets the regional fund, Syria both of its funds ordered by name.
        var real = HelpTables.Load(BoundaryData.DataDir, "US");
        var ken = Single(Step(real, "KEN", drafts: true), "UN pooled fund for Kenya (OCHA)");
        Assert.Contains(ken.Links, l => l.Url.AbsoluteUri.EndsWith("regional-humanitarian-pooled-fund-eastern-and-southern-africa", StringComparison.Ordinal));
        var syria = Step(real, "SYR", drafts: true).Where(e => e.Title.StartsWith("UN pooled fund", StringComparison.Ordinal)).ToList();
        Assert.Equal(2, syria.Count);
        Assert.Equal(["Syria Cross-border Humanitarian Fund", "Syria Humanitarian Fund"], syria.Select(e => e.Lines.Single(l => l.Contains("Humanitarian Fund", StringComparison.Ordinal))));
    }

    [Fact]
    public void NoCharitiesSaysSo()
    {
        var none = Step(Tables());
        Assert.Contains(none, e => e.Group == "Give" && e.Lines.Contains("No vetted charities listed yet"));

        // Charities that are hidden (another country, expired, a draft) count as none (Review Focus 1, 3).
        var hidden = Tables(charities: [Charity("Yemenonly", iso3: "YEM"), Charity("Old", reviewedOn: new DateOnly(2025, 11, 24)), Charity("Drafty", approved: false)]);
        var entries = Step(hidden);
        Assert.Contains(entries, e => e.Lines.Contains("No vetted charities listed yet"));
        Assert.DoesNotContain(entries, e => e.Title is "Yemenonly" or "Old" or "Drafty");

        // And a country with a shown charity does not say it.
        Assert.DoesNotContain(Step(Tables(charities: [Charity("Alpha")])), e => e.Lines.Contains("No vetted charities listed yet"));
    }

    [Fact]
    public void CharityLines()
    {
        var e = Single(Step(Tables(charities: [Charity("Alpha")])), "Alpha");
        Assert.Equal(
        [
            "Tax-deductible (IRS Publication 78, checked 1 Nov 2026)",
            "84% spent on programmes (Form 990, tax year 2024)",
            "Rated by Charity Navigator, BBB Wise Giving Alliance",
            "Working here: Alpha in SDN (checked 4 Nov 2026)",
            "Reviewed 5 Nov 2026",
        ], e.Lines);
        Assert.Equal(["Charity Navigator report", "BBB Wise Giving Alliance report", "Evidence: Alpha in SDN", "Donate page"], e.Links.Select(l => l.Text));
        Assert.Equal(["https://www.charitynavigator.org/ein/123456789", "https://give.org/charity-reviews/national/x", "https://example.org/alpha/work", "https://example.org/alpha/donate"],
            e.Links.Select(l => l.Url.AbsoluteUri));

        // Ratings are never copied: no stars, scores or the evaluators' thresholds (Q1).
        var text = string.Join("\n", e.Lines.Concat(e.Links.Select(l => l.Text))).ToLowerInvariant();
        foreach (var word in new[] { "star", "score", "out of", "3 stars", "meets all 20", "★" }) Assert.DoesNotContain(word, text);
    }

    [Fact]
    public void CharityNoteStaysOutOfThePanel()
    {
        // R16: the reviewer's note is for the owner (docs/help-review.md), never shown in the app.
        var withNote = Single(Step(Tables(charities: [Charity("Alpha") with { Note = "Reviewer disclosure for the owner." }])), "Alpha");
        var plain = Single(Step(Tables(charities: [Charity("Alpha")])), "Alpha");
        Assert.Equal(plain.Lines, withNote.Lines);
        Assert.Equal(plain.Links.Select(l => l.Text), withNote.Links.Select(l => l.Text));
        Assert.DoesNotContain(withNote.Lines.Concat(withNote.Links.Select(l => l.Text)), s => s.Contains("disclosure", StringComparison.Ordinal));
    }

    [Fact]
    public void ADraftCharityShowsOnlyWithDraftsAndIsLabelled()
    {
        var t = Tables(charities: [Charity("Alpha", approved: false)]);
        Assert.DoesNotContain(Step(t), e => e.Title == "Alpha");
        var shown = Single(Step(t, drafts: true), "Alpha");
        Assert.Equal(DesignationText.DraftLabel, shown.Lines[0]);
        // An approved row carries no label.
        Assert.DoesNotContain(DesignationText.DraftLabel, Single(Step(Tables(charities: [Charity("Alpha")]), drafts: true), "Alpha").Lines);
    }

    [Fact]
    public void ContactDraftsAreHiddenUntilApproved()
    {
        var t = Tables(profile: Profile(approved: false));
        Assert.DoesNotContain(Step(t), e => e.Group == "Contact");
        var shown = Step(t, drafts: true).Where(e => e.Group == "Contact").ToList();
        Assert.Equal(4, shown.Count);
        Assert.All(shown, e => Assert.Contains(DesignationText.DraftLabel, e.Lines));
    }

    [Fact]
    public void FundCerfAndContactLinesShowTheirDate()
    {
        var t = Tables(funds: [Fund("sdn", "Sudan Humanitarian Fund", ["SDN"]), Cerf()]);
        Assert.Contains("Checked 24 Nov 2026", Single(Step(t), "UN pooled fund for Sudan (OCHA)").Lines);
        Assert.Contains("Checked 24 Nov 2026", Assert.Single(Step(t, "ISL"), e => e.Title.Contains("CERF")).Lines);
        Assert.Contains("Checked 24 Nov 2026", Assert.Single(Step(HelpTables.Empty, "ISL"), e => e.Title.Contains("CERF")).Lines);
        // Every linked contact (not the senators' line, which carries the Senate list's own date) shows its date; the fixture
        // must have such contacts, or the loop would pass with nothing checked (m32).
        var contacts = Step(t).Where(x => x.Group == "Contact" && x.Links.Count > 0 && !x.Title.StartsWith("Your two", StringComparison.Ordinal)).ToList();
        Assert.NotEmpty(contacts);
        foreach (var e in contacts)
            Assert.Contains("Checked 24 Nov 2026", e.Lines);
    }

    [Fact]
    public void NoStateAsksForState()
    {
        var senators = Single(Step(Tables()), "Your two senators");
        Assert.Contains("Choose your state in Settings to list your two senators", senators.Lines);
        Assert.Empty(senators.Links);
        // The House line links to the official finder; the app never asks for a ZIP code or an address.
        var house = Assert.Single(Step(Tables()), e => e.Links.Any(l => l.Url.AbsoluteUri == "https://www.house.gov/representatives/find-your-representative"));
        Assert.Equal("Contact", house.Group);
        Assert.DoesNotContain(Step(Tables()), e => e.Lines.Concat([e.Title]).Any(l => l.Contains("ZIP", StringComparison.OrdinalIgnoreCase)
                                                                             && l.Contains("enter", StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public void StateShowsTwoSenators()
    {
        var e = Single(Step(Tables(), state: "MD"), "Your two senators: Maryland");
        Assert.Equal(["Angela D. Alsobrooks (D)", "Chris Van Hollen (D)"], e.Lines.Take(2));
        Assert.Contains(e.Lines, l => l.Contains("18 Nov 2026"));   // the Senate list's own date
        Assert.Equal(["https://www.alsobrooks.senate.gov/contact", "https://www.hollen.senate.gov/contact"],
            e.Links.Where(l => l.Text.StartsWith("Contact", StringComparison.Ordinal)).Select(l => l.Url.AbsoluteUri));
        Assert.DoesNotContain(Step(Tables(), state: "MD"), x => x.Lines.Any(l => l.Contains("Baldwin")));
        // A state the list does not know (a stale setting) asks again rather than showing nothing.
        Assert.Contains("Choose your state in Settings to list your two senators", Single(Step(Tables(), state: "ZZ"), "Your two senators").Lines);
    }

    [Fact]
    public void StateChangeRebuildsPanel()
    {
        var settings = new MemorySettingsStore();
        var main = new MainViewModel(WithConflict("SDN"), settings, iso => Countries.ByIso3(iso)?.Name, null, () => Now, help: Tables());
        main.Panel.Show("SDN");
        Assert.Contains("Choose your state in Settings to list your two senators", Lines(main.Panel));

        main.Settings.UserState = "MD";
        Assert.Contains("Chris Van Hollen (D)", Lines(main.Panel));
        Assert.DoesNotContain("Choose your state in Settings to list your two senators", Lines(main.Panel));
        Assert.Equal("MD", settings.Current.UserState);

        main.Settings.UserState = null;
        Assert.Contains("Choose your state in Settings to list your two senators", Lines(main.Panel));

        static IEnumerable<string> Lines(CountryPanelViewModel p) => StepSection(p).Entries!.SelectMany(e => e.Lines);
    }

    [Fact]
    public void RebuildOfTheSameCountryDoesNotScroll()
    {
        // Final review F1: choosing a state or a unit (or a feed refresh) rebuilds the open panel; the panel must stay where the
        // user scrolled to. Only Show (a new country or a new focus) asks the view to scroll.
        var settings = new MemorySettingsStore();
        var main = new MainViewModel(WithConflict("SDN"), settings, iso => Countries.ByIso3(iso)?.Name, null, () => Now, help: Tables());
        int scrolls = 0;
        main.Panel.ScrollRequested += (_, _) => scrolls++;
        main.Panel.Show("SDN", section: PanelFocus.Help);
        Assert.Equal(1, scrolls);
        var focus = main.Panel.FocusedSectionIndex;
        Assert.True(focus >= 0);
        var before = main.Panel.Sections;

        main.Settings.UserState = "MD";
        main.Settings.Units = DistanceUnit.Miles;
        main.Panel.RefreshOpen();
        Assert.NotSame(before, main.Panel.Sections);         // rebuilt
        Assert.Equal(focus, main.Panel.FocusedSectionIndex);  // same focus
        Assert.Equal(1, scrolls);                             // and no scroll request

        main.Panel.Show("SDN");                               // a new focus (none): scrolls
        Assert.Equal(2, scrolls);
        main.Panel.Show("ISL");                               // a new country: scrolls
        Assert.Equal(3, scrolls);
    }

    [Fact]
    public void UnMissionIsTheBlueBook()
    {
        var e = Single(Step(Tables(), "SDN", "Sudan"), "Sudan's mission to the UN");
        Assert.Equal("Contact", e.Group);
        var link = Assert.Single(e.Links);
        Assert.Equal("https://www.un.org/dgacm/en/content/protocol/blue-book", link.Url.AbsoluteUri);
        Assert.Equal("Sudan's mission to the UN: UN Blue Book", $"{e.Title}: {link.Text}");
        // The Blue Book is one link: no per-country list of ours (P4-10).
        Assert.DoesNotContain(e.Lines, l => l.Contains("@", StringComparison.Ordinal));
    }

    [Fact]
    public void TheStateDepartmentLink()
    {
        var e = Assert.Single(Step(Tables()), x => x.Links.Any(l => l.Url.AbsoluteUri == "https://register.state.gov/contactus/contactusform"));
        Assert.Equal("Contact", e.Group);
    }

    [Fact]
    public void EveryLinkIsHttps()
    {
        // Every country, with the real files (drafts shown, so every row is in), no state and every state: all https.
        var real = HelpTables.Load(BoundaryData.DataDir, "US");
        var withCharities = real with { Charities = [Charity("Alpha", iso3: "SDN"), Charity("Beta", iso3: "SDN")] };
        var states = real.Senators.Select(s => s.State).Distinct().Concat([(string?)null]).ToList();
        Assert.True(states.Count >= 50);
        var seen = 0;
        foreach (var country in Countries.Index.All)
            foreach (var state in states)
            {
                foreach (var e in Step(withCharities, country.Iso3, country.Name, state, drafts: true))
                    foreach (var link in e.Links)
                    {
                        seen++;
                        Assert.True(link.Url.IsAbsoluteUri && link.Url.Scheme == Uri.UriSchemeHttps, $"{country.Iso3} {state}: {e.Title}: {link.Url}");
                        Assert.False(string.IsNullOrWhiteSpace(link.Text));
                    }
            }
        Assert.True(seen > 1000);
        // The same without drafts (the shipped state: contacts and funds are drafts).
        foreach (var e in Step(real, "SDN", "Sudan")) foreach (var link in e.Links) Assert.Equal(Uri.UriSchemeHttps, link.Url.Scheme);
    }

    [Fact]
    public void NoCrisisNoStep()
    {
        // Iceland, no crisis and no appeal: no step section. Getting help in still shows.
        var panel = Panel(Tables(), ind: new IndicatorStore());
        panel.Show("ISL");
        Assert.DoesNotContain(panel.Sections, s => s.Heading == CountryPanelViewModel.StepHeading);
        Assert.Contains(panel.Sections, s => s.Heading == CountryPanelViewModel.GettingHelpHeading);

        // A crisis brings it (P4-12); so does an appeal for the year, with no crisis.
        var withCrisis = Panel(Tables(), WithConflict("ISL"));
        withCrisis.Show("ISL");
        Assert.NotNull(StepSection(withCrisis));

        var ind = new IndicatorStore();
        var cite = new SourceCitation(FtsFundingFeed.Publisher, "Requirements and funding", "2026", FtsFundingFeed.CreditUrl, "CC BY-IGO", "FTS", Now, Now, CitationDate.AsOf);
        ind.ReplaceAppeals([new AppealFunding("ISL", 7, "A plan", "Flash appeal", 2026, 3000, 1000, 34, false, cite)]);
        var withAppeal = Panel(Tables(), ind: ind);
        withAppeal.Show("ISL");
        Assert.True(withAppeal.HasNoCrisis);   // an appeal is not a crisis record: the headline still says so
        Assert.NotNull(StepSection(withAppeal));

        // No help tables given: no section at all.
        var none = Panel(null, WithConflict("ISL"));
        none.Show("ISL");
        Assert.DoesNotContain(none.Sections, s => s.Heading == CountryPanelViewModel.StepHeading);
    }

    [Fact]
    public async Task TheStepFollowsTheHelpAndFundingSections()
    {
        var ind = await AccessPanelTests.Loaded();
        var panel = Panel(Tables(), WithConflict("SDN"), ind: ind);
        panel.Show("SDN");
        var headings = panel.Sections.Select(s => s.Heading).ToList();
        Assert.Equal(CountryPanelViewModel.StepHeading, headings[^1]);
        Assert.Equal(CountryPanelViewModel.GettingHelpHeading, headings[^3]);
        Assert.Equal("Your one small step", CountryPanelViewModel.StepHeading);

        // The Help focus opens the panel at the step section.
        panel.Show("SDN", section: PanelFocus.Help);
        Assert.Equal(CountryPanelViewModel.StepHeading, panel.Sections[panel.FocusedSectionIndex].Heading);
    }

    [Fact]
    public void FooterRows()
    {
        var t = Tables(charities: [Charity("Alpha")], funds: [Fund("sdn", "Sudan Humanitarian Fund", ["SDN"]), Cerf()]);
        // Task 9: the footer links the "why-these" explainer when the user sees it (here approved; with no explainer, no link).
        var whyThese = CuratedTables.Empty with { Explainers = [ExplainerTests.Row("why-these")] };
        var panel = Panel(t, WithConflict("SDN"), curated: whyThese);
        panel.Show("SDN");
        var s = StepSection(panel);
        Assert.Equal("why-these", s.ExplainerId);
        Assert.Equal("Why these?", s.ExplainerLinkText);
        Assert.Contains(s.Rows, r => r.Label == "Why these?");
        // The newest review date of the rows shown: the fund and the contacts were approved on 24 Nov 2026, the charity reviewed on 5 Nov.
        Assert.Contains(("List last reviewed", "24 Nov 2026"), s.Rows);
        Assert.StartsWith("Source: ", s.SourceLine);
        Assert.Contains("Publication 78", s.SourceLine);

        // Nothing but the built-in CERF line: no review date to give, and it says so.
        var bare = Panel(HelpTables.Empty, WithConflict("SDN"), curated: whyThese);
        bare.Show("SDN");
        Assert.Contains(("List last reviewed", "Not reviewed yet"), StepSection(bare).Rows);
        Assert.Equal("why-these", StepSection(bare).ExplainerId);
        var noExplainer = Panel(HelpTables.Empty, WithConflict("SDN"));
        noExplainer.Show("SDN");
        Assert.Null(StepSection(noExplainer).ExplainerId);

        // Other sections have no entries, and here no explainer either (only "why-these" exists in these tables).
        Assert.All(panel.Sections.Where(x => x.Heading != CountryPanelViewModel.StepHeading), x => { Assert.Null(x.Entries); Assert.Null(x.ExplainerId); });
    }

    [Fact]
    public void HelpTablesLoadedFromTheShippedFilesShowTheFundAndNoCharities()
    {
        // The shipped state: every contact and fund was approved on 2026-11-25 and the charities are still drafts, so a normal run
        // shows the approved fund and contacts and says no charity is listed yet (P4-8, P4-13).
        var real = HelpTables.Load(BoundaryData.DataDir, "US");
        var entries = Step(real, "SDN", "Sudan");
        Assert.Contains(entries, e => e.Title.StartsWith("UN pooled fund for Sudan", StringComparison.Ordinal));
        Assert.Contains(entries, e => e.Lines.Contains("No vetted charities listed yet"));
        Assert.Contains(entries, e => e.Group == "Contact");
    }
}
