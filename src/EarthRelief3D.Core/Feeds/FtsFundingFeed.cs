using System.Globalization;
using System.Text.Json;
using EarthRelief3D.Core.Model;
using EarthRelief3D.Core.Store;
using GlobeCore.Feeds;
using GlobeCore.Logging;
using GlobeCore.Time;

namespace EarthRelief3D.Core.Feeds;

/// <summary>
/// Appeal requirements and funding from OCHA's Financial Tracking Service on HDX (docs/data-sources.md section 13; CC BY-IGO on
/// HDX, CC BY 4.0 on the FTS site, no key). One CSV, <c>fts_requirements_funding_global.csv</c>, refreshed daily.
/// <para>
/// Ruling P4-4: rows with a plan id for the clock's year; ruling P5-12: in January a country with none yet keeps last year's. A row
/// with a blank <c>id</c> is money not tied to a plan ("Not specified"): not an appeal, skipped. A plan whose id appears under
/// more than one country, or whose <c>typeName</c> contains "Regional", is flagged <see cref="AppealFunding.Regional"/>: its
/// figures are for the whole plan and are never a country's own. <c>percentFunded</c> is kept as published; a blank
/// requirements cell is null, never 0. FTS's terms prescribe the credit, "Financial Tracking Service (FTS), &lt;date&gt;,
/// &lt;URL&gt;", with the file's date.
/// </para>
/// </summary>
public sealed class FtsFundingFeed(FeedHttp http, IndicatorStore indicators, CrisisStore crises, IClock clock, IAppLog log, CountryResolver? countries = null) : IFeed
{
    public const string FeedName = "FTS";
    public const string ResourceFileName = "fts_requirements_funding_global.csv";
    public const string Publisher = "OCHA Financial Tracking Service (FTS), via HDX";
    public const string Dataset = "Requirements and funding";
    public const string Licence = "CC BY-IGO on HDX; CC BY 4.0 on the FTS site";
    public static readonly Uri PackageUrl = new("https://data.humdata.org/api/3/action/package_show?id=global-requirements-and-funding-data");
    public static readonly Uri PageUrl = new("https://data.humdata.org/dataset/global-requirements-and-funding-data");
    public static readonly Uri CreditUrl = new("https://fts.unocha.org");

    private static readonly string[] Required = ["countryCode", "id", "name", "typeName", "year", "requirements", "funding", "percentFunded"];

    private readonly CountryResolver _countries = countries ?? CountryResolver.LoadDefault();

    public const string NewYearNote = "In January, a country with no appeal for the new year yet shows last year's.";
    /// <summary>1 to 31 January (UTC): FTS may not have the new year's plans for every country yet (ruling P5-12).</summary>
    public static bool InNewYearWindow(DateTime nowUtc) => nowUtc.ToUniversalTime().Month == 1;

    public string Name => FeedName;
    public TimeSpan Interval => TimeSpan.FromDays(1);

    private sealed record PlanRow(string Iso3, int PlanId, string PlanName, string PlanType, int Year, long? Requirements, long Funding, int? Percent);

    public async Task<FeedRunResult> RunOnceAsync(CancellationToken ct)
    {
        var now = clock.UtcNow;
        var package = await http.GetStringAsync(PackageUrl, ct).ConfigureAwait(false);
        var (url, loaded) = FindResource(package);
        var csv = await http.GetBytesAsync(url, ct).ConfigureAwait(false);

        var (appeals, unplaced, year) = Read(csv, loaded, now, out var keptLastYear);
        indicators.ReplaceAppeals(appeals);
        crises.ReplaceUnplaced(Name, unplaced);
        crises.MarkChecked(Name, now);   // R12
        log.Info($"{Name}: {year}, {appeals.Count} appeal rows, {appeals.Count(a => a.Regional)} of them regional; {keptLastYear} countries kept last year's plans (new-year window); {unplaced.Count} codes not placed");
        return FeedRunResult.Ok;
    }

    private static (Uri Url, DateTime LoadedUtc) FindResource(string packageJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(packageJson);
            foreach (var r in doc.RootElement.GetProperty("result").GetProperty("resources").EnumerateArray())
            {
                if (!r.TryGetProperty("url", out var u) || !Uri.TryCreate(u.GetString(), UriKind.Absolute, out var uri)) continue;
                if (!string.Equals(Path.GetFileName(uri.AbsolutePath), ResourceFileName, StringComparison.OrdinalIgnoreCase)) continue;
                // The file's own date: FTS's credit uses the publication date of the figures, never today's.
                if (r.TryGetProperty("last_modified", out var p) && p.ValueKind == JsonValueKind.String
                    && DateTime.TryParse(p.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var d))
                    return (uri, d);
                throw new FeedFormatException($"the {ResourceFileName} resource has no last_modified date");
            }
        }
        catch (Exception e) when (e is JsonException or KeyNotFoundException or InvalidOperationException)
        {
            throw new FeedFormatException("the HDX package JSON has no result.resources list");
        }
        throw new FeedFormatException($"the HDX package has no resource named {ResourceFileName}");
    }

    private (List<AppealFunding> Appeals, List<UnplacedReport> Unplaced, int Year) Read(byte[] csv, DateTime loadedUtc, DateTime now, out int keptLastYear)
    {
        var plans = new List<PlanRow>();
        string[]? header = null;
        Dictionary<string, int> col = [];
        foreach (var row in CsvReader.Read(csv))
        {
            if (header is null) { header = row; col = CsvReader.Columns(header, ResourceFileName, Required); continue; }
            if (row.Length < header.Length) continue;
            string F(string n) => row[col[n]].Trim();
            // Money not tied to a plan has no id ("Not specified"): not an appeal.
            if (!int.TryParse(F("id"), NumberStyles.None, CultureInfo.InvariantCulture, out var id) || id <= 0) continue;
            if (!int.TryParse(F("year"), NumberStyles.None, CultureInfo.InvariantCulture, out var year)) continue;
            plans.Add(new PlanRow(F("countryCode").ToUpperInvariant(), id, F("name"), F("typeName"), year,
                Whole(F("requirements")), Whole(F("funding")) ?? 0, Whole(F("percentFunded")) is { } p ? (int)p : null));
        }

        // Ruling P5-12: this year's plans. In January a country with none yet keeps last year's (FTS may not have its new plans);
        // from 1 February only this year's. With no plan for this year at all, last year's whatever the date.
        var thisYear = plans.Where(p => p.Year == now.Year).ToList();
        var lastYear = plans.Where(p => p.Year == now.Year - 1).ToList();
        List<PlanRow> chosen;
        keptLastYear = 0;
        if (thisYear.Count == 0)
        {
            chosen = lastYear;
            if (chosen.Count > 0) log.Warn($"{Name}: no plan rows for {now.Year} in the file; using {now.Year - 1}");
        }
        else if (InNewYearWindow(now))
        {
            var haveNew = thisYear.Select(p => p.Iso3).ToHashSet(StringComparer.Ordinal);
            var kept = lastYear.Where(p => !haveNew.Contains(p.Iso3)).ToList();
            keptLastYear = kept.Select(p => p.Iso3).Distinct(StringComparer.Ordinal).Count();
            chosen = [.. thisYear, .. kept];
        }
        else chosen = thisYear;
        if (chosen.Count == 0) throw new FeedFormatException($"{ResourceFileName} has no plan rows (with an id) for {now.Year} or {now.Year - 1}");
        var year2 = chosen.Max(p => p.Year);

        // Each year's appeals carry that year's citation; a plan is regional within its own year.
        SourceCitation CitationFor(int y) => new(Publisher, Dataset, y.ToString(CultureInfo.InvariantCulture), CreditUrl, Licence,
            $"Financial Tracking Service (FTS), {loadedUtc.ToString("d MMM yyyy", CultureInfo.InvariantCulture)}, {CreditUrl.GetLeftPart(UriPartial.Authority)}",
            loadedUtc, now, CitationDate.AsOf);
        var citations = chosen.Select(p => p.Year).Distinct().ToDictionary(y => y, CitationFor);

        // Counted over all of a year's rows, before the January filter: a plan is regional within its own year even when only one of its countries keeps it.
        var countriesPerPlan = plans.GroupBy(p => (p.Year, p.PlanId)).ToDictionary(g => g.Key, g => g.Select(p => p.Iso3).Distinct().Count());
        var appeals = new List<AppealFunding>();
        var unplaced = new List<UnplacedReport>();
        var seen = new HashSet<(string, int, int)>();
        var notPlaced = new HashSet<string>(StringComparer.Ordinal);
        foreach (var p in chosen)
        {
            if (_countries.ByIso3(p.Iso3) is null)
            {
                if (notPlaced.Add(p.Iso3))
                {
                    log.Warn($"{Name}: code {p.Iso3} has no unit in the boundary set; listed as not placed");
                    unplaced.Add(new UnplacedReport(Name, $"FTS appeals for \"{p.Iso3}\"", "no unit in the boundary set for this code", citations[p.Year]));
                }
                continue;
            }
            if (!seen.Add((p.Iso3, p.Year, p.PlanId))) continue;
            var regional = countriesPerPlan[(p.Year, p.PlanId)] > 1 || p.PlanType.Contains("Regional", StringComparison.OrdinalIgnoreCase);
            appeals.Add(new AppealFunding(p.Iso3, p.PlanId, p.PlanName, p.PlanType, p.Year, p.Requirements, p.Funding, p.Percent, regional, citations[p.Year]));
        }
        return ([.. appeals.OrderBy(a => a.Iso3, StringComparer.Ordinal).ThenBy(a => a.PlanId)], unplaced, year2);
    }

    /// <summary>A whole number (USD or percent); blank or text is "not given", never 0.</summary>
    private static long? Whole(string text) =>
        decimal.TryParse(text, NumberStyles.Number | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out var d) && d >= 0
            ? (long)Math.Round(d) : null;
}
