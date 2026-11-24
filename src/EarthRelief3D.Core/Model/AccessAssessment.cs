using GlobeCore.Geo;

namespace EarthRelief3D.Core.Model;

/// <summary>One of ACAPS's nine access indicators for a country: its number, its name as the workbook words it (without the
/// number), and its score (0 to 3, as published; 0 is a real figure).</summary>
public sealed record AccessIndicator(int Number, string Name, int Score);

/// <summary>
/// ACAPS's humanitarian access assessment of a country (Plan 4, docs/data-sources.md section 12): the overall score (0 "No
/// constraints" to 5 "Extreme access constraints"; 0 is a real figure, ruling R5), the three pillar scores, the nine indicator
/// scores, the trend and the information gap, all as the file gives them. Compares by value (the lists by their members).
/// </summary>
/// <param name="Iso3">The country, already resolved to a boundary unit.</param>
/// <param name="Pillars">P1, P2, P3 in that order.</param>
/// <param name="Trend">As the file gives it (Improving, Stable, Deteriorating or New).</param>
/// <param name="InformationGap">The file's Info_gap (0 to 1); null when blank.</param>
/// <param name="Edition">For example "December 2025 edition".</param>
public sealed record AccessAssessment(string Iso3, int Score, IReadOnlyList<int> Pillars, IReadOnlyList<AccessIndicator> Indicators,
    string Trend, double? InformationGap, string Edition, SourceCitation Citation)
{
    public bool Equals(AccessAssessment? o) =>
        o is not null && (ReferenceEquals(this, o) || (Iso3 == o.Iso3 && Score == o.Score && Trend == o.Trend
            && InformationGap == o.InformationGap && Edition == o.Edition && Citation == o.Citation
            && ListEquality.Equal(Pillars, o.Pillars) && ListEquality.Equal(Indicators, o.Indicators)));

    public override int GetHashCode() => HashCode.Combine(Iso3, Score, Trend, Edition);
}
