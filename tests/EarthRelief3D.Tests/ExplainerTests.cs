using System.IO;
using System.Text.RegularExpressions;
using EarthRelief3D.App.Curated;
using EarthRelief3D.App.ViewModels;
using EarthRelief3D.Core.Curated;
using EarthRelief3D.Core.Feeds;
using EarthRelief3D.Core.Help;
using EarthRelief3D.Core.Model;
using EarthRelief3D.Core.Store;

namespace EarthRelief3D.Tests;

/// <summary>
/// The explainers (Plan 4 Task 9; spec §1 "Teaching", §4 <c>Explainer</c>, §9.3, §11; P4-13): one per crisis kind plus
/// "access" and "why these", each short, plain, cited from the authority's own site, a draft until the owner approves it,
/// and opened from the panel's "What is this, and why does it happen?" link (or the step section's "Why these?").
/// </summary>
public class ExplainerTests
{
    private static readonly DateTime Now = AccessPanelTests.Now;
    private static readonly DateOnly Written = new(2026, 11, 24);
    private static readonly CountryResolver Countries = CountryResolver.LoadDefault();

    private static string CuratedDir => Path.Combine(AppContext.BaseDirectory, "data", "curated");

    private static CuratedTables Real() => CuratedTables.Load(CuratedDir);

    /// <summary>The authority sites each explainer may cite (the authority's own pages, never news or a third party's summary).</summary>
    private static readonly Dictionary<string, string[]> Authorities = new()
    {
        ["conflict"] = ["icrc.org", "uu.se"],
        ["atrocities"] = ["un.org", "ohchr.org", "icj-cij.org"],
        ["camps"] = ["ohchr.org", "un.org"],
        ["food"] = ["ipcinfo.org", "fews.net", "fao.org", "wfp.org"],   // WFP: an IPC partner agency (final fix wave, F3)
        ["outbreaks"] = ["who.int"],
        ["displacement"] = ["unhcr.org"],
        ["access"] = ["acaps.org"],
        ["why-these"] = ["irs.gov", "ftc.gov", "charitynavigator.org", "give.org"],
    };

    internal static Explainer Row(string id = "food", string status = "approved", IReadOnlyList<string>? paragraphs = null,
        IReadOnlyList<DocumentCitation>? sources = null) =>
        new(id, "What is a food crisis?", paragraphs ?? ["A short plain paragraph.", "Another short one."],
            sources ?? [new DocumentCitation("Page one", "Publisher", new Uri("https://example.org/one"), Written),
                        new DocumentCitation("Page two", "Publisher", new Uri("https://example.org/two"), Written)],
            new Review(status, status == Review.Approved ? Written : null), Written)
        { Verification = "Opened both pages on 2026-11-24." };

    private static CuratedTables With(params Explainer[] explainers) => CuratedTablesTests.Tables() with { Explainers = explainers };

    [Fact]
    public void AllEightIds()
    {
        var t = Real();
        Assert.Equal(["conflict", "atrocities", "camps", "food", "outbreaks", "displacement", "access", "why-these"], Explainer.Ids);
        Assert.Equal(Explainer.Ids.OrderBy(i => i, StringComparer.Ordinal), t.Explainers.Select(e => e.Id).OrderBy(i => i, StringComparer.Ordinal));
        // Drafted by Claude on 2026-11-24 (P4-13): every one is a draft until the owner approves it.
        Assert.All(t.Explainers, e => Assert.Equal(new Review(Review.Draft, null), e.Review));
        Assert.All(t.Explainers, e => Assert.Equal(Written, e.ReviewedOn));
        // An unknown id and a repeated one are refused.
        Assert.Contains(CuratedTables.Validate(With(Row("weather"))), p => p.Contains("weather"));
        Assert.Contains(CuratedTables.Validate(With(Row(), Row())), p => p.Contains("used twice"));
    }

    [Fact]
    public void EveryExplainerCited()
    {
        var t = Real();
        Assert.Empty(CuratedTables.Validate(t));
        foreach (var e in t.Explainers)
        {
            Assert.True(e.Sources.Count >= 2, $"{e.Id}: {e.Sources.Count} source(s); at least 2 are needed");
            foreach (var s in e.Sources)
            {
                Assert.False(string.IsNullOrWhiteSpace(s.Title), $"{e.Id}: a source has no title");
                Assert.False(string.IsNullOrWhiteSpace(s.Publisher), $"{e.Id}: {s.Title} has no publisher");
                Assert.True(s.Url.IsAbsoluteUri && s.Url.Scheme == Uri.UriSchemeHttps, $"{e.Id}: {s.Url} is not an absolute https URL");
                Assert.True(s.Date > new DateOnly(1900, 1, 1) && s.Date <= e.ReviewedOn, $"{e.Id}: {s.Title} dated {s.Date}");
                // From the authority's own site.
                var host = s.Url.Host.ToLowerInvariant();
                Assert.True(Authorities[e.Id].Any(a => host == a || host.EndsWith("." + a, StringComparison.Ordinal)),
                    $"{e.Id}: {host} is not one of the authority sites ({string.Join(", ", Authorities[e.Id])})");
            }
            Assert.Equal(e.Sources.Count, e.Sources.Select(s => s.Url).Distinct().Count());
            // What was opened, and when, for the owner's review.
            Assert.False(string.IsNullOrWhiteSpace(e.Verification), $"{e.Id}: no verification");
            Assert.Contains("2026-11-24", e.Verification);
        }

        // A missing or insecure citation, or a single source, fails validation.
        var one = Row(sources: [new DocumentCitation("Only page", "Publisher", new Uri("https://example.org/one"), Written)]);
        Assert.Contains(CuratedTables.Validate(With(one)), p => p.Contains("food") && p.Contains("2 sources"));
        var http = Row(sources: [new DocumentCitation("Page", "Publisher", new Uri("http://example.org/one"), Written),
                                 new DocumentCitation("", "Publisher", new Uri("https://example.org/two"), default)]);
        var problems = CuratedTables.Validate(With(http));
        Assert.Contains(problems, p => p.Contains("food") && p.Contains("https"));
        Assert.Contains(problems, p => p.Contains("food") && p.Contains("title"));
        Assert.Contains(problems, p => p.Contains("food") && p.Contains("date"));
        Assert.Contains(CuratedTables.Validate(With(Row() with { Verification = null })), p => p.Contains("verification"));
        Assert.Contains(CuratedTables.Validate(With(Row() with { Review = new Review("ok", null) })), p => p.Contains("review"));
    }

    [Fact]
    public void PlainAndShort()
    {
        foreach (var e in Real().Explainers)
        {
            var text = string.Join(" ", e.Paragraphs);
            Assert.True(e.WordCount <= Explainer.MaxWords, $"{e.Id}: {e.WordCount} words (at most {Explainer.MaxWords})");
            Assert.True(e.WordCount >= 120, $"{e.Id}: only {e.WordCount} words");
            Assert.All(Explainer.Quotations(text), q => Assert.True(Explainer.Words(q) <= Explainer.MaxQuoteWords, $"{e.Id}: quotation too long: {q}"));
            // Short sentences: none longer than 30 words, and 17 or fewer on average.
            var sentences = Regex.Split(text, @"(?<=[.?!])\s+").Where(s => s.Length > 0).ToList();
            Assert.All(sentences, s => Assert.True(Explainer.Words(s) <= 30, $"{e.Id}: a long sentence ({Explainer.Words(s)} words): {s}"));
            Assert.True(sentences.Average(Explainer.Words) <= 17, $"{e.Id}: sentences average {sentences.Average(Explainer.Words):0.0} words");
            // No figures that go stale ("currently 41 million"): the panel's own sourced numbers say how many.
            Assert.DoesNotMatch(@"(?i)\b(million|billion|thousand)\b", text);
            Assert.DoesNotMatch(@"(?i)\b(currently|today|this year|right now)\b", text);
            // Nothing graphic, no alarm: words the app never uses in teaching text.
            Assert.DoesNotMatch(@"(?i)\b(horrific|gruesome|graphic|bloody|slaughter|massacred|butcher\w*)\b", text);
            Assert.False(string.IsNullOrWhiteSpace(e.Title));
        }

        // The limits are enforced on load: 301 words, or a quotation of 16 words, are refused.
        var tooLong = Row(paragraphs: [string.Join(" ", Enumerable.Repeat("word", 301)) + "."]);
        Assert.Contains(CuratedTables.Validate(With(tooLong)), p => p.Contains("food") && p.Contains("words"));
        var quote = Row(paragraphs: ["It said “" + string.Join(" ", Enumerable.Repeat("word", 16)) + "” in full."]);
        Assert.Contains(CuratedTables.Validate(With(quote)), p => p.Contains("food") && p.Contains("quotation"));
        Assert.Equal(3, Explainer.Words("Three short words."));
        Assert.Equal(["a quote"], Explainer.Quotations("Say \"a quote\" here."));
    }

    // ---------------------------------------------------------------- the panel's links

    private static CountryPanelViewModel Panel(CrisisStore store, CuratedStore curated, HelpTables? help = null) =>
        new(store, iso => Countries.ByIso3(iso)?.Name, nowUtc: () => Now, indicators: new IndicatorStore(), curated: curated, help: help ?? HelpTables.Empty);

    private static CrisisStore Sudan()
    {
        var store = new CrisisStore();
        store.Load(new CrisisSnapshot([TestData.Conflict(), TestData.Food() with { CountryIso3 = "SDN" },
            TestData.Outbreak() with { CountryIso3 = "SDN" }], [TestData.Disp() with { CountryIso3 = "SDN" }], []));
        return store;
    }

    [Fact]
    public void SectionLinksItsExplainer()
    {
        var panel = Panel(Sudan(), new CuratedStore(Real(), showDrafts: true));
        panel.Show("SDN");
        string? Id(string heading) => Assert.Single(panel.Sections, s => s.Heading == heading).ExplainerId;
        Assert.Equal("food", Id(CountryPanelViewModel.FoodHeading));
        Assert.Equal("conflict", Id(CountryPanelViewModel.ConflictHeading));
        Assert.Equal("outbreaks", Id(CountryPanelViewModel.OutbreakHeading));
        Assert.Equal("displacement", Id(CountryPanelViewModel.DisplacementHeading));
        Assert.Equal("access", Id(CountryPanelViewModel.GettingHelpHeading));
        Assert.Equal("why-these", Id(CountryPanelViewModel.StepHeading));
        // Sudan's curated designations (approved) are atrocity sections.
        Assert.Contains(panel.Sections, s => s.Heading.StartsWith("Genocide and mass atrocities", StringComparison.Ordinal));
        Assert.All(panel.Sections.Where(s => s.Heading.StartsWith("Genocide and mass atrocities", StringComparison.Ordinal)
                                             || s.Heading == CountryPanelViewModel.NotEstablishedHeading),
            s => Assert.Equal("atrocities", s.ExplainerId));
        // The needs and funding sections have no explainer.
        Assert.All(panel.Sections.Where(s => s.Heading.StartsWith("Humanitarian appeals", StringComparison.Ordinal)
                                             || s.Heading.StartsWith("People in need", StringComparison.Ordinal)
                                             || s.Heading is CountryPanelViewModel.BasicNeedsHeading or CountryPanelViewModel.ResourcesHeading),
            s => Assert.Null(s.ExplainerId));
        // The link's words: the step section's footer says "Why these?", every other "What is this, and why does it happen?".
        Assert.Equal("Why these?", Assert.Single(panel.Sections, s => s.Heading == CountryPanelViewModel.StepHeading).ExplainerLinkText);
        Assert.Equal("What is this, and why does it happen?", Assert.Single(panel.Sections, s => s.Heading == CountryPanelViewModel.FoodHeading).ExplainerLinkText);

        // A stale IPC analysis is still a food section, and links the food explainer; North Korea's camps link the camps one.
        var past = new CrisisStore();
        past.Load(new CrisisSnapshot([TestData.Food() with { CountryIso3 = "SDN", PeriodFrom = new DateOnly(2024, 1, 1), PeriodTo = new DateOnly(2024, 6, 30), IsStale = true }], [], []));
        var pastPanel = Panel(past, new CuratedStore(Real(), showDrafts: true));
        pastPanel.Show("SDN");
        Assert.Equal("food", Assert.Single(pastPanel.Sections, s => s.Heading == CountryPanelViewModel.PastFoodHeading).ExplainerId);
        var prk = Panel(new CrisisStore(), new CuratedStore(Real(), showDrafts: true));
        prk.Show("PRK");
        Assert.All(prk.Sections.Where(s => s.Heading.StartsWith("Prison camps", StringComparison.Ordinal)), s => Assert.Equal("camps", s.ExplainerId));
        Assert.Contains(prk.Sections, s => s.Heading == CountryPanelViewModel.CampsHeading);
    }

    [Fact]
    public void DraftHasNoLink()
    {
        // The shipped explainers are drafts: a normal run shows no link anywhere, and the card does not open.
        var panel = Panel(Sudan(), new CuratedStore(Real(), showDrafts: false));
        panel.Show("SDN");
        Assert.All(panel.Sections, s => Assert.Null(s.ExplainerId));
        Assert.False(panel.Explainer.Open("food"));
        Assert.False(panel.Explainer.IsOpen);

        // An approved explainer shows its link without --show-drafts; a draft beside it still shows none.
        var approved = new CuratedStore(With(Row("food"), Row("conflict", status: Review.Draft)), showDrafts: false);
        var mixed = Panel(Sudan(), approved);
        mixed.Show("SDN");
        Assert.Equal("food", Assert.Single(mixed.Sections, s => s.Heading == CountryPanelViewModel.FoodHeading).ExplainerId);
        Assert.Null(Assert.Single(mixed.Sections, s => s.Heading == CountryPanelViewModel.ConflictHeading).ExplainerId);

        // Turning drafts on (a scene's showDrafts) shows the draft's link at once.
        approved.SetShowDrafts(true);
        Assert.Equal("conflict", Assert.Single(mixed.Sections, s => s.Heading == CountryPanelViewModel.ConflictHeading).ExplainerId);
    }

    [Fact]
    public void FoodAndAccessWordingFollowTheSources()
    {
        // Final review F3: the IPC Famine thresholds as WFP words them (households, children, each "at least"); no claim the cited
        // pages do not make. And the access text uses ACAPS's level names, as the panel and legend do.
        var t = Real();
        var food = string.Join(" ", t.Explainers.Single(e => e.Id == "food").Paragraphs);
        Assert.Contains("At least one household in five faces an extreme lack of food.", food);
        Assert.Contains("At least three children in ten are acutely malnourished.", food);
        Assert.Contains("at least two people in every 10,000 die each day.", food);
        Assert.DoesNotContain("make each analysis together", food);
        Assert.DoesNotContain("About three children", food);
        Assert.Contains(t.Explainers.Single(e => e.Id == "food").Sources, s => s.Url.Host == "www.wfp.org");
        var access = string.Join(" ", t.Explainers.Single(e => e.Id == "access").Paragraphs);
        Assert.Contains("No constraints", access);
        Assert.DoesNotContain("no significant constraints", access, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AnotherCountryClosesTheCard()
    {
        // Final review: the card explains the country the panel shows; picking another country closes it, while a rebuild or a
        // new focus on the same country keeps it.
        var panel = Panel(Sudan(), new CuratedStore(Real(), showDrafts: true));
        panel.Show("SDN");
        panel.OpenExplainerCommand.Execute("food");
        panel.RefreshOpen();
        panel.Show("SDN", EarthRelief3D.App.Presentation.CrisisLayer.Food);
        Assert.True(panel.Explainer.IsOpen);
        panel.Show("ISL");
        Assert.False(panel.Explainer.IsOpen);
        Assert.Equal("", panel.Explainer.Id);
    }

    [Fact]
    public void OpensAndCloses()
    {
        var curated = new CuratedStore(Real(), showDrafts: true);
        var panel = Panel(Sudan(), curated);
        panel.Show("SDN");
        var card = panel.Explainer;
        Assert.False(card.IsOpen);

        // The panel's link opens the card with the explainer's title, paragraphs, sources and review line.
        panel.OpenExplainerCommand.Execute("food");
        Assert.True(card.IsOpen);
        var food = Real().Explainers.Single(e => e.Id == "food");
        Assert.Equal(food.Title, card.Title);
        Assert.Equal(food.Paragraphs, card.Paragraphs);
        Assert.Equal(food.Sources.Count, card.Sources.Count);
        Assert.All(card.Sources, l => Assert.Equal(Uri.UriSchemeHttps, l.Url.Scheme));
        Assert.Equal(ExplainerViewModel.SourceText(food.Sources[0]), card.Sources[0].Text);
        Assert.Contains(food.Sources[0].Publisher, card.Sources[0].Text);
        Assert.Contains("24 Nov 2026", card.Sources[0].Text);
        Assert.Equal("Draft: not yet reviewed", card.ReviewedText);

        // Another link replaces the content; Close closes it; closing the panel closes it too.
        Assert.True(card.Open("why-these"));
        Assert.Equal(Real().Explainers.Single(e => e.Id == "why-these").Title, card.Title);
        card.CloseCommand.Execute(null);
        Assert.False(card.IsOpen);
        Assert.Empty(card.Paragraphs);
        card.Open("conflict");
        panel.CloseCommand.Execute(null);
        Assert.False(card.IsOpen);

        // An unknown id opens nothing; an approved explainer says when it was reviewed.
        Assert.False(card.Open("weather"));
        Assert.False(card.IsOpen);
        var approved = new ExplainerViewModel(new CuratedStore(With(Row("food")), showDrafts: false));
        Assert.True(approved.Open("food"));
        Assert.Equal("Reviewed 24 Nov 2026", approved.ReviewedText);

        // Drafts turned off while a draft is open: the card closes rather than show an unreviewed text.
        Assert.True(card.Open("food"));
        curated.SetShowDrafts(false);
        Assert.False(card.IsOpen);
    }

    [Fact]
    public void WhyTheseExplainsTheThreeChecks()
    {
        var e = Real().Explainers.Single(x => x.Id == "why-these");
        var text = string.Join(" ", e.Paragraphs);
        // 1. Legally able to take tax-deductible gifts.
        Assert.Contains("Publication 78", text);
        Assert.Contains("tax-deductible", text);
        // 2. The two evaluators and what qualifies, never a charity's own rating.
        Assert.Contains("Charity Navigator", text);
        Assert.Contains("BBB Wise Giving Alliance", text);
        Assert.Contains("3 stars or more", text);
        Assert.Contains("all 20", text);
        Assert.DoesNotContain("★", text);
        // 3. Evidence of work in the country.
        Assert.Contains("evidence", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("working in", text);
        // The 12-month review rule, and the order: the UN fund first, then A to Z, never ranked.
        Assert.Contains("12 months", text);
        Assert.Contains("alphabetical", text);
        // §9.3: a programme share alone can mislead; hard-to-reach places cost more, and that is the real work.
        Assert.Contains("hard-to-reach", text);
        Assert.Contains("real work", text);
        Assert.Contains(e.Sources, s => s.Url.Host.EndsWith("irs.gov", StringComparison.Ordinal));
    }

    [Fact]
    public void ReviewDocumentListsTheExplainers()
    {
        var t = Real();
        var text = CuratedReview.Render(t);
        Assert.Contains("## Explainers", text);
        foreach (var e in t.Explainers)
        {
            Assert.Contains($"`{e.Id}`", text);
            foreach (var p in e.Paragraphs) Assert.Contains(p, text);
            foreach (var s in e.Sources) Assert.Contains(s.Url.ToString(), text);
            Assert.Contains(e.Verification!, text);
            Assert.Contains($"{e.WordCount} words", text);
        }
    }
}
