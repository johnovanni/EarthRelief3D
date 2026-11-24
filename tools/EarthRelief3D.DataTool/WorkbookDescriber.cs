using System.Globalization;
using System.Text;
using ExcelDataReader;

namespace EarthRelief3D.DataTool;

/// <summary>
/// <c>describe &lt;xlsx&gt;</c>: a downloaded workbook's sheets, each one's header row, row count and first rows, and on
/// request the distinct values of one column with their counts. Read only, through ExcelDataReader: how the column names in
/// docs/data-sources.md were checked against the real files (Task 7), and how to check a new release before extracting it.
/// </summary>
public static class WorkbookDescriber
{
    /// <param name="sheet">Only this sheet; null: every sheet.</param>
    /// <param name="sample">How many data rows to print after each header.</param>
    /// <param name="distinct">A column whose distinct values are counted; null: none.</param>
    /// <param name="headerRow">The 1-based row holding the column names.</param>
    public static string Describe(string path, string? sheet, int sample, string? distinct, int headerRow = 1)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var sb = new StringBuilder();
        using var stream = File.OpenRead(path);
        using var reader = ExcelReaderFactory.CreateOpenXmlReader(stream);
        do
        {
            var name = reader.Name;
            if (sheet is not null && !string.Equals(name, sheet, StringComparison.Ordinal))
            {
                sb.Append(CultureInfo.InvariantCulture, $"sheet \"{name}\" (skipped)\n");
                continue;
            }
            string[]? header = null;
            int rows = 0, row = 0, distinctCol = -1;
            var counts = new Dictionary<string, int>(StringComparer.Ordinal);
            while (reader.Read())
            {
                row++;
                if (row < headerRow) continue;
                if (header is null)
                {
                    header = [.. Enumerable.Range(0, reader.FieldCount).Select(i => Cell(reader.GetValue(i)))];
                    sb.Append(CultureInfo.InvariantCulture, $"sheet \"{name}\": {header.Length} columns\n");
                    for (int i = 0; i < header.Length; i++) sb.Append(CultureInfo.InvariantCulture, $"  [{i}] {header[i]}\n");
                    if (distinct is not null) distinctCol = Array.IndexOf(header, distinct);
                    continue;
                }
                rows++;
                if (rows <= sample)
                    sb.Append("  row: ").Append(string.Join(" | ", Enumerable.Range(0, reader.FieldCount).Select(i => Cell(reader.GetValue(i))))).Append('\n');
                if (distinctCol >= 0 && distinctCol < reader.FieldCount) Tally.Add(counts, Cell(reader.GetValue(distinctCol)));
            }
            sb.Append(CultureInfo.InvariantCulture, $"  {rows:N0} data rows\n");
            if (distinctCol >= 0)
                foreach (var (value, n) in counts.OrderByDescending(kv => kv.Value))
                    sb.Append(CultureInfo.InvariantCulture, $"    {distinct} = \"{value}\": {n:N0}\n");
        }
        while (reader.NextResult());
        return sb.ToString();
    }

    /// <summary>
    /// <c>excerpt &lt;xlsx&gt; --take "&lt;sheet&gt;|&lt;column&gt;|&lt;value;value…&gt;"…</c>: a few real rows of a workbook, kept
    /// exactly (numbers as numbers, text as text, blanks as null), for the tests' small extracts (tests/Samples). Each take
    /// is a sheet with its header row and the rows whose column holds one of the values; an empty column takes the sheet's
    /// first N rows (the value is N). Returns JSON: <c>{"file": …, "sheets": [{"name", "header", "rows"}]}</c>.
    /// </summary>
    public static string Excerpt(string path, IReadOnlyList<string> takes)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var wanted = takes.Select(t => t.Split('|')).Select(p => p.Length == 3 ? (Sheet: p[0], Column: p[1], Values: p[2].Split(';').ToHashSet(StringComparer.Ordinal))
            : throw new ArgumentException($"--take \"{string.Join('|', p)}\" is not \"<sheet>|<column>|<values>\"")).ToList();
        var sheets = new List<object>();
        using var stream = File.OpenRead(path);
        using var reader = ExcelReaderFactory.CreateOpenXmlReader(stream);
        do
        {
            // One take per sheet (the reader passes over each sheet once).
            foreach (var w in wanted.Where(w => string.Equals(w.Sheet, reader.Name, StringComparison.Ordinal)).Take(1))
            {
                if (!reader.Read()) break;
                var header = Enumerable.Range(0, reader.FieldCount).Select(i => Cell(reader.GetValue(i))).ToList();
                int col = w.Column.Length == 0 ? -1 : header.IndexOf(w.Column);
                if (w.Column.Length > 0 && col < 0) throw new ArgumentException($"sheet \"{w.Sheet}\" has no column \"{w.Column}\"");
                int first = col < 0 ? int.Parse(w.Values.Single(), CultureInfo.InvariantCulture) : 0;
                var rows = new List<object?[]>();
                while (reader.Read())
                {
                    var row = Enumerable.Range(0, reader.FieldCount).Select(i => reader.GetValue(i) switch
                    {
                        null or DBNull => null,
                        double d => (object)d,
                        var v => Cell(v),
                    }).ToArray();
                    if (col < 0 ? rows.Count < first : w.Values.Contains(Cell(reader.GetValue(col)))) rows.Add(row);
                }
                sheets.Add(new { name = w.Sheet, header, rows });
            }
        }
        while (reader.NextResult());
        return System.Text.Json.JsonSerializer.Serialize(new { file = Path.GetFileName(path), sheets },
            new System.Text.Json.JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
    }

    private static string Cell(object? v) => v switch
    {
        null or DBNull => "",
        double d => d.ToString("R", CultureInfo.InvariantCulture),
        DateTime t => t.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        _ => Convert.ToString(v, CultureInfo.InvariantCulture) ?? "",
    };
}
