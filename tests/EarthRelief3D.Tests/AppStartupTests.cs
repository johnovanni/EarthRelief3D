using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Media3D;
using AstroCore.Earth;
using EarthRelief3D.App;
using EarthRelief3D.App.Controllers;
using EarthRelief3D.App.Layers;
using EarthRelief3D.App.Presentation;
using EarthRelief3D.App.Services;
using EarthRelief3D.App.ViewModels;
using EarthRelief3D.Core.Feeds;
using EarthRelief3D.Core.Model;
using EarthRelief3D.Core.Store;
using GlobeCore.Feeds;
using GlobeCore.Geo;
using GlobeCore.Imaging;
using GlobeCore.Storage;
using GlobeCore.Wpf.Layers;
using GlobeCore.Wpf.Status;
using HelixToolkit.Wpf;

namespace EarthRelief3D.Tests;

public class AppStartupTests
{
    private static AppDataRoot TempRoot() =>
        new(Path.Combine(Path.GetTempPath(), "er3d-start-" + Guid.NewGuid().ToString("N")));

    private static void Delete(AppDataRoot root)
    {
        try { Directory.Delete(root.Root, recursive: true); } catch (IOException) { }
    }

    [Fact]
    public async Task OfflineStartShowsCacheAndAmberDots()
    {
        var root = TempRoot();
        try
        {
            Directory.CreateDirectory(root.CacheDir);
            var cached = (await SampleStore.LoadAsync()).Snapshot;
            Assert.True(new CrisisCache(root, new ListLog()).Save(cached));
            var offline = new FeedHttp("test", new SampleHttpHandler());   // every request answers 404

            using var services = AppServices.Create(root, offline, new FixedClock(SampleStore.Now), new ListLog(),
                new MemorySettingsStore(), dispatcher: null);
            var vm = services.Main;

            // The cached snapshot is shown at once: the legend carries its dates and the panel its sections.
            Assert.True(services.CacheLoaded);
            Assert.Equal(cached, services.Store.Snapshot);
            Assert.Equal(4, services.Store.Snapshot.CheckedUtc.Count);   // the cache keeps when each source was checked
            Assert.Equal(4, vm.Legend.Lines.Count);
            Assert.All(vm.Legend.Lines, l => Assert.NotEqual(LegendViewModel.NotChecked, l.AsOf));
            Assert.Equal("as of 30 Jun 2026", vm.Legend.Lines.Single(l => l.Layer == CrisisLayer.Displacement).AsOf);
            vm.Panel.Show("SDN");
            Assert.Equal(4, vm.Panel.Sections.Count);

            // One dot per source, grey until each reports.
            Assert.Equal(AppServices.FeedNames, vm.Status.Dots.Select(d => d.Name));
            Assert.Equal(["UCDP", "IPC", "WHO", "UNHCR", "Borders"], AppServices.FeedNames);
            Assert.All(vm.Status.Dots, d => Assert.Equal(FeedState.NotStarted, d.State));

            // Every feed fails (no network): amber dots, and the cached data stays.
            await services.Scheduler.TickAsync(default);
            Assert.All(vm.Status.Dots, d =>
            {
                Assert.Equal(FeedState.Retrying, d.State);
                Assert.Same(StatusDotsBrushes.Retrying, d.Brush);
            });
            Assert.Equal(cached, services.Store.Snapshot);
            Assert.Equal(4, vm.Legend.Lines.Count(l => l.AsOf != LegendViewModel.NotChecked));
            vm.Panel.Show("ISL");
            Assert.Equal(CountryPanelViewModel.NoCrisisText, vm.Panel.Headline);
        }
        finally { Delete(root); }
    }

    [Fact]
    public async Task NoCacheStartsEmptyAndShutdownSavesTheCache()
    {
        var root = TempRoot();
        try
        {
            var offline = new FeedHttp("test", new SampleHttpHandler());
            using (var services = AppServices.Create(root, offline, new FixedClock(SampleStore.Now), new ListLog(),
                       new MemorySettingsStore(), dispatcher: null))
            {
                Assert.False(services.CacheLoaded);
                services.Main.Panel.Show("ISL");
                Assert.Equal("Not yet checked: UCDP, IPC, WHO, UNHCR", services.Main.Panel.Headline);
                Assert.All(services.Main.Legend.Lines, l => Assert.Equal(LegendViewModel.NotChecked, l.AsOf));
                services.Store.Load(TestData.Snap());
                services.Shutdown();
            }
            Assert.Equal(TestData.Snap(), new CrisisCache(root, new ListLog()).Load());
        }
        finally { Delete(root); }
    }

    /// <summary>"No sound anywhere": a message box with an icon plays a Windows sound, so none may have one.</summary>
    [Fact]
    public void NothingInTheAppPlaysASound()
    {
        var sources = Directory.EnumerateFiles(Path.Combine(Repo.Root(), "src"), "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar) && !f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar))
            .ToList();
        Assert.NotEmpty(sources);
        var pattern = new System.Text.RegularExpressions.Regex(@"MessageBoxImage\.(?!None)\w+|SystemSounds|SoundPlayer|MediaPlayer|Console\.Beep");
        var hits = sources.SelectMany(f => File.ReadLines(f).Where(l => pattern.IsMatch(l)).Select(l => Path.GetFileName(f) + ": " + l.Trim())).ToList();
        Assert.Empty(hits);
        Assert.Contains(sources, f => File.ReadAllText(f).Contains("MessageBoxImage.None"));
    }

    [Fact]
    public void AppInfoUserAgentNamesTheProjectOnly()
    {
        Assert.Equal($"EarthRelief3D/{AppInfo.Version} (+https://github.com/johnovanni/EarthRelief3D)", AppInfo.UserAgent);
        Assert.Equal("earthrelief3d-", AppInfo.LogFilePrefix);
    }

    // ---------------------------------------------------------------- the scene (STA)

    private sealed record Rig(SceneController Scene, MainViewModel Vm, HelixViewport3D Viewport, CrisisStore Store, ListLog Log);

    /// <summary>A SceneController in a window that is never shown, laid out by hand at 800x600, facing 0°N 0°E.</summary>
    private static Rig NewScene(CountryResolver? resolver = null)
    {
        var store = new CrisisStore();
        store.Load(TestData.Snap());
        var countries = resolver ?? CountryResolver.LoadDefault();
        var vm = new MainViewModel(store, new MemorySettingsStore(), iso => countries.ByIso3(iso)?.Name, dispatcher: null,
            nowUtc: () => SampleStore.Now, boundaries: countries.Boundaries);
        var viewport = new HelixViewport3D
        {
            Camera = new PerspectiveCamera(new Point3D(3, 0, 0), new Vector3D(-3, 0, 0), new Vector3D(0, 0, 1), 45),
        };
        var overlay = new Canvas();
        var grid = new Grid { Children = { viewport, overlay } };
        var window = new Window { Content = grid };
        grid.Measure(new Size(800, 600));
        grid.Arrange(new Rect(0, 0, 800, 600));
        var log = new ListLog();
        var scene = new SceneController(viewport, overlay, vm, window, store, countries, new FixedClock(SampleStore.Now), log);
        return new Rig(scene, vm, viewport, store, log);
    }

    private static async Task<Rig> StartAsync(CountryResolver? resolver = null)
    {
        var rig = NewScene(resolver);
        const int w = 64, h = 32;
        var baseImage = Bgra32Image.Create(w, h);
        Array.Fill(baseImage.Pixels, (byte)200);
        var lineImage = Bgra32Image.Create(w, h);
        for (int x = 0; x < w; x++) lineImage.Pixels[(h / 2 * w + x) * 4 + 3] = 255;
        await rig.Scene.StartHostAsync(baseImage, SparseOverlay.FromImage(lineImage));
        await StaAsync.WaitUntil(() => rig.Scene.Host!.IsComposeIdle);
        return rig;
    }

    [Fact]
    public void HostHasTheLayersInOrder() => StaAsync.Run(async () =>
    {
        var rig = await StartAsync();
        var host = rig.Scene.Host!;

        Assert.Equal(
        [
            (nameof(BordersImageLayer), BordersImageLayer.Order),
            (nameof(FoodImageLayer), FoodImageLayer.Order),
            (nameof(DisplacementSceneLayer), DisplacementSceneLayer.Order),
            (nameof(ConflictSceneLayer), ConflictSceneLayer.Order),
            (nameof(OutbreakSceneLayer), OutbreakSceneLayer.Order),
            (nameof(LabelsOverlayLayer), LabelsOverlayLayer.Order),
        ], host.Layers.Select(l => (l.GetType().Name, l.ZOrder)));
        Assert.True(BordersImageLayer.Order < FoodImageLayer.Order && FoodImageLayer.Order < DisplacementSceneLayer.Order);
        Assert.Equal(5, host.Layers.Count(l => l is IPickable));

        // Food is shaded under the borders, above the night shade (so phase colours stay true at night).
        var borders = Assert.IsAssignableFrom<IImageLayer>(host.Layers.OfType<BordersImageLayer>().Single().Current);
        var food = Assert.IsAssignableFrom<IImageLayer>(host.Layers.OfType<FoodImageLayer>().Single().Current);
        Assert.Equal(ImagePass.AboveNight, borders.Pass);
        Assert.Equal(ImagePass.AboveNight, food.Pass);
        Assert.True(food.Order < borders.Order);

        // One frame draws the markers (the snapshot has a conflict, an outbreak and a displacement figure).
        host.RenderFrame();
        Assert.True(host.Layers.OfType<ConflictSceneLayer>().Single().MarkerCount > 0);
        Assert.Equal(1, host.Layers.OfType<OutbreakSceneLayer>().Single().MarkerCount);
        Assert.Equal(1, host.Layers.OfType<DisplacementSceneLayer>().Single().MarkerCount);

        // The strip's toggles show and hide the layers.
        rig.Vm.Layers.ShowConflict = false;
        Assert.False(host.Layers.OfType<ConflictSceneLayer>().Single().IsVisible);
        rig.Vm.Layers.ShowFood = false;
        Assert.False(host.Layers.OfType<FoodImageLayer>().Single().IsVisible);
        rig.Vm.Layers.ShowConflict = true;
        Assert.True(host.Layers.OfType<ConflictSceneLayer>().Single().IsVisible);
    });

    [Fact]
    public void SwapRebuildsResolverAndRedraws() => StaAsync.Run(async () =>
    {
        var resolver = BoundaryTests.FreshResolver();
        var rig = await StartAsync(resolver);
        var host = rig.Scene.Host!;
        var borders = host.Layers.OfType<BordersImageLayer>().Single();
        var food = host.Layers.OfType<FoodImageLayer>().Single();
        var bordersBefore = borders.Current;
        var foodBefore = food.Current;
        Assert.NotNull(foodBefore);
        Assert.Equal("LUX", resolver.Resolve("Luxembourg")?.Iso3);

        // The feed thread swaps the set; nothing is awaited on the UI thread.
        await Task.Run(() => resolver.Boundaries.Swap(BoundaryTests.NextEdition(resolver.Boundaries.Set)));

        Assert.Equal("BEL", resolver.Resolve("Luxembourg")?.Iso3);   // the index is already the new one
        await StaAsync.WaitUntil(() => !ReferenceEquals(borders.Current, bordersBefore) && !ReferenceEquals(food.Current, foodBefore));
        await StaAsync.WaitUntil(() => rig.Vm.Legend.BoundaryText.Contains("9.9.9"));   // named once its lines are drawn
        await StaAsync.WaitUntil(() => host.IsComposeIdle);
        Assert.NotSame(bordersBefore, borders.Current);   // the lines were rasterized again from the new set
        Assert.NotSame(foodBefore, food.Current);   // and so was the food shading, with no change in the data

        // A click now resolves through the new index.
        rig.Scene.LookAt(new GeoPosition(49.8, 6.1));   // Luxembourg
        var hit = host.Pick(new Point(400, 300), hover: false);
        Assert.NotNull(hit);
        Assert.Equal("BEL", Assert.IsType<CountryPick>(hit.Item).Iso3);
    });

    [Fact]
    public void LegendKeepsTheDrawnEditionWhenTheRedrawFails() => StaAsync.Run(async () =>
    {
        var resolver = BoundaryTests.FreshResolver();
        var rig = await StartAsync(resolver);
        var drawn = rig.Vm.Legend.BoundaryText;
        Assert.Contains("Natural Earth 5.1.2", drawn);

        // A set whose lines cannot be rasterized: the borders drawn before stay, and so does the legend that names them.
        var broken = BoundaryTests.NextEdition(resolver.Boundaries.Set) with { Lines = [new GeoLine(new UnreadablePoints())] };
        await Task.Run(() => resolver.Boundaries.Swap(broken));
        await StaAsync.WaitUntil(() => rig.Log.Lines.ToArray().Any(l => l.StartsWith("ERROR The borders could not be redrawn", StringComparison.Ordinal)));
        Assert.Equal(drawn, rig.Vm.Legend.BoundaryText);
        Assert.DoesNotContain("9.9.9", rig.Vm.Legend.BoundaryText);
    });

    /// <summary>Points that throw when read, so rasterizing the line fails.</summary>
    private sealed class UnreadablePoints : IReadOnlyList<GeoPosition>
    {
        public GeoPosition this[int index] => throw new InvalidOperationException("unreadable point");
        public int Count => 2;
        public IEnumerator<GeoPosition> GetEnumerator() => throw new InvalidOperationException("unreadable points");
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }

    [Fact]
    public void ClickInSomalilandOpensSomalia() => StaAsync.Run(async () =>
    {
        var rig = await StartAsync();
        var host = rig.Scene.Host!;
        rig.Scene.LookAt(new GeoPosition(9.56, 44.06));   // Hargeisa
        var hit = host.Pick(new Point(400, 300), hover: false);

        Assert.NotNull(hit);
        var pick = Assert.IsType<CountryPick>(hit.Item);
        Assert.Equal("SOM", pick.Iso3);
        Assert.Contains("Somalia", hit.Tooltip);
        Assert.Contains("IPC analysis period", hit.Tooltip);   // the food layer answers first over Somalia
        rig.Scene.Select(hit);
        Assert.True(rig.Vm.Panel.IsOpen);
        Assert.Equal("Somalia", rig.Vm.Panel.CountryName);

        // A click over the sea closes the panel.
        rig.Scene.LookAt(new GeoPosition(-30, -20));
        Assert.Null(host.Pick(new Point(400, 300), hover: false));
        rig.Scene.ClearSelection();
        Assert.False(rig.Vm.Panel.IsOpen);
    });

    [Fact]
    public void ACountryThatIsOnlyOutlinedIsStillPickedByTheFoodLayer() => StaAsync.Run(async () =>
    {
        var rig = await StartAsync();
        var host = rig.Scene.Host!;
        // 15% in Phase 3 or higher, some in Phase 4: no fill, a Phase 4 outline (R17).
        rig.Store.Load(new CrisisSnapshot([FoodMapTests.Counts("SOM", 1000, 600, 250, 100, 50, 0)], [], []));
        await StaAsync.WaitUntil(() => host.IsComposeIdle);
        Assert.Equal([new FoodArea("SOM", null, LayerStyle.IpcPhaseColor(4))], FoodImageLayer.Plan(rig.Store.Snapshot.Crises));
        Assert.NotNull(host.Layers.OfType<FoodImageLayer>().Single().Current);

        rig.Scene.LookAt(new GeoPosition(5, 46));   // inside Somalia, away from its edges
        var hit = host.Pick(new Point(400, 300), hover: true);
        Assert.NotNull(hit);
        Assert.Equal(FoodImageLayer.LayerId, hit.LayerId);
        Assert.Equal(new CountryPick("SOM", CrisisLayer.Food), hit.Item);
        Assert.Contains("not shaded (under 20%)", hit.Tooltip);
    });

    private sealed class BindingErrors : System.Diagnostics.TraceListener
    {
        public List<string> Lines { get; } = [];
        public override void Write(string? message) { }
        public override void WriteLine(string? message) { if (message is not null) Lines.Add(message); }
    }

    private static IEnumerable<T> Descendants<T>(DependencyObject root) where T : DependencyObject
    {
        for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(root, i);
            if (child is T t) yield return t;
            foreach (var d in Descendants<T>(child)) yield return d;
        }
    }

    /// <summary>The real window's XAML loads, lays out and binds with no binding errors (no launch: never shown).</summary>
    [Fact]
    public void MainWindowXamlLoadsAndBindsWithoutErrors() => StaAsync.Run(async () =>
    {
        var errors = new BindingErrors();
        System.Diagnostics.PresentationTraceSources.Refresh();
        System.Diagnostics.PresentationTraceSources.DataBindingSource.Listeners.Add(errors);
        System.Diagnostics.PresentationTraceSources.DataBindingSource.Switch.Level = System.Diagnostics.SourceLevels.Warning;
        try
        {
            var store = await SampleStore.LoadAsync();
            var countries = CountryResolver.LoadDefault();
            var vm = new MainViewModel(store, new MemorySettingsStore(), iso => countries.ByIso3(iso)?.Name, dispatcher: null, boundaries: countries.Boundaries);
            var window = new MainWindow(vm, store, countries, new FixedClock(SampleStore.Now), new ListLog());
            var root = (FrameworkElement)window.Content;
            vm.Panel.Show("SDN", CrisisLayer.Food);
            root.Measure(new Size(1200, 760));
            root.Arrange(new Rect(0, 0, 1200, 760));
            root.UpdateLayout();
            await Task.Delay(50);   // the panel scrolls to its focused section at Loaded priority

            var texts = Descendants<TextBlock>(root).Select(t => t.Text).ToList();
            Assert.Contains("Sudan", texts);
            Assert.Contains(CountryPanelViewModel.ConflictHeading, texts);
            Assert.Contains(CountryPanelViewModel.DisplacementHeading, texts);
            Assert.Contains(texts, t => t.StartsWith("Source: UNHCR, Mid-year 2026 figures", StringComparison.Ordinal));
            Assert.All(vm.Legend.Lines, l => Assert.Contains(l.SourceLine, texts));
            Assert.Contains(LayerStripViewModel.ComingNote, texts);
            Assert.Contains(texts, t => t.EndsWith(LegendViewModel.BoundaryNote, StringComparison.Ordinal));
            Assert.Equal(5, Descendants<System.Windows.Shapes.Ellipse>(root).Count());   // the four sources and Borders
            Assert.Contains(texts, t => t.StartsWith("Borders: Natural Earth 5.1.2 (2022-05-13); Crimea shown as part of Ukraine", StringComparison.Ordinal));
            // R17: the food section's phase bar, its caption, its rows, and the legend's famine note.
            var bar = Assert.Single(Descendants<EarthRelief3D.App.Views.PhaseBarView>(root));
            Assert.True(bar.ActualWidth > 200);
            Assert.StartsWith("IPC phase bar for Sudan, of 47,535,824 people (the five phases add up to more than the 47,535,794 people analysed): ",
                System.Windows.Automation.AutomationProperties.GetName(bar));
            Assert.Contains("The line falls in Phase 3, so Sudan is shaded Crisis, with a Phase 5 outline.", texts);
            // The caption wraps inside the panel (implicit TextBlock styles do not reach into the bar's template).
            var caption = Descendants<TextBlock>(root).Single(t => t.Text.StartsWith("The line falls in", StringComparison.Ordinal));
            Assert.Equal(TextWrapping.Wrap, caption.TextWrapping);
            Assert.True(caption.ActualHeight > 1.5 * caption.FontSize * 1.2, $"caption height {caption.ActualHeight}: one line, cut off");
            Assert.Contains("People by IPC phase, of 47,535,824 people (the five phases add up to more than the 47,535,794 people analysed)", texts);
            Assert.Contains("Phase 5 Catastrophe/Famine", texts);
            Assert.Contains(LegendViewModel.FamineNote, texts);
            Assert.Empty(errors.Lines);

            vm.Panel.Show("ISL");
            root.UpdateLayout();
            Assert.Contains(CountryPanelViewModel.NoCrisisText, Descendants<TextBlock>(root).Select(t => t.Text));
            Assert.Empty(errors.Lines);
        }
        finally
        {
            System.Diagnostics.PresentationTraceSources.DataBindingSource.Listeners.Remove(errors);
        }
    });

    [Fact]
    public void MarkerHoverNamesSourceAndDate() => StaAsync.Run(async () =>
    {
        var rig = await StartAsync();
        var host = rig.Scene.Host!;
        rig.Scene.LookAt(new GeoPosition(15.5, 32.5));   // the first test conflict event
        host.RenderFrame();
        var hit = host.Pick(new Point(400, 300), hover: true);

        Assert.NotNull(hit);
        Assert.Equal(ConflictSceneLayer.LayerId, hit.LayerId);
        Assert.Equal(new CountryPick("SDN", CrisisLayer.Conflict), hit.Item);
        Assert.Equal("Sudan · armed conflict · 30 deaths · as of 22 Nov 2026", hit.Tooltip);
    });
}
