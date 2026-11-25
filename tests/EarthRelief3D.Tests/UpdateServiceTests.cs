using System.IO;
using System.Net;
using System.Net.Http;
using EarthRelief3D.App.Services;
using EarthRelief3D.App.Settings;
using EarthRelief3D.App.ViewModels;
using EarthRelief3D.Core.Updates;
using GlobeCore.Wpf.Services;
using static EarthRelief3D.Tests.UpdateFakes;

namespace EarthRelief3D.Tests;

/// <summary>
/// Plan 5 Task 10 (P5-18, P5-19, P5-21, P5-22): when the app checks, what the banner and About say, "Later", staleness, and
/// installing (the installer is started, then the app closes; a refused permission keeps it open). A settable clock and fakes
/// stand in for time, GitHub, the signature check and Windows.
/// </summary>
public sealed class UpdateServiceTests : IDisposable
{
    private static readonly DateTime Start = new(2026, 11, 25, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Version Running = new(1, 0, 0);

    private readonly string _dir = TempDir();
    private readonly FixedClock _clock = new(Start);
    private readonly ListLog _log = new();
    private readonly FakeReleaseHandler _http = new();
    private readonly FakeSignatures _signatures = new();
    private readonly List<string> _order = [];
    private readonly RecordingOpener _opener = new();
    private int _fetches;
    private Func<string> _answer = () => Samples.Text("github_release_latest_newer.json");

    private sealed class RecordingOpener : ILinkOpener
    {
        public List<string> Opened { get; } = [];
        public void Open(string target) => Opened.Add(target);
    }

    public void Dispose()
    {
        try { Directory.Delete(_dir, recursive: true); } catch (IOException) { }
    }

    private Task<string> Fetch(CancellationToken ct)
    {
        _fetches++;
        return Task.FromResult(_answer());
    }

    private (UpdateService Service, MemorySettingsStore Settings, FakeLauncher Launcher) Make(MemorySettingsStore? settings = null,
        Func<CancellationToken, Task<string>>? fetch = null)
    {
        settings ??= new MemorySettingsStore();
        var launcher = new FakeLauncher(_order);
        var banner = new UpdateBannerViewModel(_opener, Running);
        var svc = new UpdateService(fetch ?? Fetch, new UpdateDownloader(_http, _signatures, _dir), launcher, settings, _clock, _log, Running, AppExe, banner);
        return (svc, settings, launcher);
    }

    private async Task Advance(UpdateService svc, TimeSpan by)
    {
        _clock.UtcNow += by;
        await svc.TickAsync();
    }

    [Fact]
    public async Task FirstCheckThirtySecondsAfterStart()
    {
        var (svc, _, _) = Make();
        svc.Start();
        Assert.Equal(0, _fetches);
        await Advance(svc, TimeSpan.FromSeconds(29));
        Assert.Equal(0, _fetches);
        Assert.Equal(UpdateBannerState.Hidden, svc.Banner.State);
        await Advance(svc, TimeSpan.FromSeconds(1));
        Assert.Equal(1, _fetches);
        Assert.Equal(UpdateService.FirstCheckDelay, TimeSpan.FromSeconds(30));
        // P5-19's offer.
        Assert.Equal(UpdateBannerState.Offer, svc.Banner.State);
        Assert.Equal("A new version of EarthRelief3D is available: 1.1.0 (released 3 Feb 2027). You have 1.0.0. New versions bring updated data and fixes.",
            svc.Banner.Text);
        Assert.True(svc.Banner.ShowDownload);
        Assert.True(svc.Banner.ShowWhatsNew);
        Assert.True(svc.Banner.ShowLater);
        Assert.False(svc.Banner.ShowOpenDownloadPage);
        Assert.Equal("Checked for a new version on 25 Nov 2026: 1.1.0 is available", svc.StatusText);
        // Nothing is downloaded before the user says so.
        Assert.Empty(_http.Requests);
        // "What's new" opens the release page.
        svc.Banner.WhatsNewCommand.Execute(null);
        Assert.Equal(["https://github.com/johnovanni/EarthRelief3D/releases/tag/v1.1.0"], _opener.Opened);
    }

    [Fact]
    public async Task ChecksDaily()
    {
        var (svc, _, _) = Make();
        svc.Start();
        await Advance(svc, TimeSpan.FromSeconds(30));
        Assert.Equal(1, _fetches);
        await Advance(svc, TimeSpan.FromHours(23));
        Assert.Equal(1, _fetches);
        await Advance(svc, TimeSpan.FromHours(1));
        Assert.Equal(2, _fetches);
        Assert.Equal(UpdateService.Interval, TimeSpan.FromHours(24));
        await Advance(svc, TimeSpan.FromHours(24));
        Assert.Equal(3, _fetches);
    }

    [Fact]
    public async Task OffInSettingsMeansNoRequest()
    {
        var (svc, settings, _) = Make(new MemorySettingsStore(new AppSettings { CheckForUpdates = false, UpdateChecksOffUtc = Start }));
        svc.Start();
        await Advance(svc, TimeSpan.FromSeconds(30));
        await Advance(svc, TimeSpan.FromDays(1));
        Assert.Equal(0, _fetches);
        Assert.Equal(UpdateBannerState.Hidden, svc.Banner.State);
        Assert.Equal("Checking for new versions is off (Settings)", svc.StatusText);
        Assert.Null(settings.Current.FirstUpdateCheckUtc);
        // "Check now" in About still asks: the user asked.
        await svc.CheckNowAsync();
        Assert.Equal(1, _fetches);
        Assert.Equal("Checked for a new version on 26 Nov 2026: 1.1.0 is available", svc.StatusText);
    }

    [Fact]
    public async Task SameVersionShowsNothing()
    {
        _answer = () => Samples.Text("github_release_latest_same.json");
        var (svc, _, _) = Make();
        svc.Start();
        await Advance(svc, TimeSpan.FromSeconds(30));
        Assert.Equal(1, _fetches);
        Assert.Equal(UpdateBannerState.Hidden, svc.Banner.State);
        Assert.False(svc.Banner.IsVisible);
        Assert.Equal("Checked for a new version on 25 Nov 2026: you have the newest (1.0.0)", svc.StatusText);
    }

    [Fact]
    public async Task LaterHidesUntilNextStart()
    {
        var settings = new MemorySettingsStore();
        var (svc, _, _) = Make(settings);
        svc.Start();
        await Advance(svc, TimeSpan.FromSeconds(30));
        Assert.Equal(UpdateBannerState.Offer, svc.Banner.State);
        svc.Banner.LaterCommand.Execute(null);
        Assert.Equal(UpdateBannerState.Hidden, svc.Banner.State);
        await Advance(svc, TimeSpan.FromDays(1));
        Assert.Equal(2, _fetches);
        Assert.Equal(UpdateBannerState.Hidden, svc.Banner.State);   // the same version: still hidden
        Assert.Equal("Checked for a new version on 26 Nov 2026: 1.1.0 is available", svc.StatusText);

        // The next start: it comes back while the update waits.
        var (next, _, _) = Make(settings);
        next.Start();
        await Advance(next, TimeSpan.FromSeconds(30));
        Assert.Equal(UpdateBannerState.Offer, next.Banner.State);
    }

    [Fact]
    public async Task LaterThenNewerVersionShowsAgain()
    {
        var (svc, _, _) = Make();
        svc.Start();
        await Advance(svc, TimeSpan.FromSeconds(30));
        svc.Banner.LaterCommand.Execute(null);
        _answer = () => Samples.Text("github_release_latest_newer.json").Replace("v1.1.0", "v1.2.0", StringComparison.Ordinal);
        await Advance(svc, TimeSpan.FromDays(1));
        Assert.Equal(UpdateBannerState.Offer, svc.Banner.State);
        Assert.StartsWith("A new version of EarthRelief3D is available: 1.2.0 (released 3 Feb 2027).", svc.Banner.Text);
    }

    [Fact]
    public async Task FailureIsShownInAboutAndRetriedNextDay()
    {
        // GitHub's rate limit (403) through the real fetch, over a fake handler.
        _http.Status = HttpStatusCode.Forbidden;
        var fetch = UpdateService.GitHubFetch(_http);
        int calls = 0;
        var (svc, settings, _) = Make(fetch: ct => { calls++; return fetch(ct); });
        svc.Start();
        await Advance(svc, TimeSpan.FromSeconds(30));
        Assert.Equal(1, calls);
        var request = Assert.Single(_http.Requests);
        Assert.Equal(UpdateCheck.LatestUrl, request.RequestUri!.AbsoluteUri);
        Assert.Contains("EarthRelief3D/", request.Headers.UserAgent.ToString());
        Assert.Equal("application/vnd.github+json", Assert.Single(request.Headers.Accept).MediaType);
        Assert.Equal("Could not check for a new version on 25 Nov 2026: GitHub turned the request away (HTTP 403).", svc.StatusText);
        Assert.Equal(UpdateBannerState.Hidden, svc.Banner.State);   // no stale banner on the first day
        Assert.Null(settings.Current.LastUpdateCheckOkUtc);
        Assert.Equal(Start.AddSeconds(30), settings.Current.FirstUpdateCheckUtc);
        Assert.Contains(_log.Lines, l => l.Contains("update check", StringComparison.OrdinalIgnoreCase));

        await Advance(svc, TimeSpan.FromHours(12));
        Assert.Equal(1, calls);
        _http.Status = null;
        _http.Files[UpdateCheck.LatestUrl] = System.Text.Encoding.UTF8.GetBytes(Samples.Text("github_release_latest_newer.json"));
        await Advance(svc, TimeSpan.FromHours(12));
        Assert.Equal(2, calls);
        Assert.Equal("Checked for a new version on 26 Nov 2026: 1.1.0 is available", svc.StatusText);
        Assert.Equal(UpdateBannerState.Offer, svc.Banner.State);

        // An answer that cannot be read is a failure too (429 likewise counts as GitHub's limit).
        _answer = () => "{\"message\":\"Not Found\"}";
        var (svc2, _, _) = Make();
        await svc2.CheckNowAsync();
        Assert.Equal("Could not check for a new version on 26 Nov 2026: GitHub's answer could not be read.", svc2.StatusText);
        Assert.Equal("GitHub turned the request away (HTTP 429).",
            UpdateService.ReasonFor(new HttpRequestException("x", null, HttpStatusCode.TooManyRequests)));
    }

    [Fact]
    public async Task StaleAfterThirtyDaysWithoutACheck()
    {
        Task<string> Failing(CancellationToken ct) => throw new HttpRequestException("No such host is known.");

        async Task<UpdateBannerState> After(AppSettings s, bool fetchFails = true)
        {
            var (svc, _, _) = Make(new MemorySettingsStore(s), fetchFails ? Failing : null);
            svc.Start();
            await Advance(svc, TimeSpan.FromSeconds(30));
            _clock.UtcNow = Start;
            return svc.Banner.State;
        }

        Assert.Equal(UpdateBannerState.Stale, await After(new AppSettings { LastUpdateCheckOkUtc = Start.AddDays(-31) }));
        Assert.Equal(UpdateBannerState.Hidden, await After(new AppSettings { LastUpdateCheckOkUtc = Start.AddDays(-29) }));
        Assert.Equal(UpdateBannerState.Stale, await After(new AppSettings { FirstUpdateCheckUtc = Start.AddDays(-31) }));
        Assert.Equal(UpdateBannerState.Stale, await After(new AppSettings { CheckForUpdates = false, UpdateChecksOffUtc = Start.AddDays(-31), LastUpdateCheckOkUtc = Start.AddDays(-40) }));
        Assert.Equal(UpdateBannerState.Hidden, await After(new AppSettings { CheckForUpdates = false, UpdateChecksOffUtc = Start.AddDays(-2), LastUpdateCheckOkUtc = Start.AddDays(-40) }));

        var (svc, _, _) = Make(new MemorySettingsStore(new AppSettings { LastUpdateCheckOkUtc = Start.AddDays(-31) }), Failing);
        svc.Start();
        await Advance(svc, TimeSpan.FromSeconds(30));
        Assert.Equal("EarthRelief3D has not been able to check for a new version for 30 days. Its included data may be out of date: Data shows each source's date.",
            svc.Banner.Text);
        Assert.True(svc.Banner.ShowCheckNow);
        Assert.True(svc.Banner.ShowLater);
        Assert.False(svc.Banner.ShowDownload);
        Assert.Equal(UpdateService.StaleAfter, TimeSpan.FromDays(30));
        svc.Banner.LaterCommand.Execute(null);
        Assert.Equal(UpdateBannerState.Hidden, svc.Banner.State);
    }

    [Fact]
    public async Task SuccessfulCheckSavesTheTime()
    {
        var settings = new MemorySettingsStore(new AppSettings { TextScale = 1.5 });
        var (svc, _, _) = Make(settings);
        svc.Start();
        await Advance(svc, TimeSpan.FromSeconds(30));
        Assert.Equal(Start.AddSeconds(30), settings.Current.LastUpdateCheckOkUtc);
        Assert.Equal(Start.AddSeconds(30), settings.Current.FirstUpdateCheckUtc);
        Assert.Equal(1.5, settings.Current.TextScale);   // other settings kept
        await Advance(svc, TimeSpan.FromDays(1));
        Assert.Equal(Start.AddSeconds(30).AddDays(1), settings.Current.LastUpdateCheckOkUtc);
        Assert.Equal(Start.AddSeconds(30), settings.Current.FirstUpdateCheckUtc);
    }

    /// <summary>The service with an offer for 1.1.0 whose installer is <paramref name="bytes"/>, served through a fake GitHub.</summary>
    private async Task<(UpdateService Service, FakeLauncher Launcher)> Offered(byte[] bytes)
    {
        var release = Release(bytes);
        _answer = () => Samples.Text("github_release_latest_newer.json")
            .Replace("55574528", bytes.Length.ToString(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("fe47cc5ecd60be7bb4c160857d48f1c865f493839680388927d9e7d286692599", Sha(bytes), StringComparison.Ordinal);
        _http.Files[release.Setup!.DownloadUrl.AbsoluteUri] = bytes;
        var (svc, _, launcher) = Make();
        svc.Start();
        await Advance(svc, TimeSpan.FromSeconds(30));
        Assert.Equal(UpdateBannerState.Offer, svc.Banner.State);
        return (svc, launcher);
    }

    /// <summary>
    /// R11: "Install now" starts the installer with the silent arguments, as an ordinary shell start (no "runas": the installer asks
    /// Windows itself, so it restarts the app as the user). The app does not close itself: the banner stays, without buttons.
    /// </summary>
    [Fact]
    public async Task InstallStartsTheInstallerWithoutRunas()
    {
        var bytes = Payload();
        var (svc, launcher) = await Offered(bytes);
        await svc.DownloadAndInstallAsync();
        Assert.Equal(UpdateBannerState.Ready, svc.Banner.State);
        Assert.Equal("Ready to install 1.1.0. EarthRelief3D will close, the installer will run (Windows will ask for your permission), and EarthRelief3D will start again.",
            svc.Banner.Text);
        Assert.True(svc.Banner.ShowInstallNow);
        Assert.True(svc.Banner.ShowLater);
        Assert.Empty(_order);

        svc.Banner.InstallNowCommand.Execute(null);
        var (path, args) = Assert.Single(launcher.Started);
        Assert.Equal(Path.Combine(_dir, "1.1.0", UpdateCheck.SetupName), path);
        Assert.Equal("/SILENT /SUPPRESSMSGBOXES /NORESTART /CLOSEAPPLICATIONS", args);
        Assert.Equal(UpdateService.InstallerArguments, args);
        Assert.True(launcher.FileExistedWhenStarted);
        Assert.Equal(["launch"], _order);   // nothing else: the app does not shut itself down
        // While the installer runs, the Ready text stays (it is still true) and its buttons go.
        Assert.Equal(UpdateBannerState.Ready, svc.Banner.State);
        Assert.True(svc.Banner.IsInstalling);
        Assert.False(svc.Banner.ShowInstallNow);
        Assert.False(svc.Banner.ShowLater);
        svc.Banner.InstallNowCommand.Execute(null);
        Assert.Single(launcher.Started);   // not started twice

        // The real launcher: the shell, without the runas verb.
        var start = ShellInstallerLauncher.StartInfo(path, args);
        Assert.True(start.UseShellExecute);
        Assert.Equal("", start.Verb);
        Assert.Equal(path, start.FileName);
        Assert.Equal(args, start.Arguments);
        Assert.Equal(Path.GetDirectoryName(path), start.WorkingDirectory);

        // The installer closes the app (it is being shut down) and then ends: the banner says nothing more.
        svc.Dispose();
        launcher.Exit.SetResult(0);
        await Task.Delay(100);
        Assert.Equal(UpdateBannerState.Ready, svc.Banner.State);
        Assert.DoesNotContain(_log.Lines, l => l.Contains("was not installed", StringComparison.Ordinal));
    }

    /// <summary>R11: an installer that ends while the app is still open did not install; the app says so, with the exit code, and the file goes.</summary>
    [Fact]
    public async Task InstallerThatExitsLeavesTheAppOpenAndSaysSo()
    {
        var bytes = Payload();
        var (svc, launcher) = await Offered(bytes);
        await svc.DownloadAndInstallAsync();
        svc.Banner.InstallNowCommand.Execute(null);
        Assert.True(svc.Banner.IsInstalling);
        launcher.Exit.SetResult(5);
        for (int i = 0; i < 200 && svc.Banner.State != UpdateBannerState.Failed; i++) await Task.Delay(10);   // the watch resumes on the test's context
        Assert.Equal(UpdateBannerState.Failed, svc.Banner.State);
        Assert.Equal("The update was not installed: the installer stopped (exit code 5). If Windows asked for your permission and it was not given, that is why.",
            svc.Banner.Text);
        Assert.False(svc.Banner.IsInstalling);
        Assert.True(svc.Banner.ShowOpenDownloadPage);
        Assert.True(svc.Banner.ShowLater);
        Assert.False(svc.Banner.ShowInstallNow);
        Assert.False(File.Exists(Path.Combine(_dir, "1.1.0", UpdateCheck.SetupName)));
        Assert.Contains(_log.Lines, l => l.Contains("exit code 5", StringComparison.Ordinal));
        svc.Banner.OpenDownloadPageCommand.Execute(null);
        Assert.Equal("https://github.com/johnovanni/EarthRelief3D/releases/tag/v1.1.0", Assert.Single(_opener.Opened));
    }

    /// <summary>Windows could not start the installer at all: the app stays open, says why, and the file goes.</summary>
    [Fact]
    public async Task InstallerThatCannotStartKeepsTheAppOpen()
    {
        var bytes = Payload();
        var (svc, launcher) = await Offered(bytes);
        launcher.Refuse = "Windows could not start the installer: The operation was refused.";
        await svc.DownloadAndInstallAsync();
        svc.Banner.InstallNowCommand.Execute(null);
        Assert.Equal(UpdateBannerState.Failed, svc.Banner.State);
        Assert.Equal("The update could not be used: Windows could not start the installer: The operation was refused. You can download it from its release page instead.",
            svc.Banner.Text);
        Assert.False(File.Exists(Path.Combine(_dir, "1.1.0", UpdateCheck.SetupName)));
    }

    /// <summary>R12: from the last check until the installer has started, the file is held: nothing can write, rename or delete it. Then it is let go.</summary>
    [Fact]
    public async Task TheInstallerIsLockedUntilItHasStarted()
    {
        var bytes = Payload();
        var (svc, launcher) = await Offered(bytes);
        launcher.Tamper = true;
        await svc.DownloadAndInstallAsync();
        svc.Banner.InstallNowCommand.Execute(null);
        Assert.Single(launcher.Started);
        Assert.True(launcher.WriteBlocked, "a write was let through while the installer started");
        Assert.True(launcher.RenameBlocked, "a rename was let through while the installer started");
        Assert.True(launcher.DeleteBlocked, "a delete was let through while the installer started");
        var path = Path.Combine(_dir, "1.1.0", UpdateCheck.SetupName);
        Assert.Equal(bytes, File.ReadAllBytes(path));   // unchanged
        // Let go once started: the installer (and Windows) can use the file freely.
        using (new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { }
    }

    [Fact]
    public async Task AFailedDownloadOffersOnlyTheDownloadPage()
    {
        var bytes = Payload();
        var (svc, launcher) = await Offered(bytes);
        _signatures.AppSubject = null;   // a developer build
        await svc.DownloadAndInstallAsync();
        Assert.Equal(UpdateBannerState.Failed, svc.Banner.State);
        Assert.Equal("The update could not be used: " + UpdateDownloader.UnsignedAppText + " You can download it from its release page instead.", svc.Banner.Text);
        Assert.True(svc.Banner.ShowOpenDownloadPage);
        Assert.False(svc.Banner.ShowDownload);
        Assert.False(svc.Banner.ShowInstallNow);
        // Install is not possible from a failed state: nothing is started.
        svc.Banner.InstallNowCommand.Execute(null);
        Assert.Empty(launcher.Started);
    }

    [Fact]
    public async Task CancelGoesBackToTheOffer()
    {
        var bytes = Payload();
        var (svc, launcher) = await Offered(bytes);
        _http.Gate = new TaskCompletionSource();   // the download waits until it is cancelled
        var download = svc.DownloadAndInstallAsync();
        Assert.Equal(UpdateBannerState.Downloading, svc.Banner.State);
        Assert.Equal("Downloading EarthRelief3D 1.1.0: 0 of 1 MB", svc.Banner.Text);
        Assert.True(svc.Banner.ShowCancel);
        svc.Banner.CancelCommand.Execute(null);
        await download;
        Assert.Equal(UpdateBannerState.Offer, svc.Banner.State);
        Assert.Empty(launcher.Started);
    }
}
