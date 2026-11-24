using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace EarthRelief3D.DataTool;

/// <summary>
/// <c>simplify &lt;geojson&gt; &lt;tolerance&gt;</c>, for test samples: Douglas-Peucker on every LineString and polygon
/// ring (tolerance in degrees), coordinates rounded to <see cref="Decimals"/> places; properties and Point geometries
/// untouched. A ring keeps at least four points (closed), so no polygon collapses.
/// </summary>
public static class GeoJsonSimplifier
{
    public const int Decimals = 3;

    public static string Simplify(string geoJson, double tolerance)
    {
        if (!(tolerance >= 0)) throw new ArgumentOutOfRangeException(nameof(tolerance), "The tolerance is in degrees, 0 or more.");
        var root = JsonNode.Parse(geoJson) ?? throw new InvalidDataException("empty GeoJSON");
        foreach (var geometry in Geometries(root))
        {
            var type = geometry["type"]?.GetValue<string>();
            var coords = geometry["coordinates"];
            if (coords is null) continue;
            geometry["coordinates"] = type switch
            {
                "LineString" => Line(coords.AsArray(), tolerance, ring: false),
                "MultiLineString" or "Polygon" => new JsonArray([.. coords.AsArray().Select(l => (JsonNode)Line(l!.AsArray(), tolerance, ring: type == "Polygon"))]),
                "MultiPolygon" => new JsonArray([.. coords.AsArray().Select(p => (JsonNode)new JsonArray([.. p!.AsArray().Select(l => (JsonNode)Line(l!.AsArray(), tolerance, ring: true))]))]),
                _ => coords.DeepClone(),
            };
        }
        return root.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
    }

    private static IEnumerable<JsonObject> Geometries(JsonNode root)
    {
        if (root["type"]?.GetValue<string>() == "FeatureCollection")
        {
            foreach (var f in root["features"]!.AsArray())
                if (f?["geometry"] is JsonObject g) yield return g;
        }
        else if (root["type"]?.GetValue<string>() == "Feature")
        {
            if (root["geometry"] is JsonObject g) yield return g;
        }
        else if (root is JsonObject o) yield return o;
    }

    private static JsonArray Line(JsonArray points, double tolerance, bool ring)
    {
        var pts = points.Select(p => (X: p![0]!.GetValue<double>(), Y: p[1]!.GetValue<double>())).ToList();
        var keep = new bool[pts.Count];
        if (pts.Count > 0) { keep[0] = true; keep[^1] = true; }
        Mark(pts, 0, pts.Count - 1, tolerance, keep);
        var kept = pts.Where((_, i) => keep[i]).ToList();
        if (ring && kept.Count < 4) kept = pts;   // never collapse a ring
        return new JsonArray([.. kept.Select(p => (JsonNode)new JsonArray(Round(p.X), Round(p.Y)))]);
    }

    private static void Mark(List<(double X, double Y)> pts, int first, int last, double tolerance, bool[] keep)
    {
        if (last <= first + 1) return;
        double max = -1;
        int index = -1;
        for (int i = first + 1; i < last; i++)
        {
            double d = Distance(pts[i], pts[first], pts[last]);
            if (d > max) { max = d; index = i; }
        }
        if (max <= tolerance) return;
        keep[index] = true;
        Mark(pts, first, index, tolerance, keep);
        Mark(pts, index, last, tolerance, keep);
    }

    // Distance from p to the segment a-b, in degrees (planar: enough for test samples).
    private static double Distance((double X, double Y) p, (double X, double Y) a, (double X, double Y) b)
    {
        double dx = b.X - a.X, dy = b.Y - a.Y;
        double len2 = dx * dx + dy * dy;
        double t = len2 == 0 ? 0 : Math.Clamp(((p.X - a.X) * dx + (p.Y - a.Y) * dy) / len2, 0, 1);
        double ex = a.X + t * dx - p.X, ey = a.Y + t * dy - p.Y;
        return Math.Sqrt(ex * ex + ey * ey);
    }

    private static JsonNode Round(double v) => JsonValue.Create(Math.Round(v, Decimals, MidpointRounding.AwayFromZero))!;

    /// <summary>"0.004" in invariant culture, or an error.</summary>
    public static double ParseTolerance(string text) =>
        double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var t) && t >= 0
            ? t : throw new ArgumentException($"tolerance \"{text}\" is not a number of degrees");
}
