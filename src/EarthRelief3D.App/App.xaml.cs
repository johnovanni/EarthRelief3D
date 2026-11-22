using System.IO;
using System.Windows;
using EarthRelief3D.App.Services;
using EarthRelief3D.App.Settings;
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
    private CrashHandler? _crash;
    private bool _shutDown;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var root = AppDataRoot.ForUser(AppInfo.Name,
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), PackageIdentity.FamilyName);
        // The log comes first (it always uses the real time and the usual folder).
        var log = _log = new FileLog(root.LogsDir, new SystemClock(), AppInfo.LogFilePrefix);
        log.Info($"{AppInfo.Name} {AppInfo.Version} starting" + (e.Args.Length > 0 ? " with " + string.Join(" ", e.Args) : ""));
        void ShowCrash(string message) => MessageBox.Show(message + Environment.NewLine + Environment.NewLine + "Log folder: " + root.LogsDir,
            AppInfo.Name, MessageBoxButton.OK, MessageBoxImage.Warning);
        _crash = new CrashHandler(log, message =>
        {
            if (Dispatcher.CheckAccess()) ShowCrash(message);
            else Dispatcher.BeginInvoke(() => ShowCrash(message));
        }, appName: AppInfo.Name);
        _crash.Install(this);

        // The app must still start without its data folders: settings fall back to defaults, the cache to empty, and
        // failed writes are logged.
        try
        {
            Directory.CreateDirectory(root.Root);
            Directory.CreateDirectory(root.CacheDir);
            Directory.CreateDirectory(root.LogsDir);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            log.Error($"Could not create the data folders under {root.Root}", ex);
        }

        // Saves are debounced and written off the UI thread.
        _settings = new SettingsSaver<AppSettings>(new SettingsStore(root, log), log: log);
        IClock clock = new SystemClock();
        var http = new FeedHttp(AppInfo.UserAgent);
        var services = _services = AppServices.Create(root, http, clock, log, _settings, Dispatcher);

        var window = new MainWindow(services.Main, services.Store, services.Countries, clock, log);
        window.Closed += (_, _) => ShutDownServices();
        MainWindow = window;
        services.Start();
        window.Show();
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
        Step("stop the services", () => _services?.Dispose());
        Step("save settings", () => _settings?.Flush());
        Step("stop the settings saver", () => _settings?.Dispose());
        Step("write the repeated-error counts", () => _crash?.FlushRepeats());
        _log?.Info($"{AppInfo.Name} exiting");
        _log?.Dispose();
    }

    private void Step(string what, Action action)
    {
        try { action(); }
        catch (Exception ex) { _log?.Error("Shutdown: could not " + what, ex); }
    }
}
