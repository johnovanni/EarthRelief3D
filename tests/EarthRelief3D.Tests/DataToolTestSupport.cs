using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Security;
using System.Text;
using EarthRelief3D.Core.Feeds;
using EarthRelief3D.DataTool;

namespace EarthRelief3D.Tests;

/// <summary>Tiny source files in the real shapes (docs/data-sources.md §8 to §10), zipped in a temporary folder.</summary>
internal sealed class TempDir : IDisposable
{
    public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "er3d-datatool-" + Guid.NewGuid().ToString("N"));

    public TempDir() => Directory.CreateDirectory(Path);

    public string File(string name) => System.IO.Path.Combine(Path, name);

    public void Dispose()
    {
        try { Directory.Delete(Path, recursive: true); } catch (IOException) { }
    }
}

internal static class SourceZips
{
    public static readonly DateOnly Published = new(2026, 11, 23);
    public static readonly DateOnly Downloaded = new(2026, 11, 23);

    public static CountryCodes Codes { get; } = new(CountryResolver.LoadDefault());

    /// <summary>A zip holding the given entries (name to bytes).</summary>
    public static string Zip(TempDir dir, string zipName, params (string Name, byte[] Bytes)[] entries)
    {
        var path = dir.File(zipName);
        using var zip = ZipFile.Open(path, ZipArchiveMode.Create);
        foreach (var (name, bytes) in entries)
        {
            using var s = zip.CreateEntry(name).Open();
            s.Write(bytes);
        }
        return path;
    }

    // ---------------------------------------------------------------- FAOSTAT

    public const string FaoHeader = "Area Code,Area Code (M49),Area,Item Code,Item,Element Code,Element,Year Code,Year,Unit,Value,Flag,Note";
    public const string Undernourishment = "Prevalence of undernourishment (percent) (3-year average)";
    public const string EnergyAdequacy = "Average dietary energy supply adequacy (percent) (3-year average)";

    /// <summary>One row of FAO's normalised CSV, quoted as FAO quotes it (the M49 code with its leading apostrophe).</summary>
    public static string FaoRow(int areaCode, string m49, string area, string itemCode, string item, string years, string value, string flag = "E")
    {
        var yearCode = years.Replace("-", "", StringComparison.Ordinal);
        return $"\"{areaCode}\",\"'{m49}\",\"{area}\",\"{itemCode}\",\"{item}\",\"6121\",\"Value\",\"{yearCode}\",\"{years}\",\"%\",\"{value}\",\"{flag}\",";
    }

    public static string PoU(int areaCode, string m49, string area, string years, string value, string flag = "E") =>
        FaoRow(areaCode, m49, area, "210041", Undernourishment, years, value, flag);

    public static string Adequacy(int areaCode, string m49, string area, string years, string value, string flag = "E") =>
        FaoRow(areaCode, m49, area, "21010", EnergyAdequacy, years, value, flag);

    /// <summary>FAO's zip: the normalised CSV in Latin-1 (CRLF), and the area-code list beside it.</summary>
    public static SourceFile Fao(TempDir dir, params string[] rows)
    {
        var csv = FaoHeader + "\r\n" + string.Join("\r\n", rows) + "\r\n";
        var path = Zip(dir, "Food_Security_Data_E_All_Data_(Normalized).zip",
            ("Food_Security_Data_E_All_Data_(Normalized).csv", Encoding.Latin1.GetBytes(csv)),
            ("Food_Security_Data_E_AreaCodes.csv", Encoding.Latin1.GetBytes("Area Code,M49 Code,Area\r\n")));
        return new SourceFile(path, Published, Downloaded);
    }

    // ---------------------------------------------------------------- EIA

    /// <summary>One line of EIA's INTL.txt (crude oil including lease condensate, TBPD, annual).</summary>
    public static string EiaSeries(string code, string name, string geography, string lastUpdated, params (string Year, object Value)[] data)
    {
        var points = string.Join(",", data.Select(d => d.Value is string s
            ? $"[\"{d.Year}\",\"{s}\"]"
            : $"[\"{d.Year}\",{Convert.ToDouble(d.Value, CultureInfo.InvariantCulture).ToString("R", CultureInfo.InvariantCulture)}]"));
        var start = data.Length == 0 ? "1980" : data.Min(d => d.Year);
        var end = data.Length == 0 ? "1980" : data.Max(d => d.Year);
        return $"{{\"series_id\":\"INTL.57-1-{code}-TBPD.A\",\"name\":\"Crude oil including lease condensate production, {name}, Annual\","
            + $"\"units\":\"thousand barrels per day\",\"f\":\"A\",\"copyright\":\"None\",\"source\":\"EIA, U.S. Energy Information Administration\","
            + $"\"geography\":\"{geography}\",\"start\":\"{start}\",\"end\":\"{end}\",\"last_updated\":\"{lastUpdated}\",\"geoset_id\":\"INTL.57-1-TBPD.A\","
            + $"\"data\":[{points}]}}";
    }

    /// <summary>EIA's zip: INTL.txt, one JSON object per line, with category lines and other series mixed in.</summary>
    public static SourceFile Eia(TempDir dir, params string[] lines)
    {
        string[] noise =
        [
            "{\"category_id\":\"2134978\",\"parent_category_id\":\"2134946\",\"name\":\"Crude oil including lease condensate\",\"notes\":\"\",\"childseries\":[]}",
            EiaSeries("USA", "United States", "USA", "2026-10-02T17:12:47-04:00", ("2025", 99999.0)).Replace("INTL.57-1-USA-TBPD.A", "INTL.57-1-USA-TBPD.M", StringComparison.Ordinal),
            "{\"geoset_id\":\"INTL.57-1-TBPD.A\",\"name\":\"Crude oil including lease condensate production, Annual\",\"units\":\"thousand barrels per day\"}",
        ];
        var text = string.Join("\n", noise.Concat(lines)) + "\n";
        var path = Zip(dir, "INTL.zip", ("INTL.txt", Encoding.UTF8.GetBytes(text)));
        return new SourceFile(path, new DateOnly(2026, 11, 19), Downloaded);
    }

    // ---------------------------------------------------------------- WRI Aqueduct

    public static readonly string[] AqueductColumns = ["gid_0", "name_0", "indicator_name", "weight", "score", "score_ranked", "cat", "label", "un_region", "wb_region"];

    /// <summary>One <c>country_baseline</c> row: text cells as text, numbers as numbers.</summary>
    public static object[] AqRow(string gid, string name, string indicator, string weight, double score, double cat, string label) =>
        [gid, name, indicator, weight, score, 1.0, cat, label, "Africa", "Sub-Saharan Africa"];

    /// <summary>WRI's zip: one workbook (in a folder, as WRI ships it) with a Read Me sheet and <c>country_baseline</c>.</summary>
    public static SourceFile Aqueduct(TempDir dir, params object[][] rows)
    {
        var sheets = new List<(string Name, IReadOnlyList<object[]> Rows)>
        {
            ("Read Me", [new object[] { "Aqueduct 4.0 country rankings" }]),
            ("country_baseline", [AqueductColumns.Cast<object>().ToArray(), .. rows]),
            ("country_future", [new object[] { "gid_0", "name_0" }]),
        };
        var path = Zip(dir, "aqueduct-4-0-country-rankings.zip",
            ("Aqueduct40_rankings_download_Y2023M07D05/Aqueduct40_rankings_download_Y2023M07D05.xlsx", Xlsx(sheets)));
        return new SourceFile(path, new DateOnly(2023, 8, 7), Downloaded);
    }

    /// <summary>A minimal OpenXML workbook (inline strings, no styles): enough for ExcelDataReader.</summary>
    public static byte[] Xlsx(IReadOnlyList<(string Name, IReadOnlyList<object[]> Rows)> sheets)
    {
        using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            void Add(string name, string xml)
            {
                using var w = new StreamWriter(zip.CreateEntry(name).Open(), new UTF8Encoding(false));
                w.Write(xml);
            }
            const string Main = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
            const string Rel = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
            var types = new StringBuilder("<?xml version=\"1.0\" encoding=\"UTF-8\"?><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">"
                + "<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>"
                + "<Default Extension=\"xml\" ContentType=\"application/xml\"/>"
                + "<Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>");
            var book = new StringBuilder($"<?xml version=\"1.0\" encoding=\"UTF-8\"?><workbook xmlns=\"{Main}\" xmlns:r=\"{Rel}\"><sheets>");
            var bookRels = new StringBuilder("<?xml version=\"1.0\" encoding=\"UTF-8\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">");
            for (int i = 0; i < sheets.Count; i++)
            {
                int n = i + 1;
                types.Append($"<Override PartName=\"/xl/worksheets/sheet{n}.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>");
                book.Append($"<sheet name=\"{SecurityElement.Escape(sheets[i].Name)}\" sheetId=\"{n}\" r:id=\"rId{n}\"/>");
                bookRels.Append($"<Relationship Id=\"rId{n}\" Type=\"{Rel}/worksheet\" Target=\"worksheets/sheet{n}.xml\"/>");
                var sheet = new StringBuilder($"<?xml version=\"1.0\" encoding=\"UTF-8\"?><worksheet xmlns=\"{Main}\"><sheetData>");
                for (int r = 0; r < sheets[i].Rows.Count; r++)
                {
                    sheet.Append($"<row r=\"{r + 1}\">");
                    var cells = sheets[i].Rows[r];
                    for (int c = 0; c < cells.Length; c++)
                    {
                        var at = $"{ColumnName(c)}{r + 1}";
                        sheet.Append(cells[c] switch
                        {
                            null => "",   // a blank cell (GEM's sheets have many)
                            string s => $"<c r=\"{at}\" t=\"inlineStr\"><is><t xml:space=\"preserve\">{SecurityElement.Escape(s)}</t></is></c>",
                            double d => $"<c r=\"{at}\"><v>{d.ToString("R", CultureInfo.InvariantCulture)}</v></c>",
                            _ => throw new ArgumentException("text, double or null cells only"),
                        });
                    }
                    sheet.Append("</row>");
                }
                sheet.Append("</sheetData></worksheet>");
                Add($"xl/worksheets/sheet{n}.xml", sheet.ToString());
            }
            Add("[Content_Types].xml", types.Append("</Types>").ToString());
            Add("_rels/.rels", "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">"
                + $"<Relationship Id=\"rId1\" Type=\"{Rel}/officeDocument\" Target=\"xl/workbook.xml\"/></Relationships>");
            Add("xl/workbook.xml", book.Append("</sheets></workbook>").ToString());
            Add("xl/_rels/workbook.xml.rels", bookRels.Append("</Relationships>").ToString());
        }
        return ms.ToArray();
    }

    /// <summary>A spreadsheet column's letters: 0 is A, 25 Z, 26 AA (GEM's power sheet has 54 columns).</summary>
    public static string ColumnName(int index)
    {
        var name = "";
        for (int n = index + 1; n > 0; n = (n - 1) / 26) name = (char)('A' + (n - 1) % 26) + name;
        return name;
    }

    // ---------------------------------------------------------------- Global Energy Monitor (Task 7)

    /// <summary>
    /// A workbook rebuilt from one of the real small excerpts (at most 20 rows a sheet) in tests/Samples (<c>gem_*_excerpt.json</c>, made by
    /// <c>DataTool excerpt</c> from the owner's GEM files; CC BY 4.0, credited in tests/Samples/README.md): the same sheets,
    /// column names and cells (numbers as numbers, text as text, blanks blank). <paramref name="edit"/> may change the
    /// sheets first (a test's own variant).
    /// </summary>
    public static SourceFile Gem(TempDir dir, string excerpt, DateOnly published, Action<List<(string Name, List<object?[]> Rows)>>? edit = null)
    {
        using var doc = System.Text.Json.JsonDocument.Parse(File.ReadAllText(Samples.Path(excerpt)));
        var sheets = new List<(string Name, List<object?[]> Rows)>();
        foreach (var s in doc.RootElement.GetProperty("sheets").EnumerateArray())
        {
            var rows = new List<object?[]> { s.GetProperty("header").EnumerateArray().Select(Cell).ToArray() };
            rows.AddRange(s.GetProperty("rows").EnumerateArray().Select(r => r.EnumerateArray().Select(Cell).ToArray()));
            sheets.Add((s.GetProperty("name").GetString()!, rows));
        }
        edit?.Invoke(sheets);
        var path = dir.File(doc.RootElement.GetProperty("file").GetString()!);
        File.WriteAllBytes(path, Xlsx([.. sheets.Select(s => (s.Name, (IReadOnlyList<object[]>)[.. s.Rows.Select(r => r.Select(c => c!).ToArray())]))]));
        return new SourceFile(path, published, Downloaded);
    }

    private static object? Cell(System.Text.Json.JsonElement e) => e.ValueKind switch
    {
        System.Text.Json.JsonValueKind.Number => e.GetDouble(),
        System.Text.Json.JsonValueKind.String => e.GetString(),
        _ => null,
    };
}
