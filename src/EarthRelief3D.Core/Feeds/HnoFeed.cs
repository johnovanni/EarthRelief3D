using System.Globalization;
using System.Text.Json;
using EarthRelief3D.Core.Model;
using EarthRelief3D.Core.Store;
using GlobeCore.Feeds;
using GlobeCore.Logging;
using GlobeCore.Time;

namespace EarthRelief3D.Core.Feeds;

/// <summary>
/// The UN humanitarian needs overviews (HNO) from OCHA's HPC Tools dataset on HDX (CC BY-IGO, no key): people in need and
/// targeted, in total and for five clusters (docs/data-sources.md section 6). The year's file is found in the CKAN package
/// by its file name <c>hpc_hno_&lt;year&gt;.csv</c> (this year, else the year before). Rows are the ones with a blank
/// <c>Category</c>, keyed on <c>Cluster</c> (never on <c>Description</c>, which is in the country's language): ALL, FSC, WSH,
/// SHL and HEA. A cluster with no row means "not reported in this file", never zero.
/// Also stores the series <c>hno.shelter</c>: people in need of shelter (SHL) as a share of the population (the ALL row),
/// only for countries that have an SHL row (ruling R7).
/// </summary>
public sealed class HnoFeed(FeedHttp http, IndicatorStore indicators, CrisisStore crises, IClock clock, IAppLog log, CountryResolver? countries = null) : IFeed
{
    public const string FeedName = "UN HNO";
    public const string ShelterSeriesId = "hno.shelter";
    public const string ShelterMeasures = "People in need of shelter, as a share of the population (UN humanitarian needs overviews)";
    public const string Publisher = "OCHA Humanitarian Programme Cycle Tools (HPC Tools), via HDX";
    public static readonly Uri PackageUrl = new("https://data.humdata.org/api/3/action/package_show?id=global-hpc-hno");
    public static readonly Uri PageUrl = new("https://data.humdata.org/dataset/global-hpc-hno");

    /// <summary>The cluster codes the panel and the map read.</summary>
    public static IReadOnlyList<string> Clusters { get; } = ["ALL", "FSC", "WSH", "SHL", "HEA"];

    private static readonly string[] Required = ["Country ISO3", "Description", "Cluster", "Category", "Population", "In Need", "Targeted"];

    private readonly CountryResolver _countries = countries ?? CountryResolver.LoadDefault();

    public string Name => FeedName;
    public TimeSpan Interval => TimeSpan.FromDays(7);

    public static string FileName(int year) => $"hpc_hno_{year}.csv";

    public async Task<FeedRunResult> RunOnceAsync(CancellationToken ct)
    {
        var now = clock.UtcNow;
        var package = await http.GetStringAsync(PackageUrl, ct).ConfigureAwait(false);
        // This year's file, else last year's (the new file appears some weeks into the year).
        var (year, url, loaded) = FindResource(package, now.Year) ?? FindResource(package, now.Year - 1)
            ?? throw new FeedFormatException($"the HDX package has no resource named {FileName(now.Year)} or {FileName(now.Year - 1)}");
        var csv = await http.GetBytesAsync(url, ct).ConfigureAwait(false);

        var (needs, shelter, skipped, unplaced) = Read(csv, year, loaded, now);
        indicators.ReplaceNeeds(needs, shelter);
        crises.ReplaceUnplaced(Name, unplaced);
        crises.MarkChecked(Name, now);   // R12
        log.Info($"{Name}: plan {year}, {needs.Select(n => n.Iso3).Distinct().Count()} countries, {needs.Count} rows; "
            + $"shelter share for {shelter.Values.Count}; {skipped} repeated rows skipped; {unplaced.Count} codes not placed");
        return FeedRunResult.Ok;
    }

    /// <summary>The year's CSV: its URL and when HDX says it was loaded (the as-of date; never today's).</summary>
    private static (int Year, Uri Url, DateTime LoadedUtc)? FindResource(string packageJson, int year)
    {
        try
        {
            using var doc = JsonDocument.Parse(packageJson);
            var result = doc.RootElement.GetProperty("result");
            foreach (var r in result.GetProperty("resources").EnumerateArray())
            {
                if (!r.TryGetProperty("url", out var u) || !Uri.TryCreate(u.GetString(), UriKind.Absolute, out var uri)) continue;
                if (!string.Equals(Path.GetFileName(uri.AbsolutePath), FileName(year), StringComparison.OrdinalIgnoreCase)) continue;
                // The file's own load date only: the package's date is another thing and is never called "file loaded".
                var loaded = Utc(r, "last_modified")
                    ?? throw new FeedFormatException($"the {FileName(year)} resource has no last_modified date");
                return (year, uri, loaded);
            }
        }
        catch (Exception e) when (e is JsonException or KeyNotFoundException or InvalidOperationException)
        {
            throw new FeedFormatException("the HDX package JSON has no result.resources list");
        }
        return null;
    }

    private static DateTime? Utc(JsonElement e, string name) =>
        e.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.String
        && DateTime.TryParse(p.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var d)
            ? d : null;

    private (List<HumanitarianNeed> Needs, IndicatorSeries Shelter, int Skipped, List<UnplacedReport> Unplaced) Read(
        byte[] csv, int year, DateTime loadedUtc, DateTime now)
    {
        var fileName = FileName(year);
        var yearText = year.ToString(CultureInfo.InvariantCulture);
        var citation = new SourceCitation(Publisher, $"Global HPC HNO {year}", yearText, PageUrl, "CC BY-IGO",
            $"{Publisher}: Global HPC HNO {year}; CC BY-IGO", loadedUtc, now, CitationDate.FileLoaded);

        var needs = new List<HumanitarianNeed>();
        var unplaced = new List<UnplacedReport>();
        var seen = new HashSet<(string, string)>();
        var notPlaced = new HashSet<string>(StringComparer.Ordinal);
        var skipped = 0;
        string[]? header = null;
        Dictionary<string, int> col = [];
        foreach (var row in CsvReader.Read(csv))
        {
            if (header is null) { header = row; col = CsvReader.Columns(header, fileName, Required); continue; }
            if (row.Length < header.Length) continue;
            string F(string n) => row[col[n]].Trim();
            if (F("Category").Length > 0) continue;                 // sub-groups (a population group); only the blank-category rows
            var cluster = F("Cluster").ToUpperInvariant();
            if (!Clusters.Contains(cluster)) continue;
            var iso = F("Country ISO3").ToUpperInvariant();
            if (_countries.ByIso3(iso) is null)
            {
                if (notPlaced.Add(iso))
                {
                    log.Warn($"{Name}: code {iso} has no unit in the boundary set; listed as not placed");
                    unplaced.Add(new UnplacedReport(Name, $"UN humanitarian needs for \"{iso}\"", "no unit in the boundary set for this code", citation));
                }
                continue;
            }
            if (!seen.Add((iso, cluster)))
            {
                skipped++;
                log.Warn($"{Name}: {iso} has more than one {cluster} row; the first is kept");
                continue;
            }
            needs.Add(new HumanitarianNeed(iso, cluster, cluster == "ALL" ? Count(F("Population")) : null, Count(F("In Need")),
                Count(F("Targeted")), year, citation, cluster == "ALL" && F("Description").Length > 0 ? F("Description") : null));
        }
        if (needs.Count == 0) throw new FeedFormatException($"{fileName} has no ALL, FSC, WSH, SHL or HEA rows with a blank Category");

        var shelter = new List<CountryValue>();
        foreach (var shl in needs.Where(n => n.Cluster == "SHL" && n.InNeed is not null))
        {
            var population = needs.FirstOrDefault(n => n.Iso3 == shl.Iso3 && n.Cluster == "ALL")?.Population;
            if (population is not > 0) continue;   // no denominator: no figure rather than a guess
            shelter.Add(new CountryValue(shl.Iso3, Math.Round(shl.InNeed!.Value * 100.0 / population.Value, 2), year, citation));
        }
        return (needs, new IndicatorSeries(ShelterSeriesId, ShelterMeasures, "%", [.. shelter.OrderBy(v => v.Iso3, StringComparer.Ordinal)]), skipped, unplaced);
    }

    /// <summary>A whole number of people; blank (or text) is "not given", never 0.</summary>
    private static long? Count(string text) =>
        decimal.TryParse(text, NumberStyles.Number | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out var d) && d >= 0
            ? (long)Math.Round(d) : null;
}
