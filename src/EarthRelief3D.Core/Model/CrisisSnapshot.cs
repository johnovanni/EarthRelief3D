using GlobeCore.Geo;

namespace EarthRelief3D.Core.Model;

/// <summary>Everything the UI shows. Compares by value (lists by content) so the store can skip no-op writes.</summary>
public sealed record CrisisSnapshot(IReadOnlyList<Crisis> Crises, IReadOnlyList<DisplacementFigure> Displacement,
    IReadOnlyList<UnplacedReport> Unplaced)
{
    public static readonly CrisisSnapshot Empty = new([], [], []);

    public IReadOnlyList<Crisis> ByCountry(string iso3) =>
        [.. Crises.Where(c => string.Equals(c.CountryIso3, iso3, StringComparison.OrdinalIgnoreCase))];

    public bool Equals(CrisisSnapshot? o) => o is not null && ListEquality.Equal(Crises, o.Crises)
        && ListEquality.Equal(Displacement, o.Displacement) && ListEquality.Equal(Unplaced, o.Unplaced);

    public override int GetHashCode() => HashCode.Combine(Crises.Count, Displacement.Count, Unplaced.Count);
}
