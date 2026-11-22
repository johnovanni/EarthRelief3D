using System.Globalization;
using System.Text.Json;
using EarthRelief3D.Core.Model;
using EarthRelief3D.Core.Store;
using GlobeCore.Feeds;
using GlobeCore.Logging;
using GlobeCore.Time;

namespace EarthRelief3D.Core.Feeds;

/// <summary>
/// Displaced people by country of origin from the UNHCR population API (no key). Asks for this year, then goes back one
/// year at a time (at most 3 tries) until a year has items. Displacement is a layer value, not a crisis (C11).
/// </summary>
public sealed class UnhcrDisplacementFeed(FeedHttp http, CrisisStore store, IClock clock, IAppLog log, CountryResolver? countries = null) : IFeed
{
    public const int MaxAttempts = 3;
    public static readonly Uri TermsUrl = new("https://www.unhcr.org/refugee-statistics/");

    private readonly CountryResolver _countries = countries ?? CountryResolver.LoadDefault();

    public string Name => "UNHCR";
    public TimeSpan Interval => TimeSpan.FromHours(24);

    public static Uri UrlFor(int year) => new($"https://api.unhcr.org/population/v1/population/?year={year}&coo_all=true&limit=500");

    public async Task<FeedRunResult> RunOnceAsync(CancellationToken ct)
    {
        var now = clock.UtcNow;
        for (var attempt = 0; attempt < MaxAttempts; attempt++)
        {
            var year = now.Year - attempt;
            var json = await http.GetStringAsync(UrlFor(year), ct).ConfigureAwait(false);
            var totals = Sum(json);
            if (totals.Count == 0) continue;

            var asOf = new DateTime(year, 12, 31, 0, 0, 0, DateTimeKind.Utc);
            var citation = new SourceCitation("UNHCR", "Refugee Population Statistics", year.ToString(CultureInfo.InvariantCulture), TermsUrl,
                "CC BY 4.0", "UNHCR Refugee Population Statistics Database", asOf > now ? now : asOf, now);
            var figures = new List<DisplacementFigure>();
            var skipped = 0;
            foreach (var (iso, t) in totals.OrderBy(kv => kv.Key, StringComparer.Ordinal))
            {
                if (_countries.ByIso3(iso) is null) { skipped++; continue; }
                figures.Add(new DisplacementFigure(iso, year, t.Refugees, t.AsylumSeekers, t.Idps, citation));
            }
            store.ReplaceDisplacement(figures);
            log.Info($"UNHCR: {figures.Count} countries of origin for {year}; {skipped} skipped (code not a known country)");
            return FeedRunResult.Ok;
        }
        throw new FeedFormatException($"UNHCR has no population items for {now.Year - MaxAttempts + 1} to {now.Year}");
    }

    private static Dictionary<string, (long Refugees, long AsylumSeekers, long Idps)> Sum(string json)
    {
        var totals = new Dictionary<string, (long, long, long)>(StringComparer.OrdinalIgnoreCase);
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Object || !doc.RootElement.TryGetProperty("items", out var items)
                || items.ValueKind != JsonValueKind.Array)
                throw new FeedFormatException("UNHCR response has no \"items\" list");
            foreach (var item in items.EnumerateArray())
            {
                if (!item.TryGetProperty("coo_iso", out var c) || c.ValueKind != JsonValueKind.String) continue;
                var iso = c.GetString()!.Trim().ToUpperInvariant();
                if (iso.Length != 3) continue;     // "-" and blanks
                totals.TryGetValue(iso, out var t);
                totals[iso] = (t.Item1 + Num(item, "refugees"), t.Item2 + Num(item, "asylum_seekers"), t.Item3 + Num(item, "idps"));
            }
        }
        catch (JsonException) { throw new FeedFormatException("UNHCR response is not JSON"); }
        return totals.ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Numbers arrive as JSON numbers or strings; "-" (not reported), blanks and "0" are 0.</summary>
    private static long Num(JsonElement item, string name)
    {
        if (!item.TryGetProperty(name, out var p)) return 0;
        if (p.ValueKind == JsonValueKind.Number) return p.TryGetInt64(out var n) ? n : 0;
        return p.ValueKind == JsonValueKind.String && long.TryParse(p.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var s) ? s : 0;
    }
}
