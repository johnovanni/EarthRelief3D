using System.Globalization;
using EarthRelief3D.Core.Model;

namespace EarthRelief3D.App.Presentation;

/// <summary>The unit distances are shown in (Settings, "Distances"; P4-15). Kilometres is the default.</summary>
public enum DistanceUnit { Kilometres, Miles }

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

    /// <summary>One international mile in kilometres.</summary>
    public const double KilometresPerMile = 1.609344;

    /// <summary>A distance in whole kilometres or miles with a thousands separator: "812 km", "505 mi", "12,345 km".</summary>
    public static string Distance(double km, DistanceUnit unit) => unit == DistanceUnit.Miles
        ? Math.Round(km / KilometresPerMile, MidpointRounding.AwayFromZero).ToString("N0", Inv) + " mi"
        : Math.Round(km, MidpointRounding.AwayFromZero).ToString("N0", Inv) + " km";

    /// <summary>"1 Apr 2026–31 Dec 2026".</summary>
    public static string Period(DateOnly from, DateOnly to) => Date(from) + "–" + Date(to);

    /// <summary>A fraction (0.32) as a whole percentage ("32%").</summary>
    public static string Percent(double fraction) => Math.Round(fraction * 100, MidpointRounding.AwayFromZero).ToString("0", Inv) + "%";

    /// <summary>"Source: &lt;publisher&gt;, &lt;dataset&gt; · &lt;date text&gt;".</summary>
    public static string SourceLine(SourceCitation c, string dateText) => $"Source: {c.Publisher}, {c.Dataset} · {dateText}";

    /// <summary>
    /// "IPC analysis period 1 Sep 2026–28 Feb 2027 (analysed Sep 2026)"; for a projection-only record "IPC projection
    /// period ... (analysed ...)".
    /// </summary>
    public static string FoodDate(FoodCrisis f)
    {
        var analysed = AnalysisDate(f) is { } v ? $" (analysed {v})" : "";
        return (f.ProjectionOnly ? "IPC projection period " : "IPC analysis period ") + Period(f.PeriodFrom, f.PeriodTo) + analysed;
    }

    /// <summary>The IPC "Date of analysis" ("Sep 2026"), or null when the file leaves it blank.</summary>
    public static string? AnalysisDate(FoodCrisis f) =>
        f.Citations.Count > 0 && !string.IsNullOrWhiteSpace(f.Citations[0].Version) ? f.Citations[0].Version : null;

    /// <summary>
    /// The panel line for an analysis with no current-period figures: "Sep 2026: projection only (1 Oct 2026–28 Feb 2027),
    /// no current-period figures" (after the "IPC analysis" label).
    /// </summary>
    public static string ProjectionOnlyLine(FoodCrisis f) =>
        (AnalysisDate(f) is { } v ? v + ": " : "") + $"projection only ({Period(f.PeriodFrom, f.PeriodTo)}), no current-period figures";

    /// <summary>"projection only (1 Oct 2026–28 Feb 2027, analysed Sep 2026), no current-period figures" (sources-checked list).</summary>
    public static string ProjectionOnlyNote(FoodCrisis f) =>
        $"projection only ({Period(f.PeriodFrom, f.PeriodTo)}{(AnalysisDate(f) is { } v ? ", analysed " + v : "")}), no current-period figures";

    /// <summary>"IPC: projection only (analysed Sep 2026)" (hover).</summary>
    public static string ProjectionOnlyHover(FoodCrisis f) =>
        "IPC: projection only" + (AnalysisDate(f) is { } v ? $" (analysed {v})" : "");

    /// <summary>
    /// The people in Phases 3, 4 and 5 of the current period (ruling R16), labelled "Phase 3 Crisis", "Phase 4 Emergency"
    /// and "Phase 5 Catastrophe/Famine". Empty for a projection-only record.
    /// </summary>
    public static IReadOnlyList<(string Label, long People)> PhaseCounts(FoodCrisis f) => f.ProjectionOnly ? [] :
    [
        (LayerStyle.IpcPhaseLabel(3), f.Phase3People),
        (LayerStyle.IpcPhaseLabel(4), f.Phase4People),
        (LayerStyle.IpcPhaseLabel(5), f.Phase5People),
    ];

    /// <summary>
    /// "people in Phase 3 Crisis 14,316,494, Phase 4 Emergency 5,015,231, Phase 5 Catastrophe/Famine 134,808" ("not
    /// reported" for a blank cell).
    /// </summary>
    public static string PhaseCountsText(FoodCrisis f) =>
        "people in " + string.Join(", ", PhaseCounts(f).Select((p, i) => $"{p.Label} {(f.IsReported(i + 3) ? Number(p.People) : NotReported)}"));

    // ---------------------------------------------------------------- the 20% rule in words (R17; one wording everywhere)

    /// <summary>What a blank cell in the IPC file is called on screen (never 0).</summary>
    public const string NotReported = "not reported";

    /// <summary>
    /// The faint tint of an outline-only country (owner's decision, round 2), one wording for the panel, the hover and the
    /// caption: "faint Phase 4 tint and outline: some people are in Phase 4".
    /// </summary>
    public static string FaintText(int phase) => $"faint Phase {phase} tint and outline: some people are in Phase {phase}";

    /// <summary>
    /// The panel's "Country shading" row: "Phase 3 Crisis (IPC's 20% rule)"; or "Not shaded (no phase covers 20% of the
    /// people)", followed for an outline-only country by "; faint Phase 4 tint and outline: some people are in Phase 4".
    /// </summary>
    public static string FoodShading(IpcAreaClass c)
    {
        if (c.ShadedPhase is { } p) return $"{LayerStyle.IpcPhaseLabel(p)} (IPC's 20% rule)";
        var why = c.ShareKnown ? "Not shaded (no phase covers 20% of the people)"
            : "Not shaded (share unknown: the file gives no number of people analysed)";
        return c.FaintPhase is { } f ? $"{why}; {FaintText(f)}" : why;
    }

    /// <summary>
    /// The tooltip's words for the shading: "shaded Phase 3 Crisis (20% rule)", "not shaded (no phase covers 20%)" or
    /// "not shaded (share unknown)".
    /// </summary>
    public static string FoodShadingShort(IpcAreaClass c) =>
        c.ShadedPhase is { } p ? $"shaded {LayerStyle.IpcPhaseLabel(p)} (20% rule)"
        : c.ShareKnown ? "not shaded (no phase covers 20%)" : "not shaded (share unknown)";

    /// <summary>The phase sum is D only because the file's numbers are rounded: it exceeds the people analysed by at most 1%.</summary>
    public static bool IsRoundingGap(FoodCrisis f, IpcAreaClass c) =>
        c.Source == IpcDenominator.PhaseSumLarger && (c.PhaseSum - f.PopulationAnalysed) * 100 <= f.PopulationAnalysed;

    /// <summary>
    /// A share of D in words, naming the total it is of: "41.0% of the people analysed" (D is the people analysed, or the
    /// phase sum only through rounding); "25.0% of the 1,000 people in the five phases" (D is the phase sum); over 100%
    /// (inconsistent numbers): "more than all the people analysed: the file's numbers disagree".
    /// </summary>
    public static string ShareOf(FoodCrisis f, IpcAreaClass c, double share) =>
        share > 1 + 1e-9 ? "more than all the people analysed: the file's numbers disagree"
        : c.Source == IpcDenominator.Analysed || IsRoundingGap(f, c) ? $"{FoodPercent(share)} of the people analysed"
        : $"{FoodPercent(share)} of the {Number(c.Denominator)} people in the five phases";

    /// <summary>
    /// A food share to one decimal ("30.1%"), so a small phase is not shown as 0%: "under 0.1%" for a share above 0 that
    /// would round to 0.0%, and "0%" for none.
    /// </summary>
    public static string FoodPercent(double fraction) =>
        fraction <= 0 ? "0%"
        : fraction < 0.0005 ? "under 0.1%"
        : Math.Round(fraction * 100, 1, MidpointRounding.AwayFromZero).ToString("0.0", Inv) + "%";

    /// <summary>
    /// What the shares are of, naming the denominator D (R17): "of 47,535,794 people analysed"; "of N people analysed (the
    /// sum of the five phases; the file gives no total)"; for a sum up to 1% over the people analysed (rounding; D is the
    /// sum for the arithmetic) "of 47,535,794 people analysed (the phase numbers add up to 30 more)"; for a bigger gap "of N
    /// people (the five phases add up to more than the M people analysed)"; "share unknown" with no denominator.
    /// </summary>
    public static string OfAnalysed(FoodCrisis f, IpcAreaClass c) => c.Source switch
    {
        IpcDenominator.Analysed => $"of {Number(c.Denominator)} people analysed",
        IpcDenominator.PhaseSum => $"of {Number(c.Denominator)} people analysed (the sum of the five phases; the file gives no total)",
        IpcDenominator.PhaseSumLarger when IsRoundingGap(f, c) =>
            $"of {Number(f.PopulationAnalysed)} people analysed (the phase numbers add up to {Number(c.PhaseSum - f.PopulationAnalysed)} more)",
        IpcDenominator.PhaseSumLarger =>
            $"of {Number(c.Denominator)} people (the five phases add up to more than the {Number(f.PopulationAnalysed)} people analysed)",
        _ => "share unknown",
    };

    /// <summary>Under the legend's food keys and under a phase bar shaded Phase 5 (R17): one wording.</summary>
    public const string FamineNote = "A country shaded Phase 5 is not an IPC famine classification, which also needs malnutrition and death-rate data.";

    /// <summary>
    /// The bar's caption: where the worst-20% line falls and what the map shows. "The line falls in Phase 3, so Sudan is
    /// shaded Crisis, with a Phase 5 outline."; "No phase reaches the worst 20%, so ... is not shaded" (a known share, but
    /// too few people in any reported phase); with no per-phase numbers to draw, the Phase 3 or higher share instead of a
    /// line; "Share unknown: ..." only when there is no denominator at all. For a past analysis, "... This analysis ended
    /// more than 12 months ago, so &lt;country&gt; is not shaded." For a country drawn by IPC area (P3-2,
    /// <paramref name="byArea"/>): "... &lt;country&gt; is drawn on the map by IPC area (IPC area classification), not by the
    /// 20% rule."
    /// </summary>
    public static string BarCaption(string country, FoodCrisis f, IpcAreaClass c, bool byArea = false)
    {
        string lead;
        if (!c.ShareKnown)
            lead = c.Phase3PlusPeople > 0
                ? $"Share unknown: the file gives {Number(c.Phase3PlusPeople)} people in Phase 3 or higher, but not the people analysed or the per-phase numbers"
                : "Share unknown: neither the people analysed nor the per-phase numbers are reported";
        else if (!c.CanStack)
            lead = (c.PhaseSum == 0 ? "The per-phase numbers are" : "The Phase 3 to 5 numbers are")
                   + $" not reported, but {Number(c.Phase3PlusPeople)} people ({ShareOf(f, c, c.Phase3PlusShare)}) are in Phase 3 or higher";
        else if (c.LinePhase is { } p) lead = $"The line falls in Phase {p}";
        else lead = "No phase reaches the worst 20%";
        if (f.IsStale) return $"{lead}. This analysis ended more than {FoodCrisis.StaleAfterMonths} months ago, so {country} is not shaded.";
        if (byArea) return $"{lead}. {country} is drawn on the map by IPC area (IPC area classification), not by the 20% rule.";
        var shading = c.ShadedPhase is { } s ? $"is shaded {LayerStyle.IpcPhaseName(s)}" : "is not shaded";
        var outline = c.FaintPhase is { } t ? $", with a faint Phase {t} tint and outline"
            : c.OutlinePhase is { } o ? $", with a Phase {o} outline" : "";
        return $"{lead}, so {country} {shading}{outline}.";
    }

    // ---------------------------------------------------------------- IPC areas (P3-2; one wording everywhere)

    /// <summary>
    /// An area IPC classified Phase 5: IPC's Famine classification (for an area, unlike a country shaded by the 20% rule, whose
    /// note is <see cref="FamineNote"/>).
    /// </summary>
    public const string AreaFamineLabel = "Phase 5 Famine (IPC area classification)";

    /// <summary>What a country drawn by area is shaded by (the panel's "Map shading" row).</summary>
    public const string ByAreaShading = "By IPC area: each area in IPC's own area classification (not the 20% rule)";

    /// <summary>The tooltip's words for a country drawn by area, where the mouse is on no area.</summary>
    public const string FoodShadingByArea = "shaded by IPC area (IPC area classification)";

    /// <summary>The source's short name at the end of an area's tooltip.</summary>
    public const string AreaSourceShort = "IPC via HDX";

    /// <summary>
    /// An area's classification: "IPC area classification Phase 4 Emergency"; Phase 5 is <see cref="AreaFamineLabel"/>; an
    /// area with phase 0 in the file "IPC area classification: none (area not classified in this analysis)".
    /// </summary>
    public static string AreaPhaseText(int? phase) => phase switch
    {
        5 => AreaFamineLabel,
        >= 1 and <= 4 => "IPC area classification " + LayerStyle.IpcPhaseLabel(phase.Value),
        _ => "IPC area classification: none (area not classified in this analysis)",
    };

    /// <summary>"IPC analysis period 1 Jun 2026–31 Aug 2026 (analysed Mar 2026)" for an area.</summary>
    public static string AreaDate(IpcArea a) =>
        "IPC analysis period " + Period(a.From, a.To) + (string.IsNullOrWhiteSpace(a.Citation.Version) ? "" : $" (analysed {a.Citation.Version})");

    /// <summary>"Bama, Nigeria · IPC area classification Phase 4 Emergency · IPC analysis period ... (analysed Mar 2026) · IPC via HDX".</summary>
    public static string AreaHover(IpcArea a, string country) =>
        $"{a.Name}, {country} · {AreaPhaseText(a.Phase)} · {AreaDate(a)} · {AreaSourceShort}";

    /// <summary>
    /// The panel's "IPC areas" line: "12 in Phase 3, 5 in Phase 4, 1 in Phase 5 Famine (IPC area classification)" (phases
    /// with no area left out); "none in Phase 3 or higher (of 38 areas)".
    /// </summary>
    public static string AreaCounts(IpcAreaSet set)
    {
        var parts = new List<string>();
        for (int p = 3; p <= 5; p++)
            if (set.CountInPhase(p) is > 0 and var n) parts.Add($"{n} in " + (p == 5 ? AreaFamineLabel : $"Phase {p}"));
        return parts.Count > 0 ? string.Join(", ", parts)
            : $"none in Phase 3 or higher (of {set.Areas.Count} area{(set.Areas.Count == 1 ? "" : "s")})";
    }

    /// <summary>
    /// The country's food tooltip where it is drawn by area and the mouse is on no area: "Nigeria · food crisis · shaded by
    /// IPC area (IPC area classification) · people in Phase 3 Crisis ... · IPC analysis period ..." (the national figures,
    /// never the 20% rule's shading).
    /// </summary>
    public static string FoodHoverByArea(string country, FoodCrisis f) =>
        $"{country} · {FoodKind} · {FoodShadingByArea} · {PhaseCountsText(f)} · {FoodDate(f)}";

    /// <summary>The newest as-of time among a crisis's citations.</summary>
    public static DateTime NewestAsOf(Crisis c) => c.Citations.Count == 0 ? default : c.Citations.Max(x => x.AsOfUtc);

    public static long Total(DisplacementFigure f) => f.Refugees + f.AsylumSeekers + f.Idps;

    // ---------------------------------------------------------------- hover tooltips (one line each)

    /// <summary>"Sudan · armed conflict · 12,345 deaths since Jan 2026 (UCDP) · as of 6 Nov 2026" (the UCDP text as is).</summary>
    public static string ConflictHover(string country, ConflictCrisis c) =>
        $"{country} · {ConflictKind} · {c.Severity.Text} · {AsOf(NewestAsOf(c))}";

    /// <summary>
    /// "Sudan · food crisis · shaded Phase 3 Crisis (20% rule) · worst phase with people: Phase 5 Catastrophe/Famine · people
    /// in Phase 3 Crisis 14,316,494, Phase 4 Emergency 5,015,231, Phase 5 Catastrophe/Famine 134,808 · IPC analysis period
    /// ... (analysed Jan 2026)" (R11: no "as of"; R16: people per phase; R17: the 20% rule, then the outline's phase when
    /// it is worse). GlobeCore's tooltip is text only, so the phase bar is in the panel, not here.
    /// </summary>
    public static string FoodHover(string country, FoodCrisis f)
    {
        var c = IpcAreaRule.Classify(f);
        var worse = c.FaintPhase is { } t ? $" · {FaintText(t)}"
            : c.OutlinePhase is { } o ? $" · worst phase with people: {LayerStyle.IpcPhaseLabel(o)}" : "";
        return $"{country} · {FoodKind} · {FoodShadingShort(c)}{worse} · {PhaseCountsText(f)} · {FoodDate(f)}";
    }

    /// <summary>"Kenya · disease outbreak · Zorvian fever · WHO, reported 18 Nov 2026".</summary>
    public static string OutbreakHover(string country, IReadOnlyList<OutbreakCrisis> reports)
    {
        var diseases = string.Join(", ", reports.OrderByDescending(r => r.PublishedUtc).Select(r => r.Disease).Distinct());
        return $"{country} · {OutbreakKind} · {diseases} · WHO, reported {Date(reports.Max(r => r.PublishedUtc))}";
    }

    /// <summary>"Sudan · people forced from home · 12,345,678 · UNHCR, Mid-year 2026 figures, as of 30 Jun 2026" (no "as of" for a future date).</summary>
    public static string DisplacementHover(string country, DisplacementFigure f, DateTime nowUtc) =>
        $"{country} · {DisplacementKind} · {Number(Total(f))} · UNHCR, {f.Citation.Dataset}, {DataDate(f.Citation, nowUtc)}";

    /// <summary>
    /// The country under the mouse, with the sources and dates of what is recorded there:
    /// "Sudan · armed conflict, food crisis · UCDP (as of 31 Oct 2026), IPC (analysed Sep 2026) · click for details".
    /// With no crisis it uses the check times (ruling R12): "Iceland · no crisis recorded by these sources · checked 22 Nov
    /// 2026 · ...", or "Iceland · not yet checked: UCDP, IPC, WHO, UNHCR · ..." while a source has never run. An IPC
    /// analysis with only a projection is never counted as "no crisis": "Guatemala · IPC: projection only (analysed Sep
    /// 2026) · no crisis recorded by UCDP or WHO · checked ...". The snapshot is judged at <paramref name="nowUtc"/>
    /// (WHO's 90 days, IPC's 12 months).
    /// </summary>
    public static string CountryHover(string country, string iso3, CrisisSnapshot s, DateTime nowUtc)
    {
        s = s.AsSeenAt(nowUtc);
        var crises = s.ByCountry(iso3);
        // One food record per country, the latest, as the map, the panel and the food hover use (M5).
        var latestFood = FoodCrisis.Latest(crises.OfType<FoodCrisis>());
        var projection = latestFood is { ProjectionOnly: true } ? latestFood : null;
        var kinds = new List<string>();
        var sources = new List<string>();
        var conflicts = crises.OfType<ConflictCrisis>().ToList();
        if (conflicts.Count > 0)
        {
            kinds.Add(ConflictKind);
            var newest = conflicts.SelectMany(c => c.Citations).MaxBy(c => c.AsOfUtc);
            sources.Add(newest is null ? "UCDP" : $"UCDP ({DataDate(newest, nowUtc)})");
        }
        if (latestFood is { IsStale: false, ProjectionOnly: false } food)
        {
            kinds.Add(FoodKind);
            var v = food.Citations.Count > 0 ? food.Citations[0].Version : null;
            sources.Add(string.IsNullOrWhiteSpace(v) ? $"IPC ({FoodDate(food)})" : $"IPC (analysed {v})");
        }
        var outbreaks = crises.OfType<OutbreakCrisis>().ToList();
        if (outbreaks.Count > 0)
        {
            kinds.Add(OutbreakKind);
            sources.Add($"WHO (reported {Date(outbreaks.Max(o => o.PublishedUtc))})");
        }
        if (kinds.Count > 0)
            return $"{country} · {string.Join(", ", kinds)} · {string.Join(", ", sources)}"
                + (projection is null ? "" : " · " + ProjectionOnlyHover(projection)) + $" · {ClickForDetails}";

        var notChecked = SourceDates.Unchecked(s);
        var checkedNames = SourceDates.SourceNames.Where(n => s.CheckedUtc.ContainsKey(n)).ToList();
        var parts = new List<string> { country };
        if (projection is not null)
        {
            // Not "no crisis": IPC has an analysis, only without current-period figures. UNHCR records no crises at all.
            parts.Add(ProjectionOnlyHover(projection));
            var others = new[] { "UCDP", "WHO" }.Where(checkedNames.Contains).ToList();
            if (others.Count > 0) parts.Add("no crisis recorded by " + string.Join(" or ", others));
        }
        else if (notChecked.Count == 0) parts.Add("no crisis recorded by these sources");
        else if (checkedNames.Count > 0) parts.Add("none recorded by " + string.Join(", ", checkedNames));
        if (checkedNames.Count > 0) parts.Add(CheckedDates(s, checkedNames));
        if (notChecked.Count > 0) parts.Add("not yet checked: " + string.Join(", ", notChecked));
        parts.Add(ClickForDetails);
        return string.Join(" · ", parts);
    }

    // "checked 22 Nov 2026" when the sources were checked on one day, else "checked UCDP 22 Nov 2026, IPC 21 Nov 2026".
    private static string CheckedDates(CrisisSnapshot s, IReadOnlyList<string> names)
    {
        var days = names.Select(n => Date(s.CheckedUtc[n])).ToList();
        return days.Distinct().Count() == 1 ? "checked " + days[0] : "checked " + string.Join(", ", names.Select((n, i) => n + " " + days[i]));
    }

    /// <summary>
    /// A citation's date: "as of &lt;date&gt;"; or, while that date is still in the future (UNHCR's current-year figures are
    /// as of 30 June and come out before it), "retrieved &lt;date&gt;": never "as of" a date that has not come yet.
    /// </summary>
    public static string DataDate(SourceCitation c, DateTime nowUtc) => c.DateMeaning switch
    {
        CitationDate.FileLoaded => "file loaded " + Date(c.AsOfUtc),
        CitationDate.EstimateYear => "estimate year " + c.AsOfUtc.Year.ToString(Inv),
        CitationDate.ThreeYearAverage => "average " + AverageRange(c.AsOfUtc.Year),
        CitationDate.Checked => "checked " + Date(c.AsOfUtc),
        CitationDate.Year => "year " + c.AsOfUtc.Year.ToString(Inv),
        CitationDate.Baseline => c.DateText ?? "baseline ending " + c.AsOfUtc.Year.ToString(Inv),
        CitationDate.Release => c.DateText ?? c.AsOfUtc.ToString("MMMM yyyy", Inv) + " release",
        _ => IsFuture(c, nowUtc) ? "retrieved " + Date(c.RetrievedUtc) : AsOf(c.AsOfUtc),
    };

    /// <summary>
    /// A whole series' date from its newest citation, for a legend's source line or a status dot: as
    /// <see cref="DataDate"/>, except that 3-year averages say so, "3-year averages, latest ending 2025", because the
    /// countries' own ranges differ (each figure shows its own: <see cref="DataDate"/>).
    /// </summary>
    public static string SeriesDate(SourceCitation newest, DateTime nowUtc) => newest.DateMeaning == CitationDate.ThreeYearAverage
        ? "3-year averages, latest ending " + newest.AsOfUtc.Year.ToString(Inv)
        : DataDate(newest, nowUtc);

    /// <summary>The 3 years of an average that ends in <paramref name="lastYear"/>: "2020–2022".</summary>
    public static string AverageRange(int lastYear) => $"{(lastYear - 2).ToString(Inv)}–{lastYear.ToString(Inv)}";

    /// <summary>True when the citation's as-of date has not come yet.</summary>
    public static bool IsFuture(SourceCitation c, DateTime nowUtc) => c.AsOfUtc > nowUtc;
}
