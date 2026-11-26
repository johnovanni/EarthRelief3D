using System.Windows;
using System.Windows.Media.Media3D;
using EarthRelief3D.App.Layers;
using GlobeCore.Bodies;
using AstroCore.Earth;
using GlobeCore.Wpf.Camera;
using GlobeCore.Wpf.Rendering;

namespace EarthRelief3D.Tests;

/// <summary>The hub arcs' screen geometry (ruling H1): hidden parts cut, arcs that run together drawn side by side.</summary>
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
        var r = HubArcScreen.SideBySide([A, D]);
        Assert.Equal(A, r[0]);
        Assert.Equal(D, r[1]);
    }

    /// <summary>The smallest distance from any point of <paramref name="line"/> to <paramref name="other"/>.</summary>
    private static double Gap(IReadOnlyList<Point> line, IReadOnlyList<Point> other) => line.Min(p => HubArcScreen.Distance(p, other));

    [Fact]
    public void TwoArcsSplitApart()
    {
        // With y down the normal of +x is (0, 1): B (y = 2) is on the + side of A. Each moves half the way, only where they are close.
        var r = HubArcScreen.SideBySide([A, B]);
        Assert.True(Gap(r[1], r[0]) >= HubArcScreen.Spacing - 0.1, $"gap {Gap(r[1], r[0]):F2}");
        Assert.All(r[1], p => Assert.True(p.Y > 2));
        Assert.All(r[0].Zip(A), x => Assert.True((x.First - x.Second).Length <= HubArcScreen.Spacing / 2 + 0.1));
        Assert.Equal(A[^1], r[0][^1]);   // beyond B's end A is on its true path
    }

    [Fact]
    public void ThreeArcsFanInOrder()
    {
        var r = HubArcScreen.SideBySide([A, B, E]);
        Assert.True(Gap(r[1], r[0]) >= HubArcScreen.Spacing - 0.15, $"B to A {Gap(r[1], r[0]):F2}");
        Assert.True(Gap(r[2], r[0]) >= HubArcScreen.Spacing - 0.15, $"E to A {Gap(r[2], r[0]):F2}");
        for (int k = 2; k < 8; k++)
        {
            Assert.True(r[1][k].Y > 0, "B stays on the + side");
            Assert.True(r[2][k].Y < 0, "E stays on the - side");
        }
    }

    [Fact]
    public void CrossingLinesKeepApart()
    {
        // Final review I1: on screen the port line can start 1-2 px on one side of the depot line and leave on the other; a
        // shift of the whole line pushed them together. They are pushed apart where they are close, on the side that needs least.
        var a = Line(new(0, 0), new(200, 0), 21);
        List<Point> f = [new(0, 0), new(10, -0.9), new(20, -1.6), new(30, -1.7), new(40, -1.0), new(50, 0.7), new(60, 3.3),
            new(70, 6.9), new(80, 11.1), new(90, 15.5)];
        Assert.True(HubArcScreen.RunTogether(a, f));
        var r = HubArcScreen.SideBySide([a, f]);
        Assert.True(Gap(r[1], r[0]) >= HubArcLayer.WidthPx, $"gap {Gap(r[1], r[0]):F2}");
        Assert.True((r[1][^1] - f[^1]).Length < 0.5, "the far end stays on its true path");
        Assert.All(r[1].Zip(f), x => Assert.True((x.First - x.Second).Length <= 3.0, $"moved {(x.First - x.Second).Length:F2}"));
    }

    [Fact]
    public void TieBreakIsTheGroundBearing()
    {
        // Final review I1: when neither side needs clearly less pushing, the bearing on the ground decides (clockwise: the +
        // side), which does not change as the globe turns.
        var g = Line(new(0, 0), new(80, 0.4));
        Assert.True(HubArcScreen.SideBySide([A, g], [90.0, 80.0])[1][5].Y < 0);
        Assert.True(HubArcScreen.SideBySide([A, g], [90.0, 100.0])[1][5].Y > 0);
        Assert.True(HubArcScreen.SideBySide([A, g], [359.0, 1.0])[1][5].Y > 0);   // across north
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
        var each = HubArcScreen.Shift(A, [.. Enumerable.Range(0, A.Count).Select(i => (double)i)]);
        for (int i = 0; i < A.Count; i++) Assert.Equal(i, each[i].Y, 9);
    }

    [Fact]
    public void DrawOrderLongestFirst()
    {
        Assert.Equal([1, 0, 2], HubArcScreen.DrawOrder([B, A, D]));
    }
}
