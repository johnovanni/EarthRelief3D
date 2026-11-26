using System.Windows;
using System.Windows.Media.Media3D;
using GlobeCore.Wpf.Rendering;

namespace EarthRelief3D.App.Layers;

/// <summary>
/// The hub arcs on the screen (ruling H1): each arc's path projected to pixels with the parts behind the globe cut
/// (<see cref="VisibleRuns"/>), and arcs that run together pushed apart where they are close, so they lie side by side
/// (<see cref="SideBySide"/>). Pure geometry: no WPF elements.
/// </summary>
public static class HubArcScreen
{
    /// <summary>Pixels between the centre lines of two arcs drawn side by side (3 px lines with a 1 px dark gap).</summary>
    public const double Spacing = 4;
    /// <summary>A point within this many pixels of another arc lies on it (H2): only there are arcs pushed apart (H4).</summary>
    public const double NearPx = 6;
    /// <summary>Two arcs run together when at least this share of the shorter one's points lie on the other (H2).</summary>
    public const double ShareTogether = 0.5;
    /// <summary>Points this close to an arc's start are not counted: every arc from one place meets the others there (H2).</summary>
    public const double StartPx = 2 * NearPx;
    /// <summary>When the two sides need pushing within this many pixels of each other, the bearing on the ground decides (H4).</summary>
    public const double TieBandPx = 1;
    /// <summary>Rounds of pushing: each moves every pair half of what it still lacks, so three arcs in a bundle settle too.</summary>
    public const int Rounds = 12;

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
    /// The lines drawn side by side (H4, after final review I1): lines that run together (<see cref="RunTogether"/>,
    /// joined in groups) are pushed apart sideways, each point along its own line's normal, just enough that their centre
    /// lines are <see cref="Spacing"/> apart wherever they are within <see cref="NearPx"/> of each other, each line of a
    /// pair moving half. Where they are already apart nothing moves, so a line still ends at its hub unless the hub lies on
    /// another arc. Each pair keeps one side all along (<see cref="ChooseSide"/>), so the lines never cross. Other lines are
    /// returned as they are.
    /// </summary>
    /// <param name="bearings">Each line's bearing on the ground at its start, degrees clockwise from north (optional): settles
    /// near-ties, so lines do not swap sides as the globe turns.</param>
    public static IReadOnlyList<IReadOnlyList<Point>> SideBySide(IReadOnlyList<IReadOnlyList<Point>> lines, IReadOnlyList<double>? bearings = null)
    {
        ArgumentNullException.ThrowIfNull(lines);
        if (bearings is not null && bearings.Count != lines.Count) throw new ArgumentException("One bearing per line.", nameof(bearings));
        int n = lines.Count;
        var group = Enumerable.Range(0, n).ToArray();
        int Find(int i) => group[i] == i ? i : group[i] = Find(group[i]);
        for (int i = 0; i < n; i++)
            for (int j = i + 1; j < n; j++)
                if (RunTogether(lines[i], lines[j])) group[Find(i)] = Find(j);

        // side[(a, b)]: the side of b's line that a keeps (+1: along the normal).
        var side = new Dictionary<(int, int), int>();
        for (int i = 0; i < n; i++)
            for (int j = i + 1; j < n; j++)
            {
                if (Find(i) != Find(j)) continue;
                var (reference, other) = Length(lines[i]) >= Length(lines[j]) ? (i, j) : (j, i);
                double? diff = bearings is null ? null : bearings[other] - bearings[reference];
                int s = ChooseSide(lines[other], lines[reference], diff);
                side[(other, reference)] = s;
                side[(reference, other)] = -s;
            }

        var current = lines.Select(l => (IReadOnlyList<Point>)[.. l]).ToList();
        for (int round = 0; round < Rounds && side.Count > 0; round++)
        {
            var push = current.Select(l => new double[l.Count]).ToList();
            double most = 0;
            foreach (var ((a, b), s) in side)
                for (int k = 0; k < current[a].Count; k++)
                {
                    double need = Need(current[a][k], current[b], s);
                    push[a][k] += s * need / 2;
                    most = Math.Max(most, need);
                }
            if (most < 0.01) break;
            for (int i = 0; i < n; i++)
                if (push[i].Any(v => v != 0)) current[i] = Shift(current[i], push[i]);
        }
        return current;
    }

    /// <summary>
    /// Which side of <paramref name="reference"/> <paramref name="line"/> keeps (+1: along the normal): the one that needs
    /// less pushing (<see cref="Need"/>, the most over its points). When the two differ by less than
    /// <see cref="TieBandPx"/> and <paramref name="bearingDiff"/> (the line's bearing minus the reference's, degrees) is
    /// given, clockwise on the ground is the + side (on screen, seen from outside the globe, clockwise stays clockwise).
    /// </summary>
    public static int ChooseSide(IReadOnlyList<Point> line, IReadOnlyList<Point> reference, double? bearingDiff = null)
    {
        ArgumentNullException.ThrowIfNull(line);
        ArgumentNullException.ThrowIfNull(reference);
        double plus = line.Select(p => Need(p, reference, +1)).DefaultIfEmpty(0).Max();
        double minus = line.Select(p => Need(p, reference, -1)).DefaultIfEmpty(0).Max();
        if (Math.Abs(plus - minus) < TieBandPx && bearingDiff is { } diff)
        {
            double wrapped = ((diff % 360) + 540) % 360 - 180;   // -180 to 180
            return wrapped >= 0 ? +1 : -1;
        }
        return plus <= minus ? +1 : -1;
    }

    /// <summary>How far <paramref name="p"/> must move to be <see cref="Spacing"/> from the line on side <paramref name="side"/>; 0 beyond <see cref="NearPx"/>.</summary>
    private static double Need(Point p, IReadOnlyList<Point> line, int side)
    {
        double d = SignedDistance(p, line);
        if (Math.Abs(d) > NearPx) return 0;
        return Math.Max(0, Spacing - side * d);
    }

    /// <summary>The line moved <paramref name="offset"/> pixels sideways at every point (see the per-point overload).</summary>
    public static IReadOnlyList<Point> Shift(IReadOnlyList<Point> line, double offset)
    {
        ArgumentNullException.ThrowIfNull(line);
        return Shift(line, [.. Enumerable.Repeat(offset, line.Count)]);
    }

    /// <summary>
    /// The line with each point moved its own <paramref name="offsets"/> pixels sideways, along the unit normal (−dy, dx) of
    /// the line's local direction (the neighbours on both sides; one side at the ends).
    /// </summary>
    public static IReadOnlyList<Point> Shift(IReadOnlyList<Point> line, IReadOnlyList<double> offsets)
    {
        ArgumentNullException.ThrowIfNull(line);
        ArgumentNullException.ThrowIfNull(offsets);
        if (offsets.Count != line.Count) throw new ArgumentException("One offset per point.", nameof(offsets));
        if (line.Count < 2 || offsets.All(o => o == 0)) return [.. line];
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
            result.Add(line[i] + normal * offsets[i]);
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
