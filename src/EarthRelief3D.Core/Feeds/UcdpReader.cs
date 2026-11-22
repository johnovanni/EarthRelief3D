using System.Globalization;
using System.Text.RegularExpressions;
using AstroCore.Earth;
using EarthRelief3D.Core.Logic;
using EarthRelief3D.Core.Model;
using GlobeCore.Earth;

namespace EarthRelief3D.Core.Feeds;

/// <summary>One UCDP Candidate download linked from the downloads page.</summary>
public sealed record UcdpFile(string Name, Uri Url, int EndYear, int EndMonth)
{
    public int EndIndex => EndYear * 12 + EndMonth;
}

/// <summary>The files that make up the window (P2-8): the newest quarterly cumulative file and every monthly file after its end.</summary>
public sealed record UcdpFiles(UcdpFile? Quarterly, IReadOnlyList<UcdpFile> MonthliesAfterQuarterly, string Version, DateOnly LatestCompleteMonth)
{
    public IEnumerable<UcdpFile> All => Quarterly is null ? MonthliesAfterQuarterly : MonthliesAfterQuarterly.Prepend(Quarterly);
}

/// <summary>One GED event row. The source_* columns are never read.</summary>
public sealed record UcdpEvent(long Id, int ConflictId, string ConflictName, string SideA, string SideB, string Country,
    double Lat, double Lon, DateTime StartUtc, DateTime EndUtc, int Best, int ViolenceType);

public static class UcdpReader
{
    public const string Dataset = "UCDP Candidate Events Dataset";
    public const string Attribution = "Uppsala Conflict Data Program (UCDP) Candidate Events Dataset, Hegre et al. 2020";
    public const int CrisisDeathsPerYear = 25;

    // GEDEvent_v26_0_10.csv (monthly: year, minor version 0, month) and GEDEvent_v26_01_26_09.csv (quarterly cumulative:
    // first month, then year and last month). The .xlsx twin of the monthly file is spelled GEDevent_ and is not matched.
    private static readonly Regex Href = new("href\\s*=\\s*\"(?<u>[^\"]*candidateged/(?<n>GEDEvent_[^\"/]*\\.csv))\"", RegexOptions.IgnoreCase);
    private static readonly Regex MonthlyName = new("^GEDEvent_v(?<y>\\d\\d)_0_(?<m>\\d{1,2})\\.csv$", RegexOptions.IgnoreCase);
    private static readonly Regex QuarterlyName = new("^GEDEvent_v\\d\\d_(?<m1>\\d{1,2})_(?<y>\\d\\d)_(?<m2>\\d{1,2})\\.csv$", RegexOptions.IgnoreCase);

    public static UcdpFiles FindFiles(string html, Uri pageUrl)
    {
        var monthlies = new List<UcdpFile>();
        var quarterlies = new List<UcdpFile>();
        foreach (Match m in Href.Matches(html))
        {
            var name = m.Groups["n"].Value;
            var url = new Uri(pageUrl, m.Groups["u"].Value);
            if (MonthlyName.Match(name) is { Success: true } mm)
                monthlies.Add(new UcdpFile(name, url, 2000 + int.Parse(mm.Groups["y"].Value), int.Parse(mm.Groups["m"].Value)));
            else if (QuarterlyName.Match(name) is { Success: true } qm)
                quarterlies.Add(new UcdpFile(name, url, 2000 + int.Parse(qm.Groups["y"].Value), int.Parse(qm.Groups["m2"].Value)));
        }
        if (monthlies.Count == 0 && quarterlies.Count == 0)
            throw new FeedFormatException("no UCDP candidateged/GEDEvent_*.csv links on the downloads page");

        var quarterly = quarterlies.DistinctBy(f => f.Name).OrderByDescending(f => f.EndIndex).FirstOrDefault();
        var after = monthlies.DistinctBy(f => f.Name).Where(f => quarterly is null || f.EndIndex > quarterly.EndIndex)
            .OrderBy(f => f.EndIndex).ToList();
        var newest = after.Count > 0 ? after[^1] : quarterly!;
        // Latest complete month = the release month in the newest file's name (v26_0_10 is October 2026), not the newest
        // date_end: a monthly file also carries the first days of the following month, which are not a complete month.
        return new UcdpFiles(quarterly, after, VersionOf(newest), new DateOnly(newest.EndYear, newest.EndMonth, 1));
    }

    /// <summary>"GEDEvent_v26_0_10.csv" gives "26.0.10"; the quarterly "GEDEvent_v26_01_26_09.csv" gives "26.01.26.09".</summary>
    private static string VersionOf(UcdpFile f) => f.Name["GEDEvent_v".Length..^".csv".Length].Replace('_', '.');

    private static readonly string[] Required =
    [
        "id", "type_of_violence", "conflict_new_id", "conflict_name", "side_a", "side_b", "country",
        "latitude", "longitude", "date_start", "date_end", "best",
    ];

    /// <summary>Parses one file. Rows with an unreadable number or date are skipped and counted. A missing column is a format change.</summary>
    public static (List<UcdpEvent> Events, int BadRows) ReadEvents(byte[] csv, string fileName)
    {
        var events = new List<UcdpEvent>();
        var bad = 0;
        string[]? header = null;
        Dictionary<string, int> col = [];
        foreach (var row in CsvReader.Read(csv))
        {
            if (header is null) { header = row; col = CsvReader.Columns(header, fileName, Required); continue; }
            if (row.Length < header.Length) { bad++; continue; }
            string F(string n) => row[col[n]];
            if (!long.TryParse(F("id"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var id)
                || !int.TryParse(F("conflict_new_id"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var conflict)
                || !double.TryParse(F("latitude"), NumberStyles.Float, CultureInfo.InvariantCulture, out var lat)
                || !double.TryParse(F("longitude"), NumberStyles.Float, CultureInfo.InvariantCulture, out var lon)
                || !int.TryParse(F("best"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var best)
                || !TryDate(F("date_start"), out var start) || !TryDate(F("date_end"), out var end))
            { bad++; continue; }
            _ = int.TryParse(F("type_of_violence"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var type);
            events.Add(new UcdpEvent(id, conflict, F("conflict_name"), F("side_a"), F("side_b"), F("country"), lat, lon, start, end, best, type));
        }
        if (header is null) throw new FeedFormatException($"{fileName} is empty");
        return (events, bad);
    }

    // "2026-10-25 00:00:00.000": only the date part matters.
    private static bool TryDate(string text, out DateTime utc)
    {
        utc = default;
        if (text.Length < 10 || !DateOnly.TryParseExact(text.AsSpan(0, 10), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)) return false;
        utc = d.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        return true;
    }

    public sealed record BuildResult(List<ConflictCrisis> Crises, int SkippedUnresolved, IReadOnlyList<string> UnresolvedNames, DateTime? AsOfUtc);

    /// <summary>
    /// Groups events (already de-duplicated by id) by UCDP conflict and country. A group is a crisis when its best-estimate
    /// deaths reach 25 in any calendar year of the window (C11).
    /// </summary>
    public static BuildResult Build(IReadOnlyCollection<UcdpEvent> events, CountryResolver countries, string version, DateTime nowUtc, DateOnly latestComplete)
    {
        if (events.Count == 0) return new BuildResult([], 0, [], null);
        var asOf = events.Max(e => e.EndUtc);
        var citation = new SourceCitation("Uppsala Conflict Data Program (UCDP)", Dataset, version,
            new Uri("https://ucdp.uu.se/downloads/"), "CC BY 4.0", Attribution, asOf, nowUtc);

        var asOfDay = DateOnly.FromDateTime(asOf);
        var asOfMonth = new DateOnly(asOfDay.Year, asOfDay.Month, 1);
        var lastMonth = asOfMonth > latestComplete ? asOfMonth : latestComplete;
        var firstMonth = events.Min(e => new DateOnly(e.StartUtc.Year, e.StartUtc.Month, 1));
        // C11 (ruling R9): the 25-deaths-in-a-calendar-year test is on the whole conflict, all countries together.
        var qualifying = events.GroupBy(e => e.ConflictId)
            .Where(g => g.GroupBy(e => e.StartUtc.Year).Max(y => y.Sum(e => e.Best)) >= CrisisDeathsPerYear)
            .Select(g => g.Key).ToHashSet();

        var skipped = 0;
        var unresolved = new SortedSet<string>(StringComparer.Ordinal);
        var groups = new Dictionary<(int Conflict, string Iso3), List<UcdpEvent>>();
        var resolved = new Dictionary<string, Country?>();
        foreach (var e in events)
        {
            if (!resolved.TryGetValue(e.Country, out var country)) resolved[e.Country] = country = countries.Resolve(e.Country);
            if (country is null) { skipped++; unresolved.Add(e.Country); continue; }
            if (!qualifying.Contains(e.ConflictId)) continue;
            var key = (e.ConflictId, country.Iso3);
            if (!groups.TryGetValue(key, out var list)) groups[key] = list = [];
            list.Add(e);
        }

        var crises = new List<ConflictCrisis>();
        foreach (var (key, list) in groups.OrderBy(g => g.Key.Iso3).ThenBy(g => g.Key.Conflict))
        {
            var byMonth = new Dictionary<DateOnly, int>();
            for (var m = firstMonth; m <= lastMonth; m = m.AddMonths(1)) byMonth[m] = 0;
            foreach (var e in list) byMonth[new DateOnly(e.StartUtc.Year, e.StartUtc.Month, 1)] += e.Best;

            var ordered = list.OrderBy(e => e.StartUtc).ThenBy(e => e.Id).ToList();
            var newest = ordered[^1];
            var deaths = ordered.Sum(e => e.Best);   // deaths in the window, counted since its first month
            var band = Math.Max(1, SeverityBands.ForConflict(deaths));
            var countryName = countries.ByIso3(key.Iso3)?.Name ?? key.Iso3;
            crises.Add(new ConflictCrisis(
                Id: $"ucdp-{key.Conflict}-{key.Iso3}", CountryIso3: key.Iso3, Title: $"{countryName}: {newest.ConflictName}",
                Severity: new NativeSeverity(deaths, "best-estimate deaths in the window", $"{deaths:N0} deaths since {firstMonth.ToString("MMM yyyy", CultureInfo.InvariantCulture)} (UCDP)"),
                Band: band, Began: null, Trend: Trend.FromMonths(byMonth, latestComplete), Citations: [citation],
                UcdpConflictId: key.Conflict, SideA: newest.SideA, SideB: newest.SideB,
                Events: ordered.Select(e => new ConflictEvent(e.Id, e.StartUtc, new GeoPosition(e.Lat, e.Lon), e.Best, e.ViolenceType)).ToList(),
                DeathsByMonth: byMonth));
        }
        return new BuildResult(crises, skipped, [.. unresolved], asOf);
    }
}
