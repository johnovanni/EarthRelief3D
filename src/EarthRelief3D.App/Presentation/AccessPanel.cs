using System.Globalization;
using EarthRelief3D.Core.Model;
using EarthRelief3D.Core.Store;

namespace EarthRelief3D.App.Presentation;

/// <summary>
/// The words of the Access section and shading (Plan 4 Task 3): ACAPS's six level names for the overall score, the nine
/// indicators as ACAPS's website words them, and the lines that say a country has no assessment. All wording is ACAPS's own,
/// recorded in docs/data-sources.md section 12; the app never adds a score of its own (P4-2).
/// </summary>
public static class AccessPanel
{
    /// <summary>The legend and strip wording of the shading.</summary>
    public const string Measures = "Humanitarian access constraints (ACAPS score 0–5)";

    /// <summary>The panel row's label for the overall score.</summary>
    public const string ScoreLabel = "Access constraints";

    /// <summary>The overall score's levels (0 to 5) as the About sheet of ACAPS's workbook names them.</summary>
    public static IReadOnlyList<string> LevelNames { get; } =
    [
        "No constraints", "Low access constraints", "Moderate access constraints", "High access constraints",
        "Very high access constraints", "Extreme access constraints",
    ];

    /// <summary>The indicator rows' value for an indicator the assessment has no 0 to 3 score for (ruling R8).</summary>
    public const string NoScore = "No score (information gap)";

    /// <summary>
    /// The nine indicators as ACAPS's overview page words them (docs/data-sources.md section 12, quoted 2026-11-24), by
    /// indicator number 1 to 9. The workbook's own column names are shorter; the score shown is the workbook's, unchanged.
    /// </summary>
    public static IReadOnlyDictionary<int, string> IndicatorWording { get; } = new Dictionary<int, string>
    {
        [1] = "Denial of existence of humanitarian needs or entitlements to assistance",
        [2] = "Restriction and obstruction of access to services and assistance",
        [3] = "Impediments to enter the country",
        [4] = "Restriction of movement within the country",
        [5] = "Interference into implementation of humanitarian activities",
        [6] = "Violence against humanitarian personnel, facilities, and assets",
        [7] = "Insecurity or hostilities affecting humanitarian assistance",
        [8] = "Presence of landmines, improvised explosive devices, explosive remnants of war and unexploded ordnance",
        [9] = "Physical constraints in the environment",
    };

    /// <summary>"5 of 5: Extreme access constraints" (0 is a real figure: "0 of 5: No constraints", ruling R5).</summary>
    public static string ScoreText(int score) =>
        $"{score.ToString(CultureInfo.InvariantCulture)} of 5: {LevelNames[Math.Clamp(score, 0, 5)]}";

    /// <summary>"3 of 3": one indicator's score.</summary>
    public static string IndicatorText(int score) => $"{score.ToString(CultureInfo.InvariantCulture)} of 3";

    /// <summary>"No ACAPS assessment for Iceland in the December 2025 edition".</summary>
    public static string NoAssessment(string countryName, string? edition) =>
        edition is null ? $"No ACAPS assessment for {countryName}" : $"No ACAPS assessment for {countryName} in the {edition}";

    /// <summary>
    /// The edition the loaded score series is from ("December 2025 edition"): its newest citation's date text; null when the
    /// series is not loaded or has no rows.
    /// </summary>
    public static string? EditionOf(IndicatorSnapshot snap) =>
        snap.Get(Core.Feeds.AcapsAccessFeed.ScoreSeriesId)?.Values.Select(v => v.Citation).MaxBy(c => c.AsOfUtc)?.DateText;
}
