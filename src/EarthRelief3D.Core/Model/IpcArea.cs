using AstroCore.Earth;
using GlobeCore.Geo;

namespace EarthRelief3D.Core.Model;

/// <summary>
/// One area of a country's IPC analysis (P3-2), from HDX's <c>ipc_&lt;iso3&gt;.geojson</c>: the area's name (the feature's
/// <c>title</c>), IPC's own area classification (<c>overall_phase</c>; null for 0, "not classified"), its estimated
/// population, and its shape, split at ±180° (no polygon crosses the dateline). Point features (settlements) are never areas.
/// </summary>
/// <param name="Phase">IPC's area phase, 1 to 5 (Phase 5 for an area is IPC's Famine classification); null when the area is not classified.</param>
/// <param name="Population">The file's <c>estimated_population</c>; null when blank.</param>
/// <param name="AnalysisId">The file's <c>anl_id</c>.</param>
/// <param name="From">The current period's start, from the country's <c>ipc_&lt;iso3&gt;_national_long_latest.csv</c> (the GeoJSON has no dates).</param>
public sealed record IpcArea(string Iso3, string Name, int? Phase, long? Population, IReadOnlyList<GeoPolygon> Polygons,
    string AnalysisId, DateOnly From, DateOnly To, SourceCitation Citation)
{
    public bool Contains(GeoPosition g) => Polygons.Any(p => p.Contains(g));

    public bool Equals(IpcArea? o) => o is not null && Iso3 == o.Iso3 && Name == o.Name && Phase == o.Phase && Population == o.Population
        && AnalysisId == o.AnalysisId && From == o.From && To == o.To && Citation == o.Citation && ListEquality.Equal(Polygons, o.Polygons);

    public override int GetHashCode() => HashCode.Combine(Iso3, Name, Phase, Population, AnalysisId, From, To, ListEquality.Hash(Polygons));
}

/// <summary>
/// A country's current IPC areas (ruling R4): every area with <c>ipc_period</c> C in the country's GeoJSON, all of one
/// analysis (<see cref="AnalysisId"/>), whose current period (<see cref="From"/>–<see cref="To"/>, from the same dataset's
/// national CSV) is the national record's. The map, the hover and the panel use a set only while it <see cref="Matches"/>
/// the country's latest national record; otherwise the country is shaded by the 20% rule (R17).
/// </summary>
/// <param name="AnalysisDate">The CSV's "Date of analysis" ("Mar 2026"); null when blank.</param>
public sealed record IpcAreaSet(string Iso3, string AnalysisId, string? AnalysisDate, DateOnly From, DateOnly To,
    IReadOnlyList<IpcArea> Areas, SourceCitation Citation)
{
    /// <summary>
    /// The set belongs to <paramref name="f"/>: the same country, a current record (not stale, not projection only) and the
    /// same period.
    /// </summary>
    public bool Matches(FoodCrisis? f) => f is { IsStale: false, ProjectionOnly: false }
        && string.Equals(f.CountryIso3, Iso3, StringComparison.OrdinalIgnoreCase) && f.PeriodFrom == From && f.PeriodTo == To;

    /// <summary>The areas IPC classified in <paramref name="phase"/>.</summary>
    public int CountInPhase(int phase) => Areas.Count(a => a.Phase == phase);

    /// <summary>The area at <paramref name="g"/>, or null outside every area.</summary>
    public IpcArea? AreaAt(GeoPosition g) => Areas.FirstOrDefault(a => a.Contains(g));

    public bool Equals(IpcAreaSet? o) => o is not null && Iso3 == o.Iso3 && AnalysisId == o.AnalysisId && AnalysisDate == o.AnalysisDate
        && From == o.From && To == o.To && Citation == o.Citation && ListEquality.Equal(Areas, o.Areas);

    public override int GetHashCode() => HashCode.Combine(Iso3, AnalysisId, AnalysisDate, From, To, Areas.Count);
}
