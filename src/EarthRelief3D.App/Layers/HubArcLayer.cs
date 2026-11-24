using System.Windows.Media;
using System.Windows.Media.Media3D;
using AstroCore.Earth;
using EarthRelief3D.App.Presentation;
using EarthRelief3D.Core.Logic;
using GlobeCore.Bodies;
using GlobeCore.Geo;
using GlobeCore.Wpf.Layers;
using GlobeCore.Wpf.Rendering;

namespace EarthRelief3D.App.Layers;

/// <summary>One arc to draw: the straight-line link to a hub and the place it starts from (the country's label point, P4-6).</summary>
public sealed record HubArcSpec(HubLink Link, SurfacePoint From);

/// <summary>
/// The arcs from the selected country to its nearest UN depot, airport and port (Plan 4 Task 5, P4-7). Each is a ribbon about
/// 3 px wide in its hub's colour, drawn above every point-marker layer (final review F2: they were lost among the hub markers), along the great circle and lifted in the middle (<see cref="ArcPoints"/>), built here:
/// GlobeCore is not changed. They are drawn only while the country panel is open and the "Lines to nearest hubs" toggle is on
/// (<see cref="SpecsFor"/>). <see cref="Changed"/> (any thread) says the panel, the toggle or the hubs changed; the ribbons are
/// also rebuilt when the zoom changes the size of a pixel by more than <see cref="CrisisSceneLayer.RescaleRatio"/>, as the
/// point markers are, so they stay 3 px wide.
/// </summary>
public sealed class HubArcLayer : SceneLayer
{
    /// <summary>Above every point-marker layer (130 + its number, at most 142) and below the labels (200): final review F2.</summary>
    public const int Order = 150;
    /// <summary>The ribbon's width in screen pixels.</summary>
    public const double WidthPx = 3;
    /// <summary>The tallest an arc is lifted, in Earth radii; shorter arcs are lifted by a quarter of their central angle in radians.</summary>
    public const double MaxLift = 0.08;
    public const double LiftPerRadian = 0.25;
    /// <summary>
    /// The arcs' whole path rises by this factor, so their ends lie above the highest point-marker layer (at most 1.0074 Earth
    /// radii, <see cref="PointMarkerLayer.BaseHeight"/>) and no marker mesh hides them (the geometry itself is <see cref="ArcPoints"/>).
    /// </summary>
    public const double SurfaceClearance = 1.008;

    private readonly Func<IReadOnlyList<HubArcSpec>> _specs;
    private int _dirty = 1;
    private double _builtWorldPerPx = double.NaN;

    /// <param name="specs">The arcs to draw now (read on the UI thread in <see cref="Update"/>); see <see cref="SpecsFor"/>.</param>
    public HubArcLayer(Func<IReadOnlyList<HubArcSpec>> specs)
        : base(LayerCatalog.Id(MarkerLayer.HubLines), LayerCatalog.Label(MarkerLayer.HubLines), Order)
    {
        _specs = specs ?? throw new ArgumentNullException(nameof(specs));
    }

    /// <summary>How many arcs are drawn (tests, diagnostics).</summary>
    public int ArcCount { get; private set; }

    /// <summary>The panel, the toggle or the hubs changed (any thread): the arcs are read again at the next update.</summary>
    public void Changed()
    {
        Volatile.Write(ref _dirty, 1);
        Invalidate();
    }

    public override void Update(SceneContext ctx)
    {
        double wpp = Picking.WorldPerPixel(ctx.Projector);
        bool data = Interlocked.Exchange(ref _dirty, 0) == 1;
        bool rescale = double.IsNaN(_builtWorldPerPx) || wpp > _builtWorldPerPx * CrisisSceneLayer.RescaleRatio
                       || wpp * CrisisSceneLayer.RescaleRatio < _builtWorldPerPx;
        if (!data && !rescale) return;
        _builtWorldPerPx = wpp;
        var group = new Model3DGroup();
        int arcs = 0;
        foreach (var spec in _specs() ?? [])
        {
            var path = ArcPoints(spec.From, spec.Link.Hub.Position);
            if (path.Count < 2) continue;
            var lifted = path.Select(p => new Point3D(p.X * SurfaceClearance, p.Y * SurfaceClearance, p.Z * SurfaceClearance)).ToList();
            var material = MarkerMeshes.Material(LayerStyle.HubColor(spec.Link.Hub.Kind));
            group.Children.Add(new GeometryModel3D(Ribbon(lifted, WidthPx * wpp), material) { BackMaterial = material });
            arcs++;
        }
        ArcCount = arcs;
        if (arcs == 0)
        {
            Root.Content = null;
            return;
        }
        group.Freeze();
        Root.Content = group;
    }

    // ---------------------------------------------------------------- what to draw

    /// <summary>
    /// The arcs for the panel's country: the links to its nearest UN depot, airport and port (<see cref="NearestHubFinder"/>)
    /// from the place the crisis is taken to be (P4-6, <paramref name="place"/>: the country's label point). None when the
    /// panel is closed, the toggle is off, or the country has no point to measure from.
    /// </summary>
    public static IReadOnlyList<HubArcSpec> SpecsFor(bool panelOpen, bool hubLinesOn, string iso3, Func<string, GeoPosition?> place, IReadOnlyList<Hub> hubs)
    {
        ArgumentNullException.ThrowIfNull(place);
        ArgumentNullException.ThrowIfNull(hubs);
        if (!panelOpen || !hubLinesOn || string.IsNullOrEmpty(iso3) || place(iso3) is not { } from) return [];
        SurfacePoint start = from;
        return [.. NearestHubFinder.Nearest(start, hubs).Select(l => new HubArcSpec(l, start))];
    }

    // ---------------------------------------------------------------- geometry

    /// <summary>
    /// The arc's points in Earth radii: the great circle from <paramref name="from"/> to <paramref name="to"/> in steps of 1
    /// degree or less (<see cref="SphereLines.Densify"/>, which takes the short way across the date line and over a pole), each
    /// lifted by <c>1 + h·sin(πt)</c> where t runs from 0 to 1 along the path and
    /// <c>h = min(0.08, 0.25 × the central angle in radians)</c>: on the surface at both ends, highest in the middle.
    /// </summary>
    public static IReadOnlyList<Point3D> ArcPoints(SurfacePoint from, SurfacePoint to)
    {
        var flat = SphereLines.Densify(new GeoLine([(GeoPosition)from, (GeoPosition)to]), 1.0, 1.0);
        if (flat.Count < 2) return flat;
        var a = (Vector3D)flat[0];
        var b = (Vector3D)flat[^1];
        a.Normalize();
        b.Normalize();
        double angle = Math.Acos(Math.Clamp(Vector3D.DotProduct(a, b), -1, 1));
        double h = Math.Min(MaxLift, LiftPerRadian * angle);
        var result = new List<Point3D>(flat.Count);
        for (int i = 0; i < flat.Count; i++)
        {
            double t = (double)i / (flat.Count - 1);
            double r = 1 + h * Math.Sin(Math.PI * t);
            result.Add(new Point3D(flat[i].X * r, flat[i].Y * r, flat[i].Z * r));
        }
        return result;
    }

    /// <summary>
    /// A flat strip <paramref name="width"/> wide (Earth radii) along <paramref name="path"/>, lying across the path in the
    /// surface's tangent plane: two vertices per path point (left, right), two triangles per step, counter-clockwise seen
    /// from outside the globe. Empty for a path of fewer than two points.
    /// </summary>
    public static MeshGeometry3D Ribbon(IReadOnlyList<Point3D> path, double width)
    {
        ArgumentNullException.ThrowIfNull(path);
        var positions = new Point3DCollection();
        var indices = new Int32Collection();
        if (path.Count >= 2)
        {
            var side = new Vector3D(1, 0, 0);
            for (int i = 0; i < path.Count; i++)
            {
                var tangent = path[Math.Min(i + 1, path.Count - 1)] - path[Math.Max(i - 1, 0)];
                var up = (Vector3D)path[i];
                // Left of the direction of travel, seen from outside: up × tangent. Kept from the step before if the path stalls.
                var s = Vector3D.CrossProduct(up, tangent);
                if (s.LengthSquared > 1e-18) { s.Normalize(); side = s; }
                positions.Add(path[i] + side * (width / 2));
                positions.Add(path[i] - side * (width / 2));
            }
            for (int i = 0; i < path.Count - 1; i++)
            {
                int l0 = 2 * i, r0 = l0 + 1, l1 = l0 + 2, r1 = l0 + 3;
                indices.Add(l0); indices.Add(r0); indices.Add(r1);
                indices.Add(l0); indices.Add(r1); indices.Add(l1);
            }
        }
        var mesh = new MeshGeometry3D { Positions = positions, TriangleIndices = indices };
        mesh.Freeze();
        return mesh;
    }

    /// <summary>The point the distance label sits at: the middle of the arc, at its top.</summary>
    public static Point3D MidPoint(IReadOnlyList<Point3D> path) => path[path.Count / 2];
}
