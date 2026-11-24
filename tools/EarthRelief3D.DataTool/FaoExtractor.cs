using System.Globalization;
using System.IO.Compression;
using System.Text;
using EarthRelief3D.Core.Feeds;

namespace EarthRelief3D.DataTool;

/// <summary>
/// <c>fao &lt;zip&gt;</c>: FAOSTAT's Suite of Food Security Indicators (docs/data-sources.md §8) to
/// <c>data\datasets\fao-food-security</c>: prevalence of undernourishment and dietary energy supply adequacy, each country's
/// latest 3-year average. The file is Latin-1. Areas are M49 codes, placed by <see cref="CountryCodes.FromM49"/> (P3-11):
/// "China, mainland" (FAO 41, M49 156) is CHN, FAO's "China" aggregate (351) and every other aggregate are dropped, and an
/// M49 code with no country is listed in the manifest.
/// </summary>
public static class FaoExtractor
{
    public const string FolderName = "fao-food-security";
    public const string Undernourishment = "fao.undernourishment";
    public const string EnergyAdequacy = "fao.energyAdequacy";
    public const string SourceUrl = "https://bulks-faostat.fao.org/production/Food_Security_Data_E_All_Data_(Normalized).zip";
    public const string PageUrl = "https://www.fao.org/faostat/en/#data/FS";

    private sealed record Item(string Code, string Name, string SeriesId, string Measures);

    private static readonly Item[] Items =
    [
        new("210041", "Prevalence of undernourishment (percent) (3-year average)", Undernourishment,
            "Prevalence of undernourishment (share of people, 3-year average)"),
        new("21010", "Average dietary energy supply adequacy (percent) (3-year average)", EnergyAdequacy,
            "Average dietary energy supply adequacy (supply as a share of the average requirement, 3-year average)"),
    ];

    /// <summary>FAO's "China" (mainland, Hong Kong, Macao and Taiwan together): dropped so China is counted once (P3-11).</summary>
    public const int ChinaAggregate = 351;

    /// <summary>
    /// FAO area codes of regions and country groups: 5000 and above (World, continents, LDCs, income groups at 9010 and
    /// up), 351 (China) and 420 (Sub-Saharan Africa), as in the file's AreaCodes list of 2026-11-23.
    /// </summary>
    public static bool IsAggregate(int areaCode) => areaCode >= 5000 || areaCode is ChinaAggregate or 420;

    private static readonly string[] Needed = ["Area Code", "Area Code (M49)", "Area", "Item Code", "Item", "Year", "Unit", "Value", "Flag"];

    /// <exception cref="InvalidDataException">The file is not in the shape of 2026-11-23 (a format change).</exception>
    public static Extract Run(SourceFile src, CountryCodes codes)
    {
        ArgumentNullException.ThrowIfNull(src);
        ArgumentNullException.ThrowIfNull(codes);
        using var zip = ZipFile.OpenRead(src.Path);
        var entry = zip.Entries.FirstOrDefault(e => e.Name.Contains("All_Data", StringComparison.Ordinal) && e.Name.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidDataException("FAO: the zip has no *All_Data*.csv");

        var latest = new Dictionary<(string Series, string Iso3), (ExtractRow Row, int AreaCode)>();
        var notPlaced = new List<ManifestCode>();
        var dropped = new List<ManifestCode>();
        var qualifiers = new Dictionary<string, int>(StringComparer.Ordinal);
        var flags = new Dictionary<string, int>(StringComparer.Ordinal);
        var itemsSeen = new HashSet<string>(StringComparer.Ordinal);

        using (var reader = new StreamReader(entry.Open(), Encoding.Latin1))
        {
            using var rows = CsvReader.Read(reader).GetEnumerator();
            if (!rows.MoveNext()) throw new InvalidDataException("FAO: the CSV is empty");
            var header = rows.Current;
            var col = Needed.ToDictionary(n => n, n => Array.IndexOf(header, n));
            if (col.FirstOrDefault(kv => kv.Value < 0) is { Key: { } missing }) throw new InvalidDataException($"FAO: column \"{missing}\" is missing");
            int width = col.Values.Max();
            int line = 1;
            while (rows.MoveNext())
            {
                line++;
                var r = rows.Current;
                if (r.Length <= width) throw new InvalidDataException($"FAO: line {line} is short");
                var item = Items.FirstOrDefault(i => i.Code == r[col["Item Code"]]);
                if (item is null) continue;
                if (r[col["Item"]] != item.Name)
                    throw new InvalidDataException($"FAO: item {item.Code} is now \"{r[col["Item"]]}\", not \"{item.Name}\" (format changed)");
                if (r[col["Unit"]] != "%") throw new InvalidDataException($"FAO: line {line}: unit \"{r[col["Unit"]]}\", not %");
                itemsSeen.Add(item.Code);
                var flag = r[col["Flag"]].Trim();
                Tally.Add(flags, flag.Length == 0 ? "(none)" : flag);
                var text = r[col["Value"]].Trim();
                string? qualifier = null;
                if (text.StartsWith('<') || text.StartsWith('>'))
                {
                    qualifier = text;
                    Tally.Add(qualifiers, text);
                    text = text[1..].Trim();
                }
                if (text.Length == 0) continue;   // missing (O, Q, L): an older range may be the country's latest figure
                if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                    throw new InvalidDataException($"FAO: line {line}: value \"{r[col["Value"]]}\" is not a number");

                int areaCode = int.Parse(r[col["Area Code"]], NumberStyles.Integer, CultureInfo.InvariantCulture);
                var m49 = r[col["Area Code (M49)"]].Trim().TrimStart('\'');
                var area = r[col["Area"]];
                if (IsAggregate(areaCode))
                {
                    if (!dropped.Any(d => d.Code == $"FAO {areaCode}"))
                        dropped.Add(new ManifestCode($"FAO {areaCode}", area, areaCode == ChinaAggregate
                            ? "FAO's China aggregate (mainland, Hong Kong, Macao and Taiwan): dropped so China is counted once (P3-11)"
                            : "aggregate (region or country group): dropped (P3-11)"));
                    continue;
                }
                if (codes.FromM49(m49) is not { } iso3)
                {
                    if (!notPlaced.Any(n => n.Code == $"M49 {m49}"))
                        notPlaced.Add(new ManifestCode($"M49 {m49}", area, "no country in the boundary set has this M49 code (Natural Earth UN_A3 or ISO_N3_EH)"));
                    continue;
                }
                var (year, period) = Year(r[col["Year"]], line);
                var row = new ExtractRow(item.SeriesId, iso3, year, value, qualifier, new Dictionary<string, string>
                {
                    ["period"] = period, ["m49"] = m49, ["area"] = area, ["flag"] = flag,
                });
                var key = (item.SeriesId, iso3);
                if (latest.TryGetValue(key, out var had))
                {
                    if (had.AreaCode != areaCode)
                        throw new InvalidDataException($"FAO: areas {had.AreaCode} and {areaCode} both map to {iso3} (it would be counted twice)");
                    if (had.Row.Year >= year) continue;
                }
                latest[key] = (row, areaCode);
            }
        }
        if (itemsSeen.Count == 0) throw new InvalidDataException("FAO: neither item is in the file (format changed)");
        var missingItems = Items.Where(i => !itemsSeen.Contains(i.Code)).Select(i => $"item {i.Code} \"{i.Name}\" is not in the file: its series is empty.");

        var rowsOut = Items.SelectMany(i => latest.Values.Select(v => v.Row).Where(r => r.Series == i.SeriesId).OrderBy(r => r.Iso3, StringComparer.Ordinal)).ToList();
        var pou = rowsOut.Where(r => r.Series == Undernourishment).ToList();
        var periods = rowsOut.Select(r => r.Extra["period"]).Distinct().Order(StringComparer.Ordinal).ToList();
        List<string> notes =
        [
            "Read as Latin-1 (FAO's encoding). Areas are M49 codes, placed through Natural Earth's UN_A3 (ISO_N3_EH where UN_A3 is missing: Norway, Taiwan); P3-11.",
            "A 3-year average such as 2023-2025 is stored with the year 2025, the last of its three, and the range in the period column; "
                + "dateMeaning ThreeYearAverage makes the app show every figure with its range (\"average 2023–2025\"), never as one year.",
            "Only 3-year ranges are read: a single-year value of these items would be refused by the app's loader (period must be the 3 years ending in year).",
            "Values written \"<x\" are stored as x with the qualifier \"<x\" and shown \"under x%\" (ruling R3). Qualifiers seen: "
                + (qualifiers.Count == 0 ? "none" : string.Join(", ", qualifiers.Select(q => $"\"{q.Key}\" ({q.Value} rows)"))) + ".",
            "Rows with no value (flag O, Q or L) are skipped, so a country's latest figure can be an older range than the newest in the file.",
            .. missingItems,
        ];
        foreach (var iso in new[] { "SDN", "YEM", "HTI" })
            notes.Add(pou.FirstOrDefault(r => r.Iso3 == iso) is { } r
                ? $"Check: {iso} undernourishment {r.Qualifier ?? DatasetWriter.Number(r.Value)}% ({r.Extra["period"]})."
                : $"Check: {iso} has no undernourishment figure in FAO's file (every range is missing).");

        var manifest = new DatasetManifest
        {
            Id = FolderName,
            Publisher = "Food and Agriculture Organization of the United Nations (FAO)",
            Dataset = "FAOSTAT: Suite of Food Security Indicators",
            Version = $"FAOSTAT domain FS, file of {src.Published:yyyy-MM-dd}",
            SourceUrl = SourceUrl,
            PageUrl = PageUrl,
            Licence = "CC BY 4.0",
            Attribution = $"FAO. {src.Published.Year}. FAOSTAT: Suite of Food Security Indicators. Accessed on "
                + $"{src.Downloaded.ToString("d MMMM yyyy", CultureInfo.InvariantCulture)}. {PageUrl} Licence: CC-BY-4.0.",
            DateMeaning = "ThreeYearAverage",
            AsOf = periods.Count == 0 ? "no figures" : $"each country's latest 3-year average, {periods[0]} to {periods[^1]} (FAO's estimates, flag E)",
            Published = src.Published.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            Downloaded = src.Downloaded.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            FileName = src.FileName,
            FileSha256 = src.Sha256(),
            Command = src.Command("fao"),
            Series = [.. Items.Select(i => new ManifestSeries(i.SeriesId, i.Measures, "%", $"{i.Code} {i.Name}", rowsOut.Count(r => r.Series == i.SeriesId)))],
            NotPlaced = notPlaced,
            Dropped = dropped,
            QualifiersSeen = Tally.Sorted(qualifiers),
            FlagsSeen = Tally.Sorted(flags),
            Notes = notes,
        };
        return new Extract(manifest, rowsOut, ["period", "m49", "area", "flag"]);
    }

    /// <summary>"2023-2025" to (2025, "2023-2025"); "2025" to (2025, "2025").</summary>
    private static (int Year, string Period) Year(string text, int line)
    {
        text = text.Trim();
        var last = text.Contains('-') ? text[(text.LastIndexOf('-') + 1)..] : text;
        return int.TryParse(last, NumberStyles.None, CultureInfo.InvariantCulture, out var y) && y is > 1900 and < 2200
            ? (y, text)
            : throw new InvalidDataException($"FAO: line {line}: year \"{text}\" is not a year or a range");
    }
}
