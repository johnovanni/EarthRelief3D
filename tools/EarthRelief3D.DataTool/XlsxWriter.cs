using System.Globalization;
using System.IO.Compression;
using System.Security;
using System.Text;
using System.Text.Json;

namespace EarthRelief3D.DataTool;

/// <summary>
/// Writes a small XLSX from rows of text, numbers and blanks. Used by the <c>workbook</c> verb (which builds the synthetic
/// ACAPS workbook in tests/Samples from a JSON file) and by the DataTool tests, which build tiny source workbooks.
/// </summary>
public static class XlsxWriter
{
    /// <summary>
    /// Reads <c>{ "sheets": [ { "name": "...", "rows": [[...]] } ] }</c>: a JSON string is text, a number a number, null a blank cell.
    /// </summary>
    public static IReadOnlyList<(string Name, IReadOnlyList<object[]> Rows)> ReadSpec(string json)
    {
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("sheets", out var list) || list.ValueKind != JsonValueKind.Array)
            throw new ArgumentException("the JSON needs a \"sheets\" array");
        var sheets = new List<(string Name, IReadOnlyList<object[]> Rows)>();
        foreach (var s in list.EnumerateArray())
        {
            if (!s.TryGetProperty("name", out var name) || name.ValueKind != JsonValueKind.String || !s.TryGetProperty("rows", out var rows) || rows.ValueKind != JsonValueKind.Array)
                throw new ArgumentException("each sheet needs a \"name\" and a \"rows\" array");
            var table = new List<object[]>();
            foreach (var row in rows.EnumerateArray())
            {
                if (row.ValueKind != JsonValueKind.Array) throw new ArgumentException($"sheet \"{name.GetString()}\": each row must be an array");
                table.Add([.. row.EnumerateArray().Select(c => c.ValueKind switch
                {
                    JsonValueKind.String => (object?)c.GetString(),
                    JsonValueKind.Number => c.GetDouble(),
                    JsonValueKind.Null => null,
                    _ => throw new ArgumentException($"sheet \"{name.GetString()}\": cells are text, numbers or null"),
                })!]);
            }
            sheets.Add((name.GetString()!, table));
        }
        return sheets;
    }

    /// <summary>A minimal OpenXML workbook (inline strings, no styles): enough for ExcelDataReader.</summary>
    public static byte[] Write(IReadOnlyList<(string Name, IReadOnlyList<object[]> Rows)> sheets)
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
}
