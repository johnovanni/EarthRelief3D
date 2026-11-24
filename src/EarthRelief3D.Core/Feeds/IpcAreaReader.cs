using System.Globalization;
using System.Text.Json;
using EarthRelief3D.Core.Model;
using GlobeCore.Geo;

namespace EarthRelief3D.Core.Feeds;

/// <summary>
/// Reads a country's IPC areas from HDX (P3-2; docs/data-sources.md section 7): the CKAN search that finds the country's
/// dataset, the national CSV that dates the analysis, and the GeoJSON of the areas.
/// </summary>
public static class IpcAreaReader
{
    public const string Dataset = "IPC acute food insecurity, area classification";
    public const string Attribution = "Integrated Food Security Phase Classification (IPC), via HDX; National IPC Technical Working Group";

    /// <summary>The HDX licence ids that are CC0 or public domain (all IPC datasets were <c>other-pd-nr</c> on 2026-11-23).</summary>
    private static readonly HashSet<string> PublicDomain = new(StringComparer.OrdinalIgnoreCase) { "other-pd-nr", "cc-zero", "odc-pddl" };

    public static bool IsPublicDomain(string? licenceId) => licenceId is not null && PublicDomain.Contains(licenceId);

    public static string GeoJsonName(string iso3) => $"ipc_{iso3.ToLowerInvariant()}.geojson";

    public static string NationalCsvName(string iso3) => $"ipc_{iso3.ToLowerInvariant()}_national_long_latest.csv";

    /// <summary>
    /// The decided CKAN query (data-sources section 7): the IPC organisation's packages in the country's group, with only the
    /// fields the reader needs. Dataset names use long country names, so the id is never built from a name.
    /// </summary>
    public static Uri SearchUrl(string iso3) => new("https://data.humdata.org/api/3/action/package_search?fq="
        + Uri.EscapeDataString($"organization:ipc AND groups:{iso3.ToLowerInvariant()}") + "&rows=10&fl="
        + Uri.EscapeDataString("name,license_id,res_name,res_url"));

    /// <summary>The dataset's page on HDX, the citation's link.</summary>
    public static Uri PageUrl(string packageName) => new("https://data.humdata.org/dataset/" + Uri.EscapeDataString(packageName));

    /// <param name="NationalCsvUrl">The country's national CSV, which dates the analysis; null when the package has none.</param>
    public sealed record Package(string Name, string LicenceId, Uri GeoJsonUrl, Uri? NationalCsvUrl);

    /// <summary>
    /// The package among the search results that holds exactly <c>ipc_&lt;iso3&gt;.geojson</c> (never the first package: HDX
    /// also returns the <c>ipc-country-data</c> archive and old duplicates), with that file's URL and the national CSV's.
    /// Reads the <c>fl</c> answer (<c>res_name</c> and <c>res_url</c> lists) and a full package (<c>resources</c>) alike.
    /// Null when no package has the GeoJSON.
    /// </summary>
    public static Package? FindPackage(string searchJson, string iso3)
    {
        try
        {
            using var doc = JsonDocument.Parse(searchJson);
            foreach (var p in doc.RootElement.GetProperty("result").GetProperty("results").EnumerateArray())
            {
                var files = Resources(p);
                if (!files.TryGetValue(GeoJsonName(iso3), out var geo)) continue;
                files.TryGetValue(NationalCsvName(iso3), out var csv);
                return new Package(Text(p, "name") ?? "", Text(p, "license_id") ?? "", geo, csv);
            }
            return null;
        }
        catch (Exception e) when (e is JsonException or KeyNotFoundException or InvalidOperationException)
        {
            throw new FeedFormatException("the HDX package_search answer has no result.results list");
        }
    }

    // Resource name -> URL, from "res_name"/"res_url" (fl answer) or "resources" (a full package).
    private static Dictionary<string, Uri> Resources(JsonElement p)
    {
        var map = new Dictionary<string, Uri>(StringComparer.OrdinalIgnoreCase);
        void Add(string? name, string? url)
        {
            if (name is not null && Uri.TryCreate(url, UriKind.Absolute, out var u)) map.TryAdd(name, u);
        }
        if (p.TryGetProperty("res_name", out var names) && names.ValueKind == JsonValueKind.Array
            && p.TryGetProperty("res_url", out var urls) && urls.ValueKind == JsonValueKind.Array)
        {
            var n = names.EnumerateArray().ToList();
            var u = urls.EnumerateArray().ToList();
            for (int i = 0; i < Math.Min(n.Count, u.Count); i++)
                Add(n[i].ValueKind == JsonValueKind.String ? n[i].GetString() : null, u[i].ValueKind == JsonValueKind.String ? u[i].GetString() : null);
        }
        else if (p.TryGetProperty("resources", out var res) && res.ValueKind == JsonValueKind.Array)
            foreach (var r in res.EnumerateArray()) Add(Text(r, "name"), Text(r, "url"));
        return map;
    }

    private static string? Text(JsonElement e, string name) =>
        e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    /// <summary>An analysis's current period, from the country's national CSV.</summary>
    /// <param name="DateOfAnalysis">"Mar 2026"; null when blank.</param>
    public sealed record Period(string? DateOfAnalysis, DateOnly From, DateOnly To);

    /// <param name="Current">The current period, or null when the file gives none (or several).</param>
    /// <param name="Found">What the file gives, for the log: "first projection 1 Jun 2026–31 Aug 2026".</param>
    public sealed record PeriodResult(Period? Current, string Found);

    private static readonly string[] CsvRequired = ["Date of analysis", "Validity period", "From", "To"];

    /// <summary>
    /// The current period of the analysis in <c>ipc_&lt;iso3&gt;_national_long_latest.csv</c> (one analysis per file): the
    /// rows whose "Validity period" is "current". Null when there are none, or when they disagree.
    /// </summary>
    public static PeriodResult CurrentPeriod(byte[] csv, string iso3)
    {
        string[]? header = null;
        Dictionary<string, int> col = [];
        var periods = new List<(string Kind, Period Period)>();
        foreach (var row in CsvReader.Read(csv))
        {
            if (header is null) { header = row; col = CsvReader.Columns(header, NationalCsvName(iso3), CsvRequired); continue; }
            if (row.Length < header.Length) continue;
            string F(string n) => row[col[n]].Trim();
            if (!TryDate(F("From"), out var from) || !TryDate(F("To"), out var to)) continue;
            var date = F("Date of analysis");
            var p = (F("Validity period").ToLowerInvariant(), new Period(date.Length == 0 ? null : date, from, to));
            if (!periods.Contains(p)) periods.Add(p);
        }
        if (header is null) throw new FeedFormatException($"{NationalCsvName(iso3)} is empty");
        var found = periods.Count == 0 ? "no dated rows"
            : string.Join(", ", periods.Select(p => $"{p.Kind} {Day(p.Period.From)}–{Day(p.Period.To)}"));
        var current = periods.Where(p => p.Kind == "current").Select(p => p.Period).ToList();
        return new PeriodResult(current.Count == 1 ? current[0] : null, found);
    }

    private static string Day(DateOnly d) => d.ToString("d MMM yyyy", CultureInfo.InvariantCulture);

    /// <summary>What a GeoJSON holds: the current-period areas and what else was there.</summary>
    /// <param name="Current">The areas with <c>ipc_period</c> C (Polygon and MultiPolygon features only).</param>
    /// <param name="AnalysisIds">The distinct <c>anl_id</c>s of the current-period areas, in file order.</param>
    /// <param name="Points">Point features (settlements such as "Bama IDPs"), never areas.</param>
    /// <param name="OtherPeriods">Area features of another period ("P" projection): their count by period.</param>
    /// <param name="Unreadable">Features skipped because their geometry could not be read.</param>
    public sealed record AreaFile(IReadOnlyList<IpcArea> Current, IReadOnlyList<string> AnalysisIds, int Points,
        IReadOnlyDictionary<string, int> OtherPeriods, int Unreadable);

    /// <summary>
    /// The current-period (C) areas of <c>ipc_&lt;iso3&gt;.geojson</c>, dated by <paramref name="period"/> (the GeoJSON has no
    /// dates). Polygons crossing ±180° are split there (GlobeCore's <see cref="DateLine"/>).
    /// </summary>
    public static AreaFile Read(byte[] geojson, string iso3, Period period, SourceCitation citation)
    {
        var areas = new List<IpcArea>();
        var ids = new List<string>();
        var other = new SortedDictionary<string, int>(StringComparer.Ordinal);
        int points = 0, unreadable = 0;
        JsonDocument doc;
        try { doc = JsonDocument.Parse(geojson); }
        catch (JsonException e) { throw new FeedFormatException($"{GeoJsonName(iso3)} is not JSON: {e.Message}"); }
        using (doc)
        {
            if (!doc.RootElement.TryGetProperty("features", out var features) || features.ValueKind != JsonValueKind.Array)
                throw new FeedFormatException($"{GeoJsonName(iso3)} has no features list");
            foreach (var f in features.EnumerateArray())
            {
                if (!f.TryGetProperty("geometry", out var geom) || geom.ValueKind != JsonValueKind.Object
                    || !f.TryGetProperty("properties", out var props) || props.ValueKind != JsonValueKind.Object) { unreadable++; continue; }
                var type = Text(geom, "type");
                if (type is "Point" or "MultiPoint") { points++; continue; }
                if (type is not ("Polygon" or "MultiPolygon")) { unreadable++; continue; }
                var kind = Text(props, "ipc_period") ?? "";
                if (!string.Equals(kind, "C", StringComparison.OrdinalIgnoreCase))
                {
                    other[kind.Length == 0 ? "blank" : kind] = other.GetValueOrDefault(kind.Length == 0 ? "blank" : kind) + 1;
                    continue;
                }
                List<GeoPolygon> polygons;
                try { polygons = Polygons(geom, type); }
                // A geometry with no "coordinates" (KeyNotFound) or a short position (IndexOutOfRange) is one unreadable
                // feature: counted, and the rest of the file is read (final fix wave C2).
                catch (Exception e) when (e is ArgumentException or InvalidOperationException or FormatException or KeyNotFoundException
                                              or IndexOutOfRangeException) { unreadable++; continue; }
                if (polygons.Count == 0) { unreadable++; continue; }
                var id = Text(props, "anl_id") ?? (props.TryGetProperty("anl_id", out var n) && n.ValueKind == JsonValueKind.Number ? n.GetRawText() : "");
                if (!ids.Contains(id)) ids.Add(id);
                int? phase = props.TryGetProperty("overall_phase", out var ph) && ph.ValueKind == JsonValueKind.Number && ph.TryGetInt32(out var pv)
                    && pv is >= 1 and <= 5 ? pv : null;
                areas.Add(new IpcArea(iso3, Text(props, "title") ?? "", phase, Population(props), polygons, id, period.From, period.To, citation));
            }
        }
        return new AreaFile(areas, ids, points, other, unreadable);
    }

    private static long? Population(JsonElement props)
    {
        if (!props.TryGetProperty("estimated_population", out var v)) return null;
        var text = v.ValueKind switch { JsonValueKind.String => v.GetString(), JsonValueKind.Number => v.GetRawText(), _ => null };
        return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var d) && d >= 0 ? (long)Math.Round(d) : null;
    }

    // GeoJSON [lon, lat] rings, each polygon split at the dateline.
    private static List<GeoPolygon> Polygons(JsonElement geom, string type)
    {
        var coords = geom.GetProperty("coordinates");
        var result = new List<GeoPolygon>();
        IEnumerable<JsonElement> polys = type == "Polygon" ? [coords] : coords.EnumerateArray();
        foreach (var poly in polys)
        {
            var rings = new List<IReadOnlyList<(double Lon, double Lat)>>();
            foreach (var ring in poly.EnumerateArray())
                rings.Add([.. ring.EnumerateArray().Select(pt => (pt[0].GetDouble(), pt[1].GetDouble()))]);
            if (rings.Count == 0 || rings[0].Count < 3) continue;
            result.AddRange(DateLine.SplitPolygon(rings));
        }
        return result;
    }

    private static bool TryDate(string text, out DateOnly d) =>
        DateOnly.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out d);
}
