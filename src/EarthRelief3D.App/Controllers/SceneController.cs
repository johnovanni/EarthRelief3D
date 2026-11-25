using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using AstroCore.Astronomy;
using AstroCore.Earth;
using EarthRelief3D.App.Layers;
using EarthRelief3D.App.Presentation;
using EarthRelief3D.App.ViewModels;
using EarthRelief3D.Core.Feeds;
using EarthRelief3D.Core.Store;
using GlobeCore.Earth;
using GlobeCore.Imaging;
using GlobeCore.Logging;
using GlobeCore.Regions;
using GlobeCore.Time;
using GlobeCore.Wpf.Camera;
using GlobeCore.Wpf.Layers;
using GlobeCore.Wpf.Rendering;
using HelixToolkit.Wpf;
using Microsoft.Win32;

namespace EarthRelief3D.App.Controllers;

/// <summary>
/// Owns the 3D scene (as EarthWatch3D's SceneController). Once the Blue Marble is loaded it builds GlobeCore's
/// <see cref="LayerHost"/> on <see cref="EarthBody.Instance"/> with the borders, the food shading and the Plan 3 country-value
/// shadings in the image, the displacement rings, conflict dots, outbreak crosses and the Plan 3 point markers in the scene
/// and the labels on the overlay, following the
/// LayerHost start-up sequence: add the layers, set <see cref="LayerHost.IlluminationAt"/> (night strength 0.6) and
/// <see cref="LayerHost.DisplayTimeUtc"/>, then <see cref="LayerHost.RecomposeNowAsync"/> once. It wires the layer strip's
/// toggles to the layers, a click to the country panel, and keeps the camera rules, input and animation.
/// </summary>
public sealed class SceneController
{
    /// <summary>Width of the globe image in pixels (height is half), as EarthWatch3D: the 8192 asset is scaled while decoding.</summary>
    public const int GlobeImageWidth = 4096;
    public const double NightStrength = 0.6;
    /// <summary>The day/night line moves about a quarter of a degree a minute: the display time is moved on this often.</summary>
    public static readonly TimeSpan DisplayTimeInterval = TimeSpan.FromSeconds(30);
    /// <summary>The view the window opens with: north-east Africa, where several of the sources' largest crises are.</summary>
    public static readonly GeoPosition HomeView = new(15, 25);

    private readonly HelixViewport3D _viewport;
    private readonly Canvas _overlayCanvas;
    private readonly MainViewModel _vm;
    private readonly Window _window;
    private readonly CrisisStore _store;
    private readonly CountryResolver _countries;
    private readonly IClock _clock;
    private readonly IAppLog _log;
    private readonly CameraAnimator _animator;
    private readonly TaskCompletionSource _firstCompose = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly StaWorker _sta = new("EarthRelief3D rasterizer");
    private readonly DispatcherTimer _timeTimer;
    private readonly DisplacementSceneLayer _displacement;
    private readonly ConflictSceneLayer _conflict;
    private readonly OutbreakSceneLayer _outbreaks;
    private readonly AtrocityLayer _atrocities;
    private DateOnly _atrocityDay;
    private readonly LabelsOverlayLayer _labels;
    private readonly HubArcLayer _hubArcs;
    private readonly HubLabelsLayer _hubLabels;
    private readonly IReadOnlyList<PointMarkerLayer> _points;
    private LayerHost? _host;
    private BordersImageLayer? _borders;
    private FoodImageLayer? _food;
    private IReadOnlyList<CountryValueLayer> _shadings = [];
    private bool _closed, _adjustingCamera;
    private int _imageWidth, _imageHeight, _linesVersion;
    private BoundarySet? _linesSet;   // the boundary set whose lines the borders layer shows

    /// <param name="overlay">The canvas over the viewport for the labels.</param>
    /// <param name="window">The window hosting the viewport: its Loaded, Closed, StateChanged and PreviewKeyDown events drive the scene.</param>
    /// <param name="countries">Names and polygons, units applied (picking, marker positions, food shading).</param>
    /// <param name="clock">The display time (the night shade, and the time windows the layers are judged at).</param>
    public SceneController(HelixViewport3D viewport, Canvas overlay, MainViewModel vm, Window window, CrisisStore store,
                           CountryResolver countries, IClock clock, IAppLog? log = null)
    {
        _log = log ?? new NullLog();
        _firstCompose.Task.ContinueWith(static t => _ = t.Exception, CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        _viewport = viewport ?? throw new ArgumentNullException(nameof(viewport));
        _overlayCanvas = overlay ?? throw new ArgumentNullException(nameof(overlay));
        _vm = vm ?? throw new ArgumentNullException(nameof(vm));
        _window = window ?? throw new ArgumentNullException(nameof(window));
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _countries = countries ?? throw new ArgumentNullException(nameof(countries));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));

        _animator = new CameraAnimator(() => Camera);
        _displacement = new DisplacementSceneLayer(store, _countries, () => _clock.UtcNow) { IsVisible = vm.Layers.ShowDisplacement };
        _conflict = new ConflictSceneLayer(store, _countries, () => _clock.UtcNow) { IsVisible = vm.Layers.ShowConflict };
        _outbreaks = new OutbreakSceneLayer(store, _countries, () => _clock.UtcNow) { IsVisible = vm.Layers.ShowOutbreaks };
        _labels = new LabelsOverlayLayer(store, vm.Layers, _countries, overlay, () => _clock.UtcNow, () => vm.Settings.TextScale);
        // Plan 3: the curated atrocity glyphs (C14), judged at the display time.
        _atrocities = new AtrocityLayer(vm.Curated, _countries, () => _clock.UtcNow) { IsVisible = vm.Layers.IsShown(MarkerLayer.Atrocities) };
        _atrocityDay = DateOnly.FromDateTime(_clock.UtcNow);
        // Plan 3: the marker layers drawn from lists; empty until their data is loaded (ruling R2).
        var markerData = vm.MarkerData;
        _points = [.. PointMarkerLayer.Layers.Select(m => new PointMarkerLayer(m, () => markerData.Items(m), _log) { IsVisible = vm.Layers.IsShown(m) })];
        // Plan 4 (P4-7): the straight-line arcs from the open panel's country to its nearest UN depot, airport and port, and their
        // distance labels. Both read the same arcs: the panel's country, the toggle and the hubs (P4-6: from the country's label point).
        IReadOnlyList<HubArcSpec> CurrentArcs() => HubArcLayer.SpecsFor(vm.Panel.IsOpen, vm.Layers.IsShown(MarkerLayer.HubLines), vm.Panel.Iso3,
            iso => _countries.ByIso3(iso)?.LabelPoint, vm.Hubs.Hubs);
        _hubArcs = new HubArcLayer(CurrentArcs) { IsVisible = vm.Layers.IsShown(MarkerLayer.HubLines) };
        _hubLabels = new HubLabelsLayer(CurrentArcs, () => vm.Settings.Units, overlay, () => vm.Settings.TextScale) { IsVisible = vm.Layers.IsShown(MarkerLayer.HubLines) };
        _timeTimer = new DispatcherTimer(DispatcherPriority.Background, window.Dispatcher) { Interval = DisplayTimeInterval };
        _timeTimer.Tick += OnTimeTick;

        _window.Loaded += OnLoaded;
        _window.Closed += OnClosed;
        _window.StateChanged += OnWindowStateChanged;
        _window.PreviewKeyDown += OnPreviewKeyDown;
        _viewport.CameraChanged += OnCameraChanged;
        _viewport.PreviewMouseWheel += OnPreviewMouseWheel;
        _viewport.PreviewMouseDown += OnPreviewMouseDown;
        _vm.Layers.PropertyChanged += OnLayerToggled;
        _vm.Indicators.Changed += OnIndicatorsChanged;
        _vm.MarkerData.Changed += OnMarkerDataChanged;
        _vm.Panel.PropertyChanged += OnPanelChanged;
        _vm.Hubs.Changed += OnHubsChanged;
        _vm.Settings.UnitsChanged += OnHubsChanged;
        _vm.Settings.TextScaleChanged += OnTextScaleChanged;
        SystemEvents.PowerModeChanged += OnPowerModeChanged;   // static event: unsubscribed in OnClosed
    }

    /// <summary>Completes once the first composed globe image is on the globe (cancelled if the window closes first).</summary>
    public Task FirstComposeDisplayed => _firstCompose.Task;

    /// <summary>Panels and strips over the globe: no hover picking while the mouse is over them. Add them before the window loads.</summary>
    public IList<UIElement> HoverBlockers { get; } = [];

    /// <summary>The layer host, with every layer added, once the globe image has loaded (null before).</summary>
    internal LayerHost? Host => _host;

    internal PerspectiveCamera? Camera => _viewport.Camera as PerspectiveCamera;

    // ---------------------------------------------------------------- globe image and layers

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        _window.Loaded -= OnLoaded;
        try
        {
            if (Camera is { } camera)
            {
                SetView(camera, HomeView, FitDistanceForViewport(camera));
                ApplyRotationSensitivity(camera);
            }
            var loadSet = _countries.Boundaries.Set;
            var (baseImage, lines) = await GlobeLoader.LoadAsync(GlobeImageWidth, _sta, loadSet);
            if (_closed) return;
            _ = StartHostAsync(baseImage, lines, loadSet);
        }
        catch (OperationCanceledException) when (_closed) { }
        catch (Exception ex)
        {
            _log.Error("Globe load failed", ex);
            _firstCompose.TrySetException(ex);
            // MessageBoxImage.None: a box with an icon plays a Windows sound, and the app must make no sound at all.
            if (!_closed) MessageBox.Show(_window, ex.Message, AppInfo.Name, MessageBoxButton.OK, MessageBoxImage.None);
        }
    }

    /// <summary>
    /// Builds the host and the image layers on the loaded globe image and makes the first compose (LayerHost's start-up
    /// sequence). UI thread. Completes when the first compose is on the globe.
    /// </summary>
    internal Task StartHostAsync(Bgra32Image baseImage, SparseOverlay coastAndBorders, BoundarySet? linesFrom = null)
    {
        var layers = _vm.Layers;
        _imageWidth = baseImage.Width;
        _imageHeight = baseImage.Height;
        _linesSet = linesFrom ?? _countries.Boundaries.Set;
        _vm.Legend.ShowBoundaries(_linesSet);   // the legend names the edition the lines are drawn from
        _borders = new BordersImageLayer(coastAndBorders, _store, _countries, () => _clock.UtcNow);
        _food = new FoodImageLayer(baseImage.Width, baseImage.Height, _store, _countries, _sta, _log, () => _closed, () => _clock.UtcNow) { IsVisible = layers.ShowFood };
        // Plan 3: one country-value layer per shading of the Needs and Resources groups; only the chosen one is visible (P3-6).
        var indicators = _vm.Indicators;
        _shadings = [.. CountryValueLayer.Layers.Select(l => new CountryValueLayer(l, () => indicators.Snapshot.Get(LayerCatalog.SeriesId(l)),
            ShadingScales.For(l), baseImage.Width, baseImage.Height, _countries, _sta, _log, () => _closed) { IsVisible = layers.IsShown(l) })];
        var host = _host = new LayerHost(_viewport, _overlayCanvas, EarthBody.Instance, baseImage, _log);
        foreach (var b in HoverBlockers) host.HoverBlockers.Add(b);
        // Scene layers in draw order (WPF draws 3D children in order): rings under dots under crosses, then the point markers.
        GlobeLayer[] all = [_borders, _food, .. _shadings, _displacement, _conflict, _outbreaks, _atrocities, _hubArcs, .. _points, _labels, _hubLabels];
        foreach (var layer in all) host.Add(layer);
        host.FirstComposeDisplayed += OnFirstComposeDisplayed;
        host.WhenFirstComposeDisplayed.ContinueWith(t => _firstCompose.TrySetException(t.Exception!.InnerExceptions),
            CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        host.Selected += OnSelected;
        host.SelectionCleared += OnSelectionCleared;
        host.IlluminationAt = utc => host.Body.IlluminationAt(utc, NightStrength, true);
        host.DisplayTimeUtc = _clock.UtcNow;
        var first = host.RecomposeNowAsync();
        // From here a boundary swap redraws the borders, the food shading and the markers. A swap that came while the lines
        // were loading is caught up now.
        _countries.Changed += OnBoundariesChanged;
        if (!ReferenceEquals(_countries.Boundaries.Set, _linesSet)) _ = RedrawBordersAsync();
        _timeTimer.Start();
        return first;
    }

    // Raised on the thread that swapped the boundaries (the feed's), after the resolver's index was rebuilt. The layers
    // may be invalidated from any thread; the lines are rasterized on the STA worker.
    private void OnBoundariesChanged(object? sender, EventArgs e)
    {
        if (_closed) return;
        _displacement.BoundariesChanged();
        _conflict.BoundariesChanged();
        _outbreaks.BoundariesChanged();
        _atrocities.DataChanged();
        _labels.BoundariesChanged();
        _food?.BoundariesChanged();
        foreach (var l in _shadings) l.BoundariesChanged();
        _ = RedrawBordersAsync();
    }

    private async Task RedrawBordersAsync()
    {
        int version = Interlocked.Increment(ref _linesVersion);
        try
        {
            var set = _countries.Boundaries.Set;
            int w = _imageWidth, h = _imageHeight;
            var lines = await _sta.InvokeAsync(() =>
                BordersImageLayer.RasterizeCoastAndBorders(NaturalEarthLines.Coastlines(), NaturalEarthLines.LandBoundaries(set), w, h));
            if (_closed || version != Volatile.Read(ref _linesVersion)) return;   // a newer swap redraws
            _borders?.ReplaceLines(lines);
            _linesSet = set;
            _vm.Legend.ShowBoundaries(set);   // only now: a failed redraw keeps the old lines and the old legend
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            if (!_closed) _log.Error("The borders could not be redrawn for the new boundaries", ex);
        }
    }

    private void OnFirstComposeDisplayed(object? sender, EventArgs e) => _firstCompose.TrySetResult();

    private void OnTimeTick(object? sender, EventArgs e)
    {
        if (_host is null || _closed) return;
        _host.DisplayTimeUtc = _clock.UtcNow;
        // A continuing statement counts for 12 months (C14): the glyphs are judged again when the day changes.
        var day = DateOnly.FromDateTime(_clock.UtcNow);
        if (day != _atrocityDay)
        {
            _atrocityDay = day;
            _atrocities.DataChanged();
        }
    }

    private void OnLayerToggled(object? sender, PropertyChangedEventArgs e)
    {
        if (!LayerStripViewModel.IsLayerProperty(e.PropertyName)) return;
        var l = _vm.Layers;
        _conflict.IsVisible = l.ShowConflict;
        _outbreaks.IsVisible = l.ShowOutbreaks;
        _displacement.IsVisible = l.ShowDisplacement;
        _atrocities.IsVisible = l.IsShown(MarkerLayer.Atrocities);
        foreach (var p in _points) p.IsVisible = l.IsShown(p.Which);
        if (_food is not null) _food.IsVisible = l.ShowFood;
        foreach (var s in _shadings) s.IsVisible = l.IsShown(s.Which);
        bool lines = l.IsShown(MarkerLayer.HubLines);
        _hubArcs.IsVisible = lines;
        _hubLabels.IsVisible = lines;
        _hubArcs.Changed();
        _hubLabels.Changed();
    }

    // The arcs follow the open panel: they appear when it opens, move with the country and go when it closes.
    private void OnPanelChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is not (nameof(CountryPanelViewModel.IsOpen) or nameof(CountryPanelViewModel.Iso3))) return;
        _hubArcs.Changed();
        _hubLabels.Changed();
    }

    // The hubs changed (the curated store), or the unit did (Settings): any thread.
    private void OnHubsChanged(object? sender, EventArgs e)
    {
        _hubArcs.Changed();
        _hubLabels.Changed();
    }

    // The text size changed (Settings, or a snapshot scene): the globe's labels are placed again at the new size.
    private void OnTextScaleChanged(object? sender, EventArgs e)
    {
        _labels.Changed();
        _hubLabels.Changed();
    }

    // Any thread (the feeds'): the layers only ask the host for a recompose or an update.
    private void OnIndicatorsChanged(object? sender, EventArgs e)
    {
        foreach (var s in _shadings) s.DataChanged();
    }

    private void OnMarkerDataChanged(object? sender, MarkerLayer layer)
    {
        foreach (var p in _points) if (p.Which == layer) p.DataChanged();
    }

    // ---------------------------------------------------------------- selection

    private void OnSelected(object? sender, PickResult hit) => Select(hit);

    private void OnSelectionCleared(object? sender, EventArgs e) => ClearSelection();

    /// <summary>A click hit something: a country or a marker opens the country's panel (at the marker's section, C1).</summary>
    internal void Select(PickResult hit)
    {
        if (hit.Item is CountryPick pick) _vm.Panel.Show(pick.Iso3, pick.Focus, pick.Marker);
        else if (hit.Item is PointPick { Point.CountryIso3: { } iso } point) _vm.Panel.Show(iso, marker: point.Layer);
    }

    /// <summary>A click over the sea or space closes the panel.</summary>
    internal void ClearSelection()
    {
        if (_vm.Panel.IsOpen) _vm.Panel.CloseCommand.Execute(null);
    }

    // ---------------------------------------------------------------- window and power

    private void OnWindowStateChanged(object? sender, EventArgs e)
    {
        if (_window.WindowState == WindowState.Minimized) return;
        _host?.RequestSceneUpdate();
        _host?.RecomposeIfStale();
    }

    private void OnPowerModeChanged(object? sender, PowerModeChangedEventArgs e)
    {
        if (e.Mode != PowerModes.Resume) return;
        _window.Dispatcher.BeginInvoke(() =>
        {
            if (_closed || _host is null) return;
            _host.DisplayTimeUtc = _clock.UtcNow;
            _host.RequestSceneUpdate();
            _ = _host.RecomposeNowAsync();
        });
    }

    // ---------------------------------------------------------------- camera (EarthWatch3D's rules)

    private static void SetView(PerspectiveCamera camera, GeoPosition at, double distance)
    {
        var (p, l, u) = CameraRules.LookDownOn(at, distance);
        camera.Position = p; camera.LookDirection = l; camera.UpDirection = u;
    }

    private double FitDistanceForViewport(PerspectiveCamera camera) =>
        CameraRules.FitDistance(camera.FieldOfView, _viewport.ActualWidth / Math.Max(1, _viewport.ActualHeight), CameraLimits.EarthDefault);

    private void ApplyRotationSensitivity(PerspectiveCamera camera) =>
        _viewport.RotationSensitivity = CameraRules.RotationSensitivity(((Vector3D)camera.Position).Length);

    /// <summary>Turns the camera at once (no animation) to look straight down on <paramref name="p"/>, keeping its distance.</summary>
    public void LookAt(GeoPosition p)
    {
        if (Camera is not { } camera) return;
        _animator.Cancel();
        SetView(camera, p, ((Vector3D)camera.Position).Length);
        _host?.RequestSceneUpdate();
    }

    private void OnCameraChanged(object? sender, RoutedEventArgs e)
    {
        if (_adjustingCamera || Camera is not { } camera) return;
        _adjustingCamera = true;
        try
        {
            var clamped = CameraRules.ClampDistance(camera.Position, CameraLimits.EarthDefault);
            if (clamped != camera.Position)
            {
                camera.Position = clamped;
                camera.LookDirection = -(Vector3D)clamped;
            }
            else if (!CameraRules.AimsAtOrigin(camera.Position, camera.LookDirection))
            {
                var v = (Vector3D)camera.Position;
                var (_, look, up) = CameraRules.LookDownOn(GeoPosition.FromVector(new Vector3d(v.X, v.Y, v.Z)), v.Length);
                camera.LookDirection = look;
                camera.UpDirection = up;
            }
            ApplyRotationSensitivity(camera);
        }
        finally { _adjustingCamera = false; }
    }

    private void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        e.Handled = true;
        _animator.Cancel();
        if (Camera is not { } camera) return;
        double notches = e.Delta / 120.0;
        var pos = (Vector3D)camera.Position;
        var next = CameraRules.ClampDistance(new Point3D(0, 0, 0) + pos * Math.Pow(CameraLimits.EarthDefault.ZoomPerNotch, -notches), CameraLimits.EarthDefault);
        camera.Position = next;
        camera.LookDirection = -(Vector3D)next;
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        // Esc closes the open card (Welcome, About, Data) first, then the explainer card, then the panel (Plan 4 Task 9; Plan 5 Task 4).
        if (e.Key == Key.Escape && _vm.Cards.CloseTop())
        {
            e.Handled = true;
            return;
        }
        if (e.Key == Key.Escape && _vm.Explainer.IsOpen)
        {
            e.Handled = true;
            _vm.Explainer.CloseCommand.Execute(null);
            return;
        }
        if (e.Key == Key.Escape && _vm.Panel.IsOpen)
        {
            e.Handled = true;
            ClearSelection();
            return;
        }
        if (e.Key != Key.Home) return;
        e.Handled = true;
        if (Camera is not { } camera) return;
        _animator.AnimateTo(CameraRules.LookDownOn(HomeView, FitDistanceForViewport(camera)).Position);
    }

    /// <summary>A double-click flies to the point under it (a single click is the host's: it selects).</summary>
    private void OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left || e.ClickCount != 2) return;
        e.Handled = true;
        if (Camera is not { } camera) return;
        var projector = new Projector(camera, _viewport.ActualWidth, _viewport.ActualHeight);
        if (!projector.TryHitUnitSphere(e.GetPosition(_viewport), out Point3D hit)) return;
        var geo = GeoPosition.FromVector(new Vector3d(hit.X, hit.Y, hit.Z));
        _animator.AnimateTo(CameraRules.LookDownOn(geo, ((Vector3D)camera.Position).Length).Position);
    }

    // ---------------------------------------------------------------- lifetime

    private void OnClosed(object? sender, EventArgs e)
    {
        _closed = true;
        _animator.Cancel();
        _timeTimer.Stop();
        _timeTimer.Tick -= OnTimeTick;
        SystemEvents.PowerModeChanged -= OnPowerModeChanged;
        _window.Loaded -= OnLoaded;
        _window.Closed -= OnClosed;
        _window.StateChanged -= OnWindowStateChanged;
        _window.PreviewKeyDown -= OnPreviewKeyDown;
        _viewport.CameraChanged -= OnCameraChanged;
        _viewport.PreviewMouseWheel -= OnPreviewMouseWheel;
        _viewport.PreviewMouseDown -= OnPreviewMouseDown;
        _vm.Layers.PropertyChanged -= OnLayerToggled;
        _vm.Indicators.Changed -= OnIndicatorsChanged;
        _vm.MarkerData.Changed -= OnMarkerDataChanged;
        _vm.Panel.PropertyChanged -= OnPanelChanged;
        _vm.Hubs.Changed -= OnHubsChanged;
        _vm.Settings.UnitsChanged -= OnHubsChanged;
        _vm.Settings.TextScaleChanged -= OnTextScaleChanged;
        _countries.Changed -= OnBoundariesChanged;
        _displacement.Close(); _conflict.Close(); _outbreaks.Close(); _atrocities.Close(); _labels.Close(); _food?.Close();
        _host?.Dispose();
        _sta.Dispose();
        _firstCompose.TrySetCanceled();
    }
}
