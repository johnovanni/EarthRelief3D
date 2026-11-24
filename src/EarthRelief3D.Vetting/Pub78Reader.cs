using System.IO.Compression;
using System.Text;

namespace EarthRelief3D.Vetting;

/// <summary>An employer identification number: the IRS files write nine digits; the list writes "12-3456789".</summary>
public static class Ein
{
    /// <summary>The nine digits, or null when <paramref name="s"/> is not an EIN (a header line, a blank).</summary>
    public static string? Normalize(string? s)
    {
        if (s is null) return null;
        var t = s.Trim().Replace("-", "", StringComparison.Ordinal);
        return t.Length == 9 && t.All(char.IsAsciiDigit) ? t : null;
    }

    /// <summary>"530196605" as "53-0196605".</summary>
    public static string Format(string nineDigits) => nineDigits.Length == 9 ? nineDigits[..2] + "-" + nineDigits[2..] : nineDigits;
}

/// <summary>One row of IRS Publication 78 (organisations to which gifts are deductible).</summary>
public sealed record Pub78Entry(string Ein, string Name, string City, string State, string Country, string DeductibilityCode);

/// <summary>
/// Reads the IRS Publication 78 data file (<c>data-download-pub78.zip</c>: one pipe-delimited text file with no header,
/// <c>EIN|Name|City|State|Country|Deductibility Code</c>, about 1.9 million rows). A line that is blank or has no nine-digit EIN
/// first (a header, should the IRS add one) is skipped.
/// </summary>
public static class Pub78Reader
{
    /// <param name="zip">A seekable stream of the zip.</param>
    /// <param name="only">When given, only these EINs (nine digits) are kept: the real file is 100 MB of text.</param>
    public static IReadOnlyDictionary<string, Pub78Entry> Read(Stream zip, IReadOnlySet<string>? only = null)
    {
        var found = new Dictionary<string, Pub78Entry>(StringComparer.Ordinal);
        foreach (var line in ZipText.Lines(zip))
        {
            var bar = line.IndexOf('|');
            if (bar < 9) continue;
            var ein = Ein.Normalize(line[..bar]);
            if (ein is null || (only is not null && !only.Contains(ein))) continue;
            var f = line.Split('|');
            if (f.Length < 6) continue;
            found.TryAdd(ein, new Pub78Entry(ein, f[1].Trim(), f[2].Trim(), f[3].Trim(), f[4].Trim(), f[5].Trim()));
        }
        return found;
    }
}

/// <summary>The text lines of the first file in a zip.</summary>
internal static class ZipText
{
    public static IEnumerable<string> Lines(Stream zip)
    {
        using var archive = new ZipArchive(zip, ZipArchiveMode.Read, leaveOpen: true);
        var entry = archive.Entries.FirstOrDefault(e => e.Length > 0) ?? throw new InvalidDataException("the zip is empty");
        using var reader = new StreamReader(entry.Open(), Encoding.UTF8);
        string? line;
        while ((line = reader.ReadLine()) is not null)
            if (line.Length > 0) yield return line;
    }
}

