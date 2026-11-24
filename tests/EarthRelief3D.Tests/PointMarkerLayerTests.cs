using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using AstroCore.Earth;
using EarthRelief3D.App.Layers;
using EarthRelief3D.App.Presentation;
using GlobeCore.Wpf.Layers;
using GlobeCore.Wpf.Rendering;

namespace EarthRelief3D.Tests;

/// <summary>The generic point-marker layer (P3-9): one batched mesh, at most 5,000 markers, one pick per marker.</summary>
public class PointMarkerLayerTests
{
    // Facing 0°N 0°E from 3 Earth radii, as the scene tests.
    private static (SceneContext Ctx, Projector Projector) View()
    {
        var camera = new PerspectiveCamera(new Point3D(3, 0, 0), new Vector3D(-3, 0, 0), new Vector3D(0, 0, 1), 45);
        var projector = new Projector(camera, 800, 600);
        return (new SceneContext(camera, projector, TestData.T), projector);
    }

    private static PointItem Item(double lat, double lon, string tip, string? iso = null, double size = 4) =>
        new(new GeoPosition(lat, lon), size, tip, iso);

    [Fact]
    public void CapsAt5000AndSaysSo() => StaAsync.Sta(() =>
    {
        var items = Enumerable.Range(0, PointMarkerLayer.MaxMarkers + 1)
            .Select(i => Item(-60 + i % 120, -170 + i / 120 * 8, $"plant {i}")).ToList();
        var log = new ListLog();
        var layer = new PointMarkerLayer(MarkerLayer.PowerPlants, () => items, log);
        var (ctx, _) = View();

        layer.Update(ctx);

        Assert.Equal(5000, PointMarkerLayer.MaxMarkers);
        Assert.Equal(5000, layer.MarkerCount);
        Assert.Equal(1, layer.Dropped);
        var error = Assert.Single(log.Lines, l => l.StartsWith("ERROR", StringComparison.Ordinal));
        Assert.Contains("Power plants: 5,001 markers; only the first 5,000 are drawn", error);
        // Batched: one mesh for the power plants' dark edges and one for the fills of the layer's one colour, not one per marker.
        var group = Assert.IsType<Model3DGroup>(layer.Root.Content);
        Assert.Equal(2, group.Children.Count);
        var fills = Assert.IsType<GeometryModel3D>(group.Children[1]);
        Assert.True(((MeshGeometry3D)fills.Geometry).Positions.Count > 5000);

        // A rebuild for the zoom does not log again; new data does.
        layer.DataChanged();
        layer.Update(ctx);
        Assert.Equal(2, log.Lines.Count(l => l.StartsWith("ERROR", StringComparison.Ordinal)));
    });

    [Fact]
    public void OnePickPerMarker() => StaAsync.Sta(() =>
    {
        var items = new[] { Item(0, 0, "A", "NGA"), Item(0, 6, "B", "CMR"), Item(6, 0, "C") };
        var layer = new PointMarkerLayer(MarkerLayer.Camps, () => items, new ListLog());
        var (ctx, projector) = View();
        layer.Update(ctx);
        Assert.Equal(3, layer.MarkerCount);

        foreach (var item in items)
        {
            Assert.True(projector.TryProject(MarkerMeshes.Above(item.Position, 1.0), out var at));
            var hit = layer.HitTest(at, projector, hover: true);
            Assert.NotNull(hit);
            Assert.Equal("camps", hit.LayerId);
            Assert.Equal(item.Tooltip, hit.Tooltip);
            var pick = Assert.IsType<PointPick>(hit.Item);
            Assert.Same(item, pick.Point);
            Assert.Equal(MarkerLayer.Camps, pick.Layer);
        }
        // Between the markers nothing is picked.
        Assert.True(projector.TryProject(MarkerMeshes.Above(new GeoPosition(3, 3), 1.0), out var between));
        Assert.Null(layer.HitTest(between, projector, hover: true));
        // A marker on the far side is not picked.
        var far = new PointMarkerLayer(MarkerLayer.Camps, () => [Item(0, 180, "far")], new ListLog());
        far.Update(ctx);
        Assert.Null(far.HitTest(new Point(400, 300), projector, hover: false));
    });

    [Fact]
    public void EachLayerDrawsItsOwnShapeAndColour() => StaAsync.Sta(() =>
    {
        var (ctx, _) = View();
        Color? Drawn(MarkerLayer which, PointItem item)
        {
            var layer = new PointMarkerLayer(which, () => [item], new ListLog());
            layer.Update(ctx);
            var group = (Model3DGroup)layer.Root.Content;
            return ((SolidColorBrush)((DiffuseMaterial)((GeometryModel3D)group.Children[^1]).Material).Brush).Color;
        }
        Assert.Equal(LayerStyle.SymbolColor(MarkerLayer.Camps), Drawn(MarkerLayer.Camps, Item(0, 0, "x")));
        // An item may carry its own colour (power plants by fuel group, Task 7).
        var coal = Color.FromRgb(0x40, 0x40, 0x40);
        Assert.Equal(coal, Drawn(MarkerLayer.PowerPlants, Item(0, 0, "x") with { Color = coal }));
        Assert.Equal("steel", new PointMarkerLayer(MarkerLayer.SteelPlants, () => [], new ListLog()).Id);
    });

    [Fact]
    public void NoDataDrawsNothing() => StaAsync.Sta(() =>
    {
        var layer = new PointMarkerLayer(MarkerLayer.OilGasFields, () => [], new ListLog());
        var (ctx, projector) = View();
        layer.Update(ctx);
        Assert.Equal(0, layer.MarkerCount);
        Assert.Empty(((Model3DGroup)layer.Root.Content).Children);
        Assert.Null(layer.HitTest(new Point(400, 300), projector, hover: true));
    });

    // ---------------------------------------------------------------- Plan 4 Task 4: the hub layers' shapes (P4-14)

    [Fact]
    public void TheHubLayersAreDrawnWithTheirOwnShapes() => StaAsync.Sta(() =>
    {
        Assert.Equal([MarkerLayer.Camps, MarkerLayer.PowerPlants, MarkerLayer.OilGasFields, MarkerLayer.SteelPlants, MarkerLayer.Renewables,
            MarkerLayer.UnDepots, MarkerLayer.Airports, MarkerLayer.Ports], PointMarkerLayer.Layers);
        var (ctx, projector) = View();
        foreach (var which in new[] { MarkerLayer.UnDepots, MarkerLayer.Airports, MarkerLayer.Ports })
        {
            var items = new[] { Item(0, 0, "A", "NGA", 6), Item(0, 6, "B", "CMR") };
            var layer = new PointMarkerLayer(which, () => items, new ListLog());
            layer.Update(ctx);
            Assert.Equal(2, layer.MarkerCount);
            Assert.Equal(PointMarkerLayer.OrderOf(which), layer.ZOrder);
            Assert.InRange(layer.ZOrder, 130, 199);   // before the labels (200)
            // Drawn in the layer's colour only (no dark edge), one mesh; and each marker is one pick.
            var group = Assert.IsType<Model3DGroup>(layer.Root.Content);
            Assert.Single(group.Children);
            Assert.Equal(LayerStyle.SymbolColor(which), ((SolidColorBrush)((DiffuseMaterial)((GeometryModel3D)group.Children[0]).Material).Brush).Color);
            foreach (var item in items)
            {
                Assert.True(projector.TryProject(MarkerMeshes.Above(item.Position, 1.0), out var at));
                var hit = Assert.IsType<PointPick>(layer.HitTest(at, projector, hover: false)!.Item);
                Assert.Equal((which, item), (hit.Layer, hit.Point));
            }
        }
    });

    [Fact]
    public void StarAndOutlinedHexagonAndDiamondMeshes() => StaAsync.Sta(() =>
    {
        var at = new GeoPosition(0, 0);
        static double Off(Point3D p) => Math.Sqrt(p.Y * p.Y + p.Z * p.Z) / ((Vector3D)p).Length;
        var star = MarkerMeshes.Build(m => m.AddStar(at, 0.01, 1.002));
        var hex = MarkerMeshes.Build(m => m.AddHexagonOutline(at, 0.01, 0.002, 1.002));
        var diamond = MarkerMeshes.Build(m => m.AddDiamondOutline(at, 0.01, 0.002, 1.002));
        // A star: a centre and ten points (five tips, five notches), ten triangles; its tips reach the radius, its notches do not.
        Assert.Equal(11, star.Positions.Count);
        Assert.Equal(30, star.TriangleIndices.Count);
        Assert.Equal(0.01, star.Positions.Skip(1).Max(Off), 3);
        Assert.InRange(star.Positions.Skip(1).Min(Off), 0.003, 0.005);
        Assert.Equal(5, star.Positions.Skip(1).Count(p => Off(p) > 0.0095));
        // A diamond outline: four outer and four inner corners, eight triangles, a hole in the middle (no triangle at the centre).
        Assert.Equal(8, diamond.Positions.Count);
        Assert.Equal(24, diamond.TriangleIndices.Count);
        Assert.Equal(0.01, diamond.Positions.Max(Off), 3);
        Assert.True(diamond.Positions.Min(Off) > 0.004);
        Assert.Equal(12, hex.Positions.Count);
        // All counter-clockwise seen from outside, so the far side of the globe is culled.
        foreach (var mesh in new[] { star, hex, diamond })
            for (int i = 0; i < mesh.TriangleIndices.Count; i += 3)
            {
                var a = mesh.Positions[mesh.TriangleIndices[i]];
                var b = mesh.Positions[mesh.TriangleIndices[i + 1]];
                var c = mesh.Positions[mesh.TriangleIndices[i + 2]];
                Assert.True(Vector3D.DotProduct(Vector3D.CrossProduct(b - a, c - a), (Vector3D)a) > 0, "a triangle faces inward");
            }
    });
}
