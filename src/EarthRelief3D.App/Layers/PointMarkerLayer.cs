using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using AstroCore.Earth;
using EarthRelief3D.App.Presentation;
using GlobeCore.Logging;
using GlobeCore.Wpf.Layers;
using GlobeCore.Wpf.Rendering;

namespace EarthRelief3D.App.Layers;

/// <summary>One marker of a <see cref="PointMarkerLayer"/>.</summary>
/// <param name="Size">The marker's radius in screen pixels (kept as the globe is zoomed); 0 or less: <see cref="PointMarkerLayer.DefaultSizePx"/>.</param>
/// <param name="Tooltip">The hover text, with the source and the date (every fact on screen has both).</param>
/// <param name="CountryIso3">The country whose panel a click opens; null opens none.</param>
/// <param name="Color">The marker's own colour (power plants by fuel group, Task 7); null: the layer's colour.</param>
public sealed record PointItem(GeoPosition Position, double Size, string Tooltip, string? CountryIso3, Color? Color = null);

/// <summary>What a click or hover on a point marker found: the layer and the marker itself.</summary>
public sealed record PointPick(MarkerLayer Layer, PointItem Point);

/// <summary>
/// A marker layer drawn from a list of points (prison camps, power plants, oil and gas fields, steel plants): each point
/// in the layer's shape (C8, <see cref="LayerStyle.Shape(MarkerLayer)"/>), all of one colour in one batched mesh. At most
/// <see cref="MaxMarkers"/> are drawn (P3-9: the datasets are filtered to stay under it); more are cut off and the cut is
/// logged as an error. The points are read on the UI thread in <see cref="Update"/>; <see cref="DataChanged"/> (any thread)
/// asks for that. Sizes are kept in screen pixels, so the meshes are rebuilt when the zoom changes the size of a pixel by
/// more than <see cref="CrisisSceneLayer.RescaleRatio"/>.
/// </summary>
public sealed class PointMarkerLayer : SceneLayer, IPickable
{
    public const int MaxMarkers = 5000;
    public const double DefaultSizePx = 4;
    /// <summary>Above the outbreak crosses (1.004); each layer a little higher, so stacked layers don't z-fight.</summary>
    public const double BaseHeight = 1.005;
    public const double LayerLift = 0.0002;
    /// <summary>The width of outlines (square, triangle, ring) and of the power plants' dark edge, in screen pixels.</summary>
    public const double OutlinePx = 1.5;
    private const double PickSlopPx = 3;

    /// <summary>The marker layers the scene draws with this class (atrocities have their own glyph layer, Task 5).</summary>
    public static IReadOnlyList<MarkerLayer> Layers { get; } = [MarkerLayer.Camps, MarkerLayer.PowerPlants, MarkerLayer.OilGasFields, MarkerLayer.SteelPlants, MarkerLayer.Renewables];

    /// <summary>The layer's z-order: after the crisis markers (100 to 120), before the labels (200).</summary>
    public static int OrderOf(MarkerLayer layer) => 130 + (int)layer;

    private readonly Func<IReadOnlyList<PointItem>> _items;
    private readonly IAppLog _log;
    private readonly List<(Point3D Centre, double RadiusPx, PointItem Item)> _picks = [];
    private int _dataDirty = 1;
    private double _builtWorldPerPx = double.NaN;
    private IReadOnlyList<PointItem> _shown = [];

    public PointMarkerLayer(MarkerLayer which, Func<IReadOnlyList<PointItem>> items, IAppLog log)
        : base(LayerCatalog.Id(which), LayerCatalog.Label(which), OrderOf(which))
    {
        Which = which;
        _items = items ?? throw new ArgumentNullException(nameof(items));
        _log = log ?? throw new ArgumentNullException(nameof(log));
    }

    public MarkerLayer Which { get; }

    /// <summary>How many markers are drawn (tests, diagnostics).</summary>
    public int MarkerCount { get; private set; }

    /// <summary>How many points were left out by the <see cref="MaxMarkers"/> cap at the last rebuild.</summary>
    public int Dropped { get; private set; }

    private double Height => BaseHeight + (int)Which * LayerLift;

    /// <summary>The points changed (any thread): they are read again at the next update.</summary>
    public void DataChanged()
    {
        Volatile.Write(ref _dataDirty, 1);
        Invalidate();
    }

    public override void Update(SceneContext ctx)
    {
        double wpp = Picking.WorldPerPixel(ctx.Projector);
        bool data = Interlocked.Exchange(ref _dataDirty, 0) == 1;
        bool rescale = double.IsNaN(_builtWorldPerPx) || wpp > _builtWorldPerPx * CrisisSceneLayer.RescaleRatio
                       || wpp * CrisisSceneLayer.RescaleRatio < _builtWorldPerPx;
        if (!data && !rescale) return;
        if (data)
        {
            var all = _items() ?? [];
            Dropped = Math.Max(0, all.Count - MaxMarkers);
            _shown = Dropped > 0 ? [.. all.Take(MaxMarkers)] : all;
            if (Dropped > 0)
                _log.Error(string.Create(CultureInfo.InvariantCulture,
                    $"{Title}: {all.Count:N0} markers; only the first {MaxMarkers:N0} are drawn (P3-9: the dataset's filter should keep it under {MaxMarkers:N0})"));
        }
        _builtWorldPerPx = wpp;
        Rebuild(wpp);
    }

    private void Rebuild(double wpp)
    {
        _picks.Clear();
        var shape = LayerStyle.Shape(Which);
        var layerColor = LayerStyle.SymbolColor(Which);
        var fills = new Dictionary<Color, MarkerMeshes>();
        var edges = new MarkerMeshes();
        double h = Height, line = OutlinePx * wpp;
        foreach (var item in _shown)
        {
            double px = item.Size > 0 ? item.Size : DefaultSizePx, r = px * wpp;
            var color = item.Color ?? layerColor;
            if (!fills.TryGetValue(color, out var m)) fills[color] = m = new MarkerMeshes();
            switch (shape)
            {
                case MarkerShape.Dot: m.AddDisc(item.Position, r, h); break;
                case MarkerShape.EdgedDisc:
                    m.AddDisc(item.Position, r, h);
                    edges.AddRing(item.Position, r + line / 2, line, h, 24);   // just outside the disc: no overlap
                    px += OutlinePx;
                    break;
                case MarkerShape.Hexagon:
                    m.AddHexagon(item.Position, r * 1.1, h);
                    edges.AddHexagonOutline(item.Position, r * 1.1 + line, line, h);   // just outside the fill
                    px += OutlinePx;
                    break;
                case MarkerShape.Square: m.AddSquare(item.Position, r * 0.9, h); break;
                case MarkerShape.SquareOutline: m.AddSquareOutline(item.Position, r * 0.9, line, h); break;
                case MarkerShape.Diamond: m.AddDiamond(item.Position, r * 1.2, h); break;
                case MarkerShape.TriangleOutline: m.AddTriangleOutline(item.Position, r * 1.3, line, h); break;
                case MarkerShape.Cross: m.AddCross(item.Position, r, r * 0.28, h); break;
                case MarkerShape.Ring: m.AddRing(item.Position, r, line, h); break;
                default: throw new InvalidOperationException($"{shape} is not a marker shape.");
            }
            _picks.Add((MarkerMeshes.Above(item.Position, h), px, item));
        }
        // The dark edges first, then the fills (they do not overlap).
        var group = new Model3DGroup();
        if (!edges.IsEmpty) group.Children.Add(new GeometryModel3D(edges.ToMesh(), MarkerMeshes.Material(LayerStyle.MarkerEdgeColor)));
        foreach (var (color, mesh) in fills)
            if (!mesh.IsEmpty) group.Children.Add(new GeometryModel3D(mesh.ToMesh(), MarkerMeshes.Material(color)));
        group.Freeze();
        Root.Content = group;
        MarkerCount = _picks.Count;
    }

    /// <summary>The marker under the point (the nearest, the one drawn last on a tie), facing the camera; null if none.</summary>
    public PickResult? HitTest(Point screen, Projector projector, bool hover)
    {
        PointItem? best = null;
        double bestDist = double.MaxValue;
        for (int i = _picks.Count - 1; i >= 0; i--)
        {
            var (centre, r, item) = _picks[i];
            if (!Picking.FacesCamera(centre, projector.Eye) || !projector.TryProject(centre, out var p)) continue;
            double d = Picking.Distance(p, screen);
            if (d <= r + PickSlopPx && d < bestDist) { best = item; bestDist = d; }
        }
        return best is null ? null : new PickResult(Id, new PointPick(Which, best), best.Tooltip);
    }
}
