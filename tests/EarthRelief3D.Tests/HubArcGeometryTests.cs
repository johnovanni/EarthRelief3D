using System.Windows.Media.Media3D;
using EarthRelief3D.App.Layers;
using EarthRelief3D.Core.Feeds;
using GlobeCore.Bodies;

namespace EarthRelief3D.Tests;

/// <summary>Plan 4 Task 5: the arcs from a country to its nearest hubs (P4-7; Review Focus 4: across the dateline).</summary>
public class HubArcGeometryTests
{
    private static double AngleDeg(Point3D a, Point3D b)
    {
        var u = (Vector3D)a; var v = (Vector3D)b;
        u.Normalize(); v.Normalize();
        return Math.Acos(Math.Clamp(Vector3D.DotProduct(u, v), -1, 1)) * 180 / Math.PI;
    }

    private static Point3D Unit(SurfacePoint p) => MarkerMeshes.Above(p, 1.0);

    [Fact]
    public void EndsAtBothPoints()
    {
        var from = new SurfacePoint(15.6, 32.5); var to = new SurfacePoint(19.4, 37.2);
        var path = HubArcLayer.ArcPoints(from, to);
        Assert.True(path.Count >= 3);
        Assert.Equal(0, (path[0] - Unit(from)).Length, 9);   // on the surface: the lift is 0 at both ends
        Assert.Equal(0, (path[^1] - Unit(to)).Length, 9);
    }

    [Fact]
    public void DateLineArcIsContinuous()
    {
        var path = HubArcLayer.ArcPoints(new SurfacePoint(-17.7, 178.0), new SurfacePoint(-17.7, -178.0));
        for (int i = 1; i < path.Count; i++)
            Assert.True(AngleDeg(path[i - 1], path[i]) <= 1.5, $"step {i}: {AngleDeg(path[i - 1], path[i])} degrees");
        // It goes the short way, through 180 (x < 0 all along), not the long way round the globe.
        Assert.All(path, p => Assert.True(p.X < -0.9, $"x = {p.X}"));
        Assert.True(path.Count < 10, "a 3.8 degree arc is a handful of points");
    }

    [Fact]
    public void LiftedInTheMiddle()
    {
        // 10 degrees: h = 0.25 x 0.1745 rad = 0.0436.
        var path = HubArcLayer.ArcPoints(new SurfacePoint(0, 0), new SurfacePoint(0, 10));
        double mid = ((Vector3D)path[path.Count / 2]).Length;
        Assert.Equal(1 + 0.25 * 10 * Math.PI / 180, mid, 3);
        Assert.All(path, p => Assert.InRange(((Vector3D)p).Length, 1 - 1e-9, 1.0437));
        // Never lifted above 0.08, however far.
        var far = HubArcLayer.ArcPoints(new SurfacePoint(0, 0), new SurfacePoint(0, 120));
        Assert.Equal(1.08, far.Max(p => ((Vector3D)p).Length), 3);
    }

    [Fact]
    public void KindsLiftedDifferently()
    {
        // P5-14: each hub kind has its own lift factor, so arcs that leave in nearly the same direction do not lie on top of each other.
        Assert.Equal(2.0, HubArcLayer.LiftFactor(EarthRelief3D.Core.Logic.HubKind.Port));
        Assert.Equal(1.25, HubArcLayer.LiftFactor(EarthRelief3D.Core.Logic.HubKind.Airport));
        Assert.Equal(0.5, HubArcLayer.LiftFactor(EarthRelief3D.Core.Logic.HubKind.UnDepot));
        var from = new SurfacePoint(0, 0); var to = new SurfacePoint(0, 6);   // 6 degrees: 0.0262 x 2 is below the cap
        double Mid(EarthRelief3D.Core.Logic.HubKind k)
        {
            var path = HubArcLayer.ArcPoints(from, to, HubArcLayer.LiftFactor(k));
            return ((Vector3D)path[path.Count / 2]).Length - 1;
        }
        double depot = Mid(EarthRelief3D.Core.Logic.HubKind.UnDepot);
        Assert.Equal(4.0, Mid(EarthRelief3D.Core.Logic.HubKind.Port) / depot, 3);
        Assert.Equal(2.5, Mid(EarthRelief3D.Core.Logic.HubKind.Airport) / depot, 3);
        // The cap is scaled too.
        var far = HubArcLayer.ArcPoints(new SurfacePoint(0, 0), new SurfacePoint(0, 120), 2.0);
        Assert.Equal(1 + 0.08 * 2.0, far.Max(p => ((Vector3D)p).Length), 3);
    }

    [Fact]
    public void PortArcAboveDepotArcFromSudan()
    {
        // P5-14: Sudan's label point to Port Sudan (918 km) left in almost the direction of the arc to the Dubai depot and lay under it.
        var sudan = CountryResolver.LoadDefault().ByIso3("SDN")!.LabelPoint;
        var port = HubArcLayer.ArcPoints(sudan, new SurfacePoint(19.6, 37.2), HubArcLayer.LiftFactor(EarthRelief3D.Core.Logic.HubKind.Port));
        var depot = HubArcLayer.ArcPoints(sudan, new SurfacePoint(24.896171, 55.16235), HubArcLayer.LiftFactor(EarthRelief3D.Core.Logic.HubKind.UnDepot));
        var start = (Vector3D)port[0];
        int checkedPoints = 0;
        for (int i = 1; i <= port.Count * 3 / 4; i++)   // the first picture run showed the port arc hidden after a third; now above for the first three quarters
        {
            double angle = AngleDeg(port[0], port[i]);
            // The depot path's point at the same central angle from the start.
            int j = Enumerable.Range(0, depot.Count).MinBy(k => Math.Abs(AngleDeg(depot[0], depot[k]) - angle));
            Assert.True(((Vector3D)port[i]).Length > ((Vector3D)depot[j]).Length,
                $"at {angle:F2} degrees the port arc ({((Vector3D)port[i]).Length:F5}) is not above the depot arc ({((Vector3D)depot[j]).Length:F5})");
            checkedPoints++;
        }
        Assert.True(checkedPoints >= 3);
        Assert.True(start.Length > 0);
    }

    [Fact]
    public void ArcsAreWideAndAboveTheMarkers()
    {
        // Final review F2: about 3 px wide, and lifted above the highest point-marker layer so no marker mesh hides a ribbon's ends.
        Assert.Equal(3, HubArcLayer.WidthPx);
        double highest = Enum.GetValues<EarthRelief3D.App.Presentation.MarkerLayer>()
            .Max(l => PointMarkerLayer.BaseHeight + (int)l * PointMarkerLayer.LayerLift);
        Assert.True(HubArcLayer.SurfaceClearance > highest, $"clearance {HubArcLayer.SurfaceClearance} is not above the markers at {highest}");
        Assert.True(HubArcLayer.SurfaceClearance < 1.02, "the ends still sit on the globe");
    }

    [Fact]
    public void ArcLabelsAreNeverDroppedForOverlapping() => StaAsync.Sta(() =>
    {
        // Final review F2 (m24): three labels at almost the same point are all placed (moved apart, or forced), never dropped.
        var canvas = new System.Windows.Controls.Canvas();
        canvas.Measure(new System.Windows.Size(800, 600));
        canvas.Arrange(new System.Windows.Rect(0, 0, 800, 600));
        var labels = new GlobeCore.Wpf.Rendering.LabelOverlay(canvas);
        var at = new System.Windows.Point(300, 300);
        var colour = System.Windows.Media.Colors.White;
        labels.Begin();
        int placed = HubLabelsLayer.Place(labels, [("UNHRD Dubai · 2,842 km straight-line", at, colour),
            ("Dongola Airport (DOG) · 310 km straight-line", at, colour), ("Port Sudan (SD PZU) · 700 km straight-line", new System.Windows.Point(302, 301), colour)]);
        labels.End();
        Assert.Equal(3, placed);
        var tops = canvas.Children.OfType<System.Windows.Controls.TextBlock>().Where(t => t.Visibility == System.Windows.Visibility.Visible)
            .Select(System.Windows.Controls.Canvas.GetTop).ToList();
        Assert.Equal(3, tops.Count);
        Assert.Equal(3, tops.Distinct().Count());   // moved apart, not stacked on one line
    });

    [Fact]
    public void RibbonIsAStripOfTheGivenWidth() => StaAsync.Sta(() =>
    {
        var path = HubArcLayer.ArcPoints(new SurfacePoint(0, 0), new SurfacePoint(0, 10));
        var mesh = HubArcLayer.Ribbon(path, 0.004);
        Assert.Equal(2 * path.Count, mesh.Positions.Count);
        Assert.Equal(6 * (path.Count - 1), mesh.TriangleIndices.Count);
        Assert.Equal(0.004, (mesh.Positions[0] - mesh.Positions[1]).Length, 6);
        // Counter-clockwise seen from outside: every triangle's normal points away from the globe's centre.
        for (int i = 0; i < mesh.TriangleIndices.Count; i += 3)
        {
            var a = mesh.Positions[mesh.TriangleIndices[i]]; var b = mesh.Positions[mesh.TriangleIndices[i + 1]]; var c = mesh.Positions[mesh.TriangleIndices[i + 2]];
            var normal = Vector3D.CrossProduct(b - a, c - a);
            Assert.True(Vector3D.DotProduct(normal, (Vector3D)a) > 0, $"triangle {i / 3} faces inwards");
        }
    });
}
