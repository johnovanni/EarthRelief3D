using System.Globalization;
using System.IO.Compression;
using System.Text.Json;
using System.Text.RegularExpressions;
using EarthRelief3D.Core.Feeds;

namespace EarthRelief3D.DataTool;

/// <summary>
/// <c>eia &lt;INTL.zip&gt;</c>: EIA's International bulk file (docs/data-sources.md §9) to <c>data\datasets\eia-crude-oil</c>:
/// crude oil including lease condensate, production, thousand barrels per day, each country's latest full year.
/// </summary>
/// <remarks>
/// EIA's file has no per-value estimate flag (checked 2026-11-23): each series has a frequency <c>f</c> ("A", annual) and
/// a value of "--" means none. So a year counts as full when the series is annual and was last updated after that year
/// ended; the flags seen are recorded in the manifest. Region series (a <c>geography</c> listing several countries, or a
/// region code standing for one country's series) are dropped, and so are series with no figure in the last
/// <see cref="RecentYears"/> years (former countries such as the USSR).
/// </remarks>
public static partial class EiaExtractor
{
    public const string FolderName = "eia-crude-oil";
    public const string CrudeOil = "eia.crudeOil";
    public const string SourceUrl = "https://www.eia.gov/opendata/bulk/INTL.zip";
    /// <summary>EIA's international overview (HTTP 200 to the app's User-Agent on 2026-11-23; the bulk page answers 403).</summary>
    public const string PageUrl = "https://www.eia.gov/international/";
    public const string Unit = "thousand barrels per day";
    public const string Measures = "Crude oil production, including lease condensate, in thousand barrels per day";
    public const int RecentYears = 10;

    private const string NamePrefix = "Crude oil including lease condensate production, ";
    private const string NameSuffix = ", Annual";

    [GeneratedRegex(@"^INTL\.57-1-([A-Z0-9]+)-TBPD\.A$")]
    private static partial Regex SeriesId();

    private sealed record Candidate(string Code, string Name, int Year, double Value, DateTimeOffset Updated);

    /// <exception cref="InvalidDataException">The file is not in the shape of 2026-11-23 (a format change).</exception>
    public static Extract Run(SourceFile src, CountryCodes codes)
    {
        ArgumentNullException.ThrowIfNull(src);
        ArgumentNullException.ThrowIfNull(codes);
        using var zip = ZipFile.OpenRead(src.Path);
        var entry = zip.GetEntry("INTL.txt") ?? throw new InvalidDataException("EIA: the zip has no INTL.txt");

        var flags = new Dictionary<string, int>(StringComparer.Ordinal);
        var dropped = new List<ManifestCode>();
        var notes = new List<string>();
        var candidates = new List<Candidate>();
        var noFigure = new List<(string Code, string Name, int? Last)>();
        using (var reader = new StreamReader(entry.Open()))
        {
            string? line;
            int n = 0;
            while ((line = reader.ReadLine()) is not null)
            {
                n++;
                if (line.Length == 0 || !line.Contains("\"series_id\"", StringComparison.Ordinal)) continue;
                using var doc = Parse(line, n);
                var o = doc.RootElement;
                var id = Str(o, "series_id");
                var match = SeriesId().Match(id);
                if (!match.Success) continue;
                var code = match.Groups[1].Value;
                var fullName = Str(o, "name");
                if (Str(o, "units") != Unit) throw new InvalidDataException($"EIA: {id} is in \"{Str(o, "units")}\", not {Unit} (format changed)");
                if (!fullName.StartsWith(NamePrefix, StringComparison.Ordinal) || !fullName.EndsWith(NameSuffix, StringComparison.Ordinal))
                    throw new InvalidDataException($"EIA: {id} is named \"{fullName}\" (format changed)");
                var name = fullName[NamePrefix.Length..^NameSuffix.Length];
                var f = Str(o, "f");
                Tally.Add(flags, "f=" + f);
                var geography = Str(o, "geography");
                if (!DateTimeOffset.TryParse(Str(o, "last_updated"), CultureInfo.InvariantCulture, DateTimeStyles.None, out var updated))
                    throw new InvalidDataException($"EIA: {id} has no readable last_updated");

                // Every year with a number; "--" (no value) and any other text are counted as flags.
                var years = new List<(int Year, double Value)>();
                foreach (var p in o.GetProperty("data").EnumerateArray())
                {
                    if (p.GetArrayLength() < 2 || !int.TryParse(p[0].GetString(), NumberStyles.None, CultureInfo.InvariantCulture, out var y))
                        throw new InvalidDataException($"EIA: {id} has a data point that is not [year, value]");
                    if (p[1].ValueKind == JsonValueKind.Number) years.Add((y, p[1].GetDouble()));
                    else Tally.Add(flags, $"value \"{p[1]}\"");
                }

                if (geography.Contains('+', StringComparison.Ordinal))
                {
                    dropped.Add(new ManifestCode(code, name, $"aggregate of {geography.Split('+').Length} countries: dropped"));
                    continue;
                }
                if (!string.Equals(geography, code, StringComparison.Ordinal))
                {
                    dropped.Add(new ManifestCode(code, name, $"EIA region code for {geography}, whose own series is kept: dropped so it is not counted twice"));
                    continue;
                }
                // The newest year that is over: annual, and updated after the year ended.
                var full = years.Where(y => f == "A" && updated.UtcDateTime >= new DateTime(y.Year + 1, 1, 1, 0, 0, 0, DateTimeKind.Utc))
                    .OrderByDescending(y => y.Year).ToList();
                foreach (var y in years.Where(y => !full.Contains(y)).OrderByDescending(y => y.Year))
                    notes.Add($"{code} {y.Year}: series updated {updated:yyyy-MM-dd}, before the year ended: not a full year, so not used"
                        + (full.Count > 0 ? $" ({code} {full[0].Year} kept)." : "."));
                if (full.Count == 0) { noFigure.Add((code, name, null)); continue; }
                candidates.Add(new Candidate(code, name, full[0].Year, full[0].Value, updated));
            }
        }
        if (candidates.Count == 0) throw new InvalidDataException("EIA: no country series of crude oil production found (format changed)");

        // Former countries and series with no recent figure are dropped, with the year of their last figure.
        int newest = candidates.Max(c => c.Year);
        foreach (var c in candidates.Where(c => c.Year <= newest - RecentYears).ToList())
        {
            candidates.Remove(c);
            noFigure.Add((c.Code, c.Name, c.Year));
        }
        foreach (var (code, name, last) in noFigure)
            dropped.Add(new ManifestCode(code, name, last is { } y
                ? $"no figure since {y} (a former country, or no recent data): dropped"
                : "no full-year figure in the file: dropped"));

        // Placement: by code first; a name places a code only for a country no code took (EIA's XKS is Natural Earth's KOS).
        var placed = new Dictionary<string, Candidate>(StringComparer.Ordinal);
        var unplaced = new List<Candidate>();
        foreach (var c in candidates)
        {
            if (codes.FromIso3(c.Code) is not { } iso) { unplaced.Add(c); continue; }
            if (!placed.TryAdd(iso, c)) throw new InvalidDataException($"EIA: {placed[iso].Code} and {c.Code} both map to {iso} (it would be counted twice)");
        }
        var notPlaced = new List<ManifestCode>();
        foreach (var c in unplaced)
        {
            if (codes.FromName(c.Name) is { } iso && placed.TryAdd(iso, c))
                notes.Add($"{c.Code} ({c.Name}) placed by its name as {iso}.");
            else if (c.Value == 0)   // ruling R16: nothing to show, so recorded here and not listed "Not placed on the map"
                dropped.Add(new ManifestCode(c.Code, c.Name,
                    $"no unit in the boundary set, and production was 0 in {c.Year}: recorded here, not listed in the app (ruling R16)"));
            else
                notPlaced.Add(new ManifestCode(c.Code, c.Name, "no unit in the boundary set for this code or name"));
        }

        var rows = placed.OrderBy(kv => kv.Key, StringComparer.Ordinal).Select(kv => new ExtractRow(CrudeOil, kv.Key, kv.Value.Year,
            kv.Value.Value, null, new Dictionary<string, string>
            {
                ["eia_code"] = kv.Value.Code, ["name"] = kv.Value.Name, ["last_updated"] = kv.Value.Updated.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            })).ToList();
        var top = rows.OrderByDescending(r => r.Value).Take(5).ToList();
        notes.Insert(0, "EIA's file has no per-value estimate flag: each series has a frequency f (all \"A\", annual) and \"--\" marks no value. "
            + "A year counts as full when the series was last updated after that year ended; each country's newest full year is kept.");
        notes.Insert(1, $"Series with no figure in the last {RecentYears} years of the file (newest year {newest}) are dropped as former countries or stale.");
        notes.Add("Check: top 5 producers: " + string.Join("; ", top.Select(r => $"{r.Iso3} {r.Value.ToString("N0", CultureInfo.InvariantCulture)} ({r.Year})")) + ".");
        var yearsKept = rows.Select(r => r.Year).Distinct().Order().ToList();

        var manifest = new DatasetManifest
        {
            Id = FolderName,
            Publisher = "U.S. Energy Information Administration",
            Dataset = "International Energy Statistics: crude oil including lease condensate production",
            Version = $"INTL bulk file of {src.Published:yyyy-MM-dd}",
            SourceUrl = SourceUrl,
            PageUrl = PageUrl,
            Licence = "Public domain (U.S. government work)",
            Attribution = $"Source: U.S. Energy Information Administration ({src.Published.ToString("MMM yyyy", CultureInfo.InvariantCulture)})",
            DateMeaning = "EstimateYear",
            AsOf = yearsKept.Count == 1 ? $"{yearsKept[0]} (each country's latest full year)" : $"each country's latest full year, {yearsKept[0]} to {yearsKept[^1]}",
            Published = src.Published.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            Downloaded = src.Downloaded.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            FileName = src.FileName,
            FileSha256 = src.Sha256(),
            Command = src.Command("eia"),
            Series = [new ManifestSeries(CrudeOil, Measures, Unit, "INTL.57-1-<country>-TBPD.A, " + NamePrefix.TrimEnd(',', ' '), rows.Count)],
            NotPlaced = notPlaced,
            Dropped = dropped,
            FlagsSeen = Tally.Sorted(flags),
            Notes = notes,
        };
        return new Extract(manifest, rows, ["eia_code", "name", "last_updated"]);
    }

    private static JsonDocument Parse(string line, int n)
    {
        try { return JsonDocument.Parse(line); }
        catch (JsonException ex) { throw new InvalidDataException($"EIA: INTL.txt line {n} is not JSON", ex); }
    }

    private static string Str(JsonElement o, string key) =>
        o.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() ?? "" : "";
}
