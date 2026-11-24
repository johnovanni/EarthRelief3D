using System.Text.Json;
using System.Text.Json.Serialization;
using AstroCore.Earth;

namespace EarthRelief3D.Core.Curated;

/// <summary>
/// A government whose formal determinations may be offered in Settings (C15): only when <see cref="CuratedComplete"/> is
/// true, meaning every formal determination it has made (within <see cref="Scope"/>) is in the table, each cited.
/// </summary>
/// <param name="CheckedOn">When the completeness search was last run.</param>
public sealed record Government(string Iso3, string Name, bool CuratedComplete, DateOnly CheckedOn)
{
    /// <summary>What "complete" covers, in a few words ("Secretary of State determinations published on state.gov and its archives"); null for none.</summary>
    public string? Scope { get; init; }
}

/// <summary>A link in a <see cref="Candidate"/>: <paramref name="Kind"/> is "authority" (the authority's own page) or "lead" (news, a study: never a citation).</summary>
public sealed record CandidateLink(string Title, Uri Url, string Kind);

/// <summary>
/// A designation or camp that was researched but not entered: why, what the owner could do, and the links. Shown only in
/// docs/curated-review.md (section "Awaiting confirmation / not entered").
/// </summary>
/// <param name="What">The candidate in a few words.</param>
/// <param name="Status">"awaiting confirmation" (the facts exist but could not be read on the authority's site) or "not entered".</param>
public sealed record Candidate(string Id, string What, string Status, string Reason, string? OwnerAction, IReadOnlyList<CandidateLink> Links);

/// <summary>What the user sees: drafts only with <c>--show-drafts</c> (P3-4), determinations only of the chosen governments (C15).</summary>
public sealed record CuratedFilter(bool ShowDrafts, IReadOnlySet<string> Governments);

/// <summary>
/// The curated tables in <c>data/curated</c> (spec §10.2): atrocity designations, documented camp sites and the governments
/// whose determinations are curated. <see cref="Load"/> validates them: every row cited (a title, an absolute https URL and
/// a date for every document, continuing statement, camp position and camp report), the C14 rules, and the review status.
/// </summary>
public sealed record CuratedTables(IReadOnlyList<Designation> Designations, IReadOnlyList<CampSite> Camps, IReadOnlyList<Government> Governments)
{
    public const string DesignationsFile = "designations.json";
    public const string CampsFile = "camps.json";
    public const string GovernmentsFile = "governments.json";
    /// <summary>Candidates researched but not entered (for the owner's review document only; the app never shows them).</summary>
    public const string NotEnteredFile = "not-entered.json";

    public static CuratedTables Empty { get; } = new([], [], []);

    /// <summary>The candidates awaiting confirmation or left out, with the reason and the links (docs/curated-review.md).</summary>
    public IReadOnlyList<Candidate> NotEntered { get; init; } = [];

    /// <summary>The governments the Settings flyout offers (C15): only those curated completely.</summary>
    public IReadOnlyList<Government> OfferedGovernments => [.. Governments.Where(g => g.CuratedComplete)];

    /// <summary>The designations the user sees under <paramref name="f"/>, in file order.</summary>
    public IReadOnlyList<Designation> VisibleDesignations(CuratedFilter f)
    {
        var offered = OfferedGovernments.Select(g => g.Iso3).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return [.. Designations.Where(d => (d.Review.IsApproved || f.ShowDrafts)
            && (d.Type != AuthorityType.GovernmentDetermination
                || d.GovernmentIso3 is { } g && offered.Contains(g) && f.Governments.Contains(g)))];
    }

    /// <summary>The camp sites the user sees under <paramref name="f"/>, open or reported closed (the panel lists both).</summary>
    public IReadOnlyList<CampSite> VisibleCamps(CuratedFilter f) => [.. Camps.Where(c => c.Review.IsApproved || f.ShowDrafts)];

    // ---------------------------------------------------------------- loading

    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>
    /// Reads the three files from <paramref name="dir"/> and validates them. A missing file or any problem throws
    /// <see cref="InvalidDataException"/> listing every problem: the tests run this on the real files, so a bad row fails the build.
    /// </summary>
    public static CuratedTables Load(string dir)
    {
        var designations = Read<List<Designation>>(Path.Combine(dir, DesignationsFile));
        var camps = Read<List<CampDto>>(Path.Combine(dir, CampsFile)).Select(c => c.ToCamp()).ToList();
        var governments = Read<List<GovernmentDto>>(Path.Combine(dir, GovernmentsFile)).Select(g => g.ToGovernment()).ToList();
        var notEnteredPath = Path.Combine(dir, NotEnteredFile);
        var tables = new CuratedTables(designations, camps, governments)
        {
            NotEntered = File.Exists(notEnteredPath) ? Read<List<Candidate>>(notEnteredPath) : [],
        };
        var problems = Validate(tables);
        if (problems.Count > 0)
            throw new InvalidDataException($"{dir}: {problems.Count} problem(s) in the curated tables:{Environment.NewLine}" + string.Join(Environment.NewLine, problems));
        return tables;
    }

    private static T Read<T>(string path)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(File.ReadAllText(path), Json) ?? throw new InvalidDataException($"{path} is empty");
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException($"{path}: {ex.Message}", ex);
        }
    }

    private sealed record GeoDto(double Lat, double Lon);

    private sealed record CampDto(string Id, string Iso3, string Name, string? LocalName, GeoDto? Position, DocumentCitation? PositionSource,
        string Description, List<DocumentCitation>? DocumentedBy, Review? Review, DateOnly CheckedOn, string? Verification,
        CampStatement? ReportedClosed, List<CampStatement>? StatusNotes, string? OwnerQuestion)
    {
        public CampSite ToCamp() => new(Id, Iso3, Name, LocalName,
            Position is null ? new GeoPosition(double.NaN, double.NaN) : new GeoPosition(Position.Lat, Position.Lon),
            PositionSource!, Description, DocumentedBy ?? [], Review!)
        {
            CheckedOn = CheckedOn,
            Verification = Verification,
            ReportedClosed = ReportedClosed,
            StatusNotes = StatusNotes ?? [],
            OwnerQuestion = OwnerQuestion,
        };
    }

    private sealed record GovernmentDto(string Iso3, string Name, bool CuratedComplete, DateOnly CheckedOn, string? Scope)
    {
        public Government ToGovernment() => new(Iso3, Name, CuratedComplete, CheckedOn) { Scope = Scope };
    }

    // ---------------------------------------------------------------- validation

    /// <summary>Every problem in the tables, one line each ("&lt;id&gt;: ..."); empty when they are sound.</summary>
    public static IReadOnlyList<string> Validate(CuratedTables t)
    {
        ArgumentNullException.ThrowIfNull(t);
        var problems = new List<string>();
        var ids = new HashSet<string>(StringComparer.Ordinal);
        var governments = new HashSet<string>(StringComparer.Ordinal);

        foreach (var g in t.Governments)
        {
            var who = $"government {g?.Iso3}";
            if (g is null) { problems.Add("governments: an empty entry"); continue; }
            if (!IsIso3(g.Iso3)) problems.Add($"{who}: iso3 must be three capital letters");
            if (string.IsNullOrWhiteSpace(g.Name)) problems.Add($"{who}: no name");
            if (!IsRealDate(g.CheckedOn)) problems.Add($"{who}: no checkedOn date");
            if (g.Iso3 is not null && !governments.Add(g.Iso3)) problems.Add($"{who}: listed twice");
        }

        foreach (var d in t.Designations)
        {
            if (d is null) { problems.Add("designations: an empty entry"); continue; }
            var id = string.IsNullOrWhiteSpace(d.Id) ? "(designation with no id)" : d.Id;
            if (!ids.Add(id)) problems.Add($"{id}: id used twice");
            if (!IsIso3(d.Iso3)) problems.Add($"{id}: iso3 must be three capital letters");
            Required(problems, id, d.Situation, "situation");
            Required(problems, id, d.Authority, "authority");
            Required(problems, id, d.Term, "term");
            Required(problems, id, d.Group, "group");
            Cited(problems, id, "document", d.DocumentTitle, d.DocumentUrl, d.Date);
            if (!IsRealDate(d.CheckedOn)) problems.Add($"{id}: no checkedOn date");
            ReviewOk(problems, id, d.Review);
            if (d.Continuing is { } c)
            {
                Cited(problems, id, "continuing statement", c.Title, c.Url, c.Date);
                Required(problems, id, c.Statement, "continuing statement text");
            }
            if (d.Type == AuthorityType.GovernmentDetermination)
            {
                if (string.IsNullOrWhiteSpace(d.GovernmentIso3)) problems.Add($"{id}: a government determination must name its government (governmentIso3)");
                else if (!governments.Contains(d.GovernmentIso3)) problems.Add($"{id}: government {d.GovernmentIso3} is not in {GovernmentsFile}");
            }
            else if (d.GovernmentIso3 is not null)
                problems.Add($"{id}: only a government determination names a government (governmentIso3)");
            if (d.Outcome == FindingOutcome.NotEstablished)
            {
                if (d.Type != AuthorityType.CourtJudgment) problems.Add($"{id}: only a court judgment can be \"not established\"");
                if (d.Continuing is not null) problems.Add($"{id}: a \"not established\" judgment can have no continuing statement");
            }
            // "Not established" is said once, by the outcome: a term that already says "not" would read twice or be misread.
            if (d.Term is { } term && (term.StartsWith("not ", StringComparison.OrdinalIgnoreCase)
                                       || term.Contains(" not ", StringComparison.OrdinalIgnoreCase)))
                problems.Add($"{id}: the term \"{term}\" says \"not\": give the crime as the term and set outcome NotEstablished");
        }

        foreach (var c in t.Camps)
        {
            if (c is null) { problems.Add("camps: an empty entry"); continue; }
            var id = string.IsNullOrWhiteSpace(c.Id) ? "(camp with no id)" : c.Id;
            if (!ids.Add(id)) problems.Add($"{id}: id used twice");
            if (!IsIso3(c.Iso3)) problems.Add($"{id}: iso3 must be three capital letters");
            Required(problems, id, c.Name, "name");
            Required(problems, id, c.Description, "description");
            if (!(Math.Abs(c.Position.LatitudeDeg) <= 90) || !(Math.Abs(c.Position.LongitudeDeg) <= 180)
                || c.Position is { LatitudeDeg: 0, LongitudeDeg: 0 })
                problems.Add($"{id}: no valid position (a published coordinate is required)");
            if (c.PositionSource is null) problems.Add($"{id}: no positionSource (the report that publishes the coordinate)");
            else Cited(problems, id, "position source", c.PositionSource.Title, c.PositionSource.Url, c.PositionSource.Date, c.PositionSource.Publisher);
            if (c.DocumentedBy is not { Count: > 0 }) problems.Add($"{id}: documentedBy lists no reports");
            else
            {
                foreach (var r in c.DocumentedBy) Cited(problems, id, "report", r?.Title, r?.Url, r?.Date ?? default, r?.Publisher);
                for (int i = 1; i < c.DocumentedBy.Count; i++)
                    if (c.DocumentedBy[i]?.Date > c.DocumentedBy[i - 1]?.Date) { problems.Add($"{id}: documentedBy is not newest first"); break; }
            }
            if (!IsRealDate(c.CheckedOn)) problems.Add($"{id}: no checkedOn date");
            if (c.ReportedClosed is { } closed)
            {
                StatementOk(problems, id, "closure", closed);
                // R14b: a camp is closed only when its newest cited report says so.
                var newest = c.DocumentedBy?.Where(r => r is not null).Select(r => r.Date).DefaultIfEmpty().Max() ?? default;
                if (closed.Source is not null && closed.Source.Date < newest)
                    problems.Add($"{id}: the closure statement ({closed.Source.Date}) is older than the newest cited report ({newest}); "
                                 + "a camp is closed only when its newest report says so (R14b): give it as a status note instead");
            }
            foreach (var note in c.StatusNotes ?? []) StatementOk(problems, id, "status note", note);
            ReviewOk(problems, id, c.Review);
        }

        foreach (var n in t.NotEntered)
        {
            if (n is null) { problems.Add("not-entered: an empty entry"); continue; }
            var id = string.IsNullOrWhiteSpace(n.Id) ? "(candidate with no id)" : n.Id;
            if (!ids.Add(id)) problems.Add($"{id}: id used twice");
            Required(problems, id, n.What, "description (what)");
            Required(problems, id, n.Reason, "reason");
            if (n.Status is not ("awaiting confirmation" or "not entered")) problems.Add($"{id}: status must be \"awaiting confirmation\" or \"not entered\"");
            foreach (var l in n.Links ?? [])
            {
                if (l is null || string.IsNullOrWhiteSpace(l.Title) || l.Url is not { IsAbsoluteUri: true } u || u.Scheme != Uri.UriSchemeHttps)
                    problems.Add($"{id}: every link needs a title and an absolute https URL");
                else if (l.Kind is not ("authority" or "lead")) problems.Add($"{id}: link kind must be \"authority\" or \"lead\"");
            }
        }
        return problems;
    }

    private static bool IsIso3(string? s) => s is { Length: 3 } && s.All(char.IsAsciiLetterUpper);

    private static bool IsRealDate(DateOnly d) => d > new DateOnly(1900, 1, 1);

    private static void Required(List<string> problems, string id, string? value, string what)
    {
        if (string.IsNullOrWhiteSpace(value)) problems.Add($"{id}: no {what}");
    }

    private static void Cited(List<string> problems, string id, string what, string? title, Uri? url, DateOnly date, string? publisher = "-")
    {
        if (string.IsNullOrWhiteSpace(title)) problems.Add($"{id}: the {what} has no title");
        if (url is null || !url.IsAbsoluteUri || url.Scheme != Uri.UriSchemeHttps) problems.Add($"{id}: the {what} needs an absolute https URL");
        if (!IsRealDate(date)) problems.Add($"{id}: the {what} has no date");
        if (string.IsNullOrWhiteSpace(publisher)) problems.Add($"{id}: the {what} has no publisher");
    }

    private static void StatementOk(List<string> problems, string id, string what, CampStatement? s)
    {
        if (s is null) { problems.Add($"{id}: an empty {what}"); return; }
        Required(problems, id, s.Statement, what + " statement");
        Required(problems, id, s.ShortSource, what + " short source");
        if (s.Source is null) problems.Add($"{id}: the {what} has no source");
        else Cited(problems, id, what + " source", s.Source.Title, s.Source.Url, s.Source.Date, s.Source.Publisher);
    }

    private static void ReviewOk(List<string> problems, string id, Review? r)
    {
        if (r is null) { problems.Add($"{id}: no review status"); return; }
        if (r.Status == Review.Approved)
        {
            if (r.ApprovedOn is null) problems.Add($"{id}: an approved row needs review.approvedOn");
        }
        else if (r.Status == Review.Draft)
        {
            if (r.ApprovedOn is not null) problems.Add($"{id}: a draft row has review.approvedOn");
        }
        else problems.Add($"{id}: review status \"{r.Status}\" must be \"{Review.Draft}\" or \"{Review.Approved}\"");
    }
}
