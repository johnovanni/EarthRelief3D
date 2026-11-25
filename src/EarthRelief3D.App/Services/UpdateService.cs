using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using EarthRelief3D.App.Settings;
using EarthRelief3D.App.ViewModels;
using EarthRelief3D.Core.Updates;
using GlobeCore.Logging;
using GlobeCore.Storage;
using GlobeCore.Time;

namespace EarthRelief3D.App.Services;

/// <summary>What the About card shows of the update check, and its "Check now".</summary>
public interface IUpdateStatus
{
    /// <summary>"Checked for a new version on 25 Nov 2026: you have the newest (1.0.0)", and the like.</summary>
    string StatusText { get; }
    event EventHandler? StatusChanged;
    Task CheckNowAsync();
}

/// <summary>
/// EarthRelief3D's own update check (P5-18 to P5-22). <see cref="Start"/> when the window first shows; then a timer calls
/// <see cref="TickAsync"/> often, and it asks GitHub for the newest release <see cref="FirstCheckDelay"/> after the start and every
/// <see cref="Interval"/> after that, with one request, while "Check for a new version once a day" is on. A newer release shows the
/// banner's offer; nothing is downloaded until the user chooses "Download and install" (<see cref="UpdateDownloader"/> checks the
/// file); "Install now" checks it once more, starts it (<see cref="InstallerArguments"/>) and, only once Windows has started it,
/// closes the app normally. A failed check is logged and shown in About; the next is a day later. When no check has succeeded for
/// <see cref="StaleAfter"/> the banner says the included data may be out of date. Never created in a diagnostic or offline run.
/// Runs on the UI thread (the timer is the dispatcher's).
/// </summary>
public sealed class UpdateService : IUpdateStatus, IDisposable
{
    public const string InstallerArguments = "/SILENT /SUPPRESSMSGBOXES /NORESTART /CLOSEAPPLICATIONS";
    public const string OffText = "Checking for new versions is off (Settings)";
    public const string NotYetText = "Not checked for a new version yet.";
    public static readonly TimeSpan FirstCheckDelay = TimeSpan.FromSeconds(30);
    public static readonly TimeSpan Interval = TimeSpan.FromHours(24);
    public static readonly TimeSpan StaleAfter = TimeSpan.FromDays(30);
    /// <summary>GitHub's answer for one release is a few kilobytes; more than this is not read.</summary>
    public const int MaxAnswerBytes = 1_048_576;

    private readonly Func<CancellationToken, Task<string>> _fetch;
    private readonly UpdateDownloader _downloader;
    private readonly IInstallerLauncher _launcher;
    private readonly ISettingsStore<AppSettings> _settings;
    private readonly IClock _clock;
    private readonly IAppLog _log;
    private readonly Version _running;
    private readonly string _runningExePath;
    private readonly Action _shutdown;
    private readonly CancellationTokenSource _life = new();

    private DateTime? _nextCheckUtc;
    private bool _checking;
    private string? _result;                // this session's last check, as About says it
    private ReleaseInfo? _latest;           // the newer release found, if any
    private Version? _laterFor;             // "Later" hid the offer of this version (or older) until the next start
    private bool _staleLater;               // "Later" hid the stale banner until the next start
    private CancellationTokenSource? _download;
    private string? _setupPath, _setupSha;  // the checked installer, while the banner says "Ready"

    public UpdateService(Func<CancellationToken, Task<string>> fetchLatestJson, UpdateDownloader downloader, IInstallerLauncher launcher,
        ISettingsStore<AppSettings> settings, IClock clock, IAppLog log, Version running, string runningExePath, Action shutdown,
        UpdateBannerViewModel? banner = null)
    {
        _fetch = fetchLatestJson ?? throw new ArgumentNullException(nameof(fetchLatestJson));
        _downloader = downloader ?? throw new ArgumentNullException(nameof(downloader));
        _launcher = launcher ?? throw new ArgumentNullException(nameof(launcher));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _log = log ?? throw new ArgumentNullException(nameof(log));
        _running = UpdateCheck.Normalise(running ?? throw new ArgumentNullException(nameof(running)));
        _runningExePath = runningExePath ?? "";
        _shutdown = shutdown ?? throw new ArgumentNullException(nameof(shutdown));
        Banner = banner ?? new UpdateBannerViewModel(new GlobeCore.Wpf.Services.ShellLinkOpener(), _running);
        Banner.DownloadRequested = DownloadAndInstallAsync;
        Banner.LaterRequested = Later;
        Banner.CancelRequested = CancelDownload;
        Banner.InstallRequested = InstallNow;
        Banner.CheckNowRequested = CheckNowAsync;
    }

    public UpdateBannerViewModel Banner { get; }

    public event EventHandler? StatusChanged;

    /// <summary>The About card's line about the update check.</summary>
    public string StatusText => _result ?? (_settings.Load().CheckForUpdates ? NotYetText : OffText);

    /// <summary>
    /// The real request: one GET of <see cref="UpdateCheck.LatestUrl"/> with the app's User-Agent and GitHub's JSON media type, at most
    /// 30 seconds and <see cref="MaxAnswerBytes"/>. Any answer but 200 throws <see cref="HttpRequestException"/> with its status code
    /// (a 403 or 429 is GitHub's limit on requests, and counts as a failed check).
    /// </summary>
    public static Func<CancellationToken, Task<string>> GitHubFetch(HttpMessageHandler handler)
    {
        var http = new HttpClient(handler, disposeHandler: false) { Timeout = TimeSpan.FromSeconds(30) };
        return async ct =>
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, UpdateCheck.LatestUrl);
            request.Headers.UserAgent.ParseAdd(AppInfo.UserAgent);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
            using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"GitHub answered HTTP {(int)response.StatusCode}.", null, response.StatusCode);
            await using var body = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            var buffer = new byte[MaxAnswerBytes + 1];
            int read = 0;
            while (read < buffer.Length && await body.ReadAsync(buffer.AsMemory(read), ct).ConfigureAwait(false) is var n && n > 0) read += n;
            if (read > MaxAnswerBytes) throw new FormatException("GitHub's answer is too long.");
            return Encoding.UTF8.GetString(buffer, 0, read);
        };
    }

    /// <summary>The sentence About shows for a failed check.</summary>
    public static string ReasonFor(Exception ex) => ex switch
    {
        HttpRequestException { StatusCode: HttpStatusCode.Forbidden or HttpStatusCode.TooManyRequests } h =>
            $"GitHub turned the request away (HTTP {(int)h.StatusCode!}).",
        HttpRequestException { StatusCode: { } code } => $"GitHub answered HTTP {(int)code}.",
        HttpRequestException h => "GitHub could not be reached: " + h.Message.Trim().TrimEnd('.') + ".",
        OperationCanceledException => "GitHub did not answer within 30 seconds.",
        FormatException => "GitHub's answer could not be read.",
        _ => ex.Message.Trim().TrimEnd('.') + ".",
    };

    /// <summary>When the window first shows: deletes older update folders, and plans the first check <see cref="FirstCheckDelay"/> later.</summary>
    public void Start()
    {
        try { _downloader.DeleteOldFolders(_running); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { _log.Warn("Updates: could not delete older update folders", ex); }
        _nextCheckUtc = _clock.UtcNow + FirstCheckDelay;
        if (!_settings.Load().CheckForUpdates) ShowStaleIfDue();
    }

    /// <summary>Called by the timer: checks when one is due (and only then).</summary>
    public async Task TickAsync()
    {
        if (_nextCheckUtc is not { } due || _clock.UtcNow < due || _checking) return;
        _nextCheckUtc = _clock.UtcNow + Interval;
        await CheckAsync(manual: false);
    }

    /// <summary>"Check now" (About, or the stale banner): asks at once, even when the daily check is off.</summary>
    public Task CheckNowAsync() => _checking ? Task.CompletedTask : CheckAsync(manual: true);

    /// <summary>The Settings choice changed: About's line, and the stale banner, follow.</summary>
    public void SettingsChanged()
    {
        if (_settings.Load().CheckForUpdates && Banner.State == UpdateBannerState.Stale) Banner.Hide();
        else ShowStaleIfDue();
        StatusChanged?.Invoke(this, EventArgs.Empty);
    }

    private async Task CheckAsync(bool manual)
    {
        var settings = _settings.Load();
        if (!manual && !settings.CheckForUpdates)
        {
            ShowStaleIfDue();
            return;
        }
        _checking = true;
        var now = _clock.UtcNow;
        try
        {
            if (settings.FirstUpdateCheckUtc is null) _settings.Save(_settings.Load() with { FirstUpdateCheckUtc = now });
            ReleaseInfo r;
            try
            {
                r = UpdateCheck.Parse(await _fetch(_life.Token));
                if (!UpdateCheck.HasVersionTag(r)) throw new FormatException(UpdateCheck.RefuseReason(r));
            }
            catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or FormatException or IOException)
            {
                if (_life.IsCancellationRequested) return;
                var reason = ex is FormatException { Message: { } m } && m.StartsWith("The release's tag", StringComparison.Ordinal) ? m : ReasonFor(ex);
                _log.Warn("Updates: the update check failed: " + reason, ex);
                _result = $"Could not check for a new version on {DateText(now)}: {reason}";
                ShowStaleIfDue();
                return;
            }
            _settings.Save(_settings.Load() with { LastUpdateCheckOkUtc = now });
            bool newer = UpdateCheck.IsNewer(r, _running);
            _log.Info($"Updates: the update check found {r.Tag}" + (newer ? " (newer)" : ""));
            _result = newer
                ? $"Checked for a new version on {DateText(now)}: {UpdateBannerViewModel.VersionText(r.Version)} is available"
                : $"Checked for a new version on {DateText(now)}: you have the newest ({UpdateBannerViewModel.VersionText(_running)})";
            if (Banner.State == UpdateBannerState.Stale) Banner.Hide();
            if (!newer)
            {
                if (Banner.State == UpdateBannerState.Offer) Banner.Hide();
                return;
            }
            bool laterApplies = _laterFor is { } later && UpdateCheck.Normalise(r.Version) <= later;
            bool busy = Banner.State is UpdateBannerState.Downloading or UpdateBannerState.Ready;
            if (!busy && !laterApplies && (Banner.State != UpdateBannerState.Failed || _latest is null || UpdateCheck.Normalise(r.Version) > UpdateCheck.Normalise(_latest.Version)))
                Banner.ShowOffer(r);
            if (!busy) _latest = r;
        }
        finally
        {
            _checking = false;
            StatusChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private string DateText(DateTime utc) => UpdateCheck.DateText(utc.ToLocalTime());

    /// <summary>
    /// P5-22: no check has succeeded for <see cref="StaleAfter"/>, counted from the last success, or for a copy that never succeeded
    /// from its first attempt, or with checking off from the day it was turned off (or a later success of "Check now").
    /// </summary>
    private bool IsStale(AppSettings s)
    {
        DateTime? since = s.CheckForUpdates
            ? s.LastUpdateCheckOkUtc ?? s.FirstUpdateCheckUtc
            : Max(s.UpdateChecksOffUtc, s.LastUpdateCheckOkUtc) ?? s.FirstUpdateCheckUtc;
        return since is { } t && _clock.UtcNow - t >= StaleAfter;
    }

    private static DateTime? Max(DateTime? a, DateTime? b) => a is null ? b : b is null ? a : a > b ? a : b;

    private void ShowStaleIfDue()
    {
        if (_staleLater || Banner.State != UpdateBannerState.Hidden || !IsStale(_settings.Load())) return;
        Banner.ShowStale();
    }

    /// <summary>"Download and install": downloads and checks the installer, then offers to install it. Only from the offer.</summary>
    internal async Task DownloadAndInstallAsync()
    {
        if (Banner.State != UpdateBannerState.Offer || Banner.Release is not { } r) return;
        _download?.Dispose();
        var cts = _download = CancellationTokenSource.CreateLinkedTokenSource(_life.Token);
        Banner.ShowDownloading(r, 0);
        _log.Info($"Updates: downloading {r.Tag}");
        var progress = new Progress<long>(Banner.ReportProgress);
        DownloadResult result;
        try { result = await _downloader.DownloadAsync(r, _runningExePath, progress, cts.Token); }
        finally { if (ReferenceEquals(_download, cts)) _download = null; cts.Dispose(); }
        if (_life.IsCancellationRequested) return;
        if (result.Cancelled)
        {
            _log.Info("Updates: the download was cancelled");
            Banner.ShowOffer(r);
        }
        else if (result.SetupPath is { } path && result.Sha256 is { } sha)
        {
            _setupPath = path;
            _setupSha = sha;
            _log.Info($"Updates: {r.Tag} downloaded and checked: {path}");
            Banner.ShowReady(r);
        }
        else
        {
            _log.Warn("Updates: the update could not be used: " + result.Failure);
            Banner.ShowFailed(r, result.Failure ?? "The download failed.");
        }
    }

    private void CancelDownload() => _download?.Cancel();

    /// <summary>"Install now": checks the file once more, starts it, and closes the app only once Windows has started it.</summary>
    internal void InstallNow()
    {
        if (Banner.State != UpdateBannerState.Ready || Banner.Release is not { } r || _setupPath is not { } path || _setupSha is not { } sha) return;
        _setupPath = _setupSha = null;
        if (_downloader.Verify(path, r, sha, _runningExePath) is { } failure)
        {
            _log.Warn("Updates: the installer failed its check before it was run: " + failure);
            Banner.ShowFailed(r, failure);
            return;
        }
        if (!_launcher.TryStart(path, InstallerArguments, out var error))
        {
            _log.Warn("Updates: the installer was not started: " + error);
            DeleteQuietly(path);
            Banner.ShowFailed(r, error ?? ShellInstallerLauncher.PermissionRefusedText);
            return;
        }
        _log.Info($"Updates: the installer for {r.Tag} has started; EarthRelief3D closes");
        _shutdown();
    }

    /// <summary>"Later": hides the banner until the next start, or until a still newer version is found.</summary>
    private void Later()
    {
        switch (Banner.State)
        {
            case UpdateBannerState.Stale:
                _staleLater = true;
                break;
            case UpdateBannerState.Offer or UpdateBannerState.Ready or UpdateBannerState.Failed when Banner.Release is { } r:
                _laterFor = UpdateCheck.Normalise(r.Version);
                if (_setupPath is { } p) DeleteQuietly(p);
                _setupPath = _setupSha = null;
                break;
        }
        Banner.Hide();
    }

    private static void DeleteQuietly(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
    }

    /// <summary>At exit: stops a download (its part file is deleted) and any check in flight.</summary>
    public void Dispose()
    {
        // Not disposed: a check or download still in flight reads it once more as it ends.
        _life.Cancel();
        _download?.Cancel();
    }
}
