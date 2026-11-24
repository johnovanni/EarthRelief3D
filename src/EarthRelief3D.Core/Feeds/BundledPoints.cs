using System.Globalization;
using EarthRelief3D.Core.Model;
using GlobeCore.Logging;

namespace EarthRelief3D.Core.Feeds;

/// <summary>
/// One site of a bundled point extract (Task 7, the GEM trackers): a plant or field at its source's coordinates, with the
/// country it is in (ISO3, the boundary set's unit), its capacity (null where the source gives none, as for oil and gas
/// fields), the capacity's unit, its status and its type as the source words them ("coal", "oil and gas", "BF; BOF"), and a
/// detail the source gives in its own words and units (an oil field's production, "oil 518 million bbl/y (2022)"), or "".
/// </summary>
public sealed record MarkerPoint(string Name, string Iso3, double Lat, double Lon, double? Capacity, string Unit, string Status, string Type,
    string Detail = "");

/// <summary>What loading one bundled point extract gave: its sites (none when it could not be loaded) and its citation.</summary>
/// <param name="Name">The status dot's name ("GEM").</param>
/// <param name="Problem">Why the extract was not loaded; null when it was.</param>
public sealed record BundledPointsLoad(string Name, string Folder, DatasetManifest? Manifest, SourceCitation? Citation,
    IReadOnlyList<MarkerPoint> Points, IReadOnlyList<ManifestCode> NotPlaced, string? Problem)
{
    public bool Loaded => Problem is null && Manifest is not null && Citation is not null;
}

/// <summary>
/// Loads a bundled point extract (<c>data\datasets\gem-&lt;kind&gt;\data.csv</c> with its <c>manifest.json</c>, P3-9, P3-10):
/// the columns <see cref="Columns"/>, one row per site, all cited to the manifest's source and release. As
/// <see cref="BundledDataset"/>, a missing or broken extract is logged and its layer says "Data not loaded yet"; it never
/// stops the app. A site whose country the current boundary set lacks is not drawn and is listed "Not placed on the map".
/// </summary>
public static class BundledPoints
{
    /// <summary>The columns every point extract's <c>data.csv</c> has, in this order (the source's own columns may follow).</summary>
    public static IReadOnlyList<string> Columns { get; } = ["name", "iso3", "lat", "lon", "capacity", "unit", "status", "type"];

    /// <summary>An optional column: the source's own detail for a site (<see cref="MarkerPoint.Detail"/>).</summary>
    public const string DetailColumn = "detail";

    /// <param name="name">The status dot's name, used in the log too.</param>
    /// <param name="isPlaced">True when the boundary set has a country for the ISO3 code (C16); null: every code is.</param>
    public static BundledPointsLoad Load(string name, string folder, Func<string, bool>? isPlaced, IAppLog log)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(folder);
        ArgumentNullException.ThrowIfNull(log);
        try
        {
            var manifest = DatasetManifest.Parse(File.ReadAllText(Path.Combine(folder, BundledDataset.ManifestFile)));
            var citation = Citation(manifest);
            var points = new List<MarkerPoint>();
            var notPlaced = new List<ManifestCode>(manifest.NotPlaced);
            var unplacedSites = new Dictionary<string, int>(StringComparer.Ordinal);
            using (var reader = new StreamReader(Path.Combine(folder, BundledDataset.DataFile), System.Text.Encoding.UTF8))
            {
                using var rows = CsvReader.Read(reader).GetEnumerator();
                if (!rows.MoveNext()) throw new InvalidDataException("data.csv is empty");
                var col = Columns.Select(c => Array.IndexOf(rows.Current, c)).ToArray();
                if (col.Any(i => i < 0)) throw new InvalidDataException("data.csv lacks a column of: " + string.Join(", ", Columns));
                int detailCol = Array.IndexOf(rows.Current, DetailColumn);
                int line = 1;
                while (rows.MoveNext())
                {
                    line++;
                    var r = rows.Current;
                    if (r.Length <= col.Max()) throw new InvalidDataException($"data.csv line {line} is short");
                    var iso3 = r[col[1]].Trim().ToUpperInvariant();
                    if (isPlaced is not null && !isPlaced(iso3))
                    {
                        unplacedSites[iso3] = unplacedSites.GetValueOrDefault(iso3) + 1;
                        continue;
                    }
                    double lat = Number(r[col[2]], line, "lat"), lon = Number(r[col[3]], line, "lon");
                    if (lat is < -90 or > 90 || lon is < -180 or > 180) throw new InvalidDataException($"data.csv line {line}: {lat}, {lon} is not a position");
                    double? capacity = string.IsNullOrWhiteSpace(r[col[4]]) ? null : Number(r[col[4]], line, "capacity");
                    points.Add(new MarkerPoint(r[col[0]], iso3, lat, lon, capacity, r[col[5]], r[col[6]], r[col[7]],
                        detailCol >= 0 && detailCol < r.Length ? r[detailCol] : ""));
                }
            }
            foreach (var (iso3, n) in unplacedSites.OrderBy(kv => kv.Key, StringComparer.Ordinal))
            {
                notPlaced.Add(new ManifestCode(iso3, iso3, $"no unit in the boundary set for this code: {n} {(n == 1 ? "site" : "sites")} not drawn"));
                log.Warn($"{name}: code {iso3} has no unit in the boundary set; {n} sites listed as not placed");
            }
            log.Info(string.Create(CultureInfo.InvariantCulture,
                $"{name}: bundled {manifest.Id} loaded: {points.Count:N0} sites ({manifest.Filter}); {notPlaced.Count} codes not placed"));
            return new BundledPointsLoad(name, folder, manifest, citation, points, notPlaced, null);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException or FormatException or OverflowException
                                       or ArgumentException)
        {
            var problem = ex is FileNotFoundException or DirectoryNotFoundException ? "not found" : ex.Message;
            log.Warn($"{name}: the bundled data in {folder} could not be loaded ({problem}); its layer says \"Data not loaded yet\"", ex);
            return new BundledPointsLoad(name, folder, null, null, [], [], problem);
        }
    }

    /// <summary>
    /// The manifest's source as a citation: a tracker release (<see cref="CitationDate.Release"/>, its <c>dateText</c>
    /// "September 2026 (V2) release"), dated the manifest's <c>published</c> day for ordering only.
    /// </summary>
    public static SourceCitation Citation(DatasetManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        var meaning = Enum.TryParse<CitationDate>(manifest.DateMeaning, ignoreCase: false, out var m)
            ? m : throw new InvalidDataException($"unknown dateMeaning \"{manifest.DateMeaning}\"");
        if (meaning is not CitationDate.Release) throw new InvalidDataException($"a point extract's dateMeaning is Release, not {meaning}");
        if (string.IsNullOrWhiteSpace(manifest.DateText)) throw new InvalidDataException("a Release manifest needs its dateText (\"September 2026 (V2) release\")");
        if (string.IsNullOrWhiteSpace(manifest.Filter)) throw new InvalidDataException("a point extract's manifest needs its filter (P3-9)");
        var page = Uri.TryCreate(manifest.PageUrl, UriKind.Absolute, out var u) ? u : throw new InvalidDataException("pageUrl is not a URL");
        return new SourceCitation(manifest.Publisher, manifest.Dataset, manifest.Version, page, manifest.Licence, manifest.Attribution,
            Day(manifest.Published, "published"), Day(manifest.Downloaded, "downloaded"), meaning) { DateText = manifest.DateText.Trim() };
    }

    private static double Number(string text, int line, string column) =>
        double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) && double.IsFinite(v)
            ? v : throw new InvalidDataException($"data.csv line {line}: {column} \"{text}\" is not a number");

    private static DateTime Day(string text, string field) =>
        DateTime.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var d)
            ? DateTime.SpecifyKind(d, DateTimeKind.Utc)
            : throw new InvalidDataException($"{field} \"{text}\" is not yyyy-MM-dd");
}
