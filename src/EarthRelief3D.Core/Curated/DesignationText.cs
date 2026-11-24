using System.Globalization;

namespace EarthRelief3D.Core.Curated;

/// <summary>
/// The words for a designation, one wording everywhere (tooltip, panel, review document). C14: a court judgment, a UN body
/// finding and a government determination are named as such (the government by name); provisional measures, arrest
/// warrants and pending cases say what they are and that they are not verdicts; a court's "not established" says so.
/// </summary>
public static class DesignationText
{
    /// <summary>The label of a row the owner has not yet approved (P3-4), shown only with <c>--show-drafts</c>.</summary>
    public const string DraftLabel = "Draft: not yet reviewed";

    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    /// <summary>
    /// "Court judgment: genocide", "Court judgment: genocide not established", "UN body finding: crimes against humanity",
    /// "United States government determination: genocide", "Provisional measures in a case alleging genocide: not a
    /// finding", "Arrest warrant on charges of crimes against humanity: not a verdict", "Pending: no finding yet (case
    /// alleging genocide)".
    /// </summary>
    public static string Label(Designation d, IReadOnlyList<Government> governments)
    {
        ArgumentNullException.ThrowIfNull(d);
        return d.Type switch
        {
            AuthorityType.CourtJudgment => $"Court judgment: {d.Term}" + (d.Outcome == FindingOutcome.NotEstablished ? " not established" : ""),
            AuthorityType.CourtProvisionalMeasures => $"Provisional measures in a case alleging {d.Term}: not a finding",
            AuthorityType.ArrestWarrant => $"Arrest warrant on charges of {d.Term}: not a verdict",
            AuthorityType.PendingCase => $"Pending: no finding yet (case alleging {d.Term})",
            AuthorityType.UnBodyFinding => $"UN body finding: {d.Term}",
            AuthorityType.GovernmentDetermination => $"{GovernmentName(d, governments)} determination: {d.Term}",
            _ => throw new ArgumentOutOfRangeException(nameof(d), d.Type, "Unknown authority type."),
        };
    }

    /// <summary>What kind of authority made it: "International court", "UN body", "Government (United States government)".</summary>
    public static string AuthorityKind(Designation d, IReadOnlyList<Government> governments) => d.Type switch
    {
        AuthorityType.CourtJudgment or AuthorityType.CourtProvisionalMeasures or AuthorityType.ArrestWarrant or AuthorityType.PendingCase => "court",
        AuthorityType.UnBodyFinding => "UN body",
        AuthorityType.GovernmentDetermination => "government: " + GovernmentName(d, governments),
        _ => throw new ArgumentOutOfRangeException(nameof(d), d.Type, "Unknown authority type."),
    };

    /// <summary>The label with the draft mark in front when the row is not approved.</summary>
    public static string DisplayLabel(Designation d, IReadOnlyList<Government> governments) =>
        (d.Review.IsApproved ? "" : DraftLabel + " · ") + Label(d, governments);

    /// <summary>"Myanmar · UN body finding: crimes against humanity (Fact-Finding Mission, 2018)": the authority, its kind and the year.</summary>
    public static string Hover(Designation d, string country, IReadOnlyList<Government> governments) =>
        $"{country} · {DisplayLabel(d, governments)} ({d.ShortName}, {d.Date.Year.ToString(Inv)})";

    /// <summary>"26 Feb 2007".</summary>
    public static string Date(DateOnly d) => d.ToString("d MMM yyyy", Inv);

    private static string GovernmentName(Designation d, IReadOnlyList<Government> governments) =>
        governments.FirstOrDefault(g => string.Equals(g.Iso3, d.GovernmentIso3, StringComparison.OrdinalIgnoreCase))?.Name
        ?? (d.GovernmentIso3 is { } iso ? iso + " government" : "Government");
}
