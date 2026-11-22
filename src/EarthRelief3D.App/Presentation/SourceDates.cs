using System.Globalization;
using EarthRelief3D.Core.Feeds;
using EarthRelief3D.Core.Model;

namespace EarthRelief3D.App.Presentation;

/// <summary>What one layer's data is and how recent it is, from the snapshot.</summary>
/// <param name="ShortName">The feed's name, as on its status dot ("UCDP").</param>
/// <param name="What">What the source tells us ("armed conflict").</param>
/// <param name="Source">"&lt;publisher&gt;, &lt;dataset&gt;" of the newest citation (or the source's usual name with no data).</param>
/// <param name="DateText">"as of 6 Nov 2026" (newest as-of among the layer's citations); null when nothing is loaded.</param>
public sealed record SourceSummary(CrisisLayer Layer, string ShortName, string What, string Source, string? DateText);

/// <summary>The newest date of each layer's data, for the legend and the panel's "sources checked".</summary>
public static class SourceDates
{
    private static readonly string[] AnalysisDateFormats = ["MMM yyyy", "MMMM yyyy", "yyyy-MM-dd", "yyyy-MM", "MMM-yy", "MMM yy"];

    public static IReadOnlyList<SourceSummary> All(CrisisSnapshot s) => [.. Enum.GetValues<CrisisLayer>().Select(l => For(l, s))];

    public static SourceSummary For(CrisisLayer layer, CrisisSnapshot s) => layer switch
    {
        CrisisLayer.Conflict => Newest(layer, "UCDP", "armed conflict",
            $"Uppsala Conflict Data Program (UCDP), {UcdpReader.Dataset}", s.Crises.OfType<ConflictCrisis>().SelectMany(c => c.Citations)),
        CrisisLayer.Food => Food(s),
        CrisisLayer.Outbreaks => Newest(layer, "WHO", "disease outbreaks, last 90 days", "World Health Organization, Disease Outbreak News",
            s.Crises.OfType<OutbreakCrisis>().SelectMany(c => c.Citations)),   // the layer's own citations: not the unplaced reports
        CrisisLayer.Displacement => Newest(layer, "UNHCR", "people forced from home", "UNHCR, Refugee Population Statistics Database",
            s.Displacement.Select(d => d.Citation)),
        _ => throw new ArgumentOutOfRangeException(nameof(layer)),
    };

    // The newest AsOfUtc. For UNHCR the dataset label ("Mid-year 2026 figures") says which release the date belongs to.
    private static SourceSummary Newest(CrisisLayer layer, string name, string what, string fallback, IEnumerable<SourceCitation> citations)
    {
        var newest = citations.MaxBy(c => c.AsOfUtc);
        if (newest is null) return new SourceSummary(layer, name, what, fallback, null);
        return new SourceSummary(layer, name, what, $"{newest.Publisher}, {newest.Dataset}", CrisisText.AsOf(newest.AsOfUtc));
    }

    // IPC: the citation's as-of is the analysis period's end (often in the future), so the date shown is the newest
    // "Date of analysis" (the citation's Version, such as "Sep 2026").
    private static SourceSummary Food(CrisisSnapshot s)
    {
        var citations = s.Crises.OfType<FoodCrisis>().SelectMany(c => c.Citations).ToList();
        var fallback = $"{IpcNationalReader.Publisher}, {IpcNationalReader.Dataset}";
        if (citations.Count == 0) return new SourceSummary(CrisisLayer.Food, "IPC", "food crises", fallback, null);
        var newest = citations.MaxBy(c => AnalysisDate(c.Version))!;
        var date = string.IsNullOrWhiteSpace(newest.Version) ? "analysis date not given" : "as of " + newest.Version;
        return new SourceSummary(CrisisLayer.Food, "IPC", "food crises", $"{newest.Publisher}, {newest.Dataset}", date);
    }

    /// <summary>The IPC "Date of analysis" text ("Sep 2026") as a date; unreadable text sorts first.</summary>
    public static DateTime AnalysisDate(string? text) =>
        text is not null && DateTime.TryParseExact(text.Trim(), AnalysisDateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)
            ? d : DateTime.MinValue;
}
