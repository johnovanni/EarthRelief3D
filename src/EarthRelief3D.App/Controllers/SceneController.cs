using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using AstroCore.Astronomy;
using AstroCore.Earth;
using EarthRelief3D.App.Layers;
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
/// <see cref="LayerHost"/> on <see cref="EarthBody.Instance"/> with the borders and the food shading in the image, the
/// displacement rings, conflict dots and outbreak crosses in the scene and the labels on the overlay, following the
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
    private readonly LabelsOverlayLayer _labels;
    private LayerHost? _host;
    private BordersImageLayer? _borders;
    private FoodImageLayer? _food;
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
        _labels = new LabelsOverlayLayer(store, vm.Layers, _countries, overlay, () => _clock.UtcNow);
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
        var host = _host = new LayerHost(_viewport, _overlayCanvas, EarthBody.Instance, baseImage, _log);
        foreach (var b in HoverBlockers) host.HoverBlockers.Add(b);
        // Scene layers in draw order (WPF draws 3D children in order): rings under dots under crosses.
        foreach (var layer in new GlobeLayer[] { _borders, _food, _displacement, _conflict, _outbreaks, _labels }) host.Add(layer);
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
        _labels.BoundariesChanged();
        _food?.BoundariesChanged();
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
        if (_host is not null && !_closed) _host.DisplayTimeUtc = _clock.UtcNow;
    }

    private void OnLayerToggled(object? sender, PropertyChangedEventArgs e)
    {
        var l = _vm.Layers;
        switch (e.PropertyName)
        {
            case nameof(LayerStripViewModel.ShowConflict): _conflict.IsVisible = l.ShowConflict; break;
            case nameof(LayerStripViewModel.ShowOutbreaks): _outbreaks.IsVisible = l.ShowOutbreaks; break;
            case nameof(LayerStripViewModel.ShowDisplacement): _displacement.IsVisible = l.ShowDisplacement; break;
            case nameof(LayerStripViewModel.ShowFood): if (_food is not null) _food.IsVisible = l.ShowFood; break;
        }
    }

    // ---------------------------------------------------------------- selection

    private void OnSelected(object? sender, PickResult hit) => Select(hit);

    private void OnSelectionCleared(object? sender, EventArgs e) => ClearSelection();

    /// <summary>A click hit something: a country or a marker opens the country's panel (at the marker's section, C1).</summary>
    internal void Select(PickResult hit)
    {
        if (hit.Item is CountryPick pick) _vm.Panel.Show(pick.Iso3, pick.Focus);
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
        _countries.Changed -= OnBoundariesChanged;
        _displacement.Close(); _conflict.Close(); _outbreaks.Close(); _labels.Close(); _food?.Close();
        _host?.Dispose();
        _sta.Dispose();
        _firstCompose.TrySetCanceled();
    }
}
