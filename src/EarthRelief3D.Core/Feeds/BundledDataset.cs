using System.Globalization;
using EarthRelief3D.Core.Model;
using GlobeCore.Feeds;
using GlobeCore.Logging;

namespace EarthRelief3D.Core.Feeds;

/// <summary>
/// What loading one bundled extract gave: its series (empty when it could not be loaded) and the codes not placed on the
/// map (the manifest's, and any whose country the current boundary set lacks).
/// </summary>
/// <param name="Name">The status dot's name ("FAO", "US EIA", "WRI Aqueduct").</param>
/// <param name="Problem">Why the extract was not loaded; null when it was.</param>
public sealed record BundledDatasetLoad(string Name, string Folder, DatasetManifest? Manifest, IReadOnlyList<IndicatorSeries> Series,
    IReadOnlyList<ManifestCode> NotPlaced, string? Problem)
{
    public bool Loaded => Problem is null && Manifest is not null;

    /// <summary>The newest figure's citation (the dataset's as-of), or null with no figures.</summary>
    public SourceCitation? Newest => Series.SelectMany(s => s.Values).Select(v => v.Citation).MaxBy(c => c.AsOfUtc);
}

/// <summary>
/// Loads a bundled extract (<c>data\datasets\&lt;id&gt;\data.csv</c> with its <c>manifest.json</c>, ruling P3-10) into
/// <see cref="IndicatorSeries"/>, each figure citing the manifest's source with the figure's own year. The extracts ship
/// with the app and change only with it, so their status dot says <see cref="StatusNote"/>. A missing or broken extract is
/// logged and leaves its layers "Data not loaded yet"; it never stops the app.
/// </summary>
public static class BundledDataset
{
    public const string DataFile = "data.csv";
    public const string ManifestFile = "manifest.json";
    /// <summary>The status dot's note for every bundled dataset.</summary>
    public const string StatusNote = "bundled; updated with the app";

    /// <summary>The columns every <c>data.csv</c> has (extracts add their source's own columns after them).</summary>
    public static IReadOnlyList<string> Columns { get; } = ["series", "iso3", "year", "value", "qualifier"];

    /// <summary>The bundled extracts' folder beside the program (<c>data\datasets</c>).</summary>
    public static string DatasetsDir { get; } = Path.Combine(BoundaryData.DataDir, "datasets");

    /// <param name="name">The status dot's name, used in the log too.</param>
    /// <param name="isPlaced">True when the boundary set has a country for the ISO3 code (C16); null: every code is.</param>
    public static BundledDatasetLoad Load(string name, string folder, Func<string, bool>? isPlaced, IAppLog log)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(folder);
        ArgumentNullException.ThrowIfNull(log);
        try
        {
            var manifest = DatasetManifest.Parse(File.ReadAllText(Path.Combine(folder, ManifestFile)));
            var meaning = Enum.TryParse<CitationDate>(manifest.DateMeaning, ignoreCase: false, out var m)
                ? m : throw new InvalidDataException($"unknown dateMeaning \"{manifest.DateMeaning}\"");
            if (meaning == CitationDate.Baseline && string.IsNullOrWhiteSpace(manifest.DateText))
                throw new InvalidDataException("a Baseline manifest needs its dateText (\"baseline 1979–2019 (published 2023)\")");
            var dateText = meaning == CitationDate.Baseline ? manifest.DateText!.Trim() : null;
            var downloaded = Day(manifest.Downloaded, "downloaded");
            var page = Uri.TryCreate(manifest.PageUrl, UriKind.Absolute, out var u) ? u : throw new InvalidDataException("pageUrl is not a URL");
            var citations = new Dictionary<int, SourceCitation>();
            SourceCitation Cite(int year)
            {
                if (!citations.TryGetValue(year, out var c))
                    citations[year] = c = new SourceCitation(manifest.Publisher, manifest.Dataset, manifest.Version, page, manifest.Licence,
                        manifest.Attribution, new DateTime(year, 12, 31, 0, 0, 0, DateTimeKind.Utc), downloaded, meaning) { DateText = dateText };
                return c;
            }

            var values = manifest.Series.ToDictionary(s => s.Id, _ => new List<CountryValue>(), StringComparer.Ordinal);
            var notPlaced = new List<ManifestCode>(manifest.NotPlaced);
            using (var reader = new StreamReader(Path.Combine(folder, DataFile), System.Text.Encoding.UTF8))
            {
                using var rows = CsvReader.Read(reader).GetEnumerator();
                if (!rows.MoveNext()) throw new InvalidDataException("data.csv is empty");
                var col = Columns.Select(c => Array.IndexOf(rows.Current, c)).ToArray();
                if (col.Any(i => i < 0)) throw new InvalidDataException("data.csv lacks a column of: " + string.Join(", ", Columns));
                // A 3-year average is shown as the range ending in its year, so the file's own range must be exactly that.
                int periodCol = Array.IndexOf(rows.Current, "period");
                if (meaning == CitationDate.ThreeYearAverage && periodCol < 0) throw new InvalidDataException("3-year averages need a period column");
                int line = 1;
                while (rows.MoveNext())
                {
                    line++;
                    var r = rows.Current;
                    if (r.Length <= col.Max()) throw new InvalidDataException($"data.csv line {line} is short");
                    var series = r[col[0]];
                    if (!values.TryGetValue(series, out var list)) throw new InvalidDataException($"data.csv line {line}: series \"{series}\" is not in the manifest");
                    var iso3 = r[col[1]].Trim().ToUpperInvariant();
                    if (isPlaced is not null && !isPlaced(iso3))
                    {
                        if (!notPlaced.Any(n => n.Code == iso3)) notPlaced.Add(new ManifestCode(iso3, iso3, "no unit in the boundary set for this code"));
                        continue;
                    }
                    int year = int.Parse(r[col[2]], NumberStyles.Integer, CultureInfo.InvariantCulture);
                    if (meaning == CitationDate.ThreeYearAverage && (r.Length <= periodCol || r[periodCol] != $"{year - 2}-{year}"))
                        throw new InvalidDataException($"data.csv line {line}: period \"{(r.Length > periodCol ? r[periodCol] : "")}\" is not the 3 years ending {year}");
                    double value = double.Parse(r[col[3]], NumberStyles.Float, CultureInfo.InvariantCulture);
                    var qualifier = string.IsNullOrWhiteSpace(r[col[4]]) ? null : r[col[4]];
                    list.Add(new CountryValue(iso3, value, year, Cite(year), qualifier));
                }
            }

            var all = manifest.Series.Select(s => new IndicatorSeries(s.Id, s.Measures, s.Unit, values[s.Id])).ToList();
            foreach (var n in notPlaced.Skip(manifest.NotPlaced.Count))
                log.Warn($"{name}: code {n.Code} has no unit in the boundary set; listed as not placed");
            log.Info($"{name}: bundled {manifest.Id} loaded: " + string.Join("; ", all.Select(s => $"{s.Id} {s.Values.Count} countries"))
                + $"; {notPlaced.Count} codes not placed");
            return new BundledDatasetLoad(name, folder, manifest, all, notPlaced, null);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException or FormatException or OverflowException
                                       or ArgumentException)
        {
            var problem = ex is FileNotFoundException or DirectoryNotFoundException ? "not found" : ex.Message;
            log.Warn($"{name}: the bundled data in {folder} could not be loaded ({problem}); its layers say \"Data not loaded yet\"", ex);
            return new BundledDatasetLoad(name, folder, null, [], [], problem);
        }
    }

    /// <summary>
    /// The extract's status dot: green with "&lt;the newest figure's date&gt; · bundled; updated with the app", or amber with
    /// why it was not loaded.
    /// </summary>
    /// <param name="dateText">The screen's wording of a citation's date ("estimate year 2025").</param>
    public static FeedStatus Status(BundledDatasetLoad load, Func<SourceCitation, string> dateText)
    {
        ArgumentNullException.ThrowIfNull(load);
        ArgumentNullException.ThrowIfNull(dateText);
        if (!load.Loaded)
            return new FeedStatus(load.Name, FeedState.Attention, null, load.Problem, 0, default, $"not loaded ({load.Problem}) · {StatusNote}");
        var date = load.Newest is { } c ? dateText(c) : "no figures";
        return new FeedStatus(load.Name, FeedState.Ok, null, null, 0, default, $"{date} · {StatusNote}");
    }

    private static DateTime Day(string text, string field) =>
        DateTime.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var d)
            ? DateTime.SpecifyKind(d, DateTimeKind.Utc)
            : throw new InvalidDataException($"{field} \"{text}\" is not yyyy-MM-dd");
}
