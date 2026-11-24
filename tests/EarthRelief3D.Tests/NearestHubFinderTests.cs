using EarthRelief3D.Core.Logic;
using EarthRelief3D.Core.Model;
using GlobeCore.Bodies;
using GlobeCore.Geo;

namespace EarthRelief3D.Tests;

/// <summary>Plan 4 Task 5: the nearest UN depot, airport and port by great-circle distance (P4-6; Review Focus 4: dateline, poles, ties).</summary>
public class NearestHubFinderTests
{
    private static readonly SourceCitation Cite = new("Test", "Hubs", null, new Uri("https://example.org/hubs"), "CC0", "Test.",
        new DateTime(2026, 11, 24, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 11, 24, 0, 0, 0, DateTimeKind.Utc));

    private static Hub H(HubKind kind, string name, double lat, double lon, string detail = "") =>
        new(kind, name, "FJI", new SurfacePoint(lat, lon), detail, Cite);

    [Fact]
    public void AcrossDateLine()
    {
        // From Fiji (17.7 S, 178.0 E): an airport at 179.9 W is 2.1 degrees of longitude away, one at 170.0 E is 8: the nearer is across the line.
        var from = new SurfacePoint(-17.7, 178.0);
        var links = NearestHubFinder.Nearest(from, [H(HubKind.Airport, "West", -17.7, 170.0), H(HubKind.Airport, "East", -17.7, -179.9)]);
        var link = Assert.Single(links);
        Assert.Equal("East", link.Hub.Name);
        Assert.InRange(link.DistanceKm, 200, 250);
    }

    [Fact]
    public void NearPole()
    {
        var from = new SurfacePoint(89.9, 10);
        var links = NearestHubFinder.Nearest(from,
        [
            H(HubKind.Airport, "Opposite side", 89.5, -170),   // across the pole: still near
            H(HubKind.Airport, "Far", 60, 10),
            H(HubKind.Port, "Pole port", 90, 0),
        ]);
        Assert.Equal(["Opposite side", "Pole port"], links.Select(l => l.Hub.Name));
        Assert.All(links, l => Assert.True(double.IsFinite(l.DistanceKm) && l.DistanceKm > 0));
        Assert.True(links[0].DistanceKm < 120, links[0].DistanceKm.ToString());   // 0.1 + 0.5 degrees over the pole, about 67 km
        Assert.True(links[1].DistanceKm < 12, links[1].DistanceKm.ToString());    // 0.1 degree
    }

    [Fact]
    public void TieBrokenByName()
    {
        var from = new SurfacePoint(0, 0);
        var a = NearestHubFinder.Nearest(from, [H(HubKind.Port, "Zeta", 1, 1), H(HubKind.Port, "Alpha", 1, 1), H(HubKind.Port, "Mike", 1, 1)]);
        Assert.Equal("Alpha", Assert.Single(a).Hub.Name);
        // The order the hubs come in does not matter.
        var b = NearestHubFinder.Nearest(from, [H(HubKind.Port, "Mike", 1, 1), H(HubKind.Port, "Zeta", 1, 1), H(HubKind.Port, "Alpha", 1, 1)]);
        Assert.Equal("Alpha", Assert.Single(b).Hub.Name);
    }

    [Fact]
    public void OnePerKind()
    {
        var from = new SurfacePoint(10, 10);
        var links = NearestHubFinder.Nearest(from,
        [
            H(HubKind.Port, "Far port", 20, 20), H(HubKind.Airport, "Near airport", 10, 11), H(HubKind.UnDepot, "Depot", 0, 0),
            H(HubKind.Port, "Near port", 10, 12), H(HubKind.Airport, "Far airport", 30, 30),
        ]);
        // In HubKind order, whatever the order the hubs came in.
        Assert.Equal([HubKind.UnDepot, HubKind.Airport, HubKind.Port], links.Select(l => l.Hub.Kind));
        Assert.Equal(["Depot", "Near airport", "Near port"], links.Select(l => l.Hub.Name));
    }

    [Fact]
    public void EmptyKindOmitted()
    {
        var from = new SurfacePoint(10, 10);
        var links = NearestHubFinder.Nearest(from, [H(HubKind.Port, "P", 10, 11)]);
        Assert.Equal([HubKind.Port], links.Select(l => l.Hub.Kind));
        Assert.Empty(NearestHubFinder.Nearest(from, []));
    }

    [Fact]
    public void DistanceMatchesGeoMath()
    {
        Assert.Equal(6371.0, NearestHubFinder.EarthRadiusKm);
        var from = new SurfacePoint(15.6, 32.5);
        var hub = H(HubKind.Airport, "A", 19.4, 37.2);
        var link = Assert.Single(NearestHubFinder.Nearest(from, [hub]));
        Assert.Equal(GeoMath.DistanceKm(from, hub.Position, 6371.0), link.DistanceKm);
        Assert.Same(hub, link.Hub);
    }
}
