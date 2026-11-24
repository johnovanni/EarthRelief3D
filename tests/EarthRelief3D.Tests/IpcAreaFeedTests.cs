using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using AstroCore.Earth;
using EarthRelief3D.Core.Feeds;
using EarthRelief3D.Core.Model;
using EarthRelief3D.Core.Store;
using GlobeCore.Feeds;

namespace EarthRelief3D.Tests;

/// <summary>
/// The C-period fixture for the IPC area tests (the dispatch's carry): the Nigeria sample has only projection (P) areas, so
/// these tests use a synthetic variant made here from it: every feature's <c>ipc_period</c> set to <c>C</c>, and the national
/// CSV's "first projection" rows called "current". Everything else (titles, phases, geometry, anl_id) is the sample's.
/// </summary>
internal static class IpcAreaFixture
{
    public static readonly DateTime Now = new(2026, 11, 23, 0, 0, 0, DateTimeKind.Utc);
    public static readonly DateOnly From = new(2026, 6, 1), To = new(2026, 8, 31);
    public const string AnalysisId = "87143447";
    public const string PackageName = "nigeria-acute-food-insecurity-country-data";

    private static JsonObject Package(string file = "hdx_ipc_nga_package.json") =>
        (JsonObject)JsonNode.Parse(Samples.Text(file))!["result"]!;

    private static string ResourceUrl(string name) =>
        Package()["resources"]!.AsArray().Single(r => (string?)r!["name"] == name)!["url"]!.GetValue<string>();

    public static Uri GeoJsonUrl => new(ResourceUrl("ipc_nga.geojson"));
    public static Uri CsvUrl => new(ResourceUrl("ipc_nga_national_long_latest.csv"));

    /// <summary>The national record (the global file's row) the fixture's areas belong to.</summary>
    public static FoodCrisis National(string version = "Mar 2026", DateOnly? from = null, DateOnly? to = null) =>
        FoodMapTests.Counts("NGA", 219_354_878, 81_626_425, 100_800_922, 34_619_162, 2_289_823, 18_558) with
        {
            Id = "ipc-NGA",
            Title = "Nigeria: food insecurity",
            PeriodFrom = from ?? From,
            PeriodTo = to ?? To,
            Citations = [new SourceCitation(IpcNationalReader.Publisher, IpcNationalReader.Dataset, version, IpcNationalReader.PageUrl, "CC0",
                IpcNationalReader.Attribution, (to ?? To).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc), Now)],
        };

    /// <summary>The Nigeria sample with every feature in the current period (C); <paramref name="edit"/> changes features further.</summary>
    public static string CurrentGeoJson(Action<JsonArray>? edit = null)
    {
        var root = JsonNode.Parse(Samples.Text("ipc_nga.geojson"))!;
        var features = root["features"]!.AsArray();
        foreach (var f in features) f!["properties"]!["ipc_period"] = "C";
        edit?.Invoke(features);
        return root.ToJsonString();
    }

    /// <summary>The national CSV with its one analysis given as the current period (the sample's is the first projection).</summary>
    public static string CurrentCsv() => Samples.Text("ipc_nga_national_long_latest.csv").Replace("first projection", "current", StringComparison.Ordinal);

    /// <summary>
    /// A CKAN <c>package_search</c> answer with <c>fl=name,license_id,res_name,res_url</c> (the decided query, data-sources
    /// section 7), made from the package_show sample: the archive package <c>ipc-country-data</c> first, as HDX returns
    /// it, then the country's.
    /// </summary>
    public static string Search(string packageFile = "hdx_ipc_nga_package.json", string? licence = null)
    {
        var p = Package(packageFile);
        var resources = p["resources"]!.AsArray();
        var country = new JsonObject
        {
            ["name"] = (string?)p["name"],
            ["license_id"] = licence ?? (string?)p["license_id"],
            ["res_name"] = new JsonArray([.. resources.Select(r => (JsonNode?)JsonValue.Create((string?)r!["name"]))]),
            ["res_url"] = new JsonArray([.. resources.Select(r => (JsonNode?)JsonValue.Create((string?)r!["url"]))]),
        };
        var archive = new JsonObject
        {
            ["name"] = "ipc-country-data",
            ["license_id"] = "other-pd-nr",
            ["res_name"] = new JsonArray("All Countries-2017-2023.xlsx"),
            ["res_url"] = new JsonArray("https://data.humdata.org/dataset/x/resource/y/download/All%20Countries-2017-2023.xlsx"),
        };
        return new JsonObject
        {
            ["success"] = true,
            ["result"] = new JsonObject { ["count"] = 2, ["results"] = new JsonArray(archive, country) },
        }.ToJsonString();
    }

    public static (FeedHttp Http, SampleHttpHandler Handler) Http(string? geojson = null, string? csv = null, string? search = null)
    {
        var h = new SampleHttpHandler();
        h.MapText(IpcAreaReader.SearchUrl("NGA").ToString(), search ?? Search());
        h.MapText(GeoJsonUrl.ToString(), geojson ?? CurrentGeoJson());
        h.MapText(CsvUrl.ToString(), csv ?? CurrentCsv());
        return (new FeedHttp("test", h), h);
    }

    /// <summary>The areas of the fixture, read by the feed.</summary>
    public static async Task<IpcAreaSet> SetAsync(string? geojson = null)
    {
        var (http, _) = Http(geojson);
        var store = new CrisisStore();
        await new IpcAreaFeed(http, store, new FixedClock(Now), new ListLog()).RunAsync([National()], CancellationToken.None);
        return Assert.Single(store.Snapshot.IpcAreas);
    }

    /// <summary>A point inside <paramref name="area"/> and in no other area of <paramref name="set"/>.</summary>
    public static GeoPosition Inside(IpcAreaSet set, IpcArea area)
    {
        var pts = area.Polygons.SelectMany(p => p.Rings[0]).ToList();
        double lat0 = pts.Min(p => p.LatitudeDeg), lat1 = pts.Max(p => p.LatitudeDeg);
        double lon0 = pts.Min(p => p.LongitudeDeg), lon1 = pts.Max(p => p.LongitudeDeg);
        for (int i = 1; i < 40; i++)
            for (int j = 1; j < 40; j++)
            {
                var g = new GeoPosition(lat0 + (lat1 - lat0) * i / 40, lon0 + (lon1 - lon0) * j / 40);
                if (area.Polygons.Any(p => p.Contains(g)) && set.Areas.Where(a => a != area).All(a => !a.Polygons.Any(p => p.Contains(g))))
                    return g;
            }
        throw new InvalidOperationException("no interior point found for " + area.Name);
    }
}

/// <summary>P3-2 and ruling R4: the per-country IPC areas from HDX, used only when they belong to the national record's analysis.</summary>
public class IpcAreaFeedTests
{
    private static readonly DateTime Now = IpcAreaFixture.Now;

    private static (IpcAreaFeed Feed, CrisisStore Store, ListLog Log, SampleHttpHandler Handler) Make(string? geojson = null, string? csv = null,
        string? search = null, string? cacheDir = null, FixedClock? clock = null)
    {
        var (http, h) = IpcAreaFixture.Http(geojson, csv, search);
        var store = new CrisisStore();
        var log = new ListLog();
        return (new IpcAreaFeed(http, store, clock ?? new FixedClock(Now), log, cacheDir), store, log, h);
    }

    private static int Requests(SampleHttpHandler h, Uri url) => h.Requests.Count(r => r == url.ToString());

    [Fact]
    public async Task ReadsAreasWithPhaseAndPolygons()
    {
        var (feed, store, log, _) = Make();
        await feed.RunAsync([IpcAreaFixture.National()], CancellationToken.None);

        var set = Assert.Single(store.Snapshot.IpcAreas);
        Assert.Equal("NGA", set.Iso3);
        Assert.Equal(IpcAreaFixture.AnalysisId, set.AnalysisId);
        Assert.Equal("Mar 2026", set.AnalysisDate);
        Assert.Equal((IpcAreaFixture.From, IpcAreaFixture.To), (set.From, set.To));

        // The file's features that are areas (Polygon or MultiPolygon; the points are settlements, never areas), and their phases.
        using var doc = JsonDocument.Parse(Samples.Text("ipc_nga.geojson"));
        var fileAreas = doc.RootElement.GetProperty("features").EnumerateArray()
            .Where(f => f.GetProperty("geometry").GetProperty("type").GetString() is "Polygon" or "MultiPolygon")
            .Select(f => (Title: f.GetProperty("properties").GetProperty("title").GetString()!, Phase: f.GetProperty("properties").GetProperty("overall_phase").GetInt32()))
            .ToList();
        Assert.Equal(38, fileAreas.Count);
        Assert.Equal(fileAreas.Count, set.Areas.Count);
        Assert.Equal(fileAreas.Select(a => (a.Title, a.Phase == 0 ? (int?)null : a.Phase)), set.Areas.Select(a => (a.Name, a.Phase)));
        Assert.Equal(5, set.Areas.Count(a => a.Phase is null));   // overall_phase 0: not classified, never "no crisis"
        Assert.Equal(12, set.Areas.Count(a => a.Phase == 2));
        Assert.Equal(13, set.Areas.Count(a => a.Phase == 3));
        Assert.Equal(8, set.Areas.Count(a => a.Phase == 4));
        Assert.DoesNotContain(set.Areas, a => a.Name is "Benue Refugees" or "Monguno IDPs");   // the two Point features

        var bama = set.Areas.Single(a => a.Name == "Bama");
        Assert.Equal(237_219, bama.Population);
        Assert.Equal(5, set.Areas.Single(a => a.Name == "Mbo").Polygons.Count);   // a MultiPolygon of 5 parts
        Assert.All(set.Areas, a =>
        {
            Assert.Equal("NGA", a.Iso3);
            Assert.NotEmpty(a.Polygons);
            Assert.Equal(IpcAreaFixture.AnalysisId, a.AnalysisId);
            Assert.Equal((IpcAreaFixture.From, IpcAreaFixture.To), (a.From, a.To));
            Assert.Equal(set.Citation, a.Citation);
        });
        Assert.Equal(IpcNationalReader.Publisher, set.Citation.Publisher);
        Assert.Equal("Mar 2026", set.Citation.Version);
        Assert.Equal("CC0", set.Citation.Licence);
        Assert.Equal(new Uri("https://data.humdata.org/dataset/" + IpcAreaFixture.PackageName), set.Citation.Url);
        Assert.Contains(log.Lines, l => l.Contains("NGA") && l.Contains("38 areas") && l.Contains("2 points"));
    }

    [Fact]
    public async Task SkipsWhenAnalysisDiffersFromNationalRecord()
    {
        // The national record's period is another one than the country file's current period.
        var (feed, store, log, h) = Make();
        await feed.RunAsync([IpcAreaFixture.National(from: new DateOnly(2026, 2, 1), to: new DateOnly(2026, 5, 31))], CancellationToken.None);
        Assert.Empty(store.Snapshot.IpcAreas);
        Assert.Contains(log.Lines, l => l.Contains("NGA keeps the 20% rule") && l.Contains("1 Jun 2026–31 Aug 2026") && l.Contains("1 Feb 2026–31 May 2026"));
        Assert.Equal(0, Requests(h, IpcAreaFixture.GeoJsonUrl));   // no GeoJSON download for a period that cannot match

        // The real sample: its CSV gives only the first projection, and its areas are all P.
        (feed, store, log, _) = Make(geojson: Samples.Text("ipc_nga.geojson"), csv: Samples.Text("ipc_nga_national_long_latest.csv"));
        await feed.RunAsync([IpcAreaFixture.National()], CancellationToken.None);
        Assert.Empty(store.Snapshot.IpcAreas);
        Assert.Contains(log.Lines, l => l.Contains("NGA keeps the 20% rule") && l.Contains("no current period") && l.Contains("first projection"));

        // A current period that matches, but the areas are all projections (P).
        (feed, store, log, _) = Make(geojson: Samples.Text("ipc_nga.geojson"));
        await feed.RunAsync([IpcAreaFixture.National()], CancellationToken.None);
        Assert.Empty(store.Snapshot.IpcAreas);
        Assert.Contains(log.Lines, l => l.Contains("NGA keeps the 20% rule") && l.Contains("no current-period (C) areas") && l.Contains("38 P areas") && l.Contains("2 points"));

        // Current areas from two analyses (anl_id): which one the national record is cannot be told.
        (feed, store, log, _) = Make(geojson: IpcAreaFixture.CurrentGeoJson(fs => fs[5]!["properties"]!["anl_id"] = "99999999"));
        await feed.RunAsync([IpcAreaFixture.National()], CancellationToken.None);
        Assert.Empty(store.Snapshot.IpcAreas);
        Assert.Contains(log.Lines, l => l.Contains("NGA keeps the 20% rule") && l.Contains("87143447") && l.Contains("99999999"));

        // A stale or projection-only national record: not looked up at all.
        (feed, store, log, h) = Make();
        await feed.RunAsync([IpcAreaFixture.National() with { IsStale = true }, IpcAreaFixture.National() with { Id = "x", CountryIso3 = "SOM", ProjectionOnly = true }],
            CancellationToken.None);
        Assert.Empty(store.Snapshot.IpcAreas);
        Assert.Empty(h.Requests);
    }

    [Fact]
    public async Task DownloadsOnlyWhenAnalysisDateChanges()
    {
        var dir = Path.Combine(Path.GetTempPath(), "er3d-ipcareas-" + Guid.NewGuid().ToString("N"));
        try
        {
            var clock = new FixedClock(Now);
            var (feed, store, _, h) = Make(cacheDir: dir, clock: clock);
            await feed.RunAsync([IpcAreaFixture.National()], CancellationToken.None);
            Assert.Single(store.Snapshot.IpcAreas);
            Assert.Equal(1, Requests(h, IpcAreaFixture.GeoJsonUrl));

            // The same analysis twelve hours later: nothing is downloaded, the areas stay.
            clock.UtcNow = Now.AddHours(12);
            int before = h.Requests.Count;
            await feed.RunAsync([IpcAreaFixture.National()], CancellationToken.None);
            Assert.Equal(before, h.Requests.Count);
            Assert.Single(store.Snapshot.IpcAreas);

            // A restart: the areas come from the cache folder, without a download.
            var (restarted, store2, log2, h2) = Make(cacheDir: dir, clock: clock);
            Assert.Equal(1, restarted.LoadCached([IpcAreaFixture.National()]));
            Assert.Equal(store.Snapshot.IpcAreas, store2.Snapshot.IpcAreas);
            await restarted.RunAsync([IpcAreaFixture.National()], CancellationToken.None);
            Assert.Empty(h2.Requests);
            Assert.Single(store2.Snapshot.IpcAreas);

            // A cached file of another analysis is not used at start-up.
            var (other, store3, _, _) = Make(cacheDir: dir, clock: clock);
            Assert.Equal(0, other.LoadCached([IpcAreaFixture.National(version: "Sep 2026")]));
            Assert.Empty(store3.Snapshot.IpcAreas);

            // The national analysis date changes: the country is looked up and downloaded again (here its file still has the
            // March period, which the new record's does not match: the country keeps the 20% rule).
            await feed.RunAsync([IpcAreaFixture.National(version: "Sep 2026", from: new DateOnly(2026, 10, 1), to: new DateOnly(2027, 1, 31))],
                CancellationToken.None);
            Assert.Equal(2, h.Requests.Count(r => r == IpcAreaFixture.CsvUrl.ToString()));
            Assert.Empty(store.Snapshot.IpcAreas);
            Assert.Contains(log2.Lines, l => l.Contains("from the cache"));
        }
        finally { if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true); }
    }

    [Fact]
    public async Task NonCc0DatasetIsSkippedAndLogged()
    {
        var (feed, store, log, h) = Make(search: IpcAreaFixture.Search(licence: "cc-by-nc-sa"));
        await feed.RunAsync([IpcAreaFixture.National()], CancellationToken.None);
        Assert.Empty(store.Snapshot.IpcAreas);
        Assert.Contains(log.Lines, l => l.Contains("NGA keeps the 20% rule") && l.Contains("cc-by-nc-sa") && l.Contains("not CC0 or public domain"));
        Assert.Equal(0, Requests(h, IpcAreaFixture.GeoJsonUrl));
        Assert.Equal(0, Requests(h, IpcAreaFixture.CsvUrl));
        Assert.True(IpcAreaReader.IsPublicDomain("other-pd-nr"));
        Assert.True(IpcAreaReader.IsPublicDomain("cc-zero"));
        Assert.False(IpcAreaReader.IsPublicDomain("cc-by"));
    }

    [Fact]
    public async Task NoPackageOrNoGeoJsonKeepsTheRule()
    {
        // HDX's answer for a country with no IPC data (groups:ukr): no package.
        var (feed, store, log, _) = Make(search: """{"success":true,"result":{"count":0,"results":[]}}""");
        await feed.RunAsync([IpcAreaFixture.National()], CancellationToken.None);
        Assert.Empty(store.Snapshot.IpcAreas);
        Assert.Contains(log.Lines, l => l.Contains("NGA keeps the 20% rule") && l.Contains("no IPC package with ipc_nga.geojson"));

        // A package of the country with only CSV files (the old Eswatini duplicate).
        Assert.Null(IpcAreaReader.FindPackage(IpcAreaFixture.Search("hdx_ipc_nogeo_package.json"), "SWZ"));
        // The package is chosen by resource name, never by its place in the list.
        var p = IpcAreaReader.FindPackage(IpcAreaFixture.Search(), "NGA")!;
        Assert.Equal(IpcAreaFixture.PackageName, p.Name);
        Assert.Equal(IpcAreaFixture.GeoJsonUrl, p.GeoJsonUrl);
        Assert.Equal(IpcAreaFixture.CsvUrl, p.NationalCsvUrl);
        // A package_show-shaped result (full resources) is read too.
        var show = JsonNode.Parse(Samples.Text("hdx_ipc_nga_package.json"))!["result"]!.ToJsonString();
        Assert.Equal(p, IpcAreaReader.FindPackage($$$"""{"result":{"count":1,"results":[{{{show}}}]}}""", "NGA"));
    }

    [Fact]
    public void SearchUrlIsTheDecidedCkanQuery()
    {
        var url = IpcAreaReader.SearchUrl("NGA");
        Assert.Equal("https://data.humdata.org/api/3/action/package_search?fq=organization%3Aipc%20AND%20groups%3Anga&rows=10&fl=name%2Clicense_id%2Cres_name%2Cres_url",
            url.AbsoluteUri);
        Assert.Equal("ipc_nga.geojson", IpcAreaReader.GeoJsonName("NGA"));
        Assert.Equal("ipc_nga_national_long_latest.csv", IpcAreaReader.NationalCsvName("NGA"));
    }

    [Fact]
    public async Task AnAreaAcrossTheDatelineIsSplit()
    {
        var geojson = IpcAreaFixture.CurrentGeoJson(fs =>
        {
            // Bama's ring replaced by a square from 179°E to 179°W (written as 181°).
            var bama = fs.Single(f => (string?)f!["properties"]!["title"] == "Bama")!;
            bama["geometry"] = JsonNode.Parse("""{"type":"Polygon","coordinates":[[[179,-17],[181,-17],[181,-16],[179,-16],[179,-17]]]}""");
        });
        var set = await IpcAreaFixture.SetAsync(geojson);
        var bama = set.Areas.Single(a => a.Name == "Bama");
        Assert.Equal(2, bama.Polygons.Count);
        Assert.All(bama.Polygons.SelectMany(p => p.Rings).SelectMany(r => r), g => Assert.InRange(g.LongitudeDeg, -180, 180));
    }

    [Fact]
    public async Task NationalFeedRunsTheAreaStepAndAFailedStepKeepsTheNationalFigures()
    {
        // The global sample has Nigeria as projection only, so no country is current in the fixture's country file; the step
        // runs (it searches HDX) and a 404 for the countries is logged, not a failure of the IPC source.
        var (http, h) = Samples.IpcHttp();
        var store = new CrisisStore();
        var log = new ListLog();
        var areas = new IpcAreaFeed(http, store, new FixedClock(SampleStore.Now), log);
        var result = await new IpcNationalFeed(http, store, new FixedClock(SampleStore.Now), log, areas: areas).RunOnceAsync(CancellationToken.None);
        Assert.Equal(FeedRunResult.Ok, result);
        Assert.NotEmpty(store.Snapshot.Crises.OfType<FoodCrisis>());
        Assert.Contains(h.Requests, r => r.StartsWith("https://data.humdata.org/api/3/action/package_search", StringComparison.Ordinal));
        Assert.Contains(log.Lines, l => l.StartsWith("WARN IPC areas: SDN keeps the 20% rule", StringComparison.Ordinal));
        Assert.Contains(log.Lines, l => l.StartsWith("INFO IPC areas:", StringComparison.Ordinal) && l.Contains("0 countries drawn by area"));
    }
}
