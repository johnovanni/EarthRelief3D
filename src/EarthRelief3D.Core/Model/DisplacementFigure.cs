namespace EarthRelief3D.Core.Model;

/// <summary>Displaced people by country of origin for one year. A layer value, not a crisis (C11).</summary>
public sealed record DisplacementFigure(string CountryIso3, int Year, long Refugees, long AsylumSeekers, long Idps,
    SourceCitation Citation);

/// <summary>A report the source published but that could not be placed on the globe (no country or coordinates), listed instead of dropped.</summary>
public sealed record UnplacedReport(string Source, string Title, string Reason, SourceCitation Citation);
