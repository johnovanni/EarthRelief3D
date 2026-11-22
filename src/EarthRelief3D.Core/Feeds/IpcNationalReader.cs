using System.Globalization;
using System.Text.Json;
using EarthRelief3D.Core.Logic;
using EarthRelief3D.Core.Model;
using GlobeCore.Earth;

namespace EarthRelief3D.Core.Feeds;

public static class IpcNationalReader
{
    public const string ResourceName = "ipc_global_national_wide_latest.csv";
    public const string Publisher = "Integrated Food Security Phase Classification (IPC), via HDX";
    public const string Dataset = "IPC acute food insecurity, national figures";
    public const string Attribution = "Integrated Food Security Phase Classification (IPC), via HDX; National IPC Technical Working Groups";
    public static readonly Uri PageUrl = new("https://data.humdata.org/dataset/global-acute-food-insecurity-country-data");

    /// <summary>The download URL of the "latest" national wide CSV, read from the HDX package JSON (never hard-coded).</summary>
    public static Uri FindResourceUrl(string packageJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(packageJson);
            var resources = doc.RootElement.GetProperty("result").GetProperty("resources");
            foreach (var r in resources.EnumerateArray())
                if (r.TryGetProperty("name", out var n) && n.GetString() == ResourceName
                    && r.TryGetProperty("url", out var u) && Uri.TryCreate(u.GetString(), UriKind.Absolute, out var uri))
                    return uri;
        }
        catch (Exception e) when (e is JsonException or KeyNotFoundException or InvalidOperationException)
        {
            throw new FeedFormatException("the HDX package JSON has no result.resources list");
        }
        throw new FeedFormatException($"the HDX package has no resource named {ResourceName}");
    }

    private static readonly string[] Required =
    [
        "Date of analysis", "Country", "Current from", "Current to", "Phase 3+ number current", "Phase 3+ percentage current",
        "Phase 1 number current", "Phase 2 number current", "Phase 3 number current", "Phase 4 number current", "Phase 5 number current",
    ];

    /// <param name="Crises">Current-period analyses with people in Phase 3 or higher (stale ones marked), then projection-only records.</param>
    /// <param name="ProjectionOnly">Rows with no current period but a first or second projection (kept, never crisis markers).</param>
    /// <param name="BlankPeriod">Rows with neither a current period nor a projection (skipped).</param>
    public sealed record Result(List<FoodCrisis> Crises, int ProjectionOnly, int BlankPeriod);

    // The projections, nearest first: a row with no current period is described by the first one it has.
    private static readonly (string Columns, string Name)[] Projections = [("First projection", "first projection"), ("Second projection", "second projection")];

    /// <summary>
    /// One FoodCrisis per country whose current period has more than 0 people in Phase 3 or higher, with the number of
    /// people in Phases 3, 4 and 5 (R16). Analyses whose period ended more than 12 months ago are kept, marked stale (P2-4).
    /// A row with a blank current period but a first or second projection is kept as a projection-only record (not a
    /// crisis; the panel says "projection only"); a row with neither is counted and skipped.
    /// </summary>
    public static Result Read(byte[] csv, CountryResolver countries, DateTime nowUtc)
    {
        var crises = new List<FoodCrisis>();
        var projections = new List<FoodCrisis>();
        var blank = 0;
        var usable = 0;
        string[]? header = null;
        Dictionary<string, int> col = [];
        var staleBefore = DateOnly.FromDateTime(nowUtc).AddMonths(-FoodCrisis.StaleAfterMonths);
        foreach (var row in CsvReader.Read(csv))
        {
            if (header is null) { header = row; col = CsvReader.Columns(header, ResourceName, Required); continue; }
            if (row.Length < header.Length) continue;
            // The projection columns are optional: a file without them still has current figures.
            string F(string n) => col.TryGetValue(n, out var i) ? row[i].Trim() : "";
            var iso3 = F("Country").ToUpperInvariant();
            var name = countries.ByIso3(iso3)?.Name ?? iso3;
            SourceCitation Cite(DateOnly periodEnd) => new(Publisher, Dataset, F("Date of analysis"), PageUrl, "CC0", Attribution,
                periodEnd.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc), nowUtc);

            if (!TryDate(F("Current from"), out var from) || !TryDate(F("Current to"), out var to))
            {
                if (Projection(F, iso3, name, staleBefore, Cite) is { } projected) projections.Add(projected);
                else blank++;
                continue;
            }
            usable++;
            if (!TryLong(F("Phase 3+ number current"), out var people) || people <= 0) continue;

            var highest = 3;
            for (var n = 5; n >= 4; n--)
                if (TryLong(F($"Phase {n} number current"), out var count) && count > 0) { highest = n; break; }

            crises.Add(new FoodCrisis(
                Id: $"ipc-{iso3}", CountryIso3: iso3, Title: $"{name}: food insecurity",
                Severity: new NativeSeverity(highest, "IPC phase", $"Phase {highest} ({PhaseName(highest)})"),
                Band: SeverityBands.ForFood(highest), Began: from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc), Trend: null,
                Citations: [Cite(to)], HighestPhase: highest, Phase3PlusPeople: people, Phase3PlusShare: Share(F("Phase 3+ percentage current")),
                PeriodFrom: from, PeriodTo: to, IsStale: to < staleBefore)
            {
                Phase3People = Count(F("Phase 3 number current")),
                Phase4People = Count(F("Phase 4 number current")),
                Phase5People = Count(F("Phase 5 number current")),
            });
        }
        if (header is null) throw new FeedFormatException($"{ResourceName} is empty");
        if (usable == 0) throw new FeedFormatException("no IPC row has a usable current period");
        return new Result([.. crises, .. projections], projections.Count, blank);
    }

    // A row without current-period figures, described by its nearest projection (first, else second); null when it has none.
    private static FoodCrisis? Projection(Func<string, string> F, string iso3, string name, DateOnly staleBefore, Func<DateOnly, SourceCitation> cite)
    {
        foreach (var (columns, label) in Projections)
        {
            if (!TryDate(F($"{columns} from"), out var from) || !TryDate(F($"{columns} to"), out var to)) continue;
            var lower = columns.ToLowerInvariant();   // "Phase 3+ number first projection"
            return new FoodCrisis(
                Id: $"ipc-{iso3}", CountryIso3: iso3, Title: $"{name}: IPC projection only",
                Severity: new NativeSeverity(0, "IPC phase", "projection only, no current-period figures"),
                Band: 0, Began: null, Trend: null, Citations: [cite(to)], HighestPhase: 0,
                Phase3PlusPeople: Count(F($"Phase 3+ number {lower}")), Phase3PlusShare: Share(F($"Phase 3+ percentage {lower}")),
                PeriodFrom: from, PeriodTo: to, IsStale: to < staleBefore)
            {
                ProjectionOnly = true,
                ProjectionName = label,
            };
        }
        return null;
    }

    private static long Count(string text) => TryLong(text, out var v) && v > 0 ? v : 0;

    private static double Share(string text) =>
        double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var s) ? s : 0;

    public static string PhaseName(int phase) => phase switch
    {
        3 => "Crisis",
        4 => "Emergency",
        5 => "Catastrophe/Famine",
        _ => "",
    };

    private static bool TryDate(string text, out DateOnly d) =>
        DateOnly.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out d);

    private static bool TryLong(string text, out long v)
    {
        if (long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out v)) return true;
        if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var d)) { v = (long)Math.Round(d); return true; }
        v = 0;
        return false;
    }
}
