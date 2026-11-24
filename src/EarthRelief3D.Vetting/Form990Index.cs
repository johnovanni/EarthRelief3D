using System.Text;

namespace EarthRelief3D.Vetting;

/// <summary>One return in the IRS Form 990 e-file index.</summary>
/// <param name="TaxPeriod">"202506": the year and month the tax period ends.</param>
/// <param name="ReturnType">990, 990EZ or 990PF.</param>
/// <param name="BatchId">The zip the XML is in: "2026_TEOS_XML_03A".</param>
public sealed record Form990Ref(string Ein, string TaxPeriod, string ReturnType, string ObjectId, string BatchId);

/// <summary>
/// Reads <c>index_&lt;year&gt;.csv</c> (<c>RETURN_ID, FILING_TYPE, EIN, TAX_PERIOD, SUB_DATE, TAXPAYER_NAME, RETURN_TYPE, DLN, OBJECT_ID,
/// XML_BATCH_ID</c>). Only the returns that carry a charity's programme spending are looked at: 990, 990EZ and 990PF (not 990-T,
/// the unrelated-business tax return). The latest is the one with the latest tax period.
/// </summary>
public static class Form990Index
{
    private static readonly string[] Kinds = ["990", "990EZ", "990PF"];

    public static Form990Ref? Latest(Stream csv, string ein)
    {
        var n = Ein.Normalize(ein) ?? throw new ArgumentException($"not an EIN: {ein}", nameof(ein));
        return LatestForAll(csv, new HashSet<string> { n }).GetValueOrDefault(n);
    }

    /// <summary>The latest return for each of <paramref name="eins"/> (nine digits) found in the index; one pass over the file.</summary>
    public static Dictionary<string, Form990Ref> LatestForAll(Stream csv, IReadOnlySet<string> eins)
    {
        var best = new Dictionary<string, Form990Ref>(StringComparer.Ordinal);
        if (eins.Count == 0) return best;
        using var reader = new StreamReader(csv, Encoding.UTF8, true, 1 << 16, leaveOpen: true);
        var header = reader.ReadLine() ?? throw new InvalidDataException("the 990 index is empty");
        var cols = SplitCsv(header).Select((h, i) => (h: h.Trim().ToUpperInvariant(), i)).ToDictionary(x => x.h, x => x.i);
        int Col(string name) => cols.TryGetValue(name, out var i) ? i : throw new InvalidDataException($"the 990 index has no {name} column");
        int cEin = Col("EIN"), cPeriod = Col("TAX_PERIOD"), cType = Col("RETURN_TYPE"), cObject = Col("OBJECT_ID"), cBatch = Col("XML_BATCH_ID");
        var width = new[] { cEin, cPeriod, cType, cObject, cBatch }.Max() + 1;

        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (line.Length == 0) continue;
            var f = SplitCsv(line);
            if (f.Count < width) continue;
            var ein = Ein.Normalize(f[cEin]);
            if (ein is null || !eins.Contains(ein)) continue;
            var type = f[cType].Trim();
            if (!Kinds.Contains(type, StringComparer.OrdinalIgnoreCase)) continue;
            var period = f[cPeriod].Trim();
            var obj = f[cObject].Trim();
            var batch = f[cBatch].Trim();
            if (period.Length != 6 || obj.Length == 0 || batch.Length == 0) continue;     // a row with no file to read
            var r = new Form990Ref(ein, period, type.ToUpperInvariant(), obj, batch);
            if (!best.TryGetValue(ein, out var old) || Later(r, old)) best[ein] = r;
        }
        return best;
    }

    /// <summary>A later period wins; for the same period the later filing (a higher object id: an amended return).</summary>
    private static bool Later(Form990Ref a, Form990Ref b) =>
        string.CompareOrdinal(a.TaxPeriod, b.TaxPeriod) is var c && (c > 0 || (c == 0 && string.CompareOrdinal(a.ObjectId, b.ObjectId) > 0));

    /// <summary>Splits one CSV line: commas separate, double quotes protect commas, a doubled quote is a quote.</summary>
    internal static List<string> SplitCsv(string line)
    {
        var fields = new List<string>();
        var sb = new StringBuilder();
        var quoted = false;
        for (var i = 0; i < line.Length; i++)
        {
            var ch = line[i];
            if (quoted)
            {
                if (ch == '"' && i + 1 < line.Length && line[i + 1] == '"') { sb.Append('"'); i++; }
                else if (ch == '"') quoted = false;
                else sb.Append(ch);
            }
            else if (ch == '"') quoted = true;
            else if (ch == ',') { fields.Add(sb.ToString()); sb.Clear(); }
            else sb.Append(ch);
        }
        fields.Add(sb.ToString());
        return fields;
    }
}

