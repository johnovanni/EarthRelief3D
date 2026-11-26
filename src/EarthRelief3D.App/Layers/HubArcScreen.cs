using System.Windows;
using System.Windows.Media.Media3D;
using GlobeCore.Wpf.Rendering;

namespace EarthRelief3D.App.Layers;

/// <summary>
/// The hub arcs on the screen (ruling H1): each arc's path projected to pixels with the parts behind the globe cut
/// (<see cref="VisibleRuns"/>), and arcs that run together moved apart sideways so they lie side by side, like lines on a
/// transit map (<see cref="SideOffsets"/>, <see cref="Shift"/>). Pure geometry: no WPF elements.
/// </summary>
public static class HubArcScreen
{
    /// <summary>Pixels between the centre lines of two arcs drawn side by side (3 px lines with a 1 px dark gap).</summary>
    public const double Spacing = 4;
    /// <summary>A point within this many pixels of another arc lies on it (H2).</summary>
    public const double NearPx = 6;
    /// <summary>Two arcs run together when at least this share of the shorter one's points lie on the other (H2).</summary>
    public const double ShareTogether = 0.5;
    /// <summary>Points this close to an arc's start are not counted: every arc from one place meets the others there (H2).</summary>
    public const double StartPx = 2 * NearPx;

    /// <summary>
    /// True when the camera at <paramref name="eye"/> sees <paramref name="p"/> (on or above the unit globe): the straight
    /// line from the eye to the point does not pass through the globe first.
    /// </summary>
    public static bool IsVisible(Point3D p, Point3D eye)
    {
        var e = (Vector3D)eye;
        var d = p - eye;
        double a = Vector3D.DotProduct(d, d);
        if (a <= 0) return true;
        double b = 2 * Vector3D.DotProduct(e, d);
        double c = Vector3D.DotProduct(e, e) - 1;
        double disc = b * b - 4 * a * c;
        if (disc <= 0) return true;
        double t = (-b - Math.Sqrt(disc)) / (2 * a);   // where the line first meets the globe (1 = at the point itself)
        return !(t > 0 && t < 1 - 1e-6);
    }

    /// <summary>The path in screen pixels, split where it goes behind the globe; a run of fewer than two points is dropped.</summary>
    public static IReadOnlyList<IReadOnlyList<Point>> VisibleRuns(IReadOnlyList<Point3D> path, Projector projector)
    {
        ArgumentNullException.ThrowIfNull(path);
        var runs = new List<IReadOnlyList<Point>>();
        var current = new List<Point>();
        void Close()
        {
            if (current.Count >= 2) runs.Add(current);
            current = [];
        }
        foreach (var p in path)
        {
            if (IsVisible(p, projector.Eye) && projector.TryProject(p, out var s)) current.Add(s);
            else Close();
        }
        Close();
        return runs;
    }

    /// <summary>
    /// True when the two lines run together on screen: of the shorter line's points farther than <see cref="StartPx"/> from
    /// its start, at least <see cref="ShareTogether"/> lie within <see cref="NearPx"/> of the other line. False when the
    /// shorter line has no such points (too short to tell).
    /// </summary>
    public static bool RunTogether(IReadOnlyList<Point> a, IReadOnlyList<Point> b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        var (shorter, other) = Length(a) < Length(b) ? (a, b) : (b, a);
        if (shorter.Count == 0 || other.Count < 2) return false;
        int counted = 0, near = 0;
        foreach (var p in shorter)
        {
            if ((p - shorter[0]).Length <= StartPx) continue;
            counted++;
            if (Distance(p, other) <= NearPx) near++;
        }
        return counted > 0 && near >= ShareTogether * counted;
    }

    /// <summary>
    /// One sideways offset in pixels per line (positive: along <see cref="Shift"/>'s normal). Lines that run together
    /// (<see cref="RunTogether"/>, joined in groups) are spread <see cref="Spacing"/> apart around their middle, in the order
    /// of each line's mean signed distance from the group's longest line, so they never cross. Other lines get 0.
    /// </summary>
    public static double[] SideOffsets(IReadOnlyList<IReadOnlyList<Point>> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);
        int n = lines.Count;
        var offsets = new double[n];
        var group = Enumerable.Range(0, n).ToArray();
        int Find(int i) => group[i] == i ? i : group[i] = Find(group[i]);
        for (int i = 0; i < n; i++)
            for (int j = i + 1; j < n; j++)
                if (RunTogether(lines[i], lines[j])) group[Find(i)] = Find(j);
        foreach (var members in Enumerable.Range(0, n).GroupBy(Find).Select(g => g.ToList()).Where(g => g.Count > 1))
        {
            int reference = members.OrderByDescending(i => Length(lines[i])).ThenBy(i => i).First();
            var ordered = members.OrderBy(i => i == reference ? 0 : lines[i].Average(p => SignedDistance(p, lines[reference])))
                                 .ThenBy(i => i).ToList();
            for (int slot = 0; slot < ordered.Count; slot++)
                offsets[ordered[slot]] = (slot - (ordered.Count - 1) / 2.0) * Spacing;
        }
        return offsets;
    }

    /// <summary>
    /// The line moved <paramref name="offset"/> pixels sideways at every point, along the unit normal (−dy, dx) of its local
    /// direction (the neighbours on both sides; one side at the ends).
    /// </summary>
    public static IReadOnlyList<Point> Shift(IReadOnlyList<Point> line, double offset)
    {
        ArgumentNullException.ThrowIfNull(line);
        if (offset == 0 || line.Count < 2) return [.. line];
        // Where the line stalls (repeated points) a point keeps the normal before it; stalled first points take the first one found.
        var normals = new Vector?[line.Count];
        for (int i = 0; i < line.Count; i++)
        {
            var dir = line[Math.Min(i + 1, line.Count - 1)] - line[Math.Max(i - 1, 0)];
            if (dir.LengthSquared <= 1e-18) continue;
            dir.Normalize();
            normals[i] = new Vector(-dir.Y, dir.X);
        }
        var normal = normals.FirstOrDefault(v => v.HasValue) ?? new Vector(0, 0);
        var result = new List<Point>(line.Count);
        for (int i = 0; i < line.Count; i++)
        {
            normal = normals[i] ?? normal;
            result.Add(line[i] + normal * offset);
        }
        return result;
    }

    /// <summary>The order to draw the lines in: longest first, so the shortest is on top where they meet (ties: input order).</summary>
    public static int[] DrawOrder(IReadOnlyList<IReadOnlyList<Point>> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);
        return [.. Enumerable.Range(0, lines.Count).OrderByDescending(i => Length(lines[i])).ThenBy(i => i)];
    }

    // ---------------------------------------------------------------- helpers

    /// <summary>The length of a polyline in pixels.</summary>
    public static double Length(IReadOnlyList<Point> line)
    {
        double sum = 0;
        for (int i = 1; i < line.Count; i++) sum += (line[i] - line[i - 1]).Length;
        return sum;
    }

    /// <summary>The distance from <paramref name="p"/> to the nearest point of the polyline.</summary>
    public static double Distance(Point p, IReadOnlyList<Point> line) => Math.Abs(SignedDistance(p, line));

    /// <summary>The distance to the nearest point of the polyline, positive on the side of its normal (−dy, dx).</summary>
    private static double SignedDistance(Point p, IReadOnlyList<Point> line)
    {
        if (line.Count == 0) return double.PositiveInfinity;
        if (line.Count == 1) return (p - line[0]).Length;
        double best = double.PositiveInfinity, signed = 0;
        for (int i = 1; i < line.Count; i++)
        {
            var a = line[i - 1];
            var ab = line[i] - a;
            var ap = p - a;
            double len2 = ab.LengthSquared;
            double t = len2 > 0 ? Math.Clamp((ap * ab) / len2, 0, 1) : 0;
            double d = (p - (a + ab * t)).Length;
            if (d < best)
            {
                best = d;
                double cross = ab.X * ap.Y - ab.Y * ap.X;
                signed = cross < 0 ? -d : d;
            }
        }
        return signed;
    }
}
