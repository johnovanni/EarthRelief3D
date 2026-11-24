using System.Globalization;
using System.Reflection;
using EarthRelief3D.Core.Help;

namespace EarthRelief3D.Vetting;

/// <summary>
/// <c>check [--today yyyy-MM-dd] [--offline &lt;dir&gt;] [--out &lt;file&gt;]</c>, and for tests and odd layouts
/// <c>[--data &lt;dir&gt;] [--cache &lt;dir&gt;] [--skip-links]</c>. Exit codes: 0 done (the report may still list problems), 1 a file could
/// not be read or downloaded, 2 usage. It reads <c>data/countries/US/charities.json</c> and writes one report; it never writes under
/// the data folder.
/// </summary>
public static class VettingRun
{
    public const string ProjectPage = "https://github.com/johnovanni/EarthRelief3D";
    private const string Pub78Name = "data-download-pub78.zip";
    private const string RevocationName = "data-download-revocation.zip";
    private static readonly Uri IrsBase = new("https://apps.irs.gov/pub/epostcard/");

    public static string UserAgent { get; } = $"EarthRelief3D-Vetting/{Version()} (+{ProjectPage})";

    private static string Version()
    {
        var info = typeof(VettingRun).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        var plus = info?.IndexOf('+') ?? -1;
        return plus >= 0 ? info![..plus] : info ?? "0.0.0";
    }

    private const string Usage = """
        usage: EarthRelief3D.Vetting check [--today yyyy-MM-dd] [--offline <dir>] [--out <file>]
               [--data <dir>] [--cache <dir>] [--skip-links]

          --today    the day to check against (default: today)
          --offline  read the IRS files from this folder instead of downloading them
                     (data-download-pub78.zip, data-download-revocation.zip, index_<year>.csv, <batch>.zip)
          --out      the report file (default: artifacts/vetting/vetting-report-<date>.md)
          --data     the data folder (default: <repository>/data)
          --cache    where downloads are kept (default: <repository>/.downloads/irs)
          --skip-links  do not request the charities' own links
        """;

    private sealed record Options(DateOnly Today, string? Offline, string? Out, string? Data, string? Cache, bool SkipLinks);

    public static async Task<int> RunAsync(string[] args, TextWriter output, TextWriter error, HttpMessageHandler? linkHandler = null, CancellationToken ct = default)
    {
        var options = Parse(args, error);
        if (options is null) return 2;
        try
        {
            return await CheckAsync(options, output, error, linkHandler, ct).ConfigureAwait(false);
        }
        // A malformed list (HelpTables.Load: InvalidDataException naming the file; a raw JsonException from any other read, m37) is
        // a clear error with exit 1, as a missing IRS file is.
        catch (Exception ex) when (ex is IOException or InvalidDataException or System.Text.Json.JsonException or HttpRequestException
                                   or UnauthorizedAccessException or TimeoutException or TaskCanceledException)
        {
            await error.WriteLineAsync($"error: {ex.Message}").ConfigureAwait(false);
            return 1;
        }
    }

    private static Options? Parse(string[] args, TextWriter error)
    {
        if (args.Length == 0 || args[0] != "check")
        {
            error.WriteLine(Usage);
            return null;
        }
        var today = DateOnly.FromDateTime(DateTime.Now);
        string? offline = null, outFile = null, data = null, cache = null;
        var skipLinks = false;
        for (var i = 1; i < args.Length; i++)
        {
            string? Value()
            {
                if (i + 1 < args.Length) return args[++i];
                error.WriteLine($"{args[i]} needs a value");
                return null;
            }
            switch (args[i])
            {
                case "--today":
                    var v = Value();
                    if (v is null) return Fail(error);
                    if (!DateOnly.TryParseExact(v, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out today))
                    {
                        error.WriteLine($"--today must be a date like 2026-11-24, not \"{v}\"");
                        return Fail(error);
                    }
                    break;
                case "--offline": offline = Value(); if (offline is null) return Fail(error); break;
                case "--out": outFile = Value(); if (outFile is null) return Fail(error); break;
                case "--data": data = Value(); if (data is null) return Fail(error); break;
                case "--cache": cache = Value(); if (cache is null) return Fail(error); break;
                case "--skip-links": skipLinks = true; break;
                default:
                    error.WriteLine($"unknown option {args[i]}");
                    return Fail(error);
            }
        }
        return new Options(today, offline, outFile, data, cache, skipLinks);
    }

    private static Options? Fail(TextWriter error)
    {
        error.WriteLine(Usage);
        return null;
    }

    private static string? FindRepoRoot()
    {
        foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
            for (var d = new DirectoryInfo(start); d is not null; d = d.Parent)
                if (File.Exists(Path.Combine(d.FullName, "EarthRelief3D.sln"))) return d.FullName;
        return null;
    }

    private static bool IsUnder(string path, string folder)
    {
        var f = Path.GetFullPath(folder).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        return Path.GetFullPath(path).StartsWith(f, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<int> CheckAsync(Options o, TextWriter output, TextWriter error, HttpMessageHandler? linkHandler, CancellationToken ct)
    {
        var root = FindRepoRoot();
        var data = o.Data ?? (root is null ? null : Path.Combine(root, "data"));
        if (data is null)
        {
            await error.WriteLineAsync("cannot find the repository (EarthRelief3D.sln); run from inside it or pass --data <dir>").ConfigureAwait(false);
            return 2;
        }
        var outFile = o.Out ?? Path.Combine(root ?? Directory.GetCurrentDirectory(), "artifacts", "vetting", $"vetting-report-{o.Today:yyyy-MM-dd}.md");
        if (IsUnder(outFile, data))
        {
            await error.WriteLineAsync($"the report may not be written under the data folder ({data}): the tool never edits it").ConfigureAwait(false);
            return 2;
        }
        var cache = o.Cache ?? Path.Combine(root ?? Directory.GetCurrentDirectory(), ".downloads", "irs");

        var tables = HelpTables.Load(data, "US");
        var charities = tables.Charities;
        var eins = charities.Select(c => Ein.Normalize(c.Ein)).Where(e => e is not null).Select(e => e!).ToHashSet(StringComparer.Ordinal);
        await output.WriteLineAsync($"{charities.Count} charities in {Path.Combine(data, "countries", "US", HelpTables.CharitiesFile)}").ConfigureAwait(false);

        using var irs = o.Offline is null ? new IrsFiles(cache, new VettingHttp(UserAgent), output) : new IrsFiles(o.Offline);
        var pub78Path = await irs.GetAsync(Pub78Name, new Uri(IrsBase, Pub78Name), ct).ConfigureAwait(false);
        var revocationPath = await irs.GetAsync(RevocationName, new Uri(IrsBase, RevocationName), ct).ConfigureAwait(false);

        IReadOnlyDictionary<string, Pub78Entry> pub78 = new Dictionary<string, Pub78Entry>();
        IReadOnlySet<string> revoked = new HashSet<string>();
        if (eins.Count > 0)
        {
            using (var s = File.OpenRead(pub78Path)) pub78 = Pub78Reader.Read(s, eins);
            using (var s = File.OpenRead(revocationPath)) revoked = RevocationReader.Read(s, eins);
        }

        // The latest return: this year's index first; last year's for a charity this year's has none of.
        var latest = new Dictionary<string, (Form990Ref Ref, int Year)>(StringComparer.Ordinal);
        var indexNotes = new List<string>();
        int[] years = [o.Today.Year, o.Today.Year - 1];
        foreach (var year in years)
        {
            if (year != years[0] && eins.All(latest.ContainsKey)) break;          // last year's index is only for charities this year's lacks
            var name = $"index_{year}.csv";
            var path = await irs.TryGetAsync(name, new Uri(IrsBase, $"990/xml/{year}/{name}"), ct).ConfigureAwait(false);
            if (path is null) { indexNotes.Add($"{name} not available"); continue; }
            indexNotes.Add(irs.Describe(name));
            var missing = eins.Where(e => !latest.ContainsKey(e)).ToHashSet(StringComparer.Ordinal);
            if (missing.Count == 0) continue;
            Dictionary<string, Form990Ref> found;
            using (var s = File.OpenRead(path)) found = Form990Index.LatestForAll(s, missing);
            foreach (var (ein, r) in found) latest[ein] = (r, year);
        }

        var checks = new List<CharityCheck>();
        foreach (var c in charities)
        {
            var ein = Ein.Normalize(c.Ein) ?? "";
            pub78.TryGetValue(ein, out var entry);
            Form990Ref? r = null;
            ProgrammeResult? programme = null;
            string? note = null;
            if (latest.TryGetValue(ein, out var hit))
            {
                r = hit.Ref;
                (programme, note) = await ProgrammeAsync(irs, hit.Ref, hit.Year, ct).ConfigureAwait(false);
            }
            else note = "No Form 990, 990-EZ or 990-PF for this EIN in the IRS e-file index";
            checks.Add(new CharityCheck(c, entry, revoked.Contains(ein), r, programme, note, []));
        }

        // Links.
        var statuses = new Dictionary<string, LinkStatus>(StringComparer.Ordinal);
        var refused = new HashSet<string>(StringComparer.Ordinal);      // links that lead to an evaluator: never requested
        if (!o.SkipLinks)
        {
            var urls = checks.SelectMany(k => LinksOf(k.Charity)).Select(l => l.Url).Where(u => !LinkChecker.IsEvaluatorHost(u))
                .DistinctBy(u => u.AbsoluteUri, StringComparer.Ordinal).ToList();
            using var checker = new LinkChecker(UserAgent, linkHandler);
            using var gate = new SemaphoreSlim(6);
            var results = await Task.WhenAll(urls.Select(async u =>
            {
                await gate.WaitAsync(ct).ConfigureAwait(false);
                try { return (u.AbsoluteUri, Status: (LinkStatus?)await checker.CheckAsync(u, ct).ConfigureAwait(false)); }
                catch (InvalidOperationException) { return (u.AbsoluteUri, Status: null); }
                finally { gate.Release(); }
            })).ConfigureAwait(false);
            foreach (var (uri, status) in results) { if (status is { } st) statuses[uri] = st; else refused.Add(uri); }
        }
        checks = [.. checks.Select(k => k with
        {
            Links = [.. LinksOf(k.Charity).Select(l => new LinkOutcome(l.What, l.Url,
                LinkChecker.IsEvaluatorHost(l.Url) ? null : statuses.TryGetValue(l.Url.AbsoluteUri, out var s) ? s : null))
                .Where(l => LinkChecker.IsEvaluatorHost(l.Url) || refused.Contains(l.Url.AbsoluteUri) || l.Status is not null)],
        })];

        var sources = new ReportSources(irs.Describe(Pub78Name), irs.Describe(RevocationName), string.Join("; ", indexNotes), irs.Describe(Pub78Name, forList: true));
        var expiring = VettingReport.Expiring(charities, o.Today);
        var report = VettingReport.Render(o.Today, checks, expiring, sources);

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outFile))!);
        await File.WriteAllTextAsync(outFile, report, new System.Text.UTF8Encoding(false), ct).ConfigureAwait(false);
        await output.WriteLineAsync($"{checks.Count} charities checked; {expiring.Count} expiring; {checks.Sum(k => k.Links.Count(l => l.Status == LinkStatus.Broken))} broken links.").ConfigureAwait(false);
        await output.WriteLineAsync($"report: {Path.GetFullPath(outFile)}").ConfigureAwait(false);
        return 0;
    }

    /// <summary>Every link in a charity's entry: its site, donate page, the evidence it works somewhere, and the evaluators' pages.</summary>
    private static IEnumerable<(string What, Uri Url)> LinksOf(Charity c)
    {
        yield return ("Website", c.Website);
        yield return ("Donate page", c.DonateUrl);
        foreach (var w in c.Countries ?? []) yield return ($"Evidence for {w.Iso3} ({w.EvidenceTitle})", w.Evidence);
        foreach (var r in c.Ratings ?? []) yield return ($"{r.Evaluator} report", r.ReportUrl);
        foreach (var p in c.ProfileLinks ?? []) yield return ("Evaluator profile", p);
    }

    private static async Task<(ProgrammeResult?, string?)> ProgrammeAsync(IrsFiles irs, Form990Ref r, int indexYear, CancellationToken ct)
    {
        if (r.ReturnType != "990")
            return (null, $"The latest return is a Form {r.ReturnType}, which has no Part IX line 25 (only the full Form 990 has)");
        try
        {
            var xml = await irs.ReadReturnAsync(r, indexYear, ct).ConfigureAwait(false);
            if (xml is null) return (null, $"The return's zip ({r.BatchId}.zip) is not in the offline folder");
            var doc = Form990Reader.Parse(xml);
            if (Form990Reader.ProgrammeShare(doc) is not { } p) return (null, "The return has no Part IX line 25 figures (total functional expenses and programme services)");
            return (new ProgrammeResult((double)p.Programme / p.Total, p.Programme, p.Total, p.TaxYear, Form990Reader.TaxPeriodBegin(doc) ?? "", Form990Reader.TaxPeriodEnd(doc) ?? ""), null);
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or HttpRequestException or System.Xml.XmlException or FileNotFoundException or TimeoutException or TaskCanceledException)
        {
            return (null, $"The return could not be read: {ex.Message}");
        }
    }
}

/// <summary>
/// The IRS files: from a folder (<c>--offline</c>) or downloaded into the cache folder (kept for a day). Offline or online, the
/// file names are the IRS's own.
/// </summary>
internal sealed class IrsFiles : IDisposable
{
    private readonly string _folder;
    private readonly VettingHttp? _http;
    private readonly TextWriter? _log;
    private readonly Dictionary<string, string> _described = new(StringComparer.Ordinal);

    public IrsFiles(string offlineFolder) => _folder = offlineFolder;

    public IrsFiles(string cacheFolder, VettingHttp http, TextWriter log)
    {
        _folder = cacheFolder;
        _http = http;
        _log = log;
    }

    public void Dispose() => _http?.Dispose();

    public async Task<string> GetAsync(string name, Uri url, CancellationToken ct) =>
        await TryGetAsync(name, url, ct).ConfigureAwait(false) ?? throw new FileNotFoundException(_http is null ? $"{Path.Combine(_folder, name)} is missing (--offline)" : $"{url} was not found");

    /// <summary>The file's path; null when it does not exist (the next year's index, early in January).</summary>
    public async Task<string?> TryGetAsync(string name, Uri url, CancellationToken ct)
    {
        var path = Path.Combine(_folder, name);
        if (_http is null)
        {
            if (!File.Exists(path)) return null;
            _described[name] = $"{name}, file dated {File.GetLastWriteTime(path):yyyy-MM-dd}";
            return path;
        }
        var stamp = path + ".lastmodified";
        if (!(File.Exists(path) && DateTime.Now - File.GetLastWriteTime(path) < TimeSpan.FromHours(24)))
        {
            await _log!.WriteLineAsync($"downloading {url} ...").ConfigureAwait(false);
            try
            {
                var modified = await _http.DownloadAsync(url, path, ct).ConfigureAwait(false);
                if (modified is { } m) await File.WriteAllTextAsync(stamp, m.UtcDateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), ct).ConfigureAwait(false);
                else if (File.Exists(stamp)) File.Delete(stamp);
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
        }
        var irsDate = File.Exists(stamp) ? File.ReadAllText(stamp).Trim() : null;
        _described[name] = $"{name}, {new FileInfo(path).Length / (1024.0 * 1024):0.#} MB, IRS file dated {irsDate ?? "unknown"}, downloaded {File.GetLastWriteTime(path):yyyy-MM-dd}";
        return path;
    }

    /// <summary>How the report names a file; <paramref name="forList"/> gives the short form for <c>legal.pub78File</c>.</summary>
    public string Describe(string name, bool forList = false)
    {
        if (!_described.TryGetValue(name, out var d)) return name;
        if (!forList) return d;
        var date = d.Contains("IRS file dated ", StringComparison.Ordinal) ? d[(d.IndexOf("IRS file dated ", StringComparison.Ordinal) + 15)..].Split(',')[0] : null;
        return date is null ? name : $"{name} (IRS file of {date})";
    }

    /// <summary>A return's XML: from the batch zip in the offline folder, or by range requests on the IRS server. Null when the offline zip is not there.</summary>
    public async Task<byte[]?> ReadReturnAsync(Form990Ref r, int indexYear, CancellationToken ct)
    {
        var entry = $"{r.ObjectId}_public.xml";
        if (_http is null)
        {
            var zip = Path.Combine(_folder, r.BatchId + ".zip");
            return File.Exists(zip) ? ZipRangeReader.ReadEntryFromFile(zip, entry) : null;
        }
        var url = new Uri($"https://apps.irs.gov/pub/epostcard/990/xml/{indexYear}/{r.BatchId}.zip");
        return await ZipRangeReader.ReadEntryAsync(_http, url, entry, ct, Path.Combine(_folder, "zips")).ConfigureAwait(false);
    }
}



