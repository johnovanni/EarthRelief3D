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
public sealed class UcdpCandidateFeed(FeedHttp http, CrisisStore store, IClock clock, IAppLog log, CountryResolver? countries = null) : IFeed
{
    public static readonly Uri PageUrl = new("https://ucdp.uu.se/downloads/");

    private readonly CountryResolver _countries = countries ?? CountryResolver.LoadDefault();
    private readonly Dictionary<string, (List<UcdpEvent> Events, DateTime FetchedUtc)> _cache = new(StringComparer.Ordinal);

    private static readonly TimeSpan CacheLife = TimeSpan.FromHours(24);

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
        var result = UcdpReader.Build(byId.Values, _countries, files.Version, now, files.LatestCompleteMonth);
        // Cache only after every file parsed, so a failed run retries the files it did not finish.
        foreach (var (name, entry) in fresh) _cache[name] = entry;
        store.ReplaceKind(CrisisKind.ArmedConflict, result.Crises);
        store.MarkChecked(Name, now);   // R12: checked, whatever was found
        var names = result.UnresolvedNames.Count == 0 ? "" : ": " + string.Join(", ", result.UnresolvedNames);
        log.Info($"UCDP: {result.Crises.Count} conflicts from {byId.Count} events in {files.All.Count()} files; "
            + $"{result.SkippedUnresolved} events skipped (country not resolved{names}); {bad} unreadable rows");
        return FeedRunResult.Ok;
    }
}
