using System.Text;

namespace EarthRelief3D.Core.Feeds;

/// <summary>
/// Minimal RFC 4180 reader: comma separated, double-quoted fields that may hold commas, doubled quotes and line breaks.
/// Blank lines are skipped. Rows are not checked against the header's length.
/// </summary>
public static class CsvReader
{
    public static IEnumerable<string[]> Read(TextReader reader)
    {
        var fields = new List<string>();
        var sb = new StringBuilder();
        bool inQuotes = false, wasQuoted = false, any = false;
        int c;
        while ((c = reader.Read()) >= 0)
        {
            var ch = (char)c;
            if (inQuotes)
            {
                if (ch == '"')
                {
                    if (reader.Peek() == '"') { reader.Read(); sb.Append('"'); }
                    else inQuotes = false;
                }
                else sb.Append(ch);
                continue;
            }
            switch (ch)
            {
                case '"' when sb.Length == 0 && !wasQuoted:
                    inQuotes = true; wasQuoted = true; any = true;
                    break;
                case ',':
                    fields.Add(sb.ToString()); sb.Clear(); wasQuoted = false; any = true;
                    break;
                case '\r':
                    break;
                case '\n':
                    if (any || sb.Length > 0 || fields.Count > 0)
                    {
                        fields.Add(sb.ToString());
                        yield return fields.ToArray();
                    }
                    fields.Clear(); sb.Clear(); wasQuoted = false; any = false;
                    break;
                default:
                    sb.Append(ch); any = true;
                    break;
            }
        }
        if (any || sb.Length > 0 || fields.Count > 0)
        {
            fields.Add(sb.ToString());
            yield return fields.ToArray();
        }
    }

    public static IEnumerable<string[]> Read(byte[] bytes)
    {
        using var r = new StreamReader(new MemoryStream(bytes), new UTF8Encoding(false), detectEncodingFromByteOrderMarks: true);
        foreach (var row in Read(r)) yield return row;
    }

    /// <summary>Index of each header name, or a <see cref="FeedFormatException"/> naming the first missing one.</summary>
    public static Dictionary<string, int> Columns(string[] header, string source, params string[] required)
    {
        var map = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var i = 0; i < header.Length; i++) map.TryAdd(header[i].Trim().TrimStart('﻿'), i);
        foreach (var name in required)
            if (!map.ContainsKey(name)) throw new FeedFormatException($"{source} has no \"{name}\" column");
        return map;
    }
}
