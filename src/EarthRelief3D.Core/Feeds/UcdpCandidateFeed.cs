using EarthRelief3D.Core.Model;
using EarthRelief3D.Core.Store;
using GlobeCore.Feeds;
using GlobeCore.Logging;
using GlobeCore.Time;

namespace EarthRelief3D.Core.Feeds;

/// <summary>
/// Armed conflicts from the UCDP Candidate Events CSV downloads (no key needed). Files are cached in memory by name, so a
/// run only downloads files it has not seen; the downloads page is read every run. A format change throws a
/// <see cref="FeedFormatException"/> before the store is touched.
/// </summary>
/// <param name="territories">Units UCDP does not list as countries (ruling R14b); <c>data/ucdp-territories.json</c> by default.</param>
public sealed class UcdpCandidateFeed(FeedHttp http, CrisisStore store, IClock clock, IAppLog log, CountryResolver? countries = null,
    IReadOnlySet<string>? territories = null) : IFeed
{
    public static readonly Uri PageUrl = new("https://ucdp.uu.se/downloads/");

    private readonly CountryResolver _countries = countries ?? CountryResolver.LoadDefault();
    private readonly IReadOnlySet<string> _territories = territories ?? CountryResolver.LoadDefaultUcdpTerritories();
    private readonly Dictionary<string, (List<UcdpEvent> Events, DateTime FetchedUtc)> _cache = new(StringComparer.Ordinal);

    private static readonly TimeSpan CacheLife = TimeSpan.FromHours(24);

    /// <summary>
    /// One "Not placed on the map" line per UCDP country string whose name the index does not know and whose events have
    /// coordinates in no country (ruling R14b).
    /// </summary>
    public static IReadOnlyList<UnplacedReport> Unplaced(UcdpReader.BuildResult result) => result.Citation is not { } cite ? [] :
    [
        .. result.Unplaced.Select(u => new UnplacedReport("UCDP", $"UCDP conflict events in \"{u.CountryName}\"",
            FormattableString.Invariant($"{u.Events:N0} {(u.Events == 1 ? "event" : "events")} ({u.Deaths:N0} deaths) at a point in no country, ")
            + $"and the country name \"{u.CountryName}\" is not recognised", cite)),
    ];

    public string Name => "UCDP";
    public TimeSpan Interval => TimeSpan.FromHours(6);

    public async Task<FeedRunResult> RunOnceAsync(CancellationToken ct)
    {
        var html = await http.GetStringAsync(PageUrl, ct).ConfigureAwait(false);
        var files = UcdpReader.FindFiles(html, PageUrl);

        // Quarterly first, so the later monthly release of an event id replaces it.
        var byId = new Dictionary<long, UcdpEvent>();
        var bad = 0;
        var fresh = new Dictionary<string, (List<UcdpEvent>, DateTime)>();
        var now = clock.UtcNow;
        // The newest monthly file and the quarterly file get revised under the same name, so they expire; older monthlies do not.
        var newestMonthly = files.MonthliesAfterQuarterly.Count > 0 ? files.MonthliesAfterQuarterly[^1].Name : null;
        foreach (var file in files.All)
        {
            var volatileFile = file.Name == newestMonthly || file.Name == files.Quarterly?.Name;
            List<UcdpEvent> events;
            if (_cache.TryGetValue(file.Name, out var hit) && !(volatileFile && now - hit.FetchedUtc >= CacheLife)) events = hit.Events;
            else
            {
                var bytes = await http.GetBytesAsync(file.Url, ct).ConfigureAwait(false);
                var (parsed, badRows) = UcdpReader.ReadEvents(bytes, file.Name);
                fresh[file.Name] = (events = parsed, now);
                bad += badRows;
            }
            foreach (var e in events) byId[e.Id] = e;
        }

        if (byId.Count == 0) throw new FeedFormatException("no events in the UCDP files");
        var result = UcdpReader.Build(byId.Values, _countries, _territories, files.Version, now, files.LatestCompleteMonth);
        // Cache only after every file parsed, so a failed run retries the files it did not finish.
        foreach (var (name, entry) in fresh) _cache[name] = entry;
        store.ReplaceKind(CrisisKind.ArmedConflict, result.Crises);
        store.ReplaceUnplaced(Name, Unplaced(result));
        store.MarkChecked(Name, now);   // R12: checked, whatever was found
        var notPlaced = result.Unplaced.Count == 0 ? "none"
            : string.Join(", ", result.Unplaced.Select(u => $"\"{u.CountryName}\" ({u.Events})"));
        log.Info($"UCDP: {result.Crises.Count} conflicts from {byId.Count} events in {files.All.Count()} files; "
            + $"{result.ByTerritory} events moved by their coordinates into a territory UCDP does not list, "
            + $"{result.ByLocation} with an unrecognised country name placed by their coordinates; events not placed: {notPlaced}; "
            + $"{bad} unreadable rows");
        return FeedRunResult.Ok;
    }
}
