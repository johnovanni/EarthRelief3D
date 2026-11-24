using System.Data;
using System.Globalization;
using System.IO.Compression;
using System.Text;
using EarthRelief3D.Core.Feeds;
using ExcelDataReader;

namespace EarthRelief3D.DataTool;

/// <summary>
/// <c>aqueduct &lt;zip&gt;</c>: WRI's Aqueduct 4.0 country rankings (docs/data-sources.md §10) to
/// <c>data\datasets\aqueduct-water-stress</c>: sheet <c>country_baseline</c>, <c>indicator_name = bws</c> (baseline water
/// stress), <c>weight = Tot</c> (all sectors), with WRI's columns <c>gid_0, score, cat, label</c>. The value is the
/// category (<c>cat</c>: -1 arid and low water use, 0 to 4 low to extremely high, -9999 no data) and the qualifier WRI's
/// own label, shown as written.
/// </summary>
public static class AqueductExtractor
{
    public const string FolderName = "aqueduct-water-stress";
    public const string WaterStress = "wri.bws";
    public const string Sheet = "country_baseline";
    public const string SourceUrl = "https://files.wri.org/aqueduct/aqueduct-4-0-country-rankings.zip";
    public const string PageUrl = "https://www.wri.org/data/aqueduct-40-country-rankings";
    /// <summary>The baseline period's last year (Aqueduct 4.0's baseline is 1979 to 2019, per the workbook's Read Me): the figures' year.</summary>
    public const int BaselineEndYear = 2019;
    public const string Measures = "Baseline water stress (WRI Aqueduct 4.0 category; baseline period 1979 to 2019)";

    /// <summary>WRI's citation from the workbook's Read Me ("Citations"), as Task 1 recorded it.</summary>
    public const string Citation = "Kuzma, S., M.F.P. Bierkens, S. Lakshman, T. Luo, L. Saccoccia, E. H. Sutanudjaja, and R. Van Beek. 2023. "
        + "Aqueduct 4.0: Updated decision-relevant global water risk indicators. Technical Note. Washington, DC: World Resources Institute. "
        + "Available online at: doi.org/10.46830/writn.23.00061.";

    /// <summary>Ruling R6: what confirms the licence (the workbook names none).</summary>
    public const string LicenceNote = "Licence: WRI's Open Data Commitment (https://www.wri.org/about/open-data-commitment, read 2026-11-23) says WRI has "
        + "adopted the Creative Commons Attribution 4.0 International License as its standard licence for publications and data (ruling R6). "
        + "The workbook's Read Me names no licence; WRI's landing page says \"Creative Commons\".";

    private static readonly string[] Needed = ["gid_0", "name_0", "indicator_name", "weight", "score", "cat", "label"];

    private sealed record Row(string Gid, string Name, double Cat, string Label, string Score);

    /// <exception cref="InvalidDataException">The workbook is not in the shape of 2026-11-23 (a format change).</exception>
    public static Extract Run(SourceFile src, CountryCodes codes)
    {
        ArgumentNullException.ThrowIfNull(src);
        ArgumentNullException.ThrowIfNull(codes);
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);   // ExcelDataReader needs it on .NET
        using var zip = ZipFile.OpenRead(src.Path);
        var entry = zip.Entries.FirstOrDefault(e => e.Name.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidDataException("Aqueduct: the zip has no .xlsx workbook");
        using var book = new MemoryStream();
        using (var s = entry.Open()) s.CopyTo(book);
        book.Position = 0;

        DataTable table;
        using (var reader = ExcelReaderFactory.CreateOpenXmlReader(book))
        {
            var set = reader.AsDataSet(new ExcelDataSetConfiguration
            {
                ConfigureDataTable = _ => new ExcelDataTableConfiguration { UseHeaderRow = true },
            });
            table = set.Tables[Sheet] ?? throw new InvalidDataException($"Aqueduct: the workbook has no sheet \"{Sheet}\" (format changed)");
        }
        foreach (var c in Needed)
            if (!table.Columns.Contains(c)) throw new InvalidDataException($"Aqueduct: {Sheet} has no column \"{c}\" (format changed)");

        var rows = new List<Row>();
        var labels = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (DataRow r in table.Rows)
        {
            if (Text(r["indicator_name"]) != "bws" || Text(r["weight"]) != "Tot") continue;
            var cat = Number(r["cat"], "cat", Text(r["gid_0"]));
            var label = Text(r["label"]);
            Tally.Add(labels, $"{DatasetWriter.Number(cat)}: {label}");
            var score = r["score"] is double d ? DatasetWriter.Number(d) : Text(r["score"]);
            rows.Add(new Row(Text(r["gid_0"]), Text(r["name_0"]), cat, label, score));
        }
        if (rows.Count == 0) throw new InvalidDataException("Aqueduct: no bws rows with weight Tot (format changed)");
        foreach (var g in rows.GroupBy(r => r.Gid).Where(g => g.Count() > 1))
            throw new InvalidDataException($"Aqueduct: {g.Key} has {g.Count()} bws Tot rows");

        // Placement: by code first; a name places a code only for a country no code took.
        var placed = new Dictionary<Row, string>();
        var taken = new HashSet<string>(StringComparer.Ordinal);
        var notes = new List<string> { LicenceNote };
        foreach (var r in rows)
            if (codes.FromIso3(r.Gid) is { } iso && taken.Add(iso)) placed[r] = iso;
        var notPlaced = new List<ManifestCode>();
        foreach (var r in rows.Where(r => !placed.ContainsKey(r)))
        {
            if (codes.FromName(r.Name) is { } iso && taken.Add(iso))
            {
                placed[r] = iso;
                notes.Add($"{r.Gid} ({r.Name}) placed by its name as {iso}.");
            }
            else notPlaced.Add(new ManifestCode(r.Gid, r.Name, "no unit in the boundary set for this code or name"));
        }

        var outRows = rows.Where(placed.ContainsKey).Select(r => new ExtractRow(WaterStress, placed[r], BaselineEndYear, r.Cat,
            r.Label.Length == 0 ? null : r.Label, new Dictionary<string, string>
            {
                ["gid_0"] = r.Gid, ["score"] = r.Score, ["cat"] = DatasetWriter.Number(r.Cat), ["label"] = r.Label,
            })).ToList();

        notes.Add("Read Me cautions: the indicators cannot be measured directly and have not been validated; interbasin transfers are not modelled; "
            + "compare the 2023 data with the 2019 release only with caution.");
        notes.Add($"Rows: sheet {Sheet}, indicator_name bws, weight Tot (the other weights are Dom, Ind, Irr, Liv and One). "
            + $"The figures' year is {BaselineEndYear}, the end of the 1979-2019 baseline; WRI published Aqueduct 4.0 in 2023.");
        notes.Add("WRI's -9999 \"NoData\" rows are kept as WRI wrote them; the map shows them as no figure.");
        if (!labels.Keys.Any(k => k.StartsWith("-1:", StringComparison.Ordinal)))
            notes.Add("No country row has cat -1 (\"Arid and Low Water Use\") in this file; the app's scale keeps that class for basins and later releases.");
        notes.Add("Check: categories and labels seen (cat: label, rows): " + string.Join("; ", labels.OrderBy(kv => double.Parse(kv.Key[..kv.Key.IndexOf(':')], CultureInfo.InvariantCulture))
            .Select(kv => $"{kv.Key} ({kv.Value})")) + ".");

        var manifest = new DatasetManifest
        {
            Id = FolderName,
            Publisher = "World Resources Institute",
            Dataset = "Aqueduct 4.0 country rankings: baseline water stress",
            Version = "Aqueduct 4.0, workbook " + Path.GetFileNameWithoutExtension(entry.Name),
            SourceUrl = SourceUrl,
            PageUrl = PageUrl,
            Licence = "CC BY 4.0",
            Attribution = $"World Resources Institute, Aqueduct 4.0 country rankings ({src.Published.Year}); CC BY 4.0. {Citation}",
            // A modelled baseline (final fix wave I3): the app shows the period and the publication year, never "2019".
            DateMeaning = "Baseline",
            DateText = $"baseline 1979–{BaselineEndYear} (published {src.Published.Year})",
            AsOf = $"baseline period 1979 to {BaselineEndYear} (Aqueduct 4.0, published {src.Published.Year})",
            Published = src.Published.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            Downloaded = src.Downloaded.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            FileName = src.FileName,
            FileSha256 = src.Sha256(),
            Command = src.Command("aqueduct"),
            Series = [new ManifestSeries(WaterStress, Measures, "category", $"{Sheet}: indicator_name bws, weight Tot (cat and label)", outRows.Count)],
            NotPlaced = notPlaced,
            QualifiersSeen = Tally.Sorted(labels),
            Notes = notes,
        };
        return new Extract(manifest, outRows, ["gid_0", "score", "cat", "label"]);
    }

    private static string Text(object? cell) => cell switch
    {
        null or DBNull => "",
        double d => DatasetWriter.Number(d),
        _ => Convert.ToString(cell, CultureInfo.InvariantCulture)?.Trim() ?? "",
    };

    private static double Number(object? cell, string column, string gid) => cell switch
    {
        double d => d,
        string s when double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) => v,
        _ => throw new InvalidDataException($"Aqueduct: {gid}: {column} \"{cell}\" is not a number"),
    };
}
