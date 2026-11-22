using System.Globalization;
using EarthRelief3D.Core.Model;

namespace EarthRelief3D.App.Presentation;

/// <summary>
/// The words and numbers the panel, the legend and the hover tooltips show. Every fact names its source and a date. IPC
/// citations carry the analysis period's END as their as-of time, which is often in the future, so food never says
/// "as of" a period end: it says "IPC analysis period &lt;from&gt;–&lt;to&gt; (analysed &lt;date of analysis&gt;)".
/// </summary>
public static class CrisisText
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public const string ConflictKind = "armed conflict";
    public const string FoodKind = "food crisis";
    public const string OutbreakKind = "disease outbreak";
    public const string DisplacementKind = "people forced from home";
    public const string ClickForDetails = "click for details";

    /// <summary>"6 Nov 2026".</summary>
    public static string Date(DateTime utc) => utc.ToString("d MMM yyyy", Inv);

    public static string Date(DateOnly d) => d.ToString("d MMM yyyy", Inv);

    /// <summary>"as of 6 Nov 2026".</summary>
    public static string AsOf(DateTime utc) => "as of " + Date(utc);

    public static string Number(long n) => n.ToString("N0", Inv);

    /// <summary>"1 Apr 2026–31 Dec 2026".</summary>
    public static string Period(DateOnly from, DateOnly to) => Date(from) + "–" + Date(to);

    /// <summary>A fraction (0.32) as a whole percentage ("32%").</summary>
    public static string Percent(double fraction) => Math.Round(fraction * 100, MidpointRounding.AwayFromZero).ToString("0", Inv) + "%";

    /// <summary>"Source: &lt;publisher&gt;, &lt;dataset&gt; · &lt;date text&gt;".</summary>
    public static string SourceLine(SourceCitation c, string dateText) => $"Source: {c.Publisher}, {c.Dataset} · {dateText}";

    /// <summary>"IPC analysis period 1 Sep 2026–28 Feb 2027 (analysed Sep 2026)".</summary>
    public static string FoodDate(FoodCrisis f)
    {
        var analysed = f.Citations.Count > 0 && !string.IsNullOrWhiteSpace(f.Citations[0].Version) ? $" (analysed {f.Citations[0].Version})" : "";
        return "IPC analysis period " + Period(f.PeriodFrom, f.PeriodTo) + analysed;
    }

    /// <summary>The newest as-of time among a crisis's citations.</summary>
    public static DateTime NewestAsOf(Crisis c) => c.Citations.Count == 0 ? default : c.Citations.Max(x => x.AsOfUtc);

    public static long Total(DisplacementFigure f) => f.Refugees + f.AsylumSeekers + f.Idps;

    // ---------------------------------------------------------------- hover tooltips (one line each)

    /// <summary>"Sudan · armed conflict · 12,345 deaths since Jan 2026 (UCDP) · as of 6 Nov 2026" (the UCDP text as is).</summary>
    public static string ConflictHover(string country, ConflictCrisis c) =>
        $"{country} · {ConflictKind} · {c.Severity.Text} · {AsOf(NewestAsOf(c))}";

    public static string FoodHover(string country, FoodCrisis f) => $"{country} · {FoodKind} · {f.Severity.Text} · {FoodDate(f)}";

    /// <summary>"Kenya · disease outbreak · Zorvian fever · WHO, reported 18 Nov 2026".</summary>
    public static string OutbreakHover(string country, IReadOnlyList<OutbreakCrisis> reports)
    {
        var diseases = string.Join(", ", reports.OrderByDescending(r => r.PublishedUtc).Select(r => r.Disease).Distinct());
        return $"{country} · {OutbreakKind} · {diseases} · WHO, reported {Date(reports.Max(r => r.PublishedUtc))}";
    }

    /// <summary>"Sudan · people forced from home · 12,345,678 · UNHCR, Mid-year 2026 figures, as of 30 Jun 2026".</summary>
    public static string DisplacementHover(string country, DisplacementFigure f) =>
        $"{country} · {DisplacementKind} · {Number(Total(f))} · UNHCR, {f.Citation.Dataset}, {AsOf(f.Citation.AsOfUtc)}";

    /// <summary>"Sudan · armed conflict, food crisis · click for details", or "Iceland · no crisis recorded by these sources · ...".</summary>
    public static string CountryHover(string country, IReadOnlyList<Crisis> crises)
    {
        var kinds = new List<string>();
        if (crises.Any(c => c is ConflictCrisis)) kinds.Add(ConflictKind);
        if (crises.Any(c => c is FoodCrisis { IsStale: false })) kinds.Add(FoodKind);
        if (crises.Any(c => c is OutbreakCrisis)) kinds.Add(OutbreakKind);
        var what = kinds.Count == 0 ? "no crisis recorded by these sources" : string.Join(", ", kinds);
        return $"{country} · {what} · {ClickForDetails}";
    }
}
