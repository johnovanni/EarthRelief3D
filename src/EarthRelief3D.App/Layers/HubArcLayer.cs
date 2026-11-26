using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
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
/// The arcs from the selected country to its nearest UN depot, airport and port (Plan 4 Task 5, P4-7), drawn as 2D lines on
/// the globe's overlay (ruling H1): each a line about 3 px wide in its hub's colour over a 5 px dark edge, along the great
/// circle and lifted in the middle (<see cref="ArcPoints"/>), with the parts behind the globe cut. Arcs that run together on
/// screen (Sudan's to Port Sudan and to the Dubai depot) are drawn side by side, 4 px apart, longest first, so the shortest is
/// on top where they meet (<see cref="Lines"/>, <see cref="HubArcScreen"/>). Being 2D they are above every marker (final
/// review F2); a negative Z-index keeps them under the place names and distance labels on the shared canvas. GlobeCore is not
/// changed. They are drawn only while the country panel is open and the "Lines to nearest hubs" toggle is on
/// (<see cref="SpecsFor"/>). <see cref="Changed"/> (any thread) says the panel, the toggle or the hubs changed.
/// </summary>
public sealed class HubArcLayer : OverlayLayer
{
    /// <summary>Above every point-marker layer (130 + its number, at most 142) and below the labels (200): final review F2.</summary>
    public const int Order = 150;
    /// <summary>The line's width in screen pixels.</summary>
    public const double WidthPx = 3;
    /// <summary>The dark edge under each line, in pixels: 1 px shows on each side.</summary>
    public const double EdgeWidthPx = 5;
    /// <summary>The lines' Z-index on the overlay canvas: under the labels (0).</summary>
    public const int ZIndex = -1;
    /// <summary>The tallest an arc is lifted, in Earth radii; shorter arcs are lifted by a quarter of their central angle in radians.</summary>
    public const double MaxLift = 0.08;
    public const double LiftPerRadian = 0.25;

    private readonly Func<IReadOnlyList<HubArcSpec>> _specs;
    private readonly List<(Polyline Edge, Polyline Line)> _pool = [];
    private readonly Dictionary<Color, Brush> _brushes = [];

    /// <param name="specs">The arcs to draw now (read on the UI thread in <see cref="Render"/>); see <see cref="SpecsFor"/>.</param>
    public HubArcLayer(Func<IReadOnlyList<HubArcSpec>> specs)
        : base(LayerCatalog.Id(MarkerLayer.HubLines), LayerCatalog.Label(MarkerLayer.HubLines), Order)
    {
        _specs = specs ?? throw new ArgumentNullException(nameof(specs));
    }

    /// <summary>How many arcs are drawn (tests, diagnostics): arcs with at least one visible part.</summary>
    public int ArcCount { get; private set; }

    /// <summary>The panel, the toggle or the hubs changed (any thread): the arcs are drawn again at the next render.</summary>
    public void Changed() => Invalidate();

    public override void Render(Canvas overlay, Projector projector)
    {
        ArgumentNullException.ThrowIfNull(overlay);
        var lines = Lines(_specs() ?? [], projector);
        for (int i = 0; i < lines.Count; i++)
        {
            if (i == _pool.Count) _pool.Add((Add(overlay, EdgeWidthPx), Add(overlay, WidthPx)));
            var (edge, line) = _pool[i];
            var points = new PointCollection(lines[i].Line);
            points.Freeze();
            edge.Points = points;
            edge.Stroke = BrushFor(LayerStyle.MarkerEdgeColor);
            edge.Visibility = Visibility.Visible;
            line.Points = points;
            line.Stroke = BrushFor(LayerStyle.HubColor(lines[i].Spec.Link.Hub.Kind));
            line.Visibility = Visibility.Visible;
        }
        for (int i = lines.Count; i < _pool.Count; i++) Hide(_pool[i]);
        ArcCount = lines.Select(l => l.Spec).Distinct().Count();
    }

    protected override void Clear(Canvas overlay)
    {
        foreach (var pair in _pool) Hide(pair);
        ArcCount = 0;
    }

    /// <summary>
    /// What is drawn for <paramref name="specs"/> seen through <paramref name="projector"/>: each arc's visible parts in screen
    /// pixels (<see cref="HubArcScreen.VisibleRuns"/>), moved apart sideways where they run together
    /// (<see cref="HubArcScreen.SideOffsets"/>), in drawing order, longest first (<see cref="HubArcScreen.DrawOrder"/>).
    /// </summary>
    public static IReadOnlyList<(HubArcSpec Spec, IReadOnlyList<Point> Line)> Lines(IReadOnlyList<HubArcSpec> specs, Projector projector)
    {
        ArgumentNullException.ThrowIfNull(specs);
        var runs = new List<(HubArcSpec Spec, IReadOnlyList<Point> Line)>();
        foreach (var spec in specs)
            foreach (var run in HubArcScreen.VisibleRuns(ArcPoints(spec.From, spec.Link.Hub.Position), projector))
                runs.Add((spec, run));
        var screen = runs.Select(r => r.Line).ToList();
        var offsets = HubArcScreen.SideOffsets(screen);
        return [.. HubArcScreen.DrawOrder(screen).Select(i => (runs[i].Spec, HubArcScreen.Shift(runs[i].Line, offsets[i])))];
    }

    private static Polyline Add(Canvas overlay, double width)
    {
        var p = new Polyline
        {
            StrokeThickness = width,
            StrokeLineJoin = PenLineJoin.Round,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round,
            IsHitTestVisible = false,
            Visibility = Visibility.Collapsed,
        };
        Panel.SetZIndex(p, ZIndex);
        overlay.Children.Add(p);
        return p;
    }

    private static void Hide((Polyline Edge, Polyline Line) pair)
    {
        pair.Edge.Visibility = Visibility.Collapsed;
        pair.Line.Visibility = Visibility.Collapsed;
    }

    private Brush BrushFor(Color c)
    {
        if (!_brushes.TryGetValue(c, out var b))
        {
            var brush = new SolidColorBrush(c);
            brush.Freeze();
            _brushes[c] = b = brush;
        }
        return b;
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
    /// <c>h = min(0.08, 0.25 × the central angle in radians)</c>, the same for every kind of hub (H1 withdrew P5-14's
    /// factors): on the surface at both ends, highest in the middle. The rise is drawing only (C3): distances are measured
    /// on the sphere.
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

    /// <summary>The point the distance label sits at: the middle of the arc, at its top.</summary>
    public static Point3D MidPoint(IReadOnlyList<Point3D> path) => path[path.Count / 2];
}
