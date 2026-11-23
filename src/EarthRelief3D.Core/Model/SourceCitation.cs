namespace EarthRelief3D.Core.Model;

/// <summary>Where a fact came from. Every crisis and figure carries one or more, so the UI can always name the source and its as-of date.</summary>
public sealed record SourceCitation(string Publisher, string Dataset, string? Version, Uri Url, string Licence,
    string Attribution, DateTime AsOfUtc, DateTime RetrievedUtc, CitationDate DateMeaning = CitationDate.AsOf);

/// <summary>What <see cref="SourceCitation.AsOfUtc"/> means, so the screen never calls a load date or a year "as of" a day.</summary>
public enum CitationDate
{
    /// <summary>The date the data is as of (every source before Plan 3).</summary>
    AsOf,
    /// <summary>The day the source's file was loaded (HNO): shown "file loaded 13 Feb 2026".</summary>
    FileLoaded,
    /// <summary>Only the estimate's year is known (WHO GHO); AsOfUtc is 31 Dec of it and the day is never shown: "estimate year 2024".</summary>
    EstimateYear,
}
