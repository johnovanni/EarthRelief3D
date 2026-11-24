using EarthRelief3D.Core.Model;
using GlobeCore.Bodies;
using GlobeCore.Geo;

namespace EarthRelief3D.Core.Logic;

/// <summary>The three kinds of hub the app measures to (P4-5), in the order the panel lists them.</summary>
public enum HubKind { UnDepot, Airport, Port }

/// <summary>
/// A place relief can come in through: a UN depot, an airport or a port, with where its data is from.
/// </summary>
/// <param name="Detail">An airport's IATA code or a port's UN/LOCODE; empty for a depot or when the source has none.</param>
/// <param name="Citation">The source and its date (the dataset's manifest, or the depot row's source checked on its day).</param>
public sealed record Hub(HubKind Kind, string Name, string Iso3, SurfacePoint Position, string Detail, SourceCitation Citation)
{
    /// <summary>A curated row not yet reviewed by the owner, shown only in a <c>--show-drafts</c> run (P4-13): its row and arc label say so.</summary>
    public bool IsDraft { get; init; }
}

/// <summary>A hub and its straight-line (great-circle) distance in kilometres from the crisis place.</summary>
public sealed record HubLink(Hub Hub, double DistanceKm);

/// <summary>
/// The nearest hub of each kind to a place (P4-6): great-circle distance on a 6,371 km sphere (C2, C3), so a hub across
/// the date line or over the pole is found as easily as one beside it.
/// </summary>
public static class NearestHubFinder
{
    public const double EarthRadiusKm = 6371.0;

    /// <summary>
    /// At most one link per kind, in <see cref="HubKind"/> order; a kind with no hub is left out. A tie in distance goes to
    /// the name that sorts first (ordinal), so the answer does not depend on the order the hubs come in.
    /// </summary>
    public static IReadOnlyList<HubLink> Nearest(SurfacePoint from, IReadOnlyList<Hub> hubs)
    {
        ArgumentNullException.ThrowIfNull(hubs);
        var best = new HubLink?[Enum.GetValues<HubKind>().Length];
        foreach (var hub in hubs)
        {
            double d = GeoMath.DistanceKm(from, hub.Position, EarthRadiusKm);
            if (!double.IsFinite(d)) continue;
            ref var current = ref best[(int)hub.Kind];
            if (current is null || d < current.DistanceKm
                || (d == current.DistanceKm && string.CompareOrdinal(hub.Name, current.Hub.Name) < 0))
                current = new HubLink(hub, d);
        }
        return [.. best.Where(l => l is not null).Select(l => l!)];
    }
}
