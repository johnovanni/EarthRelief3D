using System.Globalization;
using EarthRelief3D.Core.Feeds;
using EarthRelief3D.Core.Model;

namespace EarthRelief3D.App.Presentation;

/// <summary>What one layer's data is, how recent it is, and when its source was last checked.</summary>
/// <param name="ShortName">The feed's name, as on its status dot and in <see cref="CrisisSnapshot.CheckedUtc"/> ("UCDP").</param>
/// <param name="What">What the source tells us ("armed conflict").</param>
/// <param name="Source">"&lt;publisher&gt;, &lt;dataset&gt;" of the newest citation (or the source's usual name with no data).</param>
/// <param name="DateText">The data's date ("as of 6 Nov 2026"; IPC "as of Sep 2026"); null when the layer has no data.</param>
/// <param name="Newest">The newest citation, if any.</param>
/// <param name="CheckedUtc">When the source last ran successfully; null if never (ruling R12).</param>
public sealed record SourceSummary(CrisisLayer Layer, string ShortName, string What, string Source, string? DateText,
    SourceCitation? Newest, DateTime? CheckedUtc)
{
    /// <summary>
    /// The legend's date text: the data's date; or, with no data, "none recorded · checked &lt;date&gt;" (WHO: "no reports
    /// in the last 90 days · checked &lt;date&gt;"); or "not yet checked".
    /// </summary>
    public string LegendDate => DateText ?? (CheckedUtc is { } t ? SourceDates.NothingFound(ShortName) + " · " + SourceDates.Checked(t) : SourceDates.NotChecked);
}

/// <summary>The sources, the newest date of each layer's data and when each source was checked, for the legend, the panel and the hover.</summary>
public static class SourceDates
{
    public const string NotChecked = "not yet checked";

    private static readonly string[] AnalysisDateFormats = ["MMM yyyy", "MMMM yyyy", "yyyy-MM-dd", "yyyy-MM", "MMM-yy", "MMM yy"];

    /// <summary>The four sources in strip order, by feed name; also the status dots' order.</summary>
    public static IReadOnlyList<string> SourceNames { get; } = ["UCDP", "IPC", "WHO", "UNHCR"];

    /// <summary>
    /// The two needs feeds (Plan 3), after the four sources: "WHO GHO" (water, sanitation, health care) and "UN HNO" (people
    /// in need). They have status dots but are not crisis layers, so they are not in <see cref="SourceNames"/>.
    /// </summary>
    public static IReadOnlyList<string> NeedsFeedNames { get; } = [GhoFeed.FeedName, HnoFeed.FeedName];

    /// <summary>
    /// The two "Getting help in" feeds (Plan 4), after the needs feeds: "ACAPS" (access constraints) and "FTS" (appeal funding).
    /// They have status dots but are not crisis layers.
    /// </summary>
    public static IReadOnlyList<string> HelpFeedNames { get; } = [AcapsAccessFeed.FeedName, FtsFundingFeed.FeedName];

    /// <summary>The boundary update feed's name (GlobeCore's BoundaryUpdateFeed): the fifth status dot, after the four sources.</summary>
    public const string BordersFeedName = "Borders";

    public static string NameOf(CrisisLayer layer) => SourceNames[(int)layer];

    /// <summary>What a checked source that has nothing for a country says: "none recorded", or for WHO "no reports in the last 90 days".</summary>
    public static string NothingFound(string sourceName) =>
        sourceName == "WHO" ? $"no reports in the last {WhoOutbreakFeed.Window}" : "none recorded";

    /// <summary>"checked 22 Nov 2026".</summary>
    public static string Checked(DateTime utc) => "checked " + CrisisText.Date(utc);

    /// <summary>The sources never checked, in strip order.</summary>
    public static IReadOnlyList<string> Unchecked(CrisisSnapshot s) => [.. SourceNames.Where(n => !s.CheckedUtc.ContainsKey(n))];

    /// <summary>"Not yet checked: UCDP, IPC, WHO, UNHCR".</summary>
    public static string NotCheckedHeadline(IEnumerable<string> names) => "Not yet checked: " + string.Join(", ", names);

    public static IReadOnlyList<SourceSummary> All(CrisisSnapshot s, DateTime nowUtc) =>
        [.. Enum.GetValues<CrisisLayer>().Select(l => For(l, s, nowUtc))];

    public static SourceSummary For(CrisisLayer layer, CrisisSnapshot s, DateTime nowUtc) => layer switch
    {
        CrisisLayer.Conflict => Newest(layer, "armed conflict", $"Uppsala Conflict Data Program (UCDP), {UcdpReader.Dataset}",
            s.Crises.OfType<ConflictCrisis>().SelectMany(c => c.Citations), s, nowUtc),
        CrisisLayer.Food => Food(s),
        // The layer's own citations: not the unplaced reports.
        CrisisLayer.Outbreaks => Newest(layer, "disease outbreaks", "World Health Organization, Disease Outbreak News",
            s.Crises.OfType<OutbreakCrisis>().SelectMany(c => c.Citations), s, nowUtc),
        CrisisLayer.Displacement => Newest(layer, "people forced from home", "UNHCR, Refugee Population Statistics Database",
            s.Displacement.Select(d => d.Citation), s, nowUtc),
        _ => throw new ArgumentOutOfRangeException(nameof(layer)),
    };

    private static DateTime? CheckedOf(CrisisSnapshot s, CrisisLayer layer) =>
        s.CheckedUtc.TryGetValue(NameOf(layer), out var t) ? t : null;

    // The newest AsOfUtc (CrisisText.DataDate: a future as-of, such as UNHCR's mid-year date before 30 June, is not shown).
    private static SourceSummary Newest(CrisisLayer layer, string what, string fallback, IEnumerable<SourceCitation> citations,
        CrisisSnapshot s, DateTime nowUtc)
    {
        var newest = citations.MaxBy(c => c.AsOfUtc);
        var name = NameOf(layer);
        if (newest is null) return new SourceSummary(layer, name, what, fallback, null, null, CheckedOf(s, layer));
        return new SourceSummary(layer, name, what, $"{newest.Publisher}, {newest.Dataset}", CrisisText.DataDate(newest, nowUtc),
            newest, CheckedOf(s, layer));
    }

    // IPC (ruling R11): the citation's as-of is the analysis period's end (often in the future), so the date shown is the
    // newest "Date of analysis" (the citation's Version, such as "Sep 2026"). Projection-only analyses are left out: the
    // layer shades current-period figures only, so its date must not come from a row it does not draw.
    private static SourceSummary Food(CrisisSnapshot s)
    {
        var citations = s.Crises.OfType<FoodCrisis>().Where(c => !c.ProjectionOnly).SelectMany(c => c.Citations).ToList();
        var fallback = $"{IpcNationalReader.Publisher}, {IpcNationalReader.Dataset}";
        var check = CheckedOf(s, CrisisLayer.Food);
        if (citations.Count == 0) return new SourceSummary(CrisisLayer.Food, "IPC", "food crises", fallback, null, null, check);
        var newest = citations.MaxBy(c => AnalysisDate(c.Version))!;
        var date = string.IsNullOrWhiteSpace(newest.Version) ? "analysis date not given" : "as of " + newest.Version;
        return new SourceSummary(CrisisLayer.Food, "IPC", "food crises", $"{newest.Publisher}, {newest.Dataset}", date, newest, check);
    }

    /// <summary>The IPC "Date of analysis" text ("Sep 2026") as a date; unreadable text sorts first.</summary>
    public static DateTime AnalysisDate(string? text) =>
        text is not null && DateTime.TryParseExact(text.Trim(), AnalysisDateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)
            ? d : DateTime.MinValue;
}
