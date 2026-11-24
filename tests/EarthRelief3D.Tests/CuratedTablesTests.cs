using System.IO;
using AstroCore.Earth;
using EarthRelief3D.Core.Curated;

namespace EarthRelief3D.Tests;

/// <summary>
/// The curated tables (spec §10.2, C14, C15, P3-4): every row cited, interim steps never read as verdicts, a determination
/// names its government, only completely curated governments are offered, and a court's "not established" is never a
/// finding. <see cref="EveryRowIsCited"/> runs against the real <c>data/curated</c> files.
/// </summary>
public class CuratedTablesTests
{
    internal static readonly DateTime Now = new(2026, 11, 23, 0, 0, 0, DateTimeKind.Utc);

    internal static readonly Government Usa = new("USA", "United States government", true, new DateOnly(2026, 11, 23));

    internal static Designation Row(string id = "d1", string iso3 = "MMR", AuthorityType type = AuthorityType.UnBodyFinding,
        string term = "genocide", string? gov = null, Continuing? continuing = null, string status = "approved", DateOnly? date = null) =>
        new(id, iso3, "Test situation", "Test authority", type, gov, term, "Test group", date ?? new DateOnly(2020, 1, 1),
            "Test document", new Uri("https://example.org/doc"), continuing,
            new Review(status, status == "approved" ? new DateOnly(2026, 11, 23) : null)) { ShortAuthority = "Test", CheckedOn = new DateOnly(2026, 11, 23) };

    internal static Continuing Still(DateOnly date) => new("The situation continues.", new Uri("https://example.org/continuing"), date)
    {
        Title = "Test report",
    };

    internal static CampSite Camp(string id = "c1", string status = "approved") => new(id, "PRK", "Test camp", null, new GeoPosition(40, 127),
        new DocumentCitation("Test imagery report", "HRNK", new Uri("https://example.org/camp"), new DateOnly(2024, 1, 1)), "A test camp.",
        [new DocumentCitation("Test imagery report", "HRNK", new Uri("https://example.org/camp"), new DateOnly(2024, 1, 1))],
        new Review(status, status == "approved" ? new DateOnly(2026, 11, 23) : null)) { CheckedOn = new DateOnly(2026, 11, 23) };

    internal static CuratedTables Tables(IReadOnlyList<Designation>? d = null, IReadOnlyList<CampSite>? c = null, IReadOnlyList<Government>? g = null) =>
        new(d ?? [], c ?? [], g ?? [Usa]);

    private static string CuratedDir => Path.Combine(AppContext.BaseDirectory, "data", "curated");

    [Fact]
    public void EveryRowIsCited()
    {
        var t = CuratedTables.Load(CuratedDir);   // Load validates; it throws on any problem
        Assert.NotEmpty(t.Designations);
        Assert.NotEmpty(t.Camps);
        Assert.Empty(CuratedTables.Validate(t));

        static void Cited(string what, string title, Uri url, DateOnly date)
        {
            Assert.False(string.IsNullOrWhiteSpace(title), $"{what}: no title");
            Assert.True(url.IsAbsoluteUri && url.Scheme == Uri.UriSchemeHttps, $"{what}: {url} is not an absolute https URL");
            Assert.True(date > new DateOnly(1900, 1, 1) && date <= DateOnly.FromDateTime(Now), $"{what}: date {date} is not a real past date");
        }
        foreach (var d in t.Designations)
        {
            Cited(d.Id, d.DocumentTitle, d.DocumentUrl, d.Date);
            if (d.Continuing is { } c) Cited(d.Id + " continuing", c.Title, c.Url, c.Date);
            Assert.False(string.IsNullOrWhiteSpace(d.Authority), d.Id + ": no authority");
            Assert.False(string.IsNullOrWhiteSpace(d.Term), d.Id + ": no term");
        }
        foreach (var c in t.Camps)
        {
            Cited(c.Id + " position", c.PositionSource.Title, c.PositionSource.Url, c.PositionSource.Date);
            Assert.NotEmpty(c.DocumentedBy);
            foreach (var r in c.DocumentedBy) Cited(c.Id + " report", r.Title, r.Url, r.Date);
            // Newest first.
            Assert.Equal(c.DocumentedBy.OrderByDescending(r => r.Date).Select(r => r.Date), c.DocumentedBy.Select(r => r.Date));
        }
        // Drafted by Claude: every row waits for the owner (P3-4).
        Assert.All(t.Designations, d => Assert.Equal(Review.Draft, d.Review.Status));
        Assert.All(t.Camps, c => Assert.Equal(Review.Draft, c.Review.Status));
    }

    [Fact]
    public void ValidationRejectsAnUncitedRow()
    {
        var noTitle = Row() with { DocumentTitle = " " };
        var http = Row("d2") with { DocumentUrl = new Uri("http://example.org/doc") };
        var noDate = Row("d3") with { Date = default };
        var badContinuing = Row("d4", continuing: Still(new DateOnly(2026, 6, 1)) with { Title = "" });
        var problems = CuratedTables.Validate(Tables([noTitle, http, noDate, badContinuing]));
        Assert.Contains(problems, p => p.Contains("d1") && p.Contains("title"));
        Assert.Contains(problems, p => p.Contains("d2") && p.Contains("https"));
        Assert.Contains(problems, p => p.Contains("d3") && p.Contains("date"));
        Assert.Contains(problems, p => p.Contains("d4") && p.Contains("continuing"));
        var campNoReports = Camp() with { DocumentedBy = [] };
        Assert.Contains(CuratedTables.Validate(Tables(c: [campNoReports])), p => p.Contains("c1"));
        Assert.Contains(CuratedTables.Validate(Tables([Row() with { Review = new Review("ok", null) }])), p => p.Contains("review"));
    }

    [Theory]
    [InlineData(AuthorityType.CourtProvisionalMeasures, "Provisional measures")]
    [InlineData(AuthorityType.ArrestWarrant, "Arrest warrant")]
    [InlineData(AuthorityType.PendingCase, "Pending: no finding yet")]
    public void InterimStepsAreNeverVerdicts(AuthorityType type, string prefix)
    {
        foreach (var term in new[] { "genocide", "crimes against humanity" })
        {
            var label = DesignationText.Label(Row(type: type, term: term), [Usa]);
            Assert.StartsWith(prefix, label, StringComparison.Ordinal);
            Assert.DoesNotContain("judgment", label, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("finding:", label, StringComparison.OrdinalIgnoreCase);
            Assert.False(label.EndsWith(": " + term, StringComparison.Ordinal), label);
            Assert.NotEqual(term, label);
        }
        // The verdict types say what they are.
        Assert.Equal("Court judgment: genocide", DesignationText.Label(Row(type: AuthorityType.CourtJudgment), [Usa]));
        Assert.Equal("UN body finding: genocide", DesignationText.Label(Row(type: AuthorityType.UnBodyFinding), [Usa]));
        // The real rows of these types follow the same rule.
        var t = CuratedTables.Load(CuratedDir);
        foreach (var d in t.Designations.Where(d => d.Type == type))
            Assert.StartsWith(prefix, DesignationText.Label(d, t.Governments), StringComparison.Ordinal);
    }

    [Fact]
    public void GovernmentDeterminationNamesTheGovernment()
    {
        var d = Row(type: AuthorityType.GovernmentDetermination, gov: "USA");
        Assert.Equal("United States government determination: genocide", DesignationText.Label(d, [Usa]));
        Assert.Contains("United States government", DesignationText.Hover(d, "Myanmar", [Usa]));
        // Validation: a determination must name a listed government; other types must not name one.
        Assert.Contains(CuratedTables.Validate(Tables([Row(type: AuthorityType.GovernmentDetermination)])), p => p.Contains("government"));
        Assert.Contains(CuratedTables.Validate(Tables([Row(type: AuthorityType.GovernmentDetermination, gov: "FRA")])), p => p.Contains("FRA"));
        Assert.Contains(CuratedTables.Validate(Tables([Row(type: AuthorityType.CourtJudgment, gov: "USA")])), p => p.Contains("government"));
        // Every real determination names its government in its label.
        var t = CuratedTables.Load(CuratedDir);
        var real = t.Designations.Where(x => x.Type == AuthorityType.GovernmentDetermination).ToList();
        Assert.NotEmpty(real);
        Assert.All(real, x => Assert.StartsWith(t.Governments.Single(g => g.Iso3 == x.GovernmentIso3).Name + " determination: ",
            DesignationText.Label(x, t.Governments), StringComparison.Ordinal));
    }

    [Fact]
    public void OnlyCuratedCompleteGovernmentsAreOffered()
    {
        var fra = new Government("FRA", "French government", false, new DateOnly(2026, 11, 23));
        var t = Tables([Row("us", type: AuthorityType.GovernmentDetermination, gov: "USA"), Row("fr", type: AuthorityType.GovernmentDetermination, gov: "FRA")],
            g: [Usa, fra]);
        Assert.Equal(["USA"], t.OfferedGovernments.Select(g => g.Iso3));
        // Even if a settings file names it, an incomplete government's determinations are not shown.
        var visible = t.VisibleDesignations(new CuratedFilter(ShowDrafts: false, Governments: new HashSet<string> { "USA", "FRA" }));
        Assert.Equal(["us"], visible.Select(d => d.Id));
        // The real list offers the United States, curated completely (P3-5).
        var real = CuratedTables.Load(CuratedDir);
        Assert.Equal(["USA"], real.OfferedGovernments.Select(g => g.Iso3));
    }

    [Fact]
    public void NotEstablishedIsNeverAFinding()
    {
        var d = Row(type: AuthorityType.CourtJudgment) with { Outcome = FindingOutcome.NotEstablished };
        Assert.Equal("Court judgment: genocide not established", DesignationText.Label(d, [Usa]));
        // Even with a continuing statement it is never current (and validation refuses the combination).
        var withContinuing = d with { Continuing = Still(new DateOnly(2026, 10, 1)) };
        Assert.False(withContinuing.IsCurrentAt(Now));
        Assert.Contains(CuratedTables.Validate(Tables([withContinuing])), p => p.Contains("not established"));
        // Only a court judgment can be "not established".
        Assert.Contains(CuratedTables.Validate(Tables([Row(type: AuthorityType.UnBodyFinding) with { Outcome = FindingOutcome.NotEstablished }])),
            p => p.Contains("not established"));
        // A term that already says "not ..." is refused: the outcome field carries it, once.
        Assert.Contains(CuratedTables.Validate(Tables([Row(type: AuthorityType.CourtJudgment, term: "genocide not established")])),
            p => p.Contains("outcome"));
        // The real ICJ Croatia v. Serbia row is a judgment whose outcome is "not established".
        var real = CuratedTables.Load(CuratedDir).Designations.Single(x => x.Id == "icj-croatia-v-serbia-2015");
        Assert.Equal(AuthorityType.CourtJudgment, real.Type);
        Assert.Equal(FindingOutcome.NotEstablished, real.Outcome);
        Assert.EndsWith("not established", DesignationText.Label(real, [Usa]), StringComparison.Ordinal);
    }

    [Fact]
    public void ContinuingCountsForTwelveMonths()
    {
        var recent = Row(continuing: Still(new DateOnly(2026, 6, 18)));
        var old = Row(continuing: Still(new DateOnly(2025, 11, 22)));
        var future = Row(continuing: Still(new DateOnly(2026, 12, 1)));
        Assert.True(recent.IsCurrentAt(Now));
        Assert.False(old.IsCurrentAt(Now));     // a day over 12 months
        Assert.False(future.IsCurrentAt(Now));  // a statement from the future is a data error, not a current one
        Assert.False(Row().IsCurrentAt(Now));
        Assert.True(Row(continuing: Still(new DateOnly(2025, 11, 23))).IsCurrentAt(Now));   // exactly 12 months
    }

    [Fact]
    public void ReviewDocumentIsCurrent()
    {
        // docs/curated-review.md is generated from the tables (one block per row for the owner). Set ER3D_WRITE_REVIEW=1 to rewrite it.
        var t = CuratedTables.Load(CuratedDir);
        var text = CuratedReview.Render(t);
        var path = Path.Combine(Repo.Root(), "docs", "curated-review.md");
        if (Environment.GetEnvironmentVariable("ER3D_WRITE_REVIEW") == "1") File.WriteAllText(path, text);
        Assert.True(File.Exists(path), "docs/curated-review.md is missing: run the tests with ER3D_WRITE_REVIEW=1");
        Assert.Equal(text.ReplaceLineEndings("\n"), File.ReadAllText(path).ReplaceLineEndings("\n"));
        foreach (var d in t.Designations) Assert.Contains(d.Id, text);
        foreach (var c in t.Camps) Assert.Contains(c.Id, text);
        Assert.Contains("approve / correct", text);
    }
}
