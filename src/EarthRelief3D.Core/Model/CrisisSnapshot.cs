using GlobeCore.Geo;

namespace EarthRelief3D.Core.Model;

/// <summary>Everything the UI shows. Compares by value (lists by content) so the store can skip no-op writes.</summary>
public sealed record CrisisSnapshot(IReadOnlyList<Crisis> Crises, IReadOnlyList<DisplacementFigure> Displacement,
    IReadOnlyList<UnplacedReport> Unplaced)
{
    private static readonly IReadOnlyDictionary<string, DateTime> NoChecks = new Dictionary<string, DateTime>();

    public static readonly CrisisSnapshot Empty = new([], [], []);

    /// <summary>
    /// When each source last ran successfully (UTC), keyed by its feed name ("UCDP", "IPC", "WHO", "UNHCR"), including
    /// runs that found nothing (ruling R12). A source missing here has never been checked, so "nothing recorded" cannot
    /// be said for it.
    /// </summary>
    public IReadOnlyDictionary<string, DateTime> CheckedUtc { get; init; } = NoChecks;

    public IReadOnlyList<Crisis> ByCountry(string iso3) =>
        [.. Crises.Where(c => string.Equals(c.CountryIso3, iso3, StringComparison.OrdinalIgnoreCase))];

    public bool Equals(CrisisSnapshot? o) => o is not null && ListEquality.Equal(Crises, o.Crises)
        && ListEquality.Equal(Displacement, o.Displacement) && ListEquality.Equal(Unplaced, o.Unplaced)
        && ChecksEqual(CheckedUtc, o.CheckedUtc);

    public override int GetHashCode() => HashCode.Combine(Crises.Count, Displacement.Count, Unplaced.Count, CheckedUtc.Count);

    private static bool ChecksEqual(IReadOnlyDictionary<string, DateTime> a, IReadOnlyDictionary<string, DateTime> b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a.Count != b.Count) return false;
        foreach (var (k, v) in a) if (!b.TryGetValue(k, out var w) || v != w) return false;
        return true;
    }
}
