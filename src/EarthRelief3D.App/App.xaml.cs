using System.IO;
using System.Windows;
using System.Windows.Threading;
using EarthRelief3D.App.Diagnostics;
using EarthRelief3D.App.Services;
using EarthRelief3D.App.Settings;
using EarthRelief3D.App.ViewModels;
using EarthRelief3D.Core.Updates;
using GlobeCore.Feeds;
using GlobeCore.Logging;
using GlobeCore.Storage;
using GlobeCore.Time;
using GlobeCore.Wpf.Diagnostics;

namespace EarthRelief3D.App;

/// <summary>
/// Composition root (as EarthWatch3D's App.xaml.cs): data folders, log, crash handler, settings, the crisis store loaded
/// from the cache, the four feeds and their scheduler (<see cref="AppServices"/>), the view models and the window. Shuts
/// them down in order when the main window closes or the app exits.
/// </summary>
public partial class App : Application
{
    private FileLog? _log;
    private SettingsSaver<AppSettings>? _settings;
    private AppServices? _services;
    private UpdateService? _updates;
    private DispatcherTimer? _updateTimer;
    private CrashHandler? _crash;
    private bool _shutDown;
    private string? _offlineRoot;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        // Offline samples (a diagnostic run) keep the cache and settings in a temporary folder, so the user's own are not touched.
        var options = LaunchOptions.Parse(e.Args);
        var userRoot = AppDataRoot.ForUser(AppInfo.Name,
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), PackageIdentity.FamilyName);
        var root = options.OfflineSamplesDir is not null
            ? new AppDataRoot(_offlineRoot = Path.Combine(Path.GetTempPath(), OfflineSamples.TempFolderPrefix + Guid.NewGuid().ToString("N")))
            : userRoot;
        // The log comes first. It always uses the real time and the user's usual logs folder, also in an offline or snapshot
        // run, so it is kept when that run's temporary data folder is deleted at exit.
        var logsDir = userRoot.LogsDir;
        var log = _log = new FileLog(logsDir, new SystemClock(), AppInfo.LogFilePrefix);
        log.Info($"{AppInfo.Name} {AppInfo.Version} starting" + (e.Args.Length > 0 ? " with " + string.Join(" ", e.Args) : ""));
        // MessageBoxImage.None everywhere: Warning, Error and the other icons play a Windows sound, and the app must make
        // no sound at all (the owner's rule; spec anti-goals). The text says what happened instead.
        void ShowCrash(string message) => MessageBox.Show(message + Environment.NewLine + Environment.NewLine + "Log folder: " + logsDir,
            AppInfo.Name, MessageBoxButton.OK, MessageBoxImage.None);
        _crash = new CrashHandler(log, message =>
        {
            if (Dispatcher.CheckAccess()) ShowCrash(message);
            else Dispatcher.BeginInvoke(() => ShowCrash(message));
        }, appName: AppInfo.Name);
        _crash.Install(this);
        OfflineSamples.SweepStaleFolders(Path.GetTempPath(), DateTime.UtcNow, log);   // offline runs that were killed

        // Diagnostic options: validated now, and the scenes read, before anything starts.
        string? samplesDir = options.OfflineSamplesDir is { } dir ? Path.GetFullPath(dir) : null;
        var problems = new List<string>();
        if (options.Error is not null) problems.Add(options.Error);
        if (samplesDir is not null)
        {
            if (!Directory.Exists(samplesDir)) problems.Add($"--offline-samples: the folder {samplesDir} does not exist.");
            else if (OfflineSamples.Missing(samplesDir) is { Count: > 0 } missing)
                problems.Add($"--offline-samples: {samplesDir} lacks {string.Join(", ", missing)}.");
        }
        if (options.TakesPictures && samplesDir is null) problems.Add("Taking pictures needs --offline-samples (the pictures use the sample data).");
        IReadOnlyList<SnapshotScene>? scenes = null;
        string? sceneOut = null;
        if (options.SnapshotScenesFile is { } scenesFile)
        {
            try { scenes = SnapshotScenes.Load(Path.GetFullPath(scenesFile)); }
            catch (FormatException ex) { problems.Add(ex.Message); }
            sceneOut = options.SnapshotOutDir is { } od ? Path.GetFullPath(od) : null;
        }
        else if (options.SnapshotPath is { } snap)
        {
            var path = Path.GetFullPath(snap);
            sceneOut = Path.GetDirectoryName(path) ?? ".";
            scenes = [SnapshotScenes.FromOptions(options with { SnapshotPath = path }, options.DisplayTimeUtc ?? DateTime.UtcNow)];
        }
        // A scene that shows the update banner reads the synthetic release from the samples folder (P5-22): nothing is fetched.
        ReleaseInfo? updateSample = null;
        if (scenes?.Any(s => s.ShowsUpdate) == true && samplesDir is not null)
        {
            var samplePath = Path.Combine(samplesDir, SnapshotScenes.UpdateSampleName);
            try { updateSample = UpdateCheck.Parse(File.ReadAllText(samplePath)); }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or FormatException)
            {
                problems.Add($"--snapshot-scenes: a scene shows the update banner, but {samplePath} cannot be read: {ex.Message}");
            }
        }
        if (problems.Count > 0)
        {
            var message = string.Join(Environment.NewLine, problems);
            log.Error("Command line: " + message);
            MessageBox.Show(message, AppInfo.Name + ": command line", MessageBoxButton.OK, MessageBoxImage.None);
            Shutdown(2);
            return;
        }

        // The app must still start without its data folders: settings fall back to defaults, the cache to empty, and
        // failed writes are logged.
        try
        {
            Directory.CreateDirectory(root.Root);
            Directory.CreateDirectory(root.CacheDir);
            Directory.CreateDirectory(logsDir);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            log.Error($"Could not create the data folders under {root.Root}", ex);
        }

        // Saves are debounced and written off the UI thread.
        _settings = new SettingsSaver<AppSettings>(new SettingsStore(root, log), log: log);
        // A pictures run stands both clocks still so pictures repeat exactly: the feeds' clock (also the view models') at the
        // first scene's time for the whole run, which keeps the feeds from running again; the display's at each scene's
        // time. --display-time alone starts the app's clock at that time, then it runs on.
        SettableClock? displayClock = scenes is null ? null : new SettableClock(scenes[0].DisplayTimeUtc);
        IClock clock = scenes is not null ? new SettableClock(scenes[0].DisplayTimeUtc)
            : options.DisplayTimeUtc is { } start ? new OffsetClock(new SystemClock()) { Offset = start - DateTime.UtcNow }
            : new SystemClock();
        if (samplesDir is not null) log.Info($"Offline samples from {samplesDir}; data folder {root.Root}");
        // A host whose firewall challenges the app (HDX's did, 26 Nov 2026) is left alone for 1 to 6 hours, kept across restarts.
        var http = samplesDir is null
            ? new FeedHttp(AppInfo.UserAgent, pauses: new HostPauses(clock, Path.Combine(root.CacheDir, AppInfo.HostPausesFile), log))
            : new FeedHttp(AppInfo.UserAgent, OfflineSamples.CreateHandler(samplesDir));
        // Offline samples answer at once: no wait between countries in the IPC area step (it would delay the pictures' data).
        var services = _services = AppServices.Create(root, http, clock, log, _settings, Dispatcher,
            ipcAreaPause: samplesDir is null ? null : TimeSpan.Zero, showDrafts: options.ShowDrafts,
            resumeFeeds: samplesDir is null && scenes is null);

        var window = new MainWindow(services.Main, services.Store, services.Countries, displayClock ?? clock, log);
        if (samplesDir is not null) window.Title += " - SAMPLE DATA (offline)";   // sample data is never mistaken for live data
        if (options.ShowDrafts) window.Title += LaunchOptions.DraftsTitleNote;   // nor unreviewed rows for reviewed ones
        window.Closed += (_, _) => ShutDownServices();
        MainWindow = window;
        // Subscribed before the feeds start, so no first report is missed.
        var dataReady = scenes is null ? Task.CompletedTask : SnapshotScenes.AllFeedsOkOnce(services.Scheduler, AppServices.FeedNames);
        services.Start();
        if (scenes is not null && displayClock is not null && sceneOut is not null)
            _ = SnapshotScenes.RunAsync(window, window.Scene, services.Main, scenes, displayClock, sceneOut, options.DelayMs, dataReady, log,
                draftsForAll: options.ShowDrafts, updateSample: updateSample);
        else if (options.OpenPanel is { } iso)
            window.Loaded += (_, _) => services.Main.Panel.Show(iso);
        // First start: the Welcome card, once the window has been drawn. Never in a picture or offline run (it would cover the pictures).
        if (WelcomeViewModel.ShouldShow(_settings.Load(), options))
        {
            void ShowWelcome(object? s, EventArgs a)
            {
                window.ContentRendered -= ShowWelcome;
                services.Main.Cards.Show(AppCard.Welcome);
            }
            window.ContentRendered += ShowWelcome;
        }
        // EarthRelief3D's own update check (P5-18 to P5-22): never in a diagnostic or offline run. It starts once the window has
        // been drawn; the timer only looks at the clock (the first check is 30 seconds later, then one a day).
        if (!options.IsDiagnosticRun)
        {
            var updatesDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), AppInfo.Name, "updates");
            var downloader = new UpdateDownloader(UpdateDownloader.CreateHandler(), new WinTrustSignatureCheck(), updatesDir);
            var updates = _updates = new UpdateService(UpdateService.GitHubFetch(new System.Net.Http.SocketsHttpHandler()), downloader,
                new ShellInstallerLauncher(), _settings, new SystemClock(), log, AppInfo.VersionNumber, Environment.ProcessPath ?? "",
                services.Main.Updates);
            services.Main.About.AttachUpdates(updates);
            services.Main.Settings.CheckForUpdatesChanged += (_, _) => updates.SettingsChanged();
            void StartUpdates(object? s, EventArgs a)
            {
                window.ContentRendered -= StartUpdates;
                updates.Start();
                _updateTimer = new DispatcherTimer(TimeSpan.FromSeconds(5), DispatcherPriority.Background, (_, _) => _ = TickUpdatesAsync(), Dispatcher);
                _updateTimer.Start();
            }
            window.ContentRendered += StartUpdates;
        }
        window.Show();
    }

    // A failed check is handled inside the service; this only keeps an unexpected fault from going unobserved.
    private async Task TickUpdatesAsync()
    {
        try { if (_updates is { } u) await u.TickAsync(); }
        catch (Exception ex) { _log?.Error("Updates: the update check stopped unexpectedly", ex); }
    }

    /// <summary>
    /// Windows is ending the session, or the installer's Restart Manager is closing the app to update it (ruling R11: it sends the
    /// same "end session" message). The normal shutdown runs now, while there is still time: settings flushed, cache saved, logs
    /// closed. It runs once; the exit that follows finds it done.
    /// </summary>
    protected override void OnSessionEnding(SessionEndingCancelEventArgs e)
    {
        base.OnSessionEnding(e);
        if (!e.Cancel) ShutDownServices();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        ShutDownServices();
        base.OnExit(e);
    }

    /// <summary>Stops the feeds, saves the cache and the settings once, then disposes. Each step is guarded.</summary>
    private void ShutDownServices()
    {
        if (_shutDown) return;
        _shutDown = true;
        Step("stop the update check", () => { _updateTimer?.Stop(); _updates?.Dispose(); });
        Step("stop the services", () => _services?.Dispose());
        Step("save settings", () => _settings?.Flush());
        Step("stop the settings saver", () => _settings?.Dispose());
        Step("write the repeated-error counts", () => _crash?.FlushRepeats());
        _log?.Info($"{AppInfo.Name} exiting");
        _log?.Dispose();
        if (_offlineRoot is not null)   // the offline run's temporary cache and settings, deleted last (the log is elsewhere)
        {
            try { Directory.Delete(_offlineRoot, recursive: true); } catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
        }
    }

    private void Step(string what, Action action)
    {
        try { action(); }
        catch (Exception ex) { _log?.Error("Shutdown: could not " + what, ex); }
    }
}
