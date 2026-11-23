using System.Globalization;
using System.Text.Json;
using EarthRelief3D.Core.Model;
using EarthRelief3D.Core.Store;
using GlobeCore.Feeds;
using GlobeCore.Logging;
using GlobeCore.Time;

namespace EarthRelief3D.Core.Feeds;

/// <summary>
/// Water, sanitation and health-care coverage from the WHO Global Health Observatory OData API (no key): the three
/// indicators of docs/data-sources.md section 5. For each country the newest year's total (<c>Dim1</c>
/// <c>RESIDENCEAREATYPE_TOTL</c> where the indicator has that dimension) is stored <b>as the map shows it</b>: the share of
/// people <i>without</i> at least basic drinking water or sanitation (100 minus WHO's figure), and 100 minus the UHC service
/// coverage index (clamped at 0). All three are read before anything is written, so a reshaped answer keeps the old series.
/// </summary>
public sealed class GhoFeed(FeedHttp http, IndicatorStore indicators, CrisisStore crises, IClock clock, IAppLog log, CountryResolver? countries = null) : IFeed
{
    /// <summary>The GHO OData base address. The legacy API was to be retired near the end of 2025 but still answers; if it
    /// moves, this is the one place to change.</summary>
    public const string BaseUrl = "https://ghoapi.azureedge.net/api/";

    public const string WaterCode = "WSH_WATER_BASIC";
    public const string SanitationCode = "WSH_SANITATION_BASIC";
    public const string UhcCode = "UHC_INDEX_REPORTED";

    // What each stored series measures: the legend's wording without the year (§7). These equal the layer catalog's defaults
    // (a test checks), so the legend reads the same before and after the data arrives.
    public const string WaterMeasures = "Share of people without at least basic drinking water";
    public const string SanitationMeasures = "Share of people without at least basic sanitation";
    public const string UhcMeasures = "Essential health services not covered: 100 minus WHO's UHC service coverage index";

    public const string FeedName = "WHO GHO";
    public static readonly Uri PageUrl = new("https://www.who.int/data/gho");
    private const string TotalDim = "RESIDENCEAREATYPE_TOTL";
    private const string Publisher = "World Health Organization";

    private sealed record Indicator(string Code, string SeriesId, string Measures, string Unit, string Title);

    private static readonly Indicator[] Indicators =
    [
        new(WaterCode, "gho.water", WaterMeasures, "%", "Population using at least basic drinking-water services (%)"),
        new(SanitationCode, "gho.sanitation", SanitationMeasures, "%", "Population using at least basic sanitation services (%)"),
        new(UhcCode, "gho.uhc", UhcMeasures, "points", "UHC Service Coverage Index (SDG 3.8.1)"),
    ];

    private readonly CountryResolver _countries = countries ?? CountryResolver.LoadDefault();

    public string Name => FeedName;
    public TimeSpan Interval => TimeSpan.FromDays(7);

    public static Uri UrlFor(string indicatorCode) => new(BaseUrl + indicatorCode);

    public async Task<FeedRunResult> RunOnceAsync(CancellationToken ct)
    {
        var now = clock.UtcNow;
        var parsed = new List<(Indicator Indicator, Dictionary<string, (int Year, double Access)> Latest)>();
        foreach (var ind in Indicators)
        {
            var json = await http.GetStringAsync(UrlFor(ind.Code), ct).ConfigureAwait(false);
            parsed.Add((ind, Latest(ind, json, now.Year)));
        }

        // Place the codes (C16) and build the series; an unresolved code is logged and listed, never dropped silently.
        var series = new List<IndicatorSeries>();
        var notPlaced = new SortedDictionary<string, List<string>>(StringComparer.Ordinal);
        SourceCitation? anyCitation = null;
        foreach (var (ind, latest) in parsed)
        {
            var values = new List<CountryValue>();
            foreach (var (iso, (year, access)) in latest.OrderBy(kv => kv.Key, StringComparer.Ordinal))
            {
                if (_countries.ByIso3(iso) is null)
                {
                    if (!notPlaced.TryGetValue(iso, out var which)) notPlaced[iso] = which = [];
                    which.Add(ind.Code);
                    continue;
                }
                var citation = Cite(ind, year, now);
                anyCitation ??= citation;
                // Clamp: a share above 100 is a rounding artefact, and "without" is never negative. Two decimals hide the
                // binary noise of 100 - x without changing what WHO published.
                values.Add(new CountryValue(iso, Math.Round(Math.Max(0, 100 - access), 2), year, citation));
            }
            series.Add(new IndicatorSeries(ind.SeriesId, ind.Measures, ind.Unit, values));
        }

        foreach (var s in series) indicators.Replace(s.Id, s);
        var unplaced = new List<UnplacedReport>();
        foreach (var (iso, codes) in notPlaced)
        {
            log.Warn($"{Name}: code {iso} has no unit in the boundary set ({string.Join(", ", codes)}); listed as not placed");
            unplaced.Add(new UnplacedReport(Name, $"WHO GHO figures for \"{iso}\"", "no unit in the boundary set for this code",
                anyCitation ?? Cite(Indicators[0], now.Year, now)));
        }
        crises.ReplaceUnplaced(Name, unplaced);
        crises.MarkChecked(Name, now);   // R12
        log.Info($"{Name}: " + string.Join("; ", series.Select(s => $"{s.Id} {s.Values.Count} countries")) + $"; {notPlaced.Count} codes not placed");
        return FeedRunResult.Ok;
    }

    private static SourceCitation Cite(Indicator ind, int year, DateTime now) => new(
        Publisher, $"Global Health Observatory: {ind.Title}", year.ToString(CultureInfo.InvariantCulture), PageUrl, "CC BY 4.0",
        $"World Health Organization, Global Health Observatory: {ind.Title}, {year}; CC BY 4.0; retrieved {now.ToString("d MMM yyyy", CultureInfo.InvariantCulture)}",
        new DateTime(year, 12, 31, 0, 0, 0, DateTimeKind.Utc),   // TimeDim is the estimate's year; the row's own Date is when WHO loaded it
        now);

    /// <summary>Each country's newest year and its value (the percentage WHO publishes), from one indicator's answer.</summary>
    private static Dictionary<string, (int Year, double Access)> Latest(Indicator ind, string json, int clockYear)
    {
        var latest = new Dictionary<string, (int Year, double Access)>(StringComparer.OrdinalIgnoreCase);
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Object || !doc.RootElement.TryGetProperty("value", out var rows)
                || rows.ValueKind != JsonValueKind.Array)
                throw new FeedFormatException($"{ind.Code} answer has no \"value\" list");
            foreach (var row in rows.EnumerateArray())
            {
                if (row.ValueKind != JsonValueKind.Object) continue;
                // Regions, the world and income groups share the answer: countries only.
                if (Str(row, "SpatialDimType") != "COUNTRY") continue;
                var iso = Str(row, "SpatialDim")?.Trim().ToUpperInvariant();
                if (iso is not { Length: 3 }) continue;
                // WASH rows come as total, urban and rural; the total is the one wanted. UHC has no such dimension (null).
                if (Str(row, "Dim1") is { } dim && dim != TotalDim) continue;
                if (!row.TryGetProperty("TimeDim", out var t) || t.ValueKind != JsonValueKind.Number || !t.TryGetInt32(out var year)) continue;
                if (year < 1900 || year > clockYear) continue;
                if (!row.TryGetProperty("NumericValue", out var v) || v.ValueKind != JsonValueKind.Number) continue;   // null: no estimate
                var x = v.GetDouble();
                if (double.IsNaN(x) || double.IsInfinity(x)) continue;
                if (!latest.TryGetValue(iso, out var best) || year > best.Year) latest[iso] = (year, x);
            }
        }
        catch (JsonException) { throw new FeedFormatException($"{ind.Code} answer is not JSON"); }
        if (latest.Count == 0) throw new FeedFormatException($"{ind.Code} answer has no country values (SpatialDim, TimeDim, NumericValue)");
        return latest;
    }

    private static string? Str(JsonElement row, string name) =>
        row.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.String ? p.GetString() : null;
}
