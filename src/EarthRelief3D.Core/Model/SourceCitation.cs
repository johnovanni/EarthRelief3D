namespace EarthRelief3D.Core.Model;

/// <summary>Where a fact came from. Every crisis and figure carries one or more, so the UI can always name the source and its as-of date.</summary>
public sealed record SourceCitation(string Publisher, string Dataset, string? Version, Uri Url, string Licence,
    string Attribution, DateTime AsOfUtc, DateTime RetrievedUtc, CitationDate DateMeaning = CitationDate.AsOf)
{
    /// <summary>
    /// The date in words, for a <see cref="CitationDate.Baseline"/> ("baseline 1979–2019 (published 2023)", from the bundled
    /// extract's manifest); null for every other meaning.
    /// </summary>
    public string? DateText { get; init; }
}

/// <summary>What <see cref="SourceCitation.AsOfUtc"/> means, so the screen never calls a load date or a year "as of" a day.</summary>
public enum CitationDate
{
    /// <summary>The date the data is as of (every source before Plan 3).</summary>
    AsOf,
    /// <summary>The day the source's file was loaded (HNO): shown "file loaded 13 Feb 2026".</summary>
    FileLoaded,
    /// <summary>Only the estimate's year is known (WHO GHO); AsOfUtc is 31 Dec of it and the day is never shown: "estimate year 2024".</summary>
    EstimateYear,
    /// <summary>
    /// A 3-year average (FAOSTAT's food security suite): AsOfUtc is 31 Dec of its last year, and the figure is always shown with
    /// its range, "average 2020–2022", never as one year (Task 6 fix round 1).
    /// </summary>
    ThreeYearAverage,
    /// <summary>
    /// The day a curated list was last checked in its sources (the camp sites): shown "checked 23 Nov 2026", like the
    /// atrocities line; never "as of" (the list is not data as of that day) and never "retrieved".
    /// </summary>
    Checked,
    /// <summary>
    /// A full calendar year's figure, not an estimate (EIA's annual production): AsOfUtc is 31 Dec of it and the day is never
    /// shown: "year 2025" (final fix wave C4).
    /// </summary>
    Year,
    /// <summary>
    /// A modelled baseline over a period (WRI Aqueduct 4.0: 1979–2019, published 2023): <see cref="SourceCitation.DateText"/>
    /// says it in words, "baseline 1979–2019 (published 2023)", and no year is ever appended to the figure or its measure;
    /// AsOfUtc is 31 Dec of the period's last year (final fix wave I3).
    /// </summary>
    Baseline,
}
