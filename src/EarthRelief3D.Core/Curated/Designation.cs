namespace EarthRelief3D.Core.Curated;

/// <summary>
/// What kind of authority made a designation, and what kind of act it is (C14). Interim steps
/// (<see cref="CourtProvisionalMeasures"/>, <see cref="ArrestWarrant"/>, <see cref="PendingCase"/>) are never verdicts: their
/// labels say so (<see cref="DesignationText.Label"/>).
/// </summary>
public enum AuthorityType
{
    /// <summary>A court's judgment on the merits (ICJ) or a criminal trial or appeal judgment (ICTY, ICTR, ECCC, ICC).</summary>
    CourtJudgment,
    /// <summary>A court's order indicating provisional measures: not a finding.</summary>
    CourtProvisionalMeasures,
    /// <summary>An arrest warrant (reasonable grounds to believe): not a verdict.</summary>
    ArrestWarrant,
    /// <summary>A case before a court with no finding yet ("Pending: no finding yet").</summary>
    PendingCase,
    /// <summary>A finding of a UN commission, mission or office.</summary>
    UnBodyFinding,
    /// <summary>A government's formal determination (C15): only governments curated completely are offered.</summary>
    GovernmentDetermination,
}

/// <summary>
/// Whether the authority found the <see cref="Designation.Term"/> established. Only a court judgment can be
/// <see cref="NotEstablished"/> (ICJ, Croatia v. Serbia, 2015: genocide not proven). Such a row is listed in the panel's
/// history and the explainers, never drawn on the map, never a crisis, and can carry no continuing statement.
/// </summary>
public enum FindingOutcome { Established, NotEstablished }

/// <summary>
/// One designation of genocide or another atrocity crime (spec §10.2): who made it (<see cref="Authority"/>, of
/// <see cref="Type"/>), the term used, about which group and situation, when, and the document. Every label names the
/// authority, its kind and the date (C14).
/// </summary>
/// <param name="Id">Stable, unique ("icj-bosnia-v-serbia-2007").</param>
/// <param name="Iso3">The country the situation is in (where the panel lists it and the glyph is drawn).</param>
/// <param name="Situation">The situation in a few words ("Srebrenica, July 1995").</param>
/// <param name="Authority">The authority's full name ("International Court of Justice").</param>
/// <param name="GovernmentIso3">For a <see cref="AuthorityType.GovernmentDetermination"/>, the government (in governments.json); otherwise null.</param>
/// <param name="Term">The crime as the authority named it ("genocide", "crimes against humanity"); for an interim step, the crime alleged or charged.</param>
/// <param name="Group">The people the crime was committed against, in the authority's words.</param>
/// <param name="Date">The document's date.</param>
/// <param name="Continuing">A statement from the last 12 months that the situation continues; null for none (then it is history, C14).</param>
public sealed record Designation(string Id, string Iso3, string Situation, string Authority, AuthorityType Type, string? GovernmentIso3,
    string Term, string Group, DateOnly Date, string DocumentTitle, Uri DocumentUrl, Continuing? Continuing, Review Review)
{
    /// <summary>A continuing statement counts for this long after its date (C14).</summary>
    public const int ContinuingMonths = 12;

    /// <summary>The authority's short name in tooltips ("ICJ", "Commission of Inquiry"); null: <see cref="Authority"/>.</summary>
    public string? ShortAuthority { get; init; }

    /// <summary>Whether the term was found established (only a court judgment may say no).</summary>
    public FindingOutcome Outcome { get; init; } = FindingOutcome.Established;

    /// <summary>When the row's facts were last checked on the authority's own site.</summary>
    public DateOnly CheckedOn { get; init; }

    /// <summary>What was checked, and where (for the owner's review, docs/curated-review.md); never shown in the app.</summary>
    public string? Verification { get; init; }

    /// <summary>A question for the owner's review (docs/curated-review.md); never shown in the app.</summary>
    public string? OwnerQuestion { get; init; }

    /// <summary>The name in tooltips and short lines.</summary>
    public string ShortName => string.IsNullOrWhiteSpace(ShortAuthority) ? Authority : ShortAuthority;

    /// <summary>
    /// A finding: a court judgment, a UN body finding or a government determination, whose term was found established. Only
    /// a finding can put a country on the map; interim steps (provisional measures, warrants, pending cases) never can.
    /// </summary>
    public bool IsFinding => Outcome == FindingOutcome.Established
        && Type is AuthorityType.CourtJudgment or AuthorityType.UnBodyFinding or AuthorityType.GovernmentDetermination;

    /// <summary>Provisional measures, an arrest warrant or a pending case: never a verdict (C14).</summary>
    public bool IsInterim => Type is AuthorityType.CourtProvisionalMeasures or AuthorityType.ArrestWarrant or AuthorityType.PendingCase;

    /// <summary>
    /// Has a continuing statement dated within the last <see cref="ContinuingMonths"/> months of <paramref name="nowUtc"/>,
    /// and not in the future; never for a "not established" judgment.
    /// </summary>
    public bool ContinuesAt(DateTime nowUtc)
    {
        if (Outcome != FindingOutcome.Established || Continuing is not { } c) return false;
        var today = DateOnly.FromDateTime(nowUtc);
        return c.Date <= today && c.Date >= today.AddMonths(-ContinuingMonths);
    }

    /// <summary>
    /// Puts its country on the map at <paramref name="nowUtc"/> (C14): a <see cref="IsFinding">finding</see> whose
    /// continuing statement is dated within the last 12 months. An interim step never does, even with a statement.
    /// </summary>
    public bool IsCurrentAt(DateTime nowUtc) => IsFinding && ContinuesAt(nowUtc);
}

/// <summary>One country's designations as the map and the panel show them.</summary>
/// <param name="Current">On the map: the current findings, and the interim steps with a recent continuing statement listed with
/// them (empty unless the country has a current finding). Newest first.</param>
/// <param name="History">The rest, except "not established" judgments and <see cref="Interim"/>. Newest first.</param>
/// <param name="NotEstablished">Court judgments that found the crime not established. Newest first.</param>
public sealed record CountryDesignations(IReadOnlyList<Designation> Current, IReadOnlyList<Designation> History,
    IReadOnlyList<Designation> NotEstablished)
{
    /// <summary>
    /// Interim steps (provisional measures, warrants, pending cases) with a statement from the last 12 months, in a country
    /// with no current finding: not on the map, and not history either (final fix wave D2). Newest first.
    /// </summary>
    public IReadOnlyList<Designation> Interim { get; init; } = [];

    /// <summary>Splits one country's visible designations at <paramref name="nowUtc"/>.</summary>
    public static CountryDesignations Of(IEnumerable<Designation> countryRows, DateTime nowUtc)
    {
        var rows = countryRows.OrderByDescending(d => d.Date).ToList();
        var notEstablished = rows.Where(d => d.Outcome == FindingOutcome.NotEstablished).ToList();
        var rest = rows.Where(d => d.Outcome == FindingOutcome.Established).ToList();
        var onMap = rest.Any(d => d.IsCurrentAt(nowUtc));
        var current = onMap ? rest.Where(d => d.IsCurrentAt(nowUtc) || (d.IsInterim && d.ContinuesAt(nowUtc))).ToList() : [];
        List<Designation> interim = onMap ? [] : [.. rest.Where(d => d.IsInterim && d.ContinuesAt(nowUtc))];
        return new CountryDesignations(current, [.. rest.Except(current).Except(interim)], notEstablished) { Interim = interim };
    }
}

/// <summary>A statement, by the designating body or in a UN report, that the situation continues (C14).</summary>
/// <param name="Statement">The statement, quoted or closely paraphrased, with its paragraph.</param>
public sealed record Continuing(string Statement, Uri Url, DateOnly Date)
{
    /// <summary>The document's title (every citation has one, spec §10.2).</summary>
    public string Title { get; init; } = "";
}

/// <summary>The owner's review (P3-4): <see cref="Draft"/> rows are shown only with <c>--show-drafts</c>.</summary>
/// <param name="Status"><see cref="Draft"/> or <see cref="Approved"/>.</param>
/// <param name="ApprovedOn">The day the owner approved the row; null for a draft.</param>
public sealed record Review(string Status, DateOnly? ApprovedOn)
{
    public const string Draft = "draft";
    public const string Approved = "approved";

    public bool IsApproved => Status == Approved;
}
