using System.Globalization;
using System.Text.Json;
using EarthRelief3D.Core.Model;
using EarthRelief3D.Core.Store;
using GlobeCore.Feeds;
using GlobeCore.Logging;
using GlobeCore.Storage;
using GlobeCore.Time;

namespace EarthRelief3D.Core.Feeds;

/// <summary>
/// The area step of the IPC feed (P3-2, ruling R4). For each country whose latest national record is current (not stale,
/// not projection only), it finds the country's HDX dataset by the CKAN query of data-sources section 7, checks that the
/// dataset is CC0 or public domain, reads the analysis's current period from <c>ipc_&lt;iso3&gt;_national_long_latest.csv</c>
/// and, when that period is the national record's, downloads <c>ipc_&lt;iso3&gt;.geojson</c> and keeps its current-period
/// (C) areas if they are all of one analysis (<c>anl_id</c>). Otherwise the country keeps the 20% rule (R17) and the log
/// says why.
/// <para>
/// A country's files are downloaded again only when its national analysis (date of analysis and period) changes: an
/// accepted set is kept in memory and, with <c>cacheDir</c>, on disk (<c>ipc-areas\&lt;ISO3&gt;.geojson</c> and
/// <c>.json</c>), so a restart, offline too, shows the areas without a download. A GeoJSON that was downloaded and turned
/// down is tried again after <see cref="RetryRejectedAfter"/>; a failed download is tried at the next run. A turned-down
/// country is remembered on disk as well (<c>ipc-areas\&lt;ISO3&gt;.rejected.json</c>: the analysis key, when, how long,
/// and why), so a restart does not ask again before its retry time.
/// </para>
/// <para>
/// Not a feed of its own: <see cref="IpcNationalFeed"/> runs it right after it has stored the national records, which it
/// needs, and it reports through the IPC status dot. A country whose areas cannot be had keeps the 20% rule; that is
/// logged, and it does not turn the national figures, which are still good, into a failure.
/// </para>
/// </summary>
/// <param name="pause">The wait between two countries that go to HDX in one run (<see cref="DefaultPause"/>); zero for offline samples.</param>
public sealed class IpcAreaFeed(FeedHttp http, CrisisStore store, IClock clock, IAppLog log, string? cacheDir = null, TimeSpan? pause = null)
{
    public const string LogName = "IPC areas";

    /// <summary>A GeoJSON that was downloaded and turned down (P areas only, two analyses) is fetched again after this long.</summary>
    public static readonly TimeSpan RetryRejectedAfter = TimeSpan.FromHours(24);

    /// <summary>
    /// A country turned down at the search or CSV stage (no package, licence, period or date of analysis) is not asked
    /// again for this long under the same national analysis.
    /// </summary>
    public static readonly TimeSpan RetrySearchAfter = TimeSpan.FromHours(12);

    /// <summary>The wait between two countries that go to HDX in one run, so the requests are spread out.</summary>
    public static readonly TimeSpan DefaultPause = TimeSpan.FromMilliseconds(500);

    private readonly TimeSpan _pause = pause ?? DefaultPause;
    private Task _loading = Task.CompletedTask;

    /// <summary>The folder under the cache folder that holds the accepted files.</summary>
    public const string CacheFolder = "ipc-areas";

    /// <param name="RetryAfter">For a turned-down country, how long after <paramref name="RejectedUtc"/> it is asked again.</param>
    private sealed record Entry(string Key, IpcAreaSet? Set, DateTime RejectedUtc, TimeSpan RetryAfter = default);

    private readonly Dictionary<string, Entry> _entries = new(StringComparer.Ordinal);
    private readonly object _gate = new();

    private string? Dir => cacheDir is null ? null : Path.Combine(cacheDir, CacheFolder);

    /// <summary>The national analysis an area set must belong to: its date of analysis and current period.</summary>
    public static string AnalysisKey(FoodCrisis f) =>
        $"{(f.Citations.Count > 0 ? f.Citations[0].Version : null)}|{f.PeriodFrom:yyyy-MM-dd}|{f.PeriodTo:yyyy-MM-dd}";

    /// <summary>Each country's latest national record (the one the map shows), when it is current at <paramref name="nowUtc"/>.</summary>
    public static IReadOnlyList<FoodCrisis> CurrentRecords(IEnumerable<FoodCrisis> national, DateTime nowUtc) =>
    [
        .. national.GroupBy(f => f.CountryIso3.ToUpperInvariant()).OrderBy(g => g.Key, StringComparer.Ordinal)
            .Select(g => FoodCrisis.Latest(g)!)
            .Where(f => !f.ProjectionOnly && !f.IsStaleAt(nowUtc)),
    ];

    /// <summary>
    /// At start-up: the area sets on disk whose analysis is the national record's, without a download. Returns how many
    /// countries were loaded.
    /// </summary>
    public int LoadCached(IEnumerable<FoodCrisis> national)
    {
        var sets = new List<IpcAreaSet>();
        foreach (var f in CurrentRecords(national, clock.UtcNow))
        {
            var iso3 = f.CountryIso3.ToUpperInvariant();
            if (ReadCache(iso3, AnalysisKey(f)) is not { } set) continue;
            lock (_gate) _entries[iso3] = new Entry(AnalysisKey(f), set, default);
            sets.Add(set);
        }
        if (sets.Count > 0)
        {
            store.ReplaceIpcAreas(sets);
            log.Info($"{LogName}: {sets.Count} countries loaded from the cache ({string.Join(", ", sets.Select(s => s.Iso3))})");
        }
        return sets.Count;
    }

    /// <summary>
    /// <see cref="LoadCached"/> on a pool thread (start-up: up to a few MB of GeoJSON per country are parsed, never on the UI
    /// thread). The areas reach the store, and so the map, when it is done; the first <see cref="RunAsync"/> waits for it,
    /// so a run never races the load. Failures are logged.
    /// </summary>
    public Task StartLoadingCached(IEnumerable<FoodCrisis> national)
    {
        var records = national.ToList();
        return _loading = Task.Run(() =>
        {
            try { LoadCached(records); }
            catch (Exception e) { log.Error($"{LogName}: the cached areas could not be loaded", e); }
        });
    }

    /// <summary>
    /// Finds the areas of every country whose latest national record is current, and replaces the store's area sets with
    /// the ones that belong to their national record. Per-country problems are logged; cancellation is passed on.
    /// </summary>
    public async Task RunAsync(IEnumerable<FoodCrisis> national, CancellationToken ct)
    {
        await _loading.ConfigureAwait(false);   // the start-up load first (it never throws)
        var now = clock.UtcNow;
        var sets = new List<IpcAreaSet>();
        var kept = new List<string>();
        var asked = false;
        FeedBlockedException? blocked = null;
        foreach (var f in CurrentRecords(national, now))
        {
            ct.ThrowIfCancellationRequested();
            var iso3 = f.CountryIso3.ToUpperInvariant();
            var key = AnalysisKey(f);
            Entry? entry;
            lock (_gate) _entries.TryGetValue(iso3, out entry);
            if (entry is not null && entry.Key == key && (entry.Set is not null || now - entry.RejectedUtc < entry.RetryAfter))
            {
                if (entry.Set is not null) sets.Add(entry.Set); else kept.Add(iso3);
                continue;   // the same analysis: nothing is downloaded
            }
            if (ReadCache(iso3, key) is { } cached)
            {
                lock (_gate) _entries[iso3] = new Entry(key, cached, default);
                sets.Add(cached);
                log.Info($"{LogName}: {iso3} from the cache ({cached.Areas.Count} areas, analysis {cached.AnalysisId})");
                continue;
            }
            // C3: turned down under this analysis by an earlier run, and not yet due again: nothing is downloaded.
            if (ReadRejection(iso3, key, now) is { } rejected)
            {
                lock (_gate) _entries[iso3] = new Entry(key, null, rejected.RejectedUtc, rejected.RetryAfter);
                kept.Add(iso3);
                log.Info($"{LogName}: {iso3} keeps the 20% rule: turned down {rejected.RejectedUtc:yyyy-MM-dd HH:mm} UTC ({rejected.Why}); "
                    + $"asked again after {rejected.RejectedUtc + rejected.RetryAfter:yyyy-MM-dd HH:mm} UTC");
                continue;
            }
            // HDX's firewall refused a request in this run: the countries left are not asked (each would only be refused again).
            if (blocked is not null)
            {
                kept.Add(iso3);
                continue;
            }
            if (asked && _pause > TimeSpan.Zero) await Task.Delay(_pause, ct).ConfigureAwait(false);   // spread the requests to HDX
            asked = true;
            try
            {
                var (set, why, downloaded) = await FetchAsync(iso3, f, now, ct).ConfigureAwait(false);
                lock (_gate)
                {
                    // Turned down: a downloaded GeoJSON is not fetched again for a day, a search or CSV answer for 12 hours.
                    _entries[iso3] = set is not null ? new Entry(key, set, default)
                        : new Entry(key, null, now, downloaded ? RetryRejectedAfter : RetrySearchAfter);
                }
                if (set is not null)
                {
                    sets.Add(set);
                    DeleteRejection(iso3);
                }
                else
                {
                    kept.Add(iso3);
                    WriteRejection(iso3, new Rejection(key, now, downloaded ? RetryRejectedAfter : RetrySearchAfter, why));
                    log.Info($"{LogName}: {iso3} keeps the 20% rule: {why}");
                }
            }
            catch (FeedBlockedException e)
            {
                lock (_gate) _entries.Remove(iso3);
                kept.Add(iso3);
                blocked = e;
                log.Warn($"{LogName}: {iso3} and the countries after it keep the 20% rule for now: {e.Message}");
            }
            catch (Exception e) when (e is HttpRequestException or TimeoutException or FeedFormatException or FeedAuthException or IOException)
            {
                lock (_gate) _entries.Remove(iso3);
                kept.Add(iso3);
                log.Warn($"{LogName}: {iso3} keeps the 20% rule: its IPC files could not be read ({e.Message})");
            }
        }
        store.ReplaceIpcAreas(sets);
        log.Info($"{LogName}: {sets.Count} countries drawn by area ({string.Join(", ", sets.Select(s => s.Iso3))}); "
            + $"{kept.Count} keep the 20% rule" + (blocked is null ? "" : " (HDX refused requests during this run)"));
    }

    // One country: the search, the licence, the national CSV's current period, then the GeoJSON. "Downloaded" says whether
    // the GeoJSON was fetched (a turned-down GeoJSON is not fetched again for a day).
    private async Task<(IpcAreaSet? Set, string Why, bool Downloaded)> FetchAsync(string iso3, FoodCrisis f, DateTime now, CancellationToken ct)
    {
        var search = await http.GetStringAsync(IpcAreaReader.SearchUrl(iso3), ct).ConfigureAwait(false);
        if (IpcAreaReader.FindPackage(search, iso3) is not { } package)
            return (null, $"no IPC package with {IpcAreaReader.GeoJsonName(iso3)} on HDX", false);
        if (!IpcAreaReader.IsPublicDomain(package.LicenceId))
            return (null, $"the dataset {package.Name} has the licence \"{package.LicenceId}\", which is not CC0 or public domain", false);
        if (package.NationalCsvUrl is null)
            return (null, $"the dataset {package.Name} has no {IpcAreaReader.NationalCsvName(iso3)} to date its areas", false);

        var csv = await http.GetBytesAsync(package.NationalCsvUrl, ct).ConfigureAwait(false);
        var period = IpcAreaReader.CurrentPeriod(csv, iso3);
        var national = Period(f.PeriodFrom, f.PeriodTo);
        if (period.Current is not { } current)
            return (null, $"no current period in {IpcAreaReader.NationalCsvName(iso3)} ({period.Found}); the national record's is {national}", false);
        if (current.From != f.PeriodFrom || current.To != f.PeriodTo)
            return (null, $"the country file's current period is {Period(current.From, current.To)}, the national record's is {national}", false);
        // R10: the same date of analysis too (trimmed, any case). R11: a blank one on either side cannot confirm it.
        var fileDate = Normalised(current.DateOfAnalysis);
        var nationalDate = Normalised(f.Citations.Count > 0 ? f.Citations[0].Version : null);
        if (fileDate is null || nationalDate is null)
            return (null, "the date of analysis is blank in " + (fileDate is null && nationalDate is null ? "the country file and the national record"
                : fileDate is null ? $"the country file (the national record's is {nationalDate})" : $"the national record (the country file's is {fileDate})")
                + ", so the same analysis cannot be confirmed", false);
        if (!string.Equals(fileDate, nationalDate, StringComparison.OrdinalIgnoreCase))
            return (null, $"country file's date of analysis {fileDate}, national record's {nationalDate}", false);

        var geojson = await http.GetBytesAsync(package.GeoJsonUrl, ct).ConfigureAwait(false);
        var (set, why) = Build(iso3, package, current, geojson, now);
        if (set is not null) WriteCache(iso3, AnalysisKey(f), package, current, geojson, now);
        return (set, why, true);
    }

    // R4: the current-period areas, all of one analysis.
    private (IpcAreaSet? Set, string Why) Build(string iso3, IpcAreaReader.Package package, IpcAreaReader.Period period, byte[] geojson, DateTime retrievedUtc)
    {
        var cite = new SourceCitation(IpcNationalReader.Publisher, IpcAreaReader.Dataset, period.DateOfAnalysis, IpcAreaReader.PageUrl(package.Name),
            "CC0", IpcAreaReader.Attribution, period.To.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc), retrievedUtc);
        var file = IpcAreaReader.Read(geojson, iso3, period, cite);
        var others = $" ({string.Join(", ", file.OtherPeriods.Select(o => $"{o.Value} {o.Key} areas").Append($"{file.Points} points"))})";
        if (file.Current.Count == 0)
            return (null, $"{IpcAreaReader.GeoJsonName(iso3)} has no current-period (C) areas{others}");
        if (file.AnalysisIds.Count != 1)
            return (null, $"its current-period areas belong to {file.AnalysisIds.Count} analyses (anl_id {string.Join(", ", file.AnalysisIds)})");
        log.Info($"{LogName}: {iso3}: {file.Current.Count} areas of analysis {file.AnalysisIds[0]} ({Period(period.From, period.To)}); "
            + $"{file.Points} points (settlements, not areas) and {file.OtherPeriods.Values.Sum()} areas of other periods left out"
            + (file.Unreadable > 0 ? $"; {file.Unreadable} features unreadable" : ""));
        return (new IpcAreaSet(iso3, file.AnalysisIds[0], period.DateOfAnalysis, period.From, period.To, file.Current, cite), "");
    }

    private static string? Normalised(string? date) => string.IsNullOrWhiteSpace(date) ? null : date.Trim();

    private static string Period(DateOnly from, DateOnly to) =>
        from.ToString("d MMM yyyy", CultureInfo.InvariantCulture) + "–" + to.ToString("d MMM yyyy", CultureInfo.InvariantCulture);

    // ---------------------------------------------------------------- the files on disk

    /// <summary>A country turned down under the analysis <paramref name="Key"/> (C3): asked again after <paramref name="RetryAfter"/>.</summary>
    private sealed record Rejection(string Key, DateTime RejectedUtc, TimeSpan RetryAfter, string Why);

    private string? RejectionFile(string iso3) => Dir is { } dir ? Path.Combine(dir, iso3 + ".rejected.json") : null;

    // A turned-down country is remembered on disk too, so a restart does not download its files again (Nigeria's GeoJSON is
    // 1.9 MB) before the retry time.
    private void WriteRejection(string iso3, Rejection r)
    {
        if (RejectionFile(iso3) is not { } file) return;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(file)!);
            if (!AtomicFile.TryWriteAllText(file, JsonSerializer.Serialize(r), out var error))
                log.Warn($"{LogName}: could not save that {iso3} was turned down", error);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            log.Warn($"{LogName}: could not save that {iso3} was turned down", e);
        }
    }

    // The saved rejection, when it is for this analysis and not yet due again; otherwise null.
    private Rejection? ReadRejection(string iso3, string key, DateTime now)
    {
        if (RejectionFile(iso3) is not { } file || !File.Exists(file)) return null;
        try
        {
            var r = JsonSerializer.Deserialize<Rejection>(File.ReadAllText(file));
            return r is not null && r.Key == key && now >= r.RejectedUtc && now - r.RejectedUtc < r.RetryAfter ? r : null;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException or NotSupportedException)
        {
            log.Warn($"{LogName}: {iso3}'s saved rejection could not be read", e);
            return null;
        }
    }

    private void DeleteRejection(string iso3)
    {
        if (RejectionFile(iso3) is not { } file) return;
        try { File.Delete(file); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { log.Warn($"{LogName}: could not delete {file}", e); }
    }

    private sealed record CacheMeta(string Key, string Package, string Licence, string? DateOfAnalysis, DateOnly From, DateOnly To, DateTime RetrievedUtc);

    private void WriteCache(string iso3, string key, IpcAreaReader.Package package, IpcAreaReader.Period period, byte[] geojson, DateTime now)
    {
        if (Dir is not { } dir) return;
        try
        {
            Directory.CreateDirectory(dir);
            var meta = new CacheMeta(key, package.Name, package.LicenceId, period.DateOfAnalysis, period.From, period.To, now);
            // The GeoJSON first: a meta file is only ever next to its own GeoJSON.
            if (!AtomicFile.TryWriteAllBytes(Path.Combine(dir, iso3 + ".geojson"), geojson, out var error)
                || !AtomicFile.TryWriteAllText(Path.Combine(dir, iso3 + ".json"), JsonSerializer.Serialize(meta), out error))
                log.Warn($"{LogName}: could not save {iso3}'s areas to the cache", error);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            log.Warn($"{LogName}: could not save {iso3}'s areas to the cache", e);
        }
    }

    private IpcAreaSet? ReadCache(string iso3, string key)
    {
        if (Dir is not { } dir) return null;
        var metaFile = Path.Combine(dir, iso3 + ".json");
        var geoFile = Path.Combine(dir, iso3 + ".geojson");
        if (!File.Exists(metaFile) || !File.Exists(geoFile)) return null;
        try
        {
            var meta = JsonSerializer.Deserialize<CacheMeta>(File.ReadAllText(metaFile));
            if (meta is null || meta.Key != key || meta.Package is null) return null;
            var period = new IpcAreaReader.Period(meta.DateOfAnalysis, meta.From, meta.To);
            var (set, why) = Build(iso3, new IpcAreaReader.Package(meta.Package, meta.Licence ?? "", new Uri("file:///" + iso3), null),
                period, File.ReadAllBytes(geoFile), meta.RetrievedUtc);
            if (set is null) log.Warn($"{LogName}: {iso3}'s cached areas were not used: {why}");
            return set;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException or FeedFormatException or NotSupportedException)
        {
            log.Warn($"{LogName}: {iso3}'s cached areas could not be read", e);
            return null;
        }
    }
}
