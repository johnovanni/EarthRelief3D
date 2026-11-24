using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using EarthRelief3D.Core.Model;
using EarthRelief3D.Core.Store;
using ExcelDataReader;
using GlobeCore.Feeds;
using GlobeCore.Logging;
using GlobeCore.Time;

namespace EarthRelief3D.Core.Feeds;

/// <summary>
/// ACAPS's Humanitarian Access dataset from HDX (docs/data-sources.md section 12): the overall access score (0 "No constraints"
/// to 5 "Extreme access constraints"), three pillar scores, nine indicator scores, the trend and the information gap for each
/// country with an assessment. Fetched live only, never committed (ruling P4-2: ACAPS's own licence is CC BY-NC-ND 4.0, HDX says
/// CC BY; the credit names both and the values are shown unchanged).
/// <para>
/// The edition is the XLSX resource whose <b>file name</b> names the latest month and year (<c>_dec_2025.xlsx</c>, ruling R4);
/// <c>last_modified</c> does not order the editions (HDX re-saved several together). A name that does not parse is skipped and
/// logged; none parsable is a format change. Sheet <c>csv</c> gives the country-level rows (<c>Country level</c> "Yes"); sheet
/// <c>indicators</c> is joined to them on <c>Crisis ID</c>. A score of 0 is a real figure and is kept (ruling R5). A code the
/// boundary set lacks is listed under "Not placed on the map" (C16).
/// </para>
/// </summary>
public sealed class AcapsAccessFeed(FeedHttp http, IndicatorStore indicators, CrisisStore crises, IClock clock, IAppLog log, CountryResolver? countries = null) : IFeed
{
    public const string FeedName = "ACAPS";
    public const string ScoreSeriesId = "acaps.access";
    public const string ScoreMeasures = "Humanitarian access score: 0 (no constraints) to 5 (extreme access constraints), ACAPS";
    public const string Publisher = "ACAPS, via HDX";
    public const string Dataset = "Humanitarian Access dataset";
    public const string Licence = "CC BY (as stated on HDX); ACAPS's own terms: CC BY-NC-ND 4.0";
    public static readonly Uri PackageUrl = new("https://data.humdata.org/api/3/action/package_show?id=acaps-humanitarian-access-dataset");
    public static readonly Uri PageUrl = new("https://data.humdata.org/dataset/acaps-humanitarian-access-dataset");

    private static readonly string[] Months = ["jan", "feb", "mar", "apr", "may", "jun", "jul", "aug", "sep", "oct", "nov", "dec"];
    private static readonly Regex EditionName = new(@"[-_]([A-Za-z]{3,9})-?_(\d{4})\.xlsx$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
    private static readonly Regex IndicatorColumn = new(@"^([1-9])\s*[.]\s*(.+)$", RegexOptions.CultureInvariant);
    private static readonly string[] CsvRequired = ["Crisis ID", "P1", "P2", "P3", "ACCESS", "Info_gap", "Trend", "Country level", "ISO"];

    private static readonly Lazy<bool> EncodingsRegistered = new(() =>
    {
        // ExcelDataReader needs the code-page encodings on .NET (as the DataTool registers them).
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        return true;
    });

    private readonly CountryResolver _countries = countries ?? CountryResolver.LoadDefault();

    public string Name => FeedName;
    public TimeSpan Interval => TimeSpan.FromDays(7);

    /// <summary>"December 2025 edition" from <c>…_dec_2025.xlsx</c> (three-letter English month); anything else is a format change.</summary>
    public static string EditionFromFileName(string fileName)
    {
        var (year, month) = TryEdition(fileName)
            ?? throw new FeedFormatException($"the ACAPS file name \"{fileName}\" does not end with _<mon>_<yyyy>.xlsx");
        return Edition(year, month);
    }

    private static string Edition(int year, int month) =>
        CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(month) + " " + year.ToString(CultureInfo.InvariantCulture) + " edition";

    private static (int Year, int Month)? TryEdition(string fileName)
    {
        var m = EditionName.Match(fileName);
        if (!m.Success) return null;
        // The month as ACAPS writes it: the full English name ("july") or its three-letter prefix ("jul"), ruling R9; or "sept",
        // the common four-letter short form of September (m11).
        var word = m.Groups[1].Value.ToLowerInvariant();
        if (word == "sept") word = "sep";
        var month = Enumerable.Range(1, 12).FirstOrDefault(i =>
            word == Months[i - 1] || word == CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(i).ToLowerInvariant());
        return month == 0 ? null : (int.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture), month);
    }

    public async Task<FeedRunResult> RunOnceAsync(CancellationToken ct)
    {
        var now = clock.UtcNow;
        var package = await http.GetStringAsync(PackageUrl, ct).ConfigureAwait(false);
        var (year, month, url) = FindResource(package);
        var workbook = await http.GetBytesAsync(url, ct).ConfigureAwait(false);

        var (access, score, unplaced) = Read(workbook, year, month, now);
        indicators.ReplaceAccess(access, score);
        crises.ReplaceUnplaced(Name, unplaced);
        crises.MarkChecked(Name, now);   // R12
        log.Info($"{Name}: {Edition(year, month)}, {access.Count} countries; {unplaced.Count} codes not placed");
        return FeedRunResult.Ok;
    }

    /// <summary>The XLSX resource with the latest edition in its file name (R4); names that do not parse are logged and skipped.</summary>
    private (int Year, int Month, Uri Url) FindResource(string packageJson)
    {
        (int Year, int Month, Uri Url)? best = null;
        try
        {
            using var doc = JsonDocument.Parse(packageJson);
            foreach (var r in doc.RootElement.GetProperty("result").GetProperty("resources").EnumerateArray())
            {
                if (!r.TryGetProperty("url", out var u) || !Uri.TryCreate(u.GetString(), UriKind.Absolute, out var uri)) continue;
                var file = Uri.UnescapeDataString(Path.GetFileName(uri.AbsolutePath));
                if (!file.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase)) continue;
                if (TryEdition(file) is not { } edition)
                {
                    log.Warn($"{Name}: skipping \"{file}\": no month and year edition in its name (a newer edition may be hidden)");
                    continue;
                }
                var (year, month) = edition;
                if (best is null || (year, month).CompareTo((best.Value.Year, best.Value.Month)) > 0) best = (year, month, uri);
            }
        }
        catch (Exception e) when (e is JsonException or KeyNotFoundException or InvalidOperationException)
        {
            throw new FeedFormatException("the HDX package JSON has no result.resources list");
        }
        return best ?? throw new FeedFormatException("the HDX package has no XLSX resource whose file name gives an edition (_<mon>_<yyyy>.xlsx)");
    }

    private (List<AccessAssessment> Access, IndicatorSeries Score, List<UnplacedReport> Unplaced) Read(byte[] workbook, int year, int month, DateTime now)
    {
        var edition = Edition(year, month);
        var citation = new SourceCitation(Publisher, Dataset, edition[..^" edition".Length], PageUrl, Licence,
            $"ACAPS, Humanitarian Access dataset, {edition}, via HDX ({Licence})",
            new DateTime(year, month, DateTime.DaysInMonth(year, month), 0, 0, 0, DateTimeKind.Utc), now, CitationDate.Release)
        { DateText = edition };

        var sheets = ReadSheets(workbook);
        var csv = Sheet(sheets, "csv");
        var indicatorRows = Sheet(sheets, "indicators");

        var csvHeader = Header(csv);
        var col = CsvReader.Columns(csvHeader, "the ACAPS csv sheet", CsvRequired);
        var inHeader = Header(indicatorRows);
        var inCol = CsvReader.Columns(inHeader, "the ACAPS indicators sheet", "Crisis ID");
        var numbered = new List<(int Number, string Name, int Column)>();
        for (var i = 0; i < inHeader.Length; i++)
            if (IndicatorColumn.Match(inHeader[i].Trim()) is { Success: true } m)
                numbered.Add((int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture), m.Groups[2].Value.Trim(), i));
        numbered = [.. numbered.OrderBy(n => n.Number)];
        if (!numbered.Select(n => n.Number).SequenceEqual(Enumerable.Range(1, 9)))
            throw new FeedFormatException("the ACAPS indicators sheet does not have the nine numbered indicator columns (1. to 9.)");

        var byCrisis = new Dictionary<string, object?[]>(StringComparer.Ordinal);
        foreach (var row in indicatorRows.Skip(1))
            if (Text(row, inCol["Crisis ID"]) is { Length: > 0 } id) byCrisis.TryAdd(id, row);

        var access = new List<AccessAssessment>();
        var unplaced = new List<UnplacedReport>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var notPlaced = new HashSet<string>(StringComparer.Ordinal);
        var skipped = 0;
        foreach (var row in csv.Skip(1))
        {
            if (!string.Equals(Text(row, col["Country level"]), "Yes", StringComparison.OrdinalIgnoreCase)) continue;
            var iso = Text(row, col["ISO"]).ToUpperInvariant();
            var crisisName = col.TryGetValue("Crisis_name", out var nameCol) ? Text(row, nameCol) : "";
            var total = Number(row, col["ACCESS"]);
            var pillars = new[] { Number(row, col["P1"]), Number(row, col["P2"]), Number(row, col["P3"]) };
            if (total is not { } t || t is < 0 or > 5 || pillars.Any(p => p is null or < 0 or > 5))
            {
                skipped++;
                log.Warn($"{Name}: {(iso.Length > 0 ? iso : crisisName)} has no usable ACCESS or pillar score (0 to 5); skipped");
                continue;
            }
            if (iso.Length == 0 || _countries.ByIso3(iso) is null)
            {
                if (notPlaced.Add(iso + "|" + crisisName))
                {
                    log.Warn($"{Name}: code \"{iso}\" ({crisisName}) has no unit in the boundary set; listed as not placed");
                    unplaced.Add(new UnplacedReport(Name, $"ACAPS humanitarian access for \"{(iso.Length > 0 ? iso : crisisName)}\"",
                        iso.Length > 0 ? "no unit in the boundary set for this code" : "no ISO code in the file", citation));
                }
                continue;
            }
            if (!seen.Add(iso))
            {
                skipped++;
                log.Warn($"{Name}: {iso} has more than one country-level row; the first is kept");
                continue;
            }

            var scores = new List<AccessIndicator>();
            if (byCrisis.TryGetValue(Text(row, col["Crisis ID"]), out var irow))
                foreach (var (number, name, c) in numbered)
                    // A cell that is not a 0 to 3 score (ACAPS marks an information gap with X) is not a score: left out.
                    if (Number(irow, c) is { } s && s is >= 0 and <= 3) scores.Add(new AccessIndicator(number, name, s));

            double? gap = Double(row, col["Info_gap"]);
            access.Add(new AccessAssessment(iso, t, [.. pillars.Select(p => p!.Value)], scores, Text(row, col["Trend"]), gap, edition, citation));
        }
        if (access.Count == 0 && unplaced.Count == 0)
            throw new FeedFormatException("the ACAPS csv sheet has no country-level rows with an ACCESS score");

        access = [.. access.OrderBy(a => a.Iso3, StringComparer.Ordinal)];
        var series = new IndicatorSeries(ScoreSeriesId, ScoreMeasures, "score (0 to 5)",
            [.. access.Select(a => new CountryValue(a.Iso3, a.Score, year, citation))]);
        if (skipped > 0) log.Info($"{Name}: {skipped} country-level rows skipped");
        return (access, series, unplaced);
    }

    private static Dictionary<string, List<object?[]>> ReadSheets(byte[] workbook)
    {
        _ = EncodingsRegistered.Value;
        var wanted = new HashSet<string>(["csv", "indicators"], StringComparer.OrdinalIgnoreCase);
        var sheets = new Dictionary<string, List<object?[]>>(StringComparer.OrdinalIgnoreCase);
        try
        {
            using var reader = ExcelReaderFactory.CreateOpenXmlReader(new MemoryStream(workbook));
            do
            {
                if (!wanted.Contains(reader.Name)) continue;
                var rows = new List<object?[]>();
                while (reader.Read())
                {
                    var cells = new object?[reader.FieldCount];
                    for (var i = 0; i < cells.Length; i++) cells[i] = reader.GetValue(i);
                    rows.Add(cells);
                }
                sheets[reader.Name] = rows;
            } while (reader.NextResult());
        }
        catch (Exception e) when (e is ExcelDataReader.Exceptions.ExcelReaderException or InvalidDataException or IOException
                                      or NotSupportedException or InvalidOperationException)
        {
            throw new FeedFormatException("the ACAPS file could not be read as an XLSX workbook (" + e.Message + ")");
        }
        return sheets;
    }

    private static List<object?[]> Sheet(Dictionary<string, List<object?[]>> sheets, string name) =>
        sheets.TryGetValue(name, out var rows) && rows.Count > 0 ? rows : throw new FeedFormatException($"the ACAPS workbook has no \"{name}\" sheet with rows");

    private static string[] Header(List<object?[]> rows) => [.. rows[0].Select(c => Convert.ToString(c, CultureInfo.InvariantCulture)?.Trim() ?? "")];

    private static string Text(object?[] row, int i) => i < row.Length ? Convert.ToString(row[i], CultureInfo.InvariantCulture)?.Trim() ?? "" : "";

    private static double? Double(object?[] row, int i) =>
        i < row.Length && row[i] is not null
        && double.TryParse(Convert.ToString(row[i], CultureInfo.InvariantCulture), NumberStyles.Float, CultureInfo.InvariantCulture, out var d)
        && double.IsFinite(d) ? d : null;

    /// <summary>A whole-number score; blank, text or a fraction is "not a score".</summary>
    private static int? Number(object?[] row, int i) =>
        Double(row, i) is { } d && Math.Abs(d - Math.Round(d)) < 1e-9 ? (int)Math.Round(d) : null;
}
