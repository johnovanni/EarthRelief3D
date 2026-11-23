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

public class SnapshotScenesTests
{
    private static string ScenesFile => Path.Combine(AppContext.BaseDirectory, "SnapshotScenes", "scenes.json");

    [Fact]
    public void TheCommittedScenesLoad()
    {
        var scenes = SnapshotScenes.Load(ScenesFile);
        Assert.Equal(["africa", "pacific", "sudan-panel", "sudan-panel-food", "no-crisis-panel"], scenes.Select(s => s.Name));
        Assert.All(scenes, s => Assert.Equal(new DateTime(2026, 11, 22, 12, 0, 0, DateTimeKind.Utc), s.DisplayTimeUtc));
        Assert.Equal(new GeoPosition(10, 25), scenes[0].LookAt);
        Assert.Equal(["outbreaks"], scenes[1].Layers);
        Assert.Equal("SDN", scenes.Single(s => s.Name == "sudan-panel").Panel);
        Assert.Equal("ISL", scenes.Single(s => s.Name == "no-crisis-panel").Panel);
        Assert.Null(scenes[0].Panel);
        Assert.Equal(EarthRelief3D.App.Presentation.CrisisLayer.Displacement, scenes.Single(s => s.Name == "sudan-panel-food").FocusLayer);
    }

    [Theory]
    [InlineData("""{"scenes":[]}""", "no scenes")]
    [InlineData("""{"scenes":[{"name":"a","displayTimeUtc":"soon"}]}""", "not a UTC time")]
    [InlineData("""{"scenes":[{"name":"a","displayTimeUtc":"2026-11-22T12:00:00Z","layers":["rivers"]}]}""", "not a layer")]
    [InlineData("""{"scenes":[{"name":"a","displayTimeUtc":"2026-11-22T12:00:00Z","panel":"S"}]}""", "country code")]
    [InlineData("""{"scenes":[{"name":"a","displayTimeUtc":"2026-11-22T12:00:00Z"},{"name":"a","displayTimeUtc":"2026-11-22T12:00:00Z"}]}""", "used twice")]
    [InlineData("""{"scenes":[{"name":"a","displayTimeUtc":"2026-11-22T12:00:00Z","panelFocus":"food"}]}""", "needs a panel")]
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
            new IpcNationalFeed(http, store, clock, log, countries),
            new WhoOutbreakFeed(http, store, clock, log, countries),
            new UnhcrDisplacementFeed(http, store, clock, log, countries),
            new GhoFeed(http, indicators, store, clock, log, countries),
            new HnoFeed(http, indicators, store, clock, log, countries),
        ];
        foreach (var f in feeds) Assert.Equal(FeedRunResult.Ok, await f.RunOnceAsync(CancellationToken.None));
        Assert.NotNull(indicators.Snapshot.Get("gho.water"));
        Assert.NotEmpty(indicators.Snapshot.Needs);

        var s = store.Snapshot;
        Assert.Contains(s.Crises, c => c is ConflictCrisis);
        Assert.Contains(s.Crises, c => c is FoodCrisis);
        Assert.Contains(s.Crises, c => c is OutbreakCrisis);
        Assert.NotEmpty(s.Displacement);
        Assert.Equal(["IPC", "UCDP", "UN HNO", "UNHCR", "WHO", "WHO GHO"], s.CheckedUtc.Keys.Order(StringComparer.Ordinal));
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
