using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Threading;
using AstroCore.Earth;
using EarthRelief3D.App.Controllers;
using EarthRelief3D.App.Presentation;
using EarthRelief3D.App.ViewModels;
using EarthRelief3D.Core.Curated;
using GlobeCore.Feeds;
using GlobeCore.Logging;
using GlobeCore.Wpf.Diagnostics;

namespace EarthRelief3D.App.Diagnostics;

/// <summary>
/// One picture of a <c>--snapshot-scenes</c> run. <paramref name="LookAt"/> null means the home view; layers not named are
/// off; <paramref name="Panel"/> (an ISO3 code, ruling R4) opens the country panel for that country before the picture, and
/// null leaves it closed. <paramref name="PanelFocus"/> (a layer name) opens the panel scrolled to that layer's section, as a
/// click on its marker does; a long panel does not otherwise show its later sections in one picture. Besides the Plan 2
/// layer names it may be "atrocities" or "camps" (the panel's genocide and prison-camp sections, as a click on a triangle
/// or a square opens them). <paramref name="ShowDrafts"/> shows the curated rows the owner has not approved yet, labelled
/// "Draft: not yet reviewed", for this picture only, as <c>--show-drafts</c> does for a whole run (P3-4). <paramref name="Explainer"/>
/// (an explainer id, needs a panel; ruling R19) opens the explainer card for that picture, as the section's "What is this" link does.
/// <paramref name="LegendOpen"/> and <paramref name="StripOpen"/> (scene fields "legend" and "strip": "open" or "closed") show the
/// legend and the layer strip's list open (the default, so the earlier pictures still compare) or folded; nothing is saved (P5-23, P5-24).
/// </summary>
public sealed record SnapshotScene(string Name, DateTime DisplayTimeUtc, GeoPosition? LookAt, string[] Layers, string? Panel,
                                   string? PanelFocus = null, bool ShowDrafts = false, string? Explainer = null,
                                   bool LegendOpen = true, bool StripOpen = true)
{
    /// <summary>The <see cref="Explainer"/> ids: one per crisis kind, "access" and the step section's "why-these" (Plan 4 Task 9; ruling R19).</summary>
    public static IReadOnlyList<string> ExplainerNames { get; } =
    [
        CountryPanelViewModel.ConflictExplainerId, CountryPanelViewModel.FoodExplainerId, CountryPanelViewModel.OutbreaksExplainerId,
        CountryPanelViewModel.DisplacementExplainerId, CountryPanelViewModel.AtrocitiesExplainerId, CountryPanelViewModel.CampsExplainerId,
        CountryPanelViewModel.AccessExplainerId, CountryPanelViewModel.StepExplainerId,
    ];

    /// <summary>The <see cref="PanelFocus"/> names: the Plan 2 layers, then the curated marker layers (Plan 3), then "access" and "help" (Plan 4).</summary>
    public static IReadOnlyList<string> PanelFocusNames { get; } =
        [.. LaunchOptions.LayerNames, LayerCatalog.Id(MarkerLayer.Atrocities), LayerCatalog.Id(MarkerLayer.Camps), "access", "help"];

    /// <summary>The shading the scene names ("food", "water", ...; at most one), or none.</summary>
    public ShadingLayer Shading => Layers.Select(n => LayerCatalog.TryParse(n, out var sh, out _) ? sh : null)
        .FirstOrDefault(sh => sh is not null) ?? ShadingLayer.None;

    /// <summary>The marker layers the scene names ("conflict", "camps", "power", ...), in enum order.</summary>
    public IReadOnlyList<MarkerLayer> Markers => [.. Layers.Select(n => LayerCatalog.TryParse(n, out _, out var m) ? m : null)
        .OfType<MarkerLayer>().Distinct().Order()];

    /// <summary>The layer a <see cref="PanelFocus"/> name stands for.</summary>
    public CrisisLayer? FocusLayer => PanelFocus?.ToLowerInvariant() switch
    {
        "conflict" => CrisisLayer.Conflict, "food" => CrisisLayer.Food, "outbreaks" => CrisisLayer.Outbreaks,
        "displacement" => CrisisLayer.Displacement, _ => null,
    };

    /// <summary>The Plan 4 section a <see cref="PanelFocus"/> name stands for ("access", "help"), or null.</summary>
    public ViewModels.PanelFocus? FocusSection => PanelFocus?.ToLowerInvariant() switch { "access" => ViewModels.PanelFocus.Access, "help" => ViewModels.PanelFocus.Help, _ => null };

    /// <summary>The curated marker layer a <see cref="PanelFocus"/> name stands for ("atrocities", "camps"), or null.</summary>
    public MarkerLayer? FocusMarker => PanelFocus?.ToLowerInvariant() switch
    {
        "atrocities" => MarkerLayer.Atrocities, "camps" => MarkerLayer.Camps, _ => null,
    };
}

/// <summary>
/// The <c>--snapshot-scenes</c> and <c>--snapshot</c> runs. A snapshot run is deterministic by construction (as EarthWatch3D's
/// Plan 1 Task 1 found it must be): the display clock stands still at each scene's time and the feeds' clock stands still at
/// the first scene's time for the whole run (so the feeds neither run again nor change their "checked" dates), the camera
/// is moved at once (no animation), and the app has no other animation.
/// </summary>
public static class SnapshotScenes
{
    public static readonly TimeSpan DataTimeout = TimeSpan.FromSeconds(30);
    public static readonly TimeSpan SettleTimeout = TimeSpan.FromSeconds(30);

    /// <summary>The scene <c>--snapshot</c> takes: the options' camera, layers (default all) and panel at <paramref name="time"/>.</summary>
    public static SnapshotScene FromOptions(LaunchOptions o, DateTime time) =>
        new(Path.GetFileNameWithoutExtension(o.SnapshotPath ?? "snapshot"), time, o.LookAt,
            o.Layers?.ToArray() ?? LaunchOptions.LayerNames, o.OpenPanel);

    /// <summary>
    /// Reads a scenes file: <c>{"scenes":[{"name":"africa","displayTimeUtc":"2026-11-22T12:00:00Z","lookAt":[10,25],
    /// "layers":["conflict","food"],"panel":"SDN","panelFocus":"food","showDrafts":false}]}</c>. All but name and time are optional.
    /// Throws <see cref="FormatException"/> with a plain message when the file is not usable.
    /// </summary>
    public static IReadOnlyList<SnapshotScene> Load(string path)
    {
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            var list = new List<SnapshotScene>();
            foreach (var e in doc.RootElement.GetProperty("scenes").EnumerateArray())
            {
                string name = e.GetProperty("name").GetString() ?? "";
                if (name.Length == 0 || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                    throw new FormatException($"'{name}' is not usable as a picture name.");
                if (list.Any(x => x.Name == name)) throw new FormatException($"The scene name '{name}' is used twice.");
                string time = e.GetProperty("displayTimeUtc").GetString() ?? "";
                if (!LaunchOptions.TryParseUtc(time, out var utc))
                    throw new FormatException($"Scene {name}: '{time}' is not a UTC time such as {LaunchOptions.DisplayTimeExample}.");
                GeoPosition? lookAt = null;
                if (e.TryGetProperty("lookAt", out var la) && la.ValueKind == JsonValueKind.Array)
                {
                    double lat = la[0].GetDouble(), lon = la[1].GetDouble();
                    if (!(Math.Abs(lat) <= 90) || !(Math.Abs(lon) <= 180)) throw new FormatException($"Scene {name}: lookAt is out of range.");
                    lookAt = new GeoPosition(lat, lon);
                }
                var layers = e.TryGetProperty("layers", out var ls)
                    ? ls.EnumerateArray().Select(x => (x.GetString() ?? "").ToLowerInvariant()).ToArray() : [];
                if (layers.FirstOrDefault(l => !LaunchOptions.AllLayerNames.Contains(l)) is { } bad)
                    throw new FormatException($"Scene {name}: '{bad}' is not a layer. Use any of {string.Join(", ", LaunchOptions.AllLayerNames)}.");
                if (LaunchOptions.ShadingsIn(layers) is { Count: > 1 } two)
                    throw new FormatException($"Scene {name}: only one shading layer can be on at a time, not {string.Join(" and ", two)}.");
                string? panel = e.TryGetProperty("panel", out var pn) && pn.ValueKind == JsonValueKind.String ? pn.GetString() : null;
                if (panel is not null && !LaunchOptions.IsIso3(panel))
                    throw new FormatException($"Scene {name}: panel '{panel}' is not a three-letter country code.");
                string? focus = e.TryGetProperty("panelFocus", out var pf) && pf.ValueKind == JsonValueKind.String ? pf.GetString() : null;
                if (focus is not null && (panel is null || !SnapshotScene.PanelFocusNames.Contains(focus.ToLowerInvariant())))
                    throw new FormatException($"Scene {name}: panelFocus needs a panel and one of {string.Join(", ", SnapshotScene.PanelFocusNames)}.");
                string? explainer = e.TryGetProperty("explainer", out var ep) && ep.ValueKind == JsonValueKind.String ? ep.GetString() : null;
                if (explainer is not null && (panel is null || !SnapshotScene.ExplainerNames.Contains(explainer.ToLowerInvariant())))
                    throw new FormatException($"Scene {name}: explainer needs a panel and one of {string.Join(", ", SnapshotScene.ExplainerNames)}.");
                bool drafts = false;
                if (e.TryGetProperty("showDrafts", out var sd))
                {
                    if (sd.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                        throw new FormatException($"Scene {name}: showDrafts is true or false.");
                    drafts = sd.GetBoolean();
                }
                bool OpenOrClosed(string field)
                {
                    if (!e.TryGetProperty(field, out var v)) return true;   // not named: open, as in the earlier pictures
                    return (v.ValueKind == JsonValueKind.String ? v.GetString()?.ToLowerInvariant() : null) switch
                    {
                        "open" => true,
                        "closed" => false,
                        _ => throw new FormatException($"Scene {name}: {field} is \"open\" or \"closed\"."),
                    };
                }
                bool legendOpen = OpenOrClosed("legend"), stripOpen = OpenOrClosed("strip");
                list.Add(new SnapshotScene(name, utc, lookAt, layers, panel?.ToUpperInvariant(), focus?.ToLowerInvariant(), drafts, explainer?.ToLowerInvariant(),
                    legendOpen, stripOpen));
            }
            if (list.Count == 0) throw new FormatException("The file has no scenes.");
            return list;
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException or IOException
                                       or UnauthorizedAccessException or IndexOutOfRangeException)
        {
            throw new FormatException($"--snapshot-scenes: cannot read {path}: {ex.Message}", ex);
        }
    }

    /// <summary>Completes once every named feed has reported <see cref="FeedState.Ok"/> at least once. Subscribe before the scheduler starts.</summary>
    public static Task AllFeedsOkOnce(FeedScheduler scheduler, IEnumerable<string> names)
    {
        var waiting = new HashSet<string>(names, StringComparer.Ordinal);
        var done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        scheduler.StatusChanged += (_, status) =>
        {
            if (status.State != FeedState.Ok) return;
            lock (waiting) { if (waiting.Remove(status.Name) && waiting.Count == 0) done.TrySetResult(); }
        };
        return done.Task;
    }

    /// <summary>
    /// For each scene: moves <paramref name="displayClock"/> to its time, shows or hides the curated drafts (the scene's
    /// <c>showDrafts</c>, or <paramref name="draftsForAll"/>), sets exactly its layers, camera and panel, waits
    /// (every feed reported once, a compose for that time on the globe, the host idle for a while) and saves
    /// <c>&lt;outDir&gt;\&lt;name&gt;.png</c> with GlobeCore's <c>SnapshotRenderer.SavePng</c> (RenderTargetBitmap). A scene
    /// that fails is logged and the run goes on; the app exits with 0, or 1 if any scene failed or the globe never showed.
    /// </summary>
    /// <param name="draftsForAll">The run was started with <c>--show-drafts</c>: every scene shows the drafts.</param>
    public static async Task RunAsync(Window window, SceneController scene, MainViewModel vm, IReadOnlyList<SnapshotScene> scenes,
                                      SettableClock displayClock, string outDir, int delayMs, Task dataReady, IAppLog log,
                                      bool draftsForAll = false)
    {
        int exitCode = 0;
        try
        {
            Directory.CreateDirectory(outDir);
            await scene.FirstComposeDisplayed;
            foreach (var s in scenes)
            {
                try
                {
                    await dataReady.WaitAsync(DataTimeout);   // a timeout is a failure: the picture would lack data
                    displayClock.Now = s.DisplayTimeUtc;      // stands still until the next scene
                    bool drafts = draftsForAll || s.ShowDrafts;
                    if (drafts != vm.Curated.ShowDrafts)
                        log.Info($"Diagnostics: scene {s.Name} {(drafts ? "shows" : "hides")} the curated drafts (\"{DesignationText.DraftLabel}\")");
                    vm.Curated.SetShowDrafts(drafts);         // before the layers and the panel, which read the curated rows
                    var plainTitle = window.Title.Replace(LaunchOptions.DraftsTitleNote, "", StringComparison.Ordinal);
                    window.Title = drafts ? plainTitle + LaunchOptions.DraftsTitleNote : plainTitle;
                    vm.Layers.SetLayers(s.Shading, s.Markers);
                    vm.Layers.SetOpenQuietly(s.StripOpen);    // for this picture only: nothing is saved
                    vm.Legend.SetOpenQuietly(s.LegendOpen);
                    if (s.Panel is { } iso) vm.Panel.Show(iso, s.FocusLayer, s.FocusMarker, s.FocusSection);
                    else if (vm.Panel.IsOpen) vm.Panel.CloseCommand.Execute(null);
                    if (s.Explainer is { } xid && !vm.Panel.Explainer.Open(xid))
                        throw new InvalidOperationException($"Scene {s.Name}: the explainer \"{xid}\" is not shown (drafts off, or no such explainer).");
                    scene.LookAt(s.LookAt ?? SceneController.HomeView);
                    var host = scene.Host ?? throw new InvalidOperationException("The globe is not loaded yet.");
                    host.DisplayTimeUtc = displayClock.UtcNow;
                    await RecomposeDisplayedAsync(scene, s.DisplayTimeUtc);
                    await SettleAsync(scene, window.Dispatcher, log);
                    await Task.Delay(delayMs);
                    await SettleAsync(scene, window.Dispatcher, log);
                    Save(window, Path.Combine(outDir, s.Name + ".png"));
                    log.Info($"Diagnostics: saved scene {s.Name}");
                }
                catch (OperationCanceledException) { return; }   // the window closed first
                catch (Exception ex)
                {
                    Trace.TraceError($"Scene {s.Name} failed: {ex}");
                    log.Error($"Diagnostics: scene {s.Name} failed", ex);
                    exitCode = 1;
                }
            }
        }
        catch (OperationCanceledException) { return; }
        catch (Exception ex)
        {
            // The globe never showed, or the folder could not be made: end the run with 1 rather than leave it waiting.
            Trace.TraceError($"Snapshot scenes failed: {ex}");
            log.Error("Diagnostics: the snapshot scenes could not be taken", ex);
            exitCode = 1;
        }
        Application.Current.Shutdown(exitCode);
    }

    /// <summary>
    /// Waits until the layer host has been idle (no compose running, queued or owed) for 600 ms running, and the dispatcher
    /// has drained its pending work, so the markers, labels and panel of this scene are on screen. A timeout throws.
    /// </summary>
    private static async Task SettleAsync(SceneController scene, Dispatcher dispatcher, IAppLog log)
    {
        var sw = Stopwatch.StartNew();
        int idlePolls = 0;
        while (idlePolls < 6)
        {
            if (sw.Elapsed >= SettleTimeout)
                throw new TimeoutException($"the scene did not settle within {SettleTimeout.TotalSeconds:0} s");
            await dispatcher.InvokeAsync(() => { }, DispatcherPriority.ContextIdle);
            idlePolls = scene.Host is { IsComposeIdle: true } ? idlePolls + 1 : 0;
            await Task.Delay(100);
        }
        log.Info($"Diagnostics: scene settled after {sw.Elapsed.TotalSeconds:F1} s");
    }

    /// <summary>
    /// Asks the layer host for a recompose and completes when a compose for <paramref name="notBefore"/> or later is on the
    /// globe. Called after the first compose, so the host exists.
    /// </summary>
    private static async Task RecomposeDisplayedAsync(SceneController scene, DateTime notBefore)
    {
        var host = scene.Host ?? throw new InvalidOperationException("The globe is not loaded yet.");
        var shown = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        void OnComposed(object? sender, EventArgs e) { if (host.ComposedUtc >= notBefore) shown.TrySetResult(); }
        host.Composed += OnComposed;
        try
        {
            _ = host.RecomposeNowAsync();   // if a compose is running, this queues one more, which reads the new time
            await shown.Task.WaitAsync(TimeSpan.FromSeconds(30));
        }
        finally { host.Composed -= OnComposed; }
    }

    /// <summary>
    /// Renders the window's client area at the window's DPI and saves it as a PNG (GlobeCore's
    /// <see cref="SnapshotRenderer.SavePng"/>). A false result (nothing to render) is a failure.
    /// </summary>
    public static void Save(Window window, string path)
    {
        if (!SnapshotRenderer.SavePng(window, path))
            throw new InvalidOperationException($"Nothing to render for the snapshot {path}: the window has no laid-out content.");
    }
}
