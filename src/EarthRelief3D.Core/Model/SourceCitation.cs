namespace EarthRelief3D.Core.Model;

/// <summary>Where a fact came from. Every crisis and figure carries one or more, so the UI can always name the source and its as-of date.</summary>
public sealed record SourceCitation(string Publisher, string Dataset, string? Version, Uri Url, string Licence,
    string Attribution, DateTime AsOfUtc, DateTime RetrievedUtc);
