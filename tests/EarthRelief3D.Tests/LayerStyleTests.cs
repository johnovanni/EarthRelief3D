using System.IO;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using AstroCore.Earth;
using EarthRelief3D.App.Layers;
using EarthRelief3D.App.Presentation;
using EarthRelief3D.Core.Model;

namespace EarthRelief3D.Tests;

public class LayerStyleTests
{
    [Fact]
    public void IpcPhaseColoursAreTheIpcPublishedValues()
    {
        // IPC Acute Food Insecurity phase colours (IPC Icon Guidelines 2023 / IPC Mapping Guidelines; see LayerStyle).
        Assert.Equal(Color.FromRgb(0xCD, 0xFA, 0xCD), LayerStyle.IpcPhaseColor(1));
        Assert.Equal(Color.FromRgb(0xFA, 0xE6, 0x1E), LayerStyle.IpcPhaseColor(2));
        Assert.Equal(Color.FromRgb(0xE6, 0x78, 0x00), LayerStyle.IpcPhaseColor(3));
        Assert.Equal(Color.FromRgb(0xC8, 0x00, 0x00), LayerStyle.IpcPhaseColor(4));
        Assert.Equal(Color.FromRgb(0x64, 0x00, 0x00), LayerStyle.IpcPhaseColor(5));
        Assert.Throws<ArgumentOutOfRangeException>(() => LayerStyle.IpcPhaseColor(6));
        Assert.Equal(["Minimal", "Stressed", "Crisis", "Emergency", "Catastrophe/Famine"], Enumerable.Range(1, 5).Select(LayerStyle.IpcPhaseName));
        Assert.Equal("Phase 5 Catastrophe/Famine", LayerStyle.IpcPhaseLabel(5));
    }

    [Fact]
    public void IpcColoursAreRecordedWithTheirSourceUrl()
    {
        var source = File.ReadAllText(Path.Combine(Repo.Root(), "src", "EarthRelief3D.App", "Presentation", "LayerStyle.cs"));
        Assert.Contains("https://www.ipcinfo.org/", source);
        Assert.Contains("https://help.fews.net/brand/data-visualization-colors", source);
        Assert.DoesNotContain(" .../", source);   // no elided URLs
        Assert.Contains("0xE6, 0x78, 0x00", source);
    }

    [Fact]
    public void EveryGlyphIsDistinctByShape()
    {
        var layers = Enum.GetValues<CrisisLayer>();
        Assert.Equal(layers.Length, layers.Select(LayerStyle.Shape).Distinct().Count());
        Assert.Equal(layers.Length, layers.Select(LayerStyle.Symbol).Distinct().Count());
        Assert.Equal(MarkerShape.Dot, LayerStyle.Shape(CrisisLayer.Conflict));
        Assert.Equal(MarkerShape.Area, LayerStyle.Shape(CrisisLayer.Food));
        Assert.Equal(MarkerShape.Cross, LayerStyle.Shape(CrisisLayer.Outbreaks));
        Assert.Equal(MarkerShape.Ring, LayerStyle.Shape(CrisisLayer.Displacement));
    }

    [Fact]
    public void MarkerMeshesHaveTheirShapes() => StaAsync.Sta(() =>
    {
        var at = new GeoPosition(0, 0);   // the +X axis: tangent plane is Y (east) and Z (north)
        var disc = MarkerMeshes.Build(m => m.AddDisc(at, 0.01, 1.002));
        var ring = MarkerMeshes.Build(m => m.AddRing(at, 0.05, 0.004, 1.003));
        var cross = MarkerMeshes.Build(m => m.AddCross(at, 0.02, 0.005, 1.004));
        // Distance from the marker's centre, as a fraction of the point's own radius (the marker lies on a sphere).
        static double Off(Point3D p) => Math.Sqrt(p.Y * p.Y + p.Z * p.Z) / ((Vector3D)p).Length;

        // A disc is filled: it has a point at its centre.
        Assert.Contains(disc.Positions, p => Off(p) < 1e-9);
        // A ring has a hole: nothing inside its inner edge.
        Assert.All(ring.Positions, p => Assert.InRange(Off(p), 0.05 - 0.002 - 5e-4, 0.05 + 0.002 + 5e-4));
        // Markers lie at their height above the surface.
        Assert.All(ring.Positions, p => Assert.Equal(1.003, ((Vector3D)p).Length, 9));
        // A cross reaches its arm length along the axes but not along the diagonals.
        Assert.Contains(cross.Positions, p => Math.Abs(p.Y) > 0.0199 && Math.Abs(p.Z) < 0.0051);
        Assert.DoesNotContain(cross.Positions, p => Math.Abs(p.Y) > 0.006 && Math.Abs(p.Z) > 0.006);
        // Every triangle faces outward (counter-clockwise seen from outside, WPF's front face).
        foreach (var mesh in new[] { disc, ring, cross })
            for (int i = 0; i < mesh.TriangleIndices.Count; i += 3)
            {
                var a = mesh.Positions[mesh.TriangleIndices[i]];
                var b = mesh.Positions[mesh.TriangleIndices[i + 1]];
                var c = mesh.Positions[mesh.TriangleIndices[i + 2]];
                var n = Vector3D.CrossProduct(b - a, c - a);
                Assert.True(n.X > 0, "triangle faces outward");
            }
    });

    // The food shading's plan (R17, which replaced "the highest phase with people") is tested in FoodMapTests.

    [Fact]
    public void ConflictDotsAreColouredByAgeAndSizedByBand()
    {
        var asOf = new DateTime(2026, 11, 6, 0, 0, 0, DateTimeKind.Utc);
        Assert.Equal(0, ConflictSceneLayer.AgeBucket(asOf.AddDays(-10), asOf));
        Assert.Equal(1, ConflictSceneLayer.AgeBucket(asOf.AddDays(-120), asOf));
        Assert.Equal(2, ConflictSceneLayer.AgeBucket(asOf.AddDays(-250), asOf));
        Assert.Equal(3, LayerStyle.ConflictAgeColors.Count);
        Assert.Equal(3, LayerStyle.ConflictAgeColors.Distinct().Count());
        double last = 0;
        for (int band = 1; band <= 4; band++)
        {
            Assert.True(ConflictSceneLayer.DotRadiusPx(band) > last);
            last = ConflictSceneLayer.DotRadiusPx(band);
        }
    }

    [Fact]
    public void DisplacementRingAreaIsProportionalToTotal()
    {
        double r1 = DisplacementSceneLayer.RadiusFor(1_000_000), r4 = DisplacementSceneLayer.RadiusFor(4_000_000);
        Assert.Equal(4.0, r4 * r4 / (r1 * r1), 6);
        Assert.Equal(0, DisplacementSceneLayer.RadiusFor(0));
    }
}
