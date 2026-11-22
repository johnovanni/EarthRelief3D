using System.Text.Json.Nodes;
using EarthRelief3D.App.Settings;
using EarthRelief3D.Core.Feeds;
using EarthRelief3D.Core.Store;
using GlobeCore.Feeds;
using GlobeCore.Storage;

namespace EarthRelief3D.Tests;

internal static class Repo
{
    public static string Root()
    {
        var dir = new System.IO.DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !System.IO.File.Exists(System.IO.Path.Combine(dir.FullName, "EarthRelief3D.sln"))) dir = dir.Parent;
        return dir?.FullName ?? throw new System.IO.DirectoryNotFoundException("EarthRelief3D.sln not found above the test output");
    }
}

internal sealed class MemorySettingsStore(AppSettings? initial = null) : ISettingsStore<AppSettings>
{
    public AppSettings Current { get; private set; } = initial ?? new AppSettings();
    public int Saves { get; private set; }
    public AppSettings Load() => Current;
    public void Save(AppSettings settings) { Current = settings; Saves++; }
}

/// <summary>A store filled by running the four real feeds over the committed samples (clock 2026-11-22).</summary>
internal static class SampleStore
{
    public static readonly DateTime Now = new(2026, 11, 22, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// The synthetic WHO sample plus one more synthetic item for Sudan (invented, like the rest of that file), so Sudan
    /// has all four kinds of data.
    /// </summary>
    public static string WhoWithSudan()
    {
        var root = JsonNode.Parse(Samples.Text("who_don_synthetic.json"))!;
        root["value"]!.AsArray().Add(new JsonObject
        {
            ["Id"] = "test-sudan",
            ["Title"] = "Pellwick fever - Sudan",
            ["UrlName"] = "2026-DON990",
            ["DonId"] = "2026-DON990",
            ["PublicationDate"] = "2026-11-15T09:00:00Z",
            ["UseOverrideTitle"] = false,
        });
        return root.ToJsonString();
    }

    public static async Task<CrisisStore> LoadAsync()
    {
        var h = new SampleHttpHandler();
        h.Map(Samples.UcdpPage, Samples.Path("ucdp_downloads.html"));
        h.Map(Samples.UcdpMonthly, Samples.Path("ucdp_candidate_monthly.csv"));
        h.Map(Samples.UcdpQuarterly, Samples.Path("ucdp_candidate_quarterly.csv"));
        h.Map(Samples.HdxPackage, Samples.Path("hdx_ipc_package.json"));
        h.Map(Samples.IpcCsv, Samples.Path("ipc_national_wide_latest.csv"));
        h.MapText("https://www.who.int/api/news/diseaseoutbreaknews", WhoWithSudan());
        h.Map(UnhcrDisplacementFeed.UrlFor(2026).ToString(), Samples.Path("unhcr_population_coo.json"));
        var http = new FeedHttp("test", h);
        var store = new CrisisStore();
        var clock = new FixedClock(Now);
        var log = new ListLog();
        IFeed[] feeds =
        [
            new UcdpCandidateFeed(http, store, clock, log),
            new IpcNationalFeed(http, store, clock, log),
            new WhoOutbreakFeed(http, store, clock, log),
            new UnhcrDisplacementFeed(http, store, clock, log),
        ];
        foreach (var f in feeds) Assert.Equal(FeedRunResult.Ok, await f.RunOnceAsync(default));
        return store;
    }
}
