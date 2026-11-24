using System.Globalization;
using System.Text;
using ExcelDataReader;

namespace EarthRelief3D.DataTool;

/// <summary>One data row of a GEM sheet, read by column name (the names are checked when the sheet is opened).</summary>
public sealed class SheetRow
{
    private readonly IReadOnlyDictionary<string, int> _columns;
    private readonly object?[] _values;

    internal SheetRow(IReadOnlyDictionary<string, int> columns, object?[] values, int number)
    {
        _columns = columns;
        _values = values;
        RowNumber = number;
    }

    /// <summary>The row's number in the sheet (1-based, the header is row 1).</summary>
    public int RowNumber { get; }

    /// <summary>The cell as text: numbers in their shortest round-trip form, blanks as "", trimmed.</summary>
    public string Text(string column)
    {
        var v = Raw(column);
        return v switch
        {
            null or DBNull => "",
            double d => d.ToString("R", CultureInfo.InvariantCulture),
            DateTime t => t.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            _ => (Convert.ToString(v, CultureInfo.InvariantCulture) ?? "").Trim(),
        };
    }

    /// <summary>The cell as a number (a numeric cell, or text that is a plain number); null for blanks and words ("unknown", "N/A", "&gt;0").</summary>
    public double? Number(string column) => Raw(column) switch
    {
        double d when double.IsFinite(d) => d,
        string s when double.TryParse(s.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var v) && double.IsFinite(v) => v,
        _ => null,
    };

    public object? Raw(string column) =>
        _columns.TryGetValue(column, out var i) ? i < _values.Length ? _values[i] : null
            : throw new InvalidOperationException($"column \"{column}\" was not asked for");
}

/// <summary>Reads a GEM workbook through ExcelDataReader, a sheet at a time, streaming (the power tracker has 183,000 rows).</summary>
public static class GemWorkbook
{
    /// <summary>The rows of <paramref name="sheet"/>, whose first row names the columns; every column in <paramref name="needed"/> must be there.</summary>
    /// <exception cref="InvalidDataException">The sheet or a column is missing (a format change).</exception>
    public static IEnumerable<SheetRow> Rows(string path, string sheet, IReadOnlyList<string> needed, string source)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);   // ExcelDataReader needs it on .NET
        using var stream = File.OpenRead(path);
        using var reader = ExcelReaderFactory.CreateOpenXmlReader(stream);
        do
        {
            if (!string.Equals(reader.Name, sheet, StringComparison.Ordinal)) continue;
            if (!reader.Read()) throw new InvalidDataException($"{source}: the sheet \"{sheet}\" is empty (format changed)");
            var header = Enumerable.Range(0, reader.FieldCount).Select(i => Convert.ToString(reader.GetValue(i), CultureInfo.InvariantCulture)?.Trim() ?? "").ToList();
            var columns = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var c in needed)
            {
                int i = header.IndexOf(c);
                columns[c] = i >= 0 ? i : throw new InvalidDataException($"{source}: the sheet \"{sheet}\" has no column \"{c}\" (format changed)");
            }
            int n = 1;
            while (reader.Read())
            {
                n++;
                var values = new object?[reader.FieldCount];
                bool any = false;
                for (int i = 0; i < values.Length; i++)
                {
                    values[i] = reader.GetValue(i);
                    any |= values[i] is not (null or DBNull) && !(values[i] is string s && s.Trim().Length == 0);
                }
                if (any) yield return new SheetRow(columns, values, n);
            }
            yield break;
        }
        while (reader.NextResult());
        throw new InvalidDataException($"{source}: the workbook has no sheet \"{sheet}\" (format changed)");
    }

    /// <summary>Every non-blank cell of a sheet as text, in order (the "About" sheet's release and licence lines).</summary>
    public static IReadOnlyList<string> Cells(string path, string sheet, string source)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        using var stream = File.OpenRead(path);
        using var reader = ExcelReaderFactory.CreateOpenXmlReader(stream);
        do
        {
            if (!string.Equals(reader.Name, sheet, StringComparison.Ordinal)) continue;
            var cells = new List<string>();
            while (reader.Read())
                for (int i = 0; i < reader.FieldCount; i++)
                    if (Convert.ToString(reader.GetValue(i), CultureInfo.InvariantCulture)?.Trim() is { Length: > 0 } t) cells.Add(t);
            return cells;
        }
        while (reader.NextResult());
        throw new InvalidDataException($"{source}: the workbook has no sheet \"{sheet}\" (format changed)");
    }
}
