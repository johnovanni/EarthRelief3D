using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using EarthRelief3D.Core.Feeds;

namespace EarthRelief3D.DataTool;

/// <summary>A downloaded source file and its two dates, neither of which is the data's as-of.</summary>
/// <param name="Published">The file's own date (its Last-Modified): the publication date the attribution quotes.</param>
/// <param name="Downloaded">When the maintainer downloaded it: the citation's "retrieved" date.</param>
public sealed record SourceFile(string Path, DateOnly Published, DateOnly Downloaded)
{
    public string FileName => System.IO.Path.GetFileName(Path);

    public string Sha256()
    {
        using var s = File.OpenRead(Path);
        return Convert.ToHexStringLower(SHA256.HashData(s));
    }

    /// <summary>The command as recorded in the manifest: the file by name only (no local paths).</summary>
    public string Command(string verb) =>
        $"DataTool {verb} <downloads>/{FileName} --published {Published:yyyy-MM-dd} --downloaded {Downloaded:yyyy-MM-dd}";
}

/// <summary>One row of <c>data.csv</c>: the common columns, then the source's own (<see cref="Extract.ExtraColumns"/>).</summary>
public sealed record ExtractRow(string Series, string Iso3, int Year, double Value, string? Qualifier, IReadOnlyDictionary<string, string> Extra);

/// <summary>An extract, ready to be written as <c>data.csv</c> and <c>manifest.json</c>.</summary>
public sealed record Extract(DatasetManifest Manifest, IReadOnlyList<ExtractRow> Rows, IReadOnlyList<string> ExtraColumns);

/// <summary>Writes an extract's folder (P3-10): <c>data.csv</c> (UTF-8, no BOM, LF) and <c>manifest.json</c>.</summary>
public static class DatasetWriter
{
    public static void Write(Extract extract, string folder)
    {
        ArgumentNullException.ThrowIfNull(extract);
        Directory.CreateDirectory(folder);
        var sb = new StringBuilder();
        sb.Append(string.Join(",", BundledDataset.Columns.Concat(extract.ExtraColumns).Select(Quote))).Append('\n');
        foreach (var r in extract.Rows)
        {
            IEnumerable<string> cells =
            [
                r.Series, r.Iso3, r.Year.ToString(CultureInfo.InvariantCulture), Number(r.Value), r.Qualifier ?? "",
                .. extract.ExtraColumns.Select(c => r.Extra.TryGetValue(c, out var v) ? v : ""),
            ];
            sb.Append(string.Join(",", cells.Select(Quote))).Append('\n');
        }
        var utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        File.WriteAllText(Path.Combine(folder, BundledDataset.DataFile), sb.ToString(), utf8);
        File.WriteAllText(Path.Combine(folder, BundledDataset.ManifestFile), extract.Manifest.ToJson().ReplaceLineEndings("\n") + "\n", utf8);
    }

    /// <summary>The shortest text that reads back as the same double.</summary>
    public static string Number(double v) => v.ToString("R", CultureInfo.InvariantCulture);

    private static string Quote(string s) =>
        s.IndexOfAny([',', '"', '\n', '\r']) >= 0 ? "\"" + s.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"" : s;
}

/// <summary>Counts, for the manifest's "seen" lists.</summary>
internal static class Tally
{
    public static void Add(Dictionary<string, int> counts, string key) => counts[key] = counts.GetValueOrDefault(key) + 1;

    public static IReadOnlyDictionary<string, int> Sorted(Dictionary<string, int> counts) =>
        new SortedDictionary<string, int>(counts, StringComparer.Ordinal);
}
