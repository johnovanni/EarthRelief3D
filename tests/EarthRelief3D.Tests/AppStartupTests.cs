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
            // Task 6: the bundled extracts list their codes with no country; EIA's are all zero producers, recorded in its
            // manifest and not listed (ruling R16).
            Assert.Equal(cached, WithoutBundled(services.Store.Snapshot));
            Assert.DoesNotContain(services.Store.Snapshot.Unplaced, u => u.Source == "US EIA");
            Assert.Equal(4, services.Store.Snapshot.CheckedUtc.Count);   // the cache keeps when each source was checked
            Assert.Equal(4, vm.Legend.Lines.Count);
            Assert.All(vm.Legend.Lines, l => Assert.NotEqual(LegendViewModel.NotChecked, l.AsOf));
            Assert.Equal("as of 30 Jun 2026", vm.Legend.Lines.Single(l => l.Layer == CrisisLayer.Displacement).AsOf);
            vm.Panel.Show("SDN");
            // The four crisis sections, then the needs sections and Plan 4's Getting help in and appeals sections (nothing loaded for them yet). Since the owner approved
            // the curated rows (2026-11-24), Sudan also has its atrocity sections (current and history).
            Assert.Equal(4, vm.Panel.Sections.Count(s => s.Heading != CountryPanelViewModel.BasicNeedsHeading
                && s.Heading != CountryPanelViewModel.ResourcesHeading && s.Heading != CountryPanelViewModel.GettingHelpHeading && s.Heading != CountryPanelViewModel.StepHeading
                && !s.Heading.StartsWith(CountryPanelViewModel.FundingNotLoadedHeading, StringComparison.Ordinal)
                && !s.Heading.StartsWith("People in need", StringComparison.Ordinal)
                && !s.Heading.StartsWith(CountryPanelViewModel.AtrocityHeading, StringComparison.Ordinal)));
            Assert.Contains(vm.Panel.Sections, s => s.Heading == CountryPanelViewModel.AtrocityHeading);
            // Plan 4 Task 6: Sudan has a current crisis, so "Your one small step" follows. The shipped funds, contacts and all
            // sixteen charities are approved (2026-11-25): a fund, the charities that work in Sudan, the contacts.
            var step = Assert.Single(vm.Panel.Sections, s => s.Heading == CountryPanelViewModel.StepHeading);
            Assert.Contains(step.Entries!, e => e.Title.StartsWith("UN pooled fund for Sudan", StringComparison.Ordinal));
            Assert.DoesNotContain(step.Entries!, e => e.Lines.Contains("No vetted charities listed yet"));
            Assert.Contains(step.Entries!, e => e.Group == "Give" && e.Title == "International Rescue Committee");
            Assert.Contains(step.Entries!, e => e.Group == "Give" && e.Title == "Doctors Without Borders USA");   // second batch, approved
            Assert.Contains(step.Entries!, e => e.Group == "Contact");

            // One dot per source: the feeds grey until each reports, then the bundled extracts (Task 6), green from the start.
            Assert.Equal(AppServices.StatusDotNames, vm.Status.Dots.Select(d => d.Name));
            Assert.Equal(["UCDP", "IPC", "WHO", "UNHCR", "WHO GHO", "UN HNO", "ACAPS", "FTS", "Borders"], AppServices.FeedNames);
            Assert.Equal([.. AppServices.FeedNames, "FAO", "US EIA", "WRI Aqueduct", "GEM", "Hubs"], AppServices.StatusDotNames);
            var feedDots = vm.Status.Dots.Take(AppServices.FeedNames.Count).ToList();
            var bundledDots = vm.Status.Dots.Skip(AppServices.FeedNames.Count).ToList();
            Assert.All(feedDots, d => Assert.Equal(FeedState.NotStarted, d.State));
            Assert.All(bundledDots, d =>
            {
                Assert.Equal(FeedState.Ok, d.State);
                Assert.EndsWith(" · " + BundledDataset.StatusNote, d.Tooltip);
            });
            Assert.EndsWith("3-year averages, latest ending 2025 · bundled; updated with the app", bundledDots[0].Tooltip);
            Assert.EndsWith("year 2025 · bundled; updated with the app", bundledDots[1].Tooltip);
            Assert.DoesNotContain("estimate", bundledDots[1].Tooltip);   // C4: EIA's figures are full years
            Assert.EndsWith("baseline 1979–2019 (published 2023) · bundled; updated with the app", bundledDots[2].Tooltip);
            // The bundled series are in the store at once, offline.
            Assert.Equal(51.4, services.Indicators.Snapshot.Get("fao.undernourishment")!.For("HTI")!.Value);
            Assert.NotNull(services.Indicators.Snapshot.Get("fao.energyAdequacy"));
            Assert.NotNull(services.Indicators.Snapshot.Get("eia.crudeOil"));
            Assert.NotNull(services.Indicators.Snapshot.Get("wri.bws"));

            // Every feed fails (no network): amber dots, and the cached data stays. The bundled dots stay green.
            await services.Scheduler.TickAsync(default);
            Assert.All(vm.Status.Dots.Take(AppServices.FeedNames.Count), d =>
            {
                Assert.Equal(FeedState.Retrying, d.State);
                Assert.Same(StatusDotsBrushes.Retrying, d.Brush);
            });
            Assert.All(vm.Status.Dots.Skip(AppServices.FeedNames.Count), d => Assert.Equal(FeedState.Ok, d.State));
            Assert.Equal(cached, WithoutBundled(services.Store.Snapshot));
            Assert.Equal(4, vm.Legend.Lines.Count(l => l.AsOf != LegendViewModel.NotChecked));
            vm.Panel.Show("ISL");
            Assert.Equal(CountryPanelViewModel.NoCrisisText, vm.Panel.Headline);
        }
        finally { Delete(root); }
    }

    /// <summary>The snapshot without the bundled extracts' "not placed" rows (Task 6), to compare with a cached one.</summary>
    private static CrisisSnapshot WithoutBundled(CrisisSnapshot s) =>
        s with { Unplaced = [.. s.Unplaced.Where(u => !AppServices.BundledDatasets.Any(d => d.Name == u.Source) && u.Source != ResourceMarkers.DotName && u.Source != HubMarkers.DotName)] };

    /// <summary>A log that also records the thread each line was written on.</summary>
    private sealed class ThreadLog : GlobeCore.Logging.IAppLog
    {
        private readonly object _gate = new();
        private readonly List<(string Line, int Thread)> _entries = [];
        public IReadOnlyList<(string Line, int Thread)> Entries { get { lock (_gate) return [.. _entries]; } }
        public IEnumerable<string> Lines => Entries.Select(e => e.Line);
        private void Add(string m) { lock (_gate) _entries.Add((m, Environment.CurrentManagedThreadId)); }
        public void Info(string m) => Add("INFO " + m);
        public void Warn(string m, Exception? e = null) => Add("WARN " + m);
        public void Error(string m, Exception? e = null) => Add("ERROR " + m);
    }

    [Fact]
    public async Task CachedIpcAreasLoadOffTheUiThreadAndReachTheMap()
    {
        var root = TempRoot();
        try
        {
            // The last run: Nigeria's national record (crises.json) and its accepted areas (cache\ipc-areas).
            Directory.CreateDirectory(root.CacheDir);
            Assert.True(new CrisisCache(root, new ListLog()).Save(new CrisisSnapshot([IpcAreaFixture.National()], [], [])));
            var (http, _) = IpcAreaFixture.Http();
            var lastRun = new CrisisStore();
            await new IpcAreaFeed(http, lastRun, new FixedClock(IpcAreaFixture.Now), new ListLog(), root.CacheDir, TimeSpan.Zero)
                .RunAsync([IpcAreaFixture.National()], CancellationToken.None);
            var expected = Assert.Single(lastRun.Snapshot.IpcAreas);

            var offline = new FeedHttp("test", new SampleHttpHandler());   // every request answers 404
            var log = new ThreadLog();
            int uiThread = Environment.CurrentManagedThreadId;
            using var services = AppServices.Create(root, offline, new FixedClock(IpcAreaFixture.Now), log, new MemorySettingsStore(),
                dispatcher: null, ipcAreaPause: TimeSpan.Zero);
            await services.IpcAreasLoaded.WaitAsync(TimeSpan.FromSeconds(30));

            Assert.Equal(expected, Assert.Single(services.Store.Snapshot.IpcAreas));
            Assert.Contains(log.Lines, l => l.Contains("1 countries loaded from the cache (NGA)"));
            // Parsed and written on a pool thread, not on the thread that called Create (the UI thread in the app).
            var loaded = Assert.Single(log.Entries, e => e.Line.Contains("loaded from the cache"));
            Assert.NotEqual(uiThread, loaded.Thread);
            // The map draws Nigeria by area.
            Assert.Same(services.Store.Snapshot.IpcAreas[0], Assert.Single(FoodImageLayer.Plan(services.Store.Snapshot.AsSeenAt(IpcAreaFixture.Now))).Areas);

            // The feeds fail offline (IPC's national file first, so no area step): the cached areas stay.
            await services.Scheduler.TickAsync(default);
            Assert.Single(services.Store.Snapshot.IpcAreas);
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
                Assert.Null(services.Indicators.Snapshot.Get("gho.water"));
                services.Indicators.Replace("gho.water", CountryValueLayerTests.Water(new CountryValue("SDN", 40, 2024, CountryValueLayerTests.Gho)));
                Assert.Same(services.Indicators, services.Main.Indicators);
                services.Shutdown();
            }
            Assert.Equal(TestData.Snap(), new CrisisCache(root, new ListLog()).Load());
            // The country values are cached too, and the next start shows them at once.
            Assert.Equal(40, new IndicatorCache(root, new ListLog()).Load()!.Get("gho.water")!.For("SDN")!.Value);
            using (var again = AppServices.Create(root, new FeedHttp("test", new SampleHttpHandler()), new FixedClock(SampleStore.Now), new ListLog(),
                       new MemorySettingsStore(), dispatcher: null))
                Assert.Equal(40, again.Indicators.Snapshot.Get("gho.water")!.For("SDN")!.Value);
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
            // Plan 3: one country-value layer per shading of the Needs and Resources groups (one shown at a time).
            .. CountryValueLayer.Layers.Select(l => (nameof(CountryValueLayer), CountryValueLayer.OrderOf(l))),
            (nameof(DisplacementSceneLayer), DisplacementSceneLayer.Order),
            (nameof(ConflictSceneLayer), ConflictSceneLayer.Order),
            (nameof(OutbreakSceneLayer), OutbreakSceneLayer.Order),
            // Plan 3: the curated atrocity glyphs (Task 5), between the outbreak crosses and the point markers.
            (nameof(AtrocityLayer), AtrocityLayer.Order),
            // Plan 3: the marker layers drawn from lists (camps, power plants, oil and gas fields, steel plants).
            .. PointMarkerLayer.Layers.Select(l => (nameof(PointMarkerLayer), PointMarkerLayer.OrderOf(l))),
            // Plan 4 Task 5: the arcs to the nearest hubs, above every point marker (final review F2); their labels above the country labels.
            (nameof(HubArcLayer), HubArcLayer.Order),
            (nameof(LabelsOverlayLayer), LabelsOverlayLayer.Order),
            (nameof(HubLabelsLayer), HubLabelsLayer.Order),
        ], host.Layers.Select(l => (l.GetType().Name, l.ZOrder)));
        Assert.True(BordersImageLayer.Order < FoodImageLayer.Order && FoodImageLayer.Order < DisplacementSceneLayer.Order);
        Assert.Equal(6 + CountryValueLayer.Layers.Count + PointMarkerLayer.Layers.Count, host.Layers.Count(l => l is IPickable));
        Assert.True(OutbreakSceneLayer.Order < AtrocityLayer.Order && AtrocityLayer.Order < PointMarkerLayer.OrderOf(MarkerLayer.Camps));
        Assert.All(Enum.GetValues<MarkerLayer>(), l => Assert.True(PointMarkerLayer.OrderOf(l) < HubArcLayer.Order, $"{l} draws over the arcs"));
        Assert.True(HubArcLayer.Order < LabelsOverlayLayer.Order);
        Assert.False(host.Layers.OfType<AtrocityLayer>().Single().IsVisible);   // off by default, as the other Plan 3 markers
        Assert.Equal(8, CountryValueLayer.Layers.Count);
        Assert.Equal([MarkerLayer.Camps, MarkerLayer.PowerPlants, MarkerLayer.OilGasFields, MarkerLayer.SteelPlants, MarkerLayer.Renewables,
            MarkerLayer.UnDepots, MarkerLayer.Airports, MarkerLayer.Ports], PointMarkerLayer.Layers);
        Assert.All(host.Layers.OfType<CountryValueLayer>(), l => Assert.False(l.IsVisible));   // food is today's shading
        Assert.All(host.Layers.OfType<PointMarkerLayer>(), l => Assert.False(l.IsVisible));

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
        Assert.Equal([new FoodArea("SOM", null, LayerStyle.IpcPhaseColor(4), Faint: LayerStyle.IpcPhaseColor(4))], FoodImageLayer.Plan(rig.Store.Snapshot.Crises));
        Assert.NotNull(host.Layers.OfType<FoodImageLayer>().Single().Current);

        rig.Scene.LookAt(new GeoPosition(5, 46));   // inside Somalia, away from its edges
        var hit = host.Pick(new Point(400, 300), hover: true);
        Assert.NotNull(hit);
        Assert.Equal(FoodImageLayer.LayerId, hit.LayerId);
        Assert.Equal(new CountryPick("SOM", CrisisLayer.Food), hit.Item);
        Assert.Contains("not shaded (no phase covers 20%) · faint Phase 4 tint and outline: some people are in Phase 4", hit.Tooltip);
    });

    [Fact]
    public void ShadingAndMarkerTogglesDriveThePlan3Layers() => StaAsync.Run(async () =>
    {
        var rig = await StartAsync();
        var host = rig.Scene.Host!;
        var food = host.Layers.OfType<FoodImageLayer>().Single();
        var water = host.Layers.OfType<CountryValueLayer>().Single(l => l.Which == ShadingLayer.Water);
        Assert.Equal("water", water.Id);

        // One shading at a time: water on, food off.
        rig.Vm.Layers.Shading = ShadingLayer.Water;
        Assert.True(water.IsVisible);
        Assert.False(food.IsVisible);
        Assert.Single(host.Layers.OfType<ImageLayer>(), l => l.IsVisible && l is not BordersImageLayer);
        await StaAsync.WaitUntil(() => host.IsComposeIdle);
        Assert.Null(water.Current);   // R2: no data yet, nothing drawn

        rig.Vm.Indicators.Replace("gho.water", CountryValueLayerTests.Water(new CountryValue("SOM", 45, 2024, CountryValueLayerTests.Gho)));
        await StaAsync.WaitUntil(() => water.Current is not null && host.IsComposeIdle);
        rig.Scene.LookAt(new GeoPosition(5, 46));   // inside Somalia
        var hit = host.Pick(new Point(400, 300), hover: true);
        Assert.NotNull(hit);
        Assert.Equal("water", hit.LayerId);
        Assert.Equal("Somalia · Share of people without at least basic drinking water, 2024: 45.0% · WHO GHO", hit.Tooltip);
        Assert.Equal(new CountryPick("SOM", null), hit.Item);

        // Markers stack: camps on with conflict; a click on a camp opens its country's panel.
        var camps = host.Layers.OfType<PointMarkerLayer>().Single(l => l.Which == MarkerLayer.Camps);
        rig.Vm.Layers.Show(MarkerLayer.Camps, true);
        Assert.True(camps.IsVisible);
        Assert.True(host.Layers.OfType<ConflictSceneLayer>().Single().IsVisible);
        rig.Vm.MarkerData.Replace(MarkerLayer.Camps, new PointSet(
            [new PointItem(new GeoPosition(5, 46), 6, "Camp · test row", "SOM")], TestData.Cite("Test")));
        host.RenderFrame();
        Assert.Equal(1, camps.MarkerCount);
        var campHit = host.Pick(new Point(400, 300), hover: false);
        Assert.NotNull(campHit);
        Assert.Equal("camps", campHit.LayerId);
        rig.Scene.Select(campHit);
        Assert.True(rig.Vm.Panel.IsOpen);
        Assert.Equal("Somalia", rig.Vm.Panel.CountryName);

        rig.Vm.Layers.ShowFood = true;
        Assert.False(water.IsVisible);
        Assert.True(food.IsVisible);
        // C1: a hidden shading lets its image go (a 4096×2048 image is about 32 MB), and builds it again when shown.
        Assert.Null(water.Current);
        rig.Vm.Layers.Shading = ShadingLayer.Water;
        await StaAsync.WaitUntil(() => water.Current is not null && host.IsComposeIdle);
        Assert.NotNull(water.Current);
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
            Assert.Contains(LayerStripViewModel.GettingHelpGroup, texts);
            // Plan 3 and 4: the Needs, Resources and Getting help in groups list their layers. R20: with none of their layers on they start closed,
            // their headers saying so; opened, they list their toggles.
            Assert.Contains("Needs", texts);
            Assert.Contains("Resources", texts);
            Assert.DoesNotContain("Water", texts);
            // (Getting help in starts open: the lines to the nearest hubs are on by default, R3.)
            Assert.Equal(2, Descendants<TextBlock>(root).Count(t => t.Text == "closed" && t.Visibility == Visibility.Visible));
            foreach (var g in vm.Layers.Groups) g.IsOpen = true;
            root.UpdateLayout();
            texts = Descendants<TextBlock>(root).Select(t => t.Text).ToList();
            Assert.Contains("Water", texts);
            Assert.Contains("Steel plants", texts);
            Assert.Equal(0, Descendants<TextBlock>(root).Count(t => t.Text == "closed" && t.Visibility == Visibility.Visible));
            Assert.Contains(texts, t => t.EndsWith(LegendViewModel.BoundaryNote, StringComparison.Ordinal));
            // The four sources, the two needs feeds, the two getting-help feeds (Plan 4), Borders, the three bundled extracts (Task 6), GEM's (Task 7) and the hubs' (Plan 4 Task 4),
            // and the two radio buttons of Settings' "Distances" (Task 5).
            Assert.Equal(16, Descendants<System.Windows.Shapes.Ellipse>(root).Count());
            Assert.Contains(texts, t => t.StartsWith("Borders: Natural Earth 5.1.2 (2022-05-13); Crimea shown as part of Ukraine", StringComparison.Ordinal));
            // R17: the food section's phase bar, its caption, its rows, and the legend's famine note.
            var bar = Assert.Single(Descendants<EarthRelief3D.App.Views.PhaseBarView>(root));
            Assert.True(bar.ActualWidth > 200);
            Assert.StartsWith("IPC phase bar for Sudan, of 47,535,794 people analysed (the phase numbers add up to 30 more): ",
                System.Windows.Automation.AutomationProperties.GetName(bar));
            Assert.Contains("The line falls in Phase 3, so Sudan is shaded Crisis, with a Phase 5 outline.", texts);
            // The caption wraps inside the panel (implicit TextBlock styles do not reach into the bar's template).
            var caption = Descendants<TextBlock>(root).Single(t => t.Text.StartsWith("The line falls in", StringComparison.Ordinal));
            Assert.Equal(TextWrapping.Wrap, caption.TextWrapping);
            Assert.True(caption.ActualHeight > 1.5 * caption.FontSize * 1.2, $"caption height {caption.ActualHeight}: one line, cut off");
            Assert.Contains("People by IPC phase, of 47,535,794 people analysed (the phase numbers add up to 30 more)", texts);
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
    public void AFocusedSectionOpensAtItsTop() => StaAsync.Run(async () =>
    {
        // R20: a scene's focus and a marker click both open the panel with the section's heading at the top of the view,
        // never mid-section (BringIntoView only scrolled the section's end into view).
        var store = await SampleStore.LoadAsync();
        var countries = CountryResolver.LoadDefault();
        // An empty indicator store still gives the needs sections ("Data not loaded yet"), so displacement is not the last.
        var vm = new MainViewModel(store, new MemorySettingsStore(), iso => countries.ByIso3(iso)?.Name, dispatcher: null,
            () => SampleStore.Now, countries.Boundaries, new IndicatorStore());
        var window = new MainWindow(vm, store, countries, new FixedClock(SampleStore.Now), new ListLog());
        var root = (FrameworkElement)window.Content;
        root.Measure(new Size(1200, 760));
        root.Arrange(new Rect(0, 0, 1200, 760));
        root.UpdateLayout();
        var panel = Descendants<EarthRelief3D.App.Views.CountryPanel>(root).Single();
        var scroller = (ScrollViewer)panel.FindName("Scroller");
        var list = (ItemsControl)panel.FindName("SectionList");

        foreach (var layer in new[] { CrisisLayer.Displacement, CrisisLayer.Conflict, CrisisLayer.Food, CrisisLayer.Displacement })
        {
            vm.Panel.Show("SDN", layer);
            root.UpdateLayout();
            await Task.Delay(50);   // the panel scrolls at Loaded priority
            root.UpdateLayout();
            Assert.True(vm.Panel.FocusedSectionIndex >= 0);
            var item = (FrameworkElement)list.ItemContainerGenerator.ContainerFromIndex(vm.Panel.FocusedSectionIndex);
            var y = item.TranslatePoint(new Point(0, 0), scroller).Y;
            Assert.True(scroller.ScrollableHeight > 0, "the Sudan panel should scroll");
            bool atEnd = Math.Abs(scroller.VerticalOffset - scroller.ScrollableHeight) < 1;
            Assert.True(Math.Abs(y) < 1 || (atEnd && y > 0), $"{layer}: the section's top is {y:0} px below the view's top (offset {scroller.VerticalOffset:0} of {scroller.ScrollableHeight:0})");
            if (layer == CrisisLayer.Displacement) Assert.False(atEnd, "pick a section the panel can scroll to the top of");
        }
    });

    [Fact]
    public void TheExplainerCardIsOpaqueAndNeverCoversTheLayerStrip() => StaAsync.Run(async () =>
    {
        // Final review F4: the card is opaque (the legend's text showed through). And at a narrow width it shrinks rather than
        // cover the layer strip; the window has a minimum width that keeps it readable.
        var store = await SampleStore.LoadAsync();
        var countries = CountryResolver.LoadDefault();
        var vm = new MainViewModel(store, new MemorySettingsStore(), iso => countries.ByIso3(iso)?.Name, dispatcher: null,
            () => SampleStore.Now, countries.Boundaries, new IndicatorStore(),
            curated: new EarthRelief3D.App.Curated.CuratedStore(EarthRelief3D.Core.Curated.CuratedTables.Load(Path.Combine(AppContext.BaseDirectory, "data", "curated")), showDrafts: true));
        var window = new MainWindow(vm, store, countries, new FixedClock(SampleStore.Now), new ListLog());
        var root = (FrameworkElement)window.Content;
        vm.Panel.Show("SDN");
        Assert.True(vm.Panel.Explainer.Open("food"));
        var card = Descendants<EarthRelief3D.App.Views.ExplainerCard>(root).Single();
        var strip = Descendants<EarthRelief3D.App.Views.LayerStrip>(root).Single();
        var border = (Border)card.FindName("Card");
        Assert.Equal(255, ((System.Windows.Media.SolidColorBrush)border.Background).Color.A);

        Assert.True(window.MinWidth >= 900, $"MinWidth {window.MinWidth}");
        foreach (var width in new[] { 1200.0, window.MinWidth })
        {
            root.Measure(new Size(width, 760));
            root.Arrange(new Rect(0, 0, width, 760));
            root.UpdateLayout();
            await Task.Delay(20);
            root.UpdateLayout();
            double stripRight = strip.TranslatePoint(new Point(strip.ActualWidth, 0), root).X;
            double cardLeft = card.TranslatePoint(new Point(0, 0), root).X;
            Assert.True(cardLeft >= stripRight, $"at {width} px the card starts at {cardLeft:0}, over the strip that ends at {stripRight:0}");
            Assert.True(card.ActualWidth >= 250, $"at {width} px the card is only {card.ActualWidth:0} px wide");
            Assert.True(card.ActualWidth <= 420);
        }
    });

    [Fact]
    public void ARebuildKeepsWhereThePanelWasScrolledTo() => StaAsync.Run(async () =>
    {
        // Final review F1: a change of unit (or state, or a feed refresh) writes the open panel again; the view keeps its
        // scroll offset instead of jumping back to the top or to the focused section.
        var store = await SampleStore.LoadAsync();
        var countries = CountryResolver.LoadDefault();
        var vm = new MainViewModel(store, new MemorySettingsStore(), iso => countries.ByIso3(iso)?.Name, dispatcher: null,
            () => SampleStore.Now, countries.Boundaries, new IndicatorStore());
        var window = new MainWindow(vm, store, countries, new FixedClock(SampleStore.Now), new ListLog());
        var root = (FrameworkElement)window.Content;
        root.Measure(new Size(1200, 760));
        root.Arrange(new Rect(0, 0, 1200, 760));
        root.UpdateLayout();
        var panel = Descendants<EarthRelief3D.App.Views.CountryPanel>(root).Single();
        var scroller = (ScrollViewer)panel.FindName("Scroller");

        vm.Panel.Show("SDN", CrisisLayer.Conflict);
        root.UpdateLayout();
        await Task.Delay(50);
        root.UpdateLayout();
        Assert.True(scroller.ScrollableHeight > 100, "the Sudan panel should scroll");
        double target = Math.Round(scroller.ScrollableHeight * 0.6);
        scroller.ScrollToVerticalOffset(target);   // the user scrolls down
        root.UpdateLayout();
        Assert.Equal(target, scroller.VerticalOffset, 0);

        vm.Settings.Units = EarthRelief3D.App.Presentation.DistanceUnit.Miles;   // rebuilds the open panel
        root.UpdateLayout();
        await Task.Delay(50);
        root.UpdateLayout();
        Assert.Equal(target, scroller.VerticalOffset, 0);

        vm.Panel.RefreshOpen();   // as a feed refresh does
        root.UpdateLayout();
        await Task.Delay(50);
        root.UpdateLayout();
        Assert.Equal(target, scroller.VerticalOffset, 0);

        vm.Panel.Show("SDN");   // a new Show (no focus) opens at the top
        root.UpdateLayout();
        await Task.Delay(50);
        root.UpdateLayout();
        Assert.Equal(0, scroller.VerticalOffset, 0);
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
