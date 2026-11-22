using System.Globalization;
using System.Text.Json;
using EarthRelief3D.Core.Logic;
using EarthRelief3D.Core.Model;
using EarthRelief3D.Core.Store;
using GlobeCore.Feeds;
using GlobeCore.Logging;
using GlobeCore.Time;

namespace EarthRelief3D.Core.Feeds;

/// <summary>
/// Outbreaks from WHO Disease Outbreak News (the site's content API, no key). Any report in the last 90 days, measured
/// against the injected clock, is an outbreak crisis (C11), one per country in its title. WHO text is CC BY-NC-SA, so it is
/// only fetched at run time (C6). A reshaped document throws a <see cref="FeedFormatException"/> before the store is touched.
/// </summary>
public sealed class WhoOutbreakFeed(FeedHttp http, CrisisStore store, IClock clock, IAppLog log, CountryResolver? countries = null) : IFeed
{
    public static readonly Uri ApiUrl = new("https://www.who.int/api/news/diseaseoutbreaknews?$top=50&$orderby=PublicationDate desc");
    public static readonly Uri PageUrl = new("https://www.who.int/emergencies/disease-outbreak-news");
    public const string Window = "90 days";
    private static readonly TimeSpan WindowLength = TimeSpan.FromDays(90);

    private readonly CountryResolver _countries = countries ?? CountryResolver.LoadDefault();

    public string Name => "WHO";
    public TimeSpan Interval => TimeSpan.FromHours(3);

    public async Task<FeedRunResult> RunOnceAsync(CancellationToken ct)
    {
        var json = await http.GetStringAsync(ApiUrl, ct).ConfigureAwait(false);
        var now = clock.UtcNow;
        var (crises, unplaced, items) = Read(json, now);
        store.ReplaceKind(CrisisKind.Outbreak, crises);
        store.ReplaceUnplaced(Name, unplaced);
        log.Info($"WHO: {crises.Count} outbreak markers from {items} reports in the last {Window}; {unplaced.Count} not placed");
        return FeedRunResult.Ok;
    }

    private (List<Crisis> Crises, List<UnplacedReport> Unplaced, int Items) Read(string json, DateTime now)
    {
        JsonElement value;
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Object || !doc.RootElement.TryGetProperty("value", out var v)
                || v.ValueKind != JsonValueKind.Array)
                throw new FeedFormatException("WHO Disease Outbreak News has no \"value\" list");
            value = v.Clone();
        }
        catch (JsonException) { throw new FeedFormatException("WHO Disease Outbreak News is not JSON"); }

        var crises = new List<Crisis>();
        var unplaced = new List<UnplacedReport>();
        var cutoff = now - WindowLength;
        var items = 0;
        foreach (var item in value.EnumerateArray())
        {
            string? Str(string n) => item.TryGetProperty(n, out var p) && p.ValueKind == JsonValueKind.String ? p.GetString() : null;
            var title = Str("Title");
            var urlName = Str("UrlName");
            if (title is null || urlName is null
                || !DateTime.TryParse(Str("PublicationDate"), CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var published))
                throw new FeedFormatException("a WHO item lacks Title, UrlName or PublicationDate");
            if (published < cutoff) continue;
            items++;
            if (item.TryGetProperty("UseOverrideTitle", out var uo) && uo.ValueKind == JsonValueKind.True
                && !string.IsNullOrWhiteSpace(Str("OverrideTitle"))) title = Str("OverrideTitle")!;

            var (disease, places) = WhoTitleParser.Parse(title);
            var reportUrl = new Uri("https://www.who.int/emergencies/disease-outbreak-news/item/" + urlName);
            var citation = new SourceCitation("World Health Organization", "Disease Outbreak News", Str("DonId"), reportUrl,
                "CC BY-NC-SA 3.0 IGO", "World Health Organization, Disease Outbreak News", published, now);
            var donId = Str("DonId") ?? urlName;

            // "Trinidad and Tobago" must not be split on "and": a right-hand side that is one country wins.
            var whole = places.Count > 1 ? _countries.Resolve(string.Join(" and ", places)) : null;
            if (whole is not null) places = [string.Join(" and ", places)];
            if (places.Count == 0)
                unplaced.Add(new UnplacedReport(Name, title, "no country in the title", citation));

            var seen = new HashSet<string>();
            foreach (var place in places)
            {
                var country = whole ?? _countries.Resolve(place);
                if (country is null) { unplaced.Add(new UnplacedReport(Name, title, $"country name not recognised: {place}", citation)); continue; }
                if (!seen.Add(country.Iso3)) continue;
                crises.Add(new OutbreakCrisis($"who-{urlName}-{country.Iso3}", country.Iso3, $"{disease}: {country.Name}",
                    new NativeSeverity(0, "WHO Outbreak News", "Outbreak News report"), SeverityBands.ForOutbreak(), published, null,
                    [citation], disease, donId, published, reportUrl));
            }
        }
        return (crises, unplaced, items);
    }
}
