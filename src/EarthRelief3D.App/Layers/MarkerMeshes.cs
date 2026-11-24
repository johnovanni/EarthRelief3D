using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using AstroCore.Earth;
using EarthRelief3D.App.Presentation;
using GlobeCore.Earth;
using GlobeCore.Wpf.Layers;
using GlobeCore.Wpf.Rendering;

namespace EarthRelief3D.App.Layers;

/// <summary>What a click or hover found: the country whose panel opens, and the section to open it at (C1).</summary>
public sealed record CountryPick(string Iso3, CrisisLayer? Focus)
{
    /// <summary>A Plan 3 marker layer whose section the panel opens at (atrocities); null for none.</summary>
    public MarkerLayer? Marker { get; init; }
}

/// <summary>
/// Builds marker meshes on the globe (unit equatorial radius): filled discs, rings and crosses, each laid in the tangent
/// plane at a point and then pressed onto a sphere of the given height, so even a ring hundreds of km wide hugs the
/// globe. Triangles are counter-clockwise seen from outside (WPF's front face), so markers on the far side are culled.
/// </summary>
public sealed class MarkerMeshes
{
    private readonly Point3DCollection _positions = [];
    private readonly Int32Collection _indices = [];

    public static MeshGeometry3D Build(Action<MarkerMeshes> add)
    {
        var m = new MarkerMeshes();
        add(m);
        return m.ToMesh();
    }

    public bool IsEmpty => _positions.Count == 0;

    /// <summary>A filled disc of <paramref name="radius"/> (Earth radii) at <paramref name="height"/> (Earth radii from the centre).</summary>
    public void AddDisc(GeoPosition at, double radius, double height, int segments = 12)
    {
        var (n, e1, e2) = Frame(at);
        int centre = Add(On(n, e1, e2, 0, 0, height));
        int first = _positions.Count;
        for (int i = 0; i < segments; i++)
        {
            double t = 2 * Math.PI * i / segments;
            Add(On(n, e1, e2, radius * Math.Cos(t), radius * Math.Sin(t), height));
        }
        for (int i = 0; i < segments; i++) Tri(centre, first + i, first + (i + 1) % segments);
    }

    /// <summary>A ring (an annulus <paramref name="width"/> wide) of <paramref name="radius"/> to its middle.</summary>
    public void AddRing(GeoPosition at, double radius, double width, double height, int segments = 48)
    {
        var (n, e1, e2) = Frame(at);
        double inner = Math.Max(0, radius - width / 2), outer = radius + width / 2;
        int first = _positions.Count;
        for (int i = 0; i < segments; i++)
        {
            double t = 2 * Math.PI * i / segments, c = Math.Cos(t), s = Math.Sin(t);
            Add(On(n, e1, e2, inner * c, inner * s, height));
            Add(On(n, e1, e2, outer * c, outer * s, height));
        }
        for (int i = 0; i < segments; i++)
        {
            int a = first + 2 * i, b = a + 1, j = first + 2 * ((i + 1) % segments), k = j + 1;
            Tri(a, b, k);
            Tri(a, k, j);
        }
    }

    /// <summary>An upright cross (+): arms <paramref name="armLength"/> from the centre east-west and north-south.</summary>
    public void AddCross(GeoPosition at, double armLength, double armHalfWidth, double height)
    {
        var (n, e1, e2) = Frame(at);
        Quad(n, e1, e2, -armLength, -armHalfWidth, armLength, armHalfWidth, height);   // east-west bar
        Quad(n, e1, e2, -armHalfWidth, -armLength, armHalfWidth, armLength, height);   // north-south bar
    }

    /// <summary>A filled square, <paramref name="half"/> from the centre to each side, its sides east-west and north-south.</summary>
    public void AddSquare(GeoPosition at, double half, double height)
    {
        var (n, e1, e2) = Frame(at);
        Quad(n, e1, e2, -half, -half, half, half, height);
    }

    /// <summary>A square outline: <paramref name="half"/> to the outer edge, bars <paramref name="width"/> wide, a hole inside.</summary>
    public void AddSquareOutline(GeoPosition at, double half, double width, double height)
    {
        var (n, e1, e2) = Frame(at);
        double i = Math.Max(0, half - width);
        Quad(n, e1, e2, -half, -half, half, -i, height);   // south bar
        Quad(n, e1, e2, -half, i, half, half, height);     // north bar
        Quad(n, e1, e2, -half, -i, -i, i, height);         // west bar
        Quad(n, e1, e2, i, -i, half, i, height);           // east bar
    }

    /// <summary>A filled diamond: points <paramref name="radius"/> from the centre to the east, north, west and south.</summary>
    public void AddDiamond(GeoPosition at, double radius, double height)
    {
        var (n, e1, e2) = Frame(at);
        int e = Add(On(n, e1, e2, radius, 0, height));
        int no = Add(On(n, e1, e2, 0, radius, height));
        int w = Add(On(n, e1, e2, -radius, 0, height));
        int s = Add(On(n, e1, e2, 0, -radius, height));
        Tri(e, no, w);
        Tri(e, w, s);
    }

    /// <summary>
    /// A hollow triangle pointing north: corners <paramref name="radius"/> from the centre, sides <paramref name="width"/>
    /// wide (measured square to each side), a hole inside.
    /// </summary>
    public void AddTriangleOutline(GeoPosition at, double radius, double width, double height)
    {
        var (n, e1, e2) = Frame(at);
        double inner = Math.Max(0, radius - 2 * width);   // the inradius is half the circumradius
        int first = _positions.Count;
        for (int k = 0; k < 3; k++)
        {
            double t = Math.PI / 2 + 2 * Math.PI * k / 3, c = Math.Cos(t), s = Math.Sin(t);   // counter-clockwise
            Add(On(n, e1, e2, radius * c, radius * s, height));
            Add(On(n, e1, e2, inner * c, inner * s, height));
        }
        for (int k = 0; k < 3; k++)
        {
            int v = first + 2 * k, u = v + 1, v2 = first + 2 * ((k + 1) % 3), u2 = v2 + 1;
            Tri(v, v2, u2);
            Tri(v, u2, u);
        }
    }

    public MeshGeometry3D ToMesh()
    {
        var mesh = new MeshGeometry3D { Positions = _positions, TriangleIndices = _indices };
        mesh.Freeze();
        return mesh;
    }

    /// <summary>The unit normal at <paramref name="g"/> and two tangent unit vectors (east, north) with e1 × e2 = n.</summary>
    public static (Vector3D N, Vector3D E1, Vector3D E2) Frame(GeoPosition g)
    {
        var u = g.ToUnitVector();
        var n = new Vector3D(u.X, u.Y, u.Z);
        var e1 = Vector3D.CrossProduct(new Vector3D(0, 0, 1), n);   // east (undefined at the poles: any perpendicular)
        if (e1.LengthSquared < 1e-12) e1 = Vector3D.CrossProduct(new Vector3D(0, 1, 0), n);
        e1.Normalize();
        var e2 = Vector3D.CrossProduct(n, e1);
        return (n, e1, e2);
    }

    /// <summary>The point <paramref name="x"/> east and <paramref name="y"/> north of the frame's centre, on the sphere of <paramref name="height"/>.</summary>
    public static Point3D On(Vector3D n, Vector3D e1, Vector3D e2, double x, double y, double height)
    {
        var v = n + x * e1 + y * e2;
        v.Normalize();
        return (Point3D)(v * height);
    }

    /// <summary>The point over <paramref name="g"/> at <paramref name="height"/>.</summary>
    public static Point3D Above(GeoPosition g, double height)
    {
        var u = g.ToUnitVector();
        return new Point3D(u.X * height, u.Y * height, u.Z * height);
    }

    private void Quad(Vector3D n, Vector3D e1, Vector3D e2, double x0, double y0, double x1, double y1, double height)
    {
        int a = Add(On(n, e1, e2, x0, y0, height));
        int b = Add(On(n, e1, e2, x1, y0, height));
        int c = Add(On(n, e1, e2, x1, y1, height));
        int d = Add(On(n, e1, e2, x0, y1, height));
        Tri(a, b, c);
        Tri(a, c, d);
    }

    private int Add(Point3D p)
    {
        _positions.Add(p);
        return _positions.Count - 1;
    }

    private void Tri(int a, int b, int c)
    {
        _indices.Add(a);
        _indices.Add(b);
        _indices.Add(c);
    }

    /// <summary>A flat colour: the window's white ambient light shows it unshaded, as EarthWatch3D's markers.</summary>
    public static Material Material(Color c)
    {
        var brush = new SolidColorBrush(c);
        brush.Freeze();
        var m = new DiffuseMaterial(brush);
        m.Freeze();
        return m;
    }
}

/// <summary>Screen-space helpers shared by the pickable layers.</summary>
internal static class Picking
{
    /// <summary>True when <paramref name="p"/> (on or just above the globe) faces the camera.</summary>
    public static bool FacesCamera(Point3D p, Point3D eye) => Vector3D.DotProduct((Vector3D)p, eye - p) > 0;

    /// <summary>World length (Earth radii) of one screen pixel at the point of the globe nearest the camera.</summary>
    public static double WorldPerPixel(Projector projector)
    {
        var eye = (Vector3D)projector.Eye;
        if (eye.Length <= 1) return 1e-3;
        eye.Normalize();
        double px = projector.PixelSize(1.0, (Point3D)eye);
        return double.IsFinite(px) && px > 0 ? 1.0 / px : 1e-3;
    }

    /// <summary>The country under the screen point (units applied: Somaliland is Somalia), or null over the sea or space.</summary>
    public static Country? CountryAt(Point screen, Projector projector, CountryIndex index) =>
        PositionAt(screen, projector) is { } g ? index.Find(g) : null;

    /// <summary>The place on the globe under the screen point, or null over space.</summary>
    public static GeoPosition? PositionAt(Point screen, Projector projector) =>
        projector.TryHitUnitSphere(screen, out var hit) ? GeoPosition.FromVector(new AstroCore.Astronomy.Vector3d(hit.X, hit.Y, hit.Z)) : null;

    public static double Distance(Point a, Point b) => (a - b).Length;
}
