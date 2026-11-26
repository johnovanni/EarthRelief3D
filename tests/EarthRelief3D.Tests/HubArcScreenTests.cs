using System.Windows;
using System.Windows.Media.Media3D;
using EarthRelief3D.App.Layers;
using GlobeCore.Bodies;
using AstroCore.Earth;
using GlobeCore.Wpf.Camera;
using GlobeCore.Wpf.Rendering;

namespace EarthRelief3D.Tests;

/// <summary>The hub arcs' screen geometry (ruling R18): hidden parts cut, arcs that run together drawn side by side.</summary>
public class HubArcScreenTests
{
    /// <summary>A projector for a camera looking straight down on <paramref name="at"/> from <paramref name="distance"/> Earth radii, 45 degrees wide, as the app's.</summary>
    internal static Projector LookingDownOn(GeoPosition at, double distance, double width, double height)
    {
        Projector result = default;
        StaAsync.Sta(() =>
        {
            var (p, l, u) = CameraRules.LookDownOn(at, distance);
            result = new Projector(new PerspectiveCamera(p, l, u, 45), width, height);
        });
        return result;
    }

    private static List<Point> Line(Point a, Point b, int n = 10) =>
        [.. Enumerable.Range(0, n).Select(i => new Point(a.X + (b.X - a.X) * i / (n - 1), a.Y + (b.Y - a.Y) * i / (n - 1)))];

    private static readonly List<Point> A = Line(new(0, 0), new(100, 0));
    private static readonly List<Point> B = Line(new(0, 2), new(60, 2));
    private static readonly List<Point> C = Line(new(0, 0), new(0, 100));
    private static readonly List<Point> D = Line(new(0, 0), new(8, 0));
    private static readonly List<Point> E = Line(new(0, -2), new(70, -2));

    [Fact]
    public void IsVisibleFrontAndBack()
    {
        var eye = new Point3D(0, 0, 3);
        Assert.True(HubArcScreen.IsVisible(new Point3D(0, 0, 1.01), eye));
        Assert.False(HubArcScreen.IsVisible(new Point3D(0, 0, -1.01), eye));
        Assert.True(HubArcScreen.IsVisible(new Point3D(0.5, 0, 0.88), eye));
        Assert.True(HubArcScreen.IsVisible(new Point3D(0.9, 0, 0.45), eye));      // 63 degrees out: the limb from 3 radii is at 70.5
        Assert.False(HubArcScreen.IsVisible(new Point3D(1.01, 0, 0), eye));        // 90 degrees out: behind the limb
        Assert.False(HubArcScreen.IsVisible(new Point3D(0.3, 0, -0.96), eye));
    }

    [Fact]
    public void VisibleRunsStopAtTheEdge()
    {
        var projector = LookingDownOn(new GeoPosition(0, 0), 3, 1000, 800);
        var path = HubArcLayer.ArcPoints(new SurfacePoint(0, 60), new SurfacePoint(0, 120));
        var runs = HubArcScreen.VisibleRuns(path, projector);
        Assert.Single(runs);
        Assert.True(runs[0].Count < path.Count, "the part behind the limb is cut");
        var visible = path.Where(p => HubArcScreen.IsVisible(p, projector.Eye)).ToList();
        Assert.Equal(visible.Count, runs[0].Count);
        for (int i = 0; i < visible.Count; i++)
        {
            Assert.True(projector.TryProject(visible[i], out var s));
            Assert.Equal(s, runs[0][i]);
        }
    }

    [Fact]
    public void ParallelLinesRunTogether()
    {
        Assert.True(HubArcScreen.RunTogether(A, B));
        Assert.True(HubArcScreen.RunTogether(B, A));
        Assert.False(HubArcScreen.RunTogether(A, C));
    }

    [Fact]
    public void TinyArcIsNotBundled()
    {
        Assert.False(HubArcScreen.RunTogether(A, D));
        Assert.Equal([0.0, 0.0], HubArcScreen.SideOffsets([A, D]));
    }

    [Fact]
    public void TwoArcsSplitApart()
    {
        // With y down the normal of +x is (0, 1): B (y = 2) is on the + side of A.
        Assert.Equal([-2.0, 2.0], HubArcScreen.SideOffsets([A, B]));
    }

    [Fact]
    public void ThreeArcsFanInOrder()
    {
        Assert.Equal([0.0, 4.0, -4.0], HubArcScreen.SideOffsets([A, B, E]));
    }

    [Fact]
    public void ShiftMovesAlongTheNormal()
    {
        var shifted = HubArcScreen.Shift(A, 2);
        Assert.Equal(A.Count, shifted.Count);
        for (int i = 0; i < A.Count; i++)
        {
            Assert.Equal(A[i].X, shifted[i].X, 9);
            Assert.Equal(2, shifted[i].Y, 9);
        }
    }

    [Fact]
    public void DrawOrderLongestFirst()
    {
        Assert.Equal([1, 0, 2], HubArcScreen.DrawOrder([B, A, D]));
    }
}
