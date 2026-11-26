using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using AstroCore.Earth;
using EarthRelief3D.App.Layers;
using EarthRelief3D.Core.Feeds;
using EarthRelief3D.App.Presentation;
using EarthRelief3D.Core.Logic;
using EarthRelief3D.Core.Model;
using GlobeCore.Bodies;
using GlobeCore.Wpf.Camera;
using GlobeCore.Wpf.Rendering;

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
    public void OneLiftForEveryKind()
    {
        // H1 withdraws P5-14 and R14: every kind of hub has the same gentle rise (6 degrees: 0.25 x 0.1047 rad = 0.0262).
        var path = HubArcLayer.ArcPoints(new SurfacePoint(0, 0), new SurfacePoint(0, 6));
        Assert.Equal(1 + 0.25 * 6 * Math.PI / 180, ((Vector3D)path[path.Count / 2]).Length, 3);
    }

    [Fact]
    public void ArcsAboveTheMarkers()
    {
        // Final review F2: about 3 px wide and above every marker; H1: drawn on the overlay, under the labels.
        Assert.Equal(3, HubArcLayer.WidthPx);
        Assert.Equal(5, HubArcLayer.EdgeWidthPx);
        Assert.True(typeof(GlobeCore.Wpf.Layers.OverlayLayer).IsAssignableFrom(typeof(HubArcLayer)));
        Assert.All(Enum.GetValues<MarkerLayer>(), l => Assert.True(PointMarkerLayer.OrderOf(l) < HubArcLayer.Order));
        Assert.True(HubArcLayer.Order < LabelsOverlayLayer.Order);
    }

    // ---------------------------------------------------------------- sudan-help (owner item 15, H1)

    private static readonly DateTime Now = new(2026, 11, 22, 12, 0, 0, DateTimeKind.Utc);

    private static HubArcSpec Spec(HubKind kind, string name, double lat, double lon)
    {
        var hub = new Hub(kind, name, "XXX", new SurfacePoint(lat, lon), "",
            new SourceCitation("test", "x", null, new Uri("https://example.org"), "x", "x", Now, Now));
        return new HubArcSpec(new HubLink(hub, 0), CountryResolver.LoadDefault().ByIso3("SDN")!.LabelPoint);
    }

    private static readonly HubArcSpec PortSudan = Spec(HubKind.Port, "Port Sudan", 19.6, 37.2);
    private static readonly HubArcSpec Dubai = Spec(HubKind.UnDepot, "UNHRD Dubai", 24.896171, 55.16235);
    private static readonly HubArcSpec Dongola = Spec(HubKind.Airport, "Dongola Airport", 19.15, 30.43);

    /// <summary>The camera of the sudan-help picture: straight down on 15 N 35 E at the fitted distance for a 1482 x 903 window.</summary>
    private static Projector SudanHelpCamera() => HubArcScreenTests.LookingDownOn(new GeoPosition(15, 35),
        CameraRules.FitDistance(45, 1482.0 / 903, CameraLimits.EarthDefault), 1482, 903);

    [Fact]
    public void PortArcBesideDepotArcInSudanHelp()
    {
        var projector = SudanHelpCamera();
        var lines = HubArcLayer.Lines([Dubai, PortSudan, Dongola], projector);
        var port = lines.Single(l => l.Spec == PortSudan).Line;
        var depot = lines.Single(l => l.Spec == Dubai).Line;
        var airport = lines.Single(l => l.Spec == Dongola).Line;
        // What the camera sees: the port line never lies on the depot line, from end to end: their 3 px coloured lines never
        // overlap (ruling H3), and the port line, drawn after it, is on top with its own dark edge between them.
        foreach (var p in port)
            Assert.True(HubArcScreen.Distance(p, depot) >= HubArcLayer.WidthPx, $"port point {p} is {HubArcScreen.Distance(p, depot):F2} px from the depot line");
        // It still ends at Port Sudan, and the airport line, leaving in another direction, is not moved.
        Assert.True(projector.TryProject(MarkerMeshes.Above(new SurfacePoint(19.6, 37.2), 1.0), out var portSudan));
        Assert.True((port[^1] - portSudan).Length <= 3, $"the port line ends {(port[^1] - portSudan).Length:F2} px from Port Sudan");
        Assert.True(projector.TryProject(MarkerMeshes.Above(PortSudan.From, 1.0), out var start));
        Assert.True((airport[0] - start).Length < 0.01);
        // Drawn longest first: the short port line on top of the depot line.
        Assert.True(lines.ToList().FindIndex(l => l.Spec == Dubai) < lines.ToList().FindIndex(l => l.Spec == PortSudan));
    }

    private static Canvas RenderInto(HubArcLayer layer, Canvas? canvas = null)
    {
        canvas ??= new Canvas();
        layer.Render(canvas, SudanHelpCamera());
        return canvas;
    }

    [Fact]
    public void ArcsDrawUnderLabels() => StaAsync.Sta(() =>
    {
        var layer = new HubArcLayer(() => [Dubai, PortSudan, Dongola]);
        var canvas = RenderInto(layer);
        var shapes = canvas.Children.OfType<Polyline>().Where(p => p.Visibility == Visibility.Visible).ToList();
        Assert.Equal(6, shapes.Count);
        Assert.Equal(3, layer.ArcCount);
        Assert.All(shapes, s => Assert.True(Panel.GetZIndex(s) < 0, "an arc would draw over a label"));
        for (int i = 0; i < shapes.Count; i += 2)
        {
            var edge = shapes[i]; var line = shapes[i + 1];
            Assert.Equal(5, edge.StrokeThickness);
            Assert.Equal(LayerStyle.MarkerEdgeColor, ((SolidColorBrush)edge.Stroke).Color);
            Assert.Equal(3, line.StrokeThickness);
            Assert.Contains(((SolidColorBrush)line.Stroke).Color, new[] { LayerStyle.UnDepotColor, LayerStyle.PortColor, LayerStyle.AirportColor });
            Assert.True(canvas.Children.IndexOf(edge) < canvas.Children.IndexOf(line), "the edge is under its line");
        }
        // The depot (longest) is drawn first, the port last of the two.
        var colours = shapes.Where((_, i) => i % 2 == 1).Select(s => ((SolidColorBrush)s.Stroke).Color).ToList();
        Assert.True(colours.IndexOf(LayerStyle.UnDepotColor) < colours.IndexOf(LayerStyle.PortColor));
    });

    [Fact]
    public void ClearsWhenNoArcs() => StaAsync.Sta(() =>
    {
        IReadOnlyList<HubArcSpec> specs = [Dubai, PortSudan, Dongola];
        var layer = new HubArcLayer(() => specs);
        var canvas = RenderInto(layer);
        specs = [];
        layer.Changed();
        RenderInto(layer, canvas);
        Assert.DoesNotContain(canvas.Children.OfType<Polyline>(), p => p.Visibility == Visibility.Visible);
        Assert.Equal(0, layer.ArcCount);
    });
}
