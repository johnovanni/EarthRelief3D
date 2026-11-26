using System.IO;
using System.Net.Http;
using AstroCore.Earth;
using EarthRelief3D.App.Diagnostics;
using EarthRelief3D.Core.Feeds;
using EarthRelief3D.Core.Model;
using EarthRelief3D.Core.Store;
using GlobeCore.Feeds;

namespace EarthRelief3D.Tests;

public class LaunchOptionsTests
{
    [Fact]
    public void NoArgumentsMeansNoDiagnostics()
    {
        var o = LaunchOptions.Parse([]);
        Assert.Equal(new LaunchOptions(), o);
        Assert.False(o.IsDiagnosticRun);
        Assert.False(o.TakesPictures);
    }

    [Fact]
    public void ParsesSceneOptions()
    {
        var o = LaunchOptions.Parse(["--offline-samples", @"tests\Samples", "--snapshot-scenes", "s.json", "--snapshot-out", "out",
            "--display-time", "2026-11-22T12:00:00Z", "--delay-ms", "800"]);
        Assert.Equal(@"tests\Samples", o.OfflineSamplesDir);
        Assert.Equal("s.json", o.SnapshotScenesFile);
        Assert.Equal("out", o.SnapshotOutDir);
        Assert.Equal(new DateTime(2026, 11, 22, 12, 0, 0, DateTimeKind.Utc), o.DisplayTimeUtc);
        Assert.Equal(800, o.DelayMs);
        Assert.True(o.TakesPictures);
        Assert.True(o.IsDiagnosticRun);
        Assert.Null(o.Error);
    }

    [Fact]
    public void ParsesASingleSnapshot()
    {
        var o = LaunchOptions.Parse(["--snapshot", @"C:\tmp\a b.png", "--look-at", "15,30", "--layers", "Conflict, food", "--open-panel", "sdn"]);
        Assert.Equal(@"C:\tmp\a b.png", o.SnapshotPath);
        Assert.Equal(new GeoPosition(15, 30), o.LookAt);
        Assert.Equal(["conflict", "food"], o.Layers);
        Assert.Equal("SDN", o.OpenPanel);
        Assert.Null(o.Error);
    }

    [Theory]
    [InlineData("--snapshot-scenes", "s.json")]
    [InlineData("--snapshot-out", "out")]
    public void ScenesAndOutAreRejectedAlone(string option, string value) =>
        Assert.Equal("--snapshot-scenes and --snapshot-out go together: give both, or neither.", LaunchOptions.Parse([option, value]).Error);

    [Theory]
    [InlineData("--display-time", "yesterday")]
    [InlineData("--display-time", "2026-11-22T12:00:00")]   // no zone: refused, not guessed
    [InlineData("--look-at", "91,0")]
    [InlineData("--look-at", "15")]
    [InlineData("--layers", "conflict,rivers")]
    [InlineData("--open-panel", "SD")]
    public void BadValuesAreReported(string option, string value) => Assert.NotNull(LaunchOptions.Parse([option, value]).Error);

    [Fact]
    public void MissingValuesAreReportedAndUnknownArgumentsIgnored()
    {
        Assert.StartsWith("--offline-samples needs a value", LaunchOptions.Parse(["--offline-samples", "--snapshot-out", "x"]).Error);
        Assert.Null(LaunchOptions.Parse(["--whatever", "--delay-ms", "soon"]).Error);
    }
}

public partial class SnapshotScenesTests
{
    private static string ScenesFile => Path.Combine(AppContext.BaseDirectory, "SnapshotScenes", "scenes.json");

    [Fact]
    public void TheCommittedScenesLoad()
    {
        var scenes = SnapshotScenes.Load(ScenesFile);
        Assert.Equal(["africa", "pacific", "sudan-panel", "sudan-panel-food", "no-crisis-panel", "water", "resources", "renewables", "myanmar-panel", "dprk-camps", "access", "sudan-access", "sudan-help", "sudan-explainer-food",
            "first-start", "about", "data", "text-150", "text-150-narrow", "sudan-charities", "closed-cards", "update-offer", "update-ready", "update-stale"],
            scenes.Select(s => s.Name));
        Assert.All(scenes, s => Assert.Equal(new DateTime(2026, 11, 22, 12, 0, 0, DateTimeKind.Utc), s.DisplayTimeUtc));
        Assert.Equal(new GeoPosition(10, 25), scenes[0].LookAt);
        Assert.Equal(["outbreaks"], scenes[1].Layers);
        Assert.Equal("SDN", scenes.Single(s => s.Name == "sudan-panel").Panel);
        Assert.Equal("ISL", scenes.Single(s => s.Name == "no-crisis-panel").Panel);
        Assert.Null(scenes[0].Panel);
        Assert.Equal(EarthRelief3D.App.Presentation.CrisisLayer.Displacement, scenes.Single(s => s.Name == "sudan-panel-food").FocusLayer);

        // Plan 3 (Task 8): the five Plan 2 scenes keep their drafts off, so their pictures compare with the baseline.
        Assert.All(scenes.Take(5), s => Assert.False(s.ShowDrafts));
        var water = scenes.Single(s => s.Name == "water");
        Assert.Equal(EarthRelief3D.App.Presentation.ShadingLayer.Water, water.Shading);
        Assert.Empty(water.Markers);
        var resources = scenes.Single(s => s.Name == "resources");
        Assert.Equal(new GeoPosition(25, 50), resources.LookAt);
        Assert.Equal(EarthRelief3D.App.Presentation.ShadingLayer.OilProduction, resources.Shading);
        // Task 7 (after ruling R18): the three GEM marker layers over the oil production shading.
        Assert.Equal([EarthRelief3D.App.Presentation.MarkerLayer.PowerPlants, EarthRelief3D.App.Presentation.MarkerLayer.OilGasFields,
            EarthRelief3D.App.Presentation.MarkerLayer.SteelPlants], resources.Markers);   // R25: renewables have their own scene
        // R25: the renewables over the water-stress shading, Europe, no other markers.
        var renewables = scenes.Single(s => s.Name == "renewables");
        Assert.Equal(new GeoPosition(50, 10), renewables.LookAt);
        Assert.Equal(EarthRelief3D.App.Presentation.ShadingLayer.WaterStress, renewables.Shading);
        Assert.Equal([EarthRelief3D.App.Presentation.MarkerLayer.Renewables], renewables.Markers);
        var myanmar = scenes.Single(s => s.Name == "myanmar-panel");
        Assert.True(myanmar.ShowDrafts);
        Assert.Equal("MMR", myanmar.Panel);
        Assert.Equal(EarthRelief3D.App.Presentation.MarkerLayer.Atrocities, myanmar.FocusMarker);
        Assert.Null(myanmar.FocusLayer);
        var camps = scenes.Single(s => s.Name == "dprk-camps");
        Assert.True(camps.ShowDrafts);
        Assert.Equal(new GeoPosition(40, 127), camps.LookAt);
        Assert.Equal([EarthRelief3D.App.Presentation.MarkerLayer.Camps], camps.Markers);
        Assert.Equal(EarthRelief3D.App.Presentation.MarkerLayer.Camps, camps.FocusMarker);
        // Ruling R19: no nigeria-areas scene (the offline NGA record is projection-only; FoodAreaMapTests draws the areas).
        Assert.DoesNotContain(scenes, s => s.Name == "nigeria-areas");
    }

    [Theory]
    [InlineData("""{"scenes":[]}""", "no scenes")]
    [InlineData("""{"scenes":[{"name":"a","displayTimeUtc":"soon"}]}""", "not a UTC time")]
    [InlineData("""{"scenes":[{"name":"a","displayTimeUtc":"2026-11-22T12:00:00Z","layers":["rivers"]}]}""", "not a layer")]
    [InlineData("""{"scenes":[{"name":"a","displayTimeUtc":"2026-11-22T12:00:00Z","panel":"S"}]}""", "country code")]
    [InlineData("""{"scenes":[{"name":"a","displayTimeUtc":"2026-11-22T12:00:00Z"},{"name":"a","displayTimeUtc":"2026-11-22T12:00:00Z"}]}""", "used twice")]
    [InlineData("""{"scenes":[{"name":"a","displayTimeUtc":"2026-11-22T12:00:00Z","panelFocus":"food"}]}""", "needs a panel")]
    [InlineData("""{"scenes":[{"name":"a","displayTimeUtc":"2026-11-22T12:00:00Z","panel":"MMR","panelFocus":"oil"}]}""", "needs a panel")]
    [InlineData("""{"scenes":[{"name":"a","displayTimeUtc":"2026-11-22T12:00:00Z","showDrafts":"yes"}]}""", "true or false")]
    [InlineData("""{"scenes":[{"name":"a","displayTimeUtc":"2026-11-22T12:00:00Z","explainer":"food"}]}""", "explainer needs a panel")]
    [InlineData("""{"scenes":[{"name":"a","displayTimeUtc":"2026-11-22T12:00:00Z","panel":"SDN","explainer":"rivers"}]}""", "explainer needs a panel")]
    [InlineData("not json", "cannot read")]
    public void BadFilesGetPlainMessages(string json, string expected)
    {
        var path = Path.Combine(Path.GetTempPath(), "er3d-scenes-" + Guid.NewGuid().ToString("N") + ".json");
        File.WriteAllText(path, json);
        try { Assert.Contains(expected, Assert.Throws<FormatException>(() => SnapshotScenes.Load(path)).Message); }
        finally { File.Delete(path); }
    }

    [Fact]
    public void ScenesNameThePlan3LayersWithOneShadingAtMost()
    {
        var path = Path.Combine(Path.GetTempPath(), "er3d-scenes-" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            File.WriteAllText(path, """{"scenes":[{"name":"w","displayTimeUtc":"2026-11-22T12:00:00Z","layers":["water","power","Conflict"]}]}""");
            var s = Assert.Single(SnapshotScenes.Load(path));
            Assert.Equal(EarthRelief3D.App.Presentation.ShadingLayer.Water, s.Shading);
            Assert.Equal([EarthRelief3D.App.Presentation.MarkerLayer.Conflict, EarthRelief3D.App.Presentation.MarkerLayer.PowerPlants], s.Markers);
            File.WriteAllText(path, """{"scenes":[{"name":"w","displayTimeUtc":"2026-11-22T12:00:00Z","layers":["water","food"]}]}""");
            Assert.Contains("one shading layer", Assert.Throws<FormatException>(() => SnapshotScenes.Load(path)).Message);
        }
        finally { File.Delete(path); }
        // The Plan 2 names still mean what they did.
        var old = SnapshotScenes.Load(ScenesFile)[0];
        Assert.Equal(EarthRelief3D.App.Presentation.ShadingLayer.Food, old.Shading);
        Assert.Equal(3, old.Markers.Count);
        Assert.Null(LaunchOptions.Parse(["--layers", "oil,steel,camps"]).Error);
        Assert.NotNull(LaunchOptions.Parse(["--layers", "oil,hunger"]).Error);
    }

    [Fact]
    public void AccessFocusIsAccepted()
    {
        var path = Path.Combine(Path.GetTempPath(), "er3d-scenes-" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            File.WriteAllText(path, """
                {"scenes":[
                  {"name":"a","displayTimeUtc":"2026-11-22T12:00:00Z","layers":["access"],"panel":"sdn","panelFocus":"Access"},
                  {"name":"b","displayTimeUtc":"2026-11-22T12:00:00Z","panel":"SDN","panelFocus":"food"},
                  {"name":"c","displayTimeUtc":"2026-11-22T12:00:00Z","panel":"SDN","panelFocus":"Help"}]}
                """);
            var scenes = SnapshotScenes.Load(path);
            Assert.Equal(EarthRelief3D.App.ViewModels.PanelFocus.Access, scenes[0].FocusSection);
            Assert.Null(scenes[0].FocusLayer);
            Assert.Null(scenes[0].FocusMarker);
            Assert.Equal(EarthRelief3D.App.Presentation.ShadingLayer.Access, scenes[0].Shading);   // "access" is also a --layers name
            Assert.Null(scenes[1].FocusSection);
            Assert.Equal(EarthRelief3D.App.ViewModels.PanelFocus.Help, scenes[2].FocusSection);
            Assert.Contains("access", SnapshotScene.PanelFocusNames);
            Assert.Contains("help", SnapshotScene.PanelFocusNames);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void LegendAndStripFieldsAreRead()
    {
        var path = Path.Combine(Path.GetTempPath(), "er3d-scenes-" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            File.WriteAllText(path, """
                {"scenes":[
                  {"name":"a","displayTimeUtc":"2026-11-22T12:00:00Z"},
                  {"name":"b","displayTimeUtc":"2026-11-22T12:00:00Z","legend":"closed","strip":"closed"},
                  {"name":"c","displayTimeUtc":"2026-11-22T12:00:00Z","legend":"Open","strip":"OPEN"}]}
                """);
            var scenes = SnapshotScenes.Load(path);
            Assert.True(scenes[0].LegendOpen);   // the default: the earlier pictures still compare
            Assert.True(scenes[0].StripOpen);
            Assert.False(scenes[1].LegendOpen);
            Assert.False(scenes[1].StripOpen);
            Assert.True(scenes[2].LegendOpen);
            Assert.True(scenes[2].StripOpen);
        }
        finally { File.Delete(path); }
    }

    [Theory]
    [InlineData("""{"scenes":[{"name":"a","displayTimeUtc":"2026-11-22T12:00:00Z","legend":"folded"}]}""", "legend is \"open\" or \"closed\"")]
    [InlineData("""{"scenes":[{"name":"a","displayTimeUtc":"2026-11-22T12:00:00Z","strip":"shut"}]}""", "strip is \"open\" or \"closed\"")]
    [InlineData("""{"scenes":[{"name":"a","displayTimeUtc":"2026-11-22T12:00:00Z","legend":true}]}""", "legend is \"open\" or \"closed\"")]
    public void LegendAndStripAnyOtherValueIsRefusedPlainly(string json, string expected)
    {
        var path = Path.Combine(Path.GetTempPath(), "er3d-scenes-" + Guid.NewGuid().ToString("N") + ".json");
        File.WriteAllText(path, json);
        try { Assert.Contains(expected, Assert.Throws<FormatException>(() => SnapshotScenes.Load(path)).Message); }
        finally { File.Delete(path); }
    }

    [Fact]
    public void TextScaleAndWindowSizeAreRead()
    {
        var path = Path.Combine(Path.GetTempPath(), "er3d-scenes-" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            File.WriteAllText(path, """
                {"scenes":[
                  {"name":"a","displayTimeUtc":"2026-11-22T12:00:00Z"},
                  {"name":"b","displayTimeUtc":"2026-11-22T12:00:00Z","textScale":1.5,"windowSize":[1000,760]},
                  {"name":"c","displayTimeUtc":"2026-11-22T12:00:00Z","textScale":1.25,"windowSize":[1000,600]},
                  {"name":"d","displayTimeUtc":"2026-11-22T12:00:00Z","textScale":1}]}
                """);
            var scenes = SnapshotScenes.Load(path);
            Assert.Equal(1.0, scenes[0].TextScale);   // the default: the earlier pictures still compare
            Assert.Null(scenes[0].WindowSize);
            Assert.Equal(1.5, scenes[1].TextScale);
            Assert.Equal(new System.Windows.Size(1000, 760), scenes[1].WindowSize);
            Assert.Equal(1.25, scenes[2].TextScale);
            Assert.Equal(new System.Windows.Size(1000, 600), scenes[2].WindowSize);
            Assert.Equal(1.0, scenes[3].TextScale);
        }
        finally { File.Delete(path); }
    }

    [Theory]
    [InlineData("""{"scenes":[{"name":"a","displayTimeUtc":"2026-11-22T12:00:00Z","textScale":1.3}]}""")]
    [InlineData("""{"scenes":[{"name":"a","displayTimeUtc":"2026-11-22T12:00:00Z","textScale":"big"}]}""")]
    [InlineData("""{"scenes":[{"name":"a","displayTimeUtc":"2026-11-22T12:00:00Z","textScale":2}]}""")]
    public void BadTextScaleIsRefused(string json)
    {
        var path = Path.Combine(Path.GetTempPath(), "er3d-scenes-" + Guid.NewGuid().ToString("N") + ".json");
        File.WriteAllText(path, json);
        try
        {
            var message = Assert.Throws<FormatException>(() => SnapshotScenes.Load(path)).Message;
            Assert.Contains("Scene a: textScale is 1, 1.25 or 1.5", message);
        }
        finally { File.Delete(path); }
    }

    [Theory]
    [InlineData("[999,760]")]
    [InlineData("[1200,599]")]
    [InlineData("[1200]")]
    [InlineData("[1200,700,5]")]
    [InlineData("\"1200x700\"")]
    [InlineData("[\"a\",\"b\"]")]
    public void TooSmallWindowIsRefused(string size)
    {
        var path = Path.Combine(Path.GetTempPath(), "er3d-scenes-" + Guid.NewGuid().ToString("N") + ".json");
        File.WriteAllText(path, "{\"scenes\":[{\"name\":\"a\",\"displayTimeUtc\":\"2026-11-22T12:00:00Z\",\"windowSize\":" + size + "}]}");
        try
        {
            var message = Assert.Throws<FormatException>(() => SnapshotScenes.Load(path)).Message;
            Assert.Contains("Scene a: windowSize is [width, height], at least 1000 by 600", message);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void ExplainerFieldOpensTheCard()
    {
        var path = Path.Combine(Path.GetTempPath(), "er3d-scenes-" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            File.WriteAllText(path, """
                {"scenes":[
                  {"name":"a","displayTimeUtc":"2026-11-22T12:00:00Z","panel":"SDN","panelFocus":"food","explainer":"Food","showDrafts":true},
                  {"name":"b","displayTimeUtc":"2026-11-22T12:00:00Z","panel":"SDN"}]}
                """);
            var scenes = SnapshotScenes.Load(path);
            Assert.Equal("food", scenes[0].Explainer);
            Assert.Null(scenes[1].Explainer);
            Assert.Contains("why-these", SnapshotScene.ExplainerNames);
            Assert.Contains("access", SnapshotScene.ExplainerNames);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void PlanFourScenesAreInTheFile()
    {
        var scenes = SnapshotScenes.Load(ScenesFile);
        Assert.Equal(24, scenes.Count);   // fourteen from Plans 2 to 4, ten from Plan 5
        var access = scenes.Single(s => s.Name == "access");
        Assert.Equal(EarthRelief3D.App.Presentation.ShadingLayer.Access, access.Shading);
        var help = scenes.Single(s => s.Name == "sudan-help");
        Assert.Equal(EarthRelief3D.App.ViewModels.PanelFocus.Help, help.FocusSection);
        Assert.True(help.ShowDrafts);
        Assert.Contains(EarthRelief3D.App.Presentation.MarkerLayer.HubLines, help.Markers);
        var ex = scenes.Single(s => s.Name == "sudan-explainer-food");
        Assert.Equal("food", ex.Explainer);
        Assert.Equal("SDN", ex.Panel);
        Assert.True(ex.ShowDrafts);
        Assert.Equal(EarthRelief3D.App.ViewModels.PanelFocus.Access, scenes.Single(s => s.Name == "sudan-access").FocusSection);

        // Plan 5 (Task 11): the ten new scenes' fields.
        Assert.Equal(EarthRelief3D.App.ViewModels.PanelFocus.Charities, scenes.Single(s => s.Name == "sudan-charities").FocusSection);
        Assert.False(scenes.Single(s => s.Name == "first-start").LegendOpen);
        Assert.Equal(EarthRelief3D.App.ViewModels.AppCard.Welcome, scenes.Single(s => s.Name == "first-start").AppCard);
        Assert.Equal(1.5, scenes.Single(s => s.Name == "text-150-narrow").TextScale);
        Assert.Equal(new System.Windows.Size(1000, 760), scenes.Single(s => s.Name == "text-150-narrow").WindowSize);
        Assert.True(scenes.Single(s => s.Name == "text-150").OpensSettings);
        var closed = scenes.Single(s => s.Name == "closed-cards");
        Assert.False(closed.StripOpen);
        Assert.False(closed.LegendOpen);
        Assert.Equal(["offer", "ready", "stale"], scenes.Where(s => s.Name.StartsWith("update-", StringComparison.Ordinal)).Select(s => s.Update));
    }

    [Fact]
    public void ScenesMayFocusTheCuratedSectionsAndShowDrafts()
    {
        var path = Path.Combine(Path.GetTempPath(), "er3d-scenes-" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            File.WriteAllText(path, """
                {"scenes":[
                  {"name":"a","displayTimeUtc":"2026-11-22T12:00:00Z","panel":"pse","panelFocus":"Atrocities","showDrafts":true},
                  {"name":"c","displayTimeUtc":"2026-11-22T12:00:00Z","panel":"PRK","panelFocus":"camps","showDrafts":false},
                  {"name":"f","displayTimeUtc":"2026-11-22T12:00:00Z","panel":"SDN","panelFocus":"food"}]}
                """);
            var scenes = SnapshotScenes.Load(path);
            Assert.Equal("PSE", scenes[0].Panel);
            Assert.Equal(EarthRelief3D.App.Presentation.MarkerLayer.Atrocities, scenes[0].FocusMarker);
            Assert.True(scenes[0].ShowDrafts);
            Assert.Equal(EarthRelief3D.App.Presentation.MarkerLayer.Camps, scenes[1].FocusMarker);
            Assert.False(scenes[1].ShowDrafts);
            Assert.Equal(EarthRelief3D.App.Presentation.CrisisLayer.Food, scenes[2].FocusLayer);
            Assert.Null(scenes[2].FocusMarker);
            Assert.False(scenes[2].ShowDrafts);   // absent means off
            Assert.Equal(["conflict", "food", "outbreaks", "displacement", "atrocities", "camps", "access", "help", "charities"], SnapshotScene.PanelFocusNames);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void ASingleSnapshotBecomesOneScene()
    {
        var o = LaunchOptions.Parse(["--snapshot", @"C:\tmp\sudan.png", "--open-panel", "SDN"]);
        var s = SnapshotScenes.FromOptions(o, new DateTime(2026, 11, 22, 12, 0, 0, DateTimeKind.Utc));
        Assert.Equal("sudan", s.Name);
        Assert.Equal("SDN", s.Panel);
        Assert.Equal(LaunchOptions.LayerNames, s.Layers);
    }
}

public class OfflineSamplesTests
{
    private static string SamplesDir => Path.Combine(AppContext.BaseDirectory, "Samples");

    [Fact]
    public void EverySampleFileIsThere() => Assert.Empty(OfflineSamples.Missing(SamplesDir));

    [Fact]
    public async Task EveryFeedRunsOverTheHandlerWithoutAnUnmappedRequest()
    {
        var handler = OfflineSamples.CreateHandler(SamplesDir);
        var http = new FeedHttp("test", handler);
        var store = new CrisisStore();
        var indicators = new IndicatorStore();
        var clock = new FixedClock(new DateTime(2026, 11, 22, 12, 0, 0, DateTimeKind.Utc));
        var log = new ListLog();
        var countries = CountryResolver.LoadDefault();
        IFeed[] feeds =
        [
            new UcdpCandidateFeed(http, store, clock, log, countries),
            // With the IPC area step (P3-2), as the app runs it: the offline search finds no area package for any country.
            new IpcNationalFeed(http, store, clock, log, countries, new IpcAreaFeed(http, store, clock, log, cacheDir: null, pause: TimeSpan.Zero)),
            new WhoOutbreakFeed(http, store, clock, log, countries),
            new UnhcrDisplacementFeed(http, store, clock, log, countries),
            new GhoFeed(http, indicators, store, clock, log, countries),
            new HnoFeed(http, indicators, store, clock, log, countries),
            new AcapsAccessFeed(http, indicators, store, clock, log, countries),
            new FtsFundingFeed(http, indicators, store, clock, log, countries),
        ];
        // Every data feed of the app is here (Borders has its own test: BoundaryTests).
        Assert.Equal(EarthRelief3D.App.Services.AppServices.FeedNames.Where(n => n != "Borders"), feeds.Select(f => f.Name));
        foreach (var f in feeds) Assert.Equal(FeedRunResult.Ok, await f.RunOnceAsync(CancellationToken.None));
        Assert.Contains(log.Lines, l => l.Contains("IPC areas: 0 countries drawn by area", StringComparison.Ordinal));
        Assert.NotNull(indicators.Snapshot.Get("gho.water"));
        Assert.NotEmpty(indicators.Snapshot.Needs);

        var s = store.Snapshot;
        Assert.Contains(s.Crises, c => c is ConflictCrisis);
        Assert.Contains(s.Crises, c => c is FoodCrisis);
        Assert.Contains(s.Crises, c => c is OutbreakCrisis);
        Assert.NotEmpty(s.Displacement);
        Assert.NotEmpty(indicators.Snapshot.Access);
        Assert.NotEmpty(indicators.Snapshot.Appeals);
        Assert.Equal(["ACAPS", "FTS", "IPC", "UCDP", "UN HNO", "UNHCR", "WHO", "WHO GHO"], s.CheckedUtc.Keys.Order(StringComparer.Ordinal));
        // Every request was answered: a 404 would have thrown (the feeds treat it as a failure), and the log has no error.
        Assert.DoesNotContain(log.Lines, l => l.StartsWith("ERROR", StringComparison.Ordinal));
    }

    [Fact]
    public async Task TheHandlerAnswers404ForAnythingElse()
    {
        var h = OfflineSamples.CreateHandler(SamplesDir);
        using var client = new HttpClient(h);
        Assert.Equal(System.Net.HttpStatusCode.NotFound, (await client.GetAsync("https://example.org/")).StatusCode);
    }
}
