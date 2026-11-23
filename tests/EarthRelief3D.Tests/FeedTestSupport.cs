using System.IO;
using EarthRelief3D.Core.Feeds;
using GlobeCore.Feeds;
using GlobeCore.Logging;
using GlobeCore.Time;

namespace EarthRelief3D.Tests;

internal sealed class FixedClock(DateTime now) : IClock { public DateTime UtcNow { get; set; } = now; }

internal sealed class ListLog : IAppLog
{
    public List<string> Lines { get; } = [];
    public void Info(string m) => Lines.Add("INFO " + m);
    public void Warn(string m, Exception? e = null) => Lines.Add("WARN " + m);
    public void Error(string m, Exception? e = null) => Lines.Add("ERROR " + m);
}

internal static class Samples
{
    public static string Path(string name) => System.IO.Path.Combine(AppContext.BaseDirectory, "Samples", name);
    public static string Text(string name) => File.ReadAllText(Path(name));

    public const string UcdpPage = "https://ucdp.uu.se/downloads/";
    public const string UcdpMonthly = "https://ucdp.uu.se/downloads/candidateged/GEDEvent_v26_0_10.csv";
    public const string UcdpQuarterly = "https://ucdp.uu.se/downloads/candidateged/GEDEvent_v26_01_26_09.csv";
    public const string HdxPackage = "https://data.humdata.org/api/3/action/package_show?id=global-acute-food-insecurity-country-data";
    public const string IpcCsv = "https://data.humdata.org/dataset/7a7e7428-b8d7-4d2e-91d3-19100500e016/resource/2e4f7475-105b-4fae-81f7-7c32076096b6/download/ipc_global_national_wide_latest.csv";

    public static (FeedHttp Http, SampleHttpHandler Handler) UcdpHttp()
    {
        var h = new SampleHttpHandler();
        h.Map(UcdpPage, Path("ucdp_downloads.html"));
        h.Map(UcdpMonthly, Path("ucdp_candidate_monthly.csv"));
        h.Map(UcdpQuarterly, Path("ucdp_candidate_quarterly.csv"));
        return (new FeedHttp("test", h), h);
    }

    public const string HnoCsv2026 = "https://data.humdata.org/dataset/8326ed53-8f3a-47f9-a2aa-83ab4ecee476/resource/edb91329-0e6b-4ebc-b6cb-051b2a11e536/download/hpc_hno_2026.csv";

    /// <summary>The three WHO GHO indicators answered from the synthetic samples.</summary>
    public static (FeedHttp Http, SampleHttpHandler Handler) GhoHttp()
    {
        var h = new SampleHttpHandler();
        h.Map(GhoFeed.UrlFor(GhoFeed.WaterCode).ToString(), Path("gho_wsh_water_basic.json"));
        h.Map(GhoFeed.UrlFor(GhoFeed.SanitationCode).ToString(), Path("gho_wsh_sanitation_basic.json"));
        h.Map(GhoFeed.UrlFor(GhoFeed.UhcCode).ToString(), Path("gho_uhc_index.json"));
        return (new FeedHttp("test", h), h);
    }

    /// <summary>The HDX needs package and its 2026 CSV.</summary>
    public static (FeedHttp Http, SampleHttpHandler Handler) HnoHttp()
    {
        var h = new SampleHttpHandler();
        h.Map(HnoFeed.PackageUrl.ToString(), Path("hdx_hno_package.json"));
        h.Map(HnoCsv2026, Path("hpc_hno_2026.csv"));
        return (new FeedHttp("test", h), h);
    }

    public static (FeedHttp Http, SampleHttpHandler Handler) IpcHttp()
    {
        var h = new SampleHttpHandler();
        h.Map(HdxPackage, Path("hdx_ipc_package.json"));
        h.Map(IpcCsv, Path("ipc_national_wide_latest.csv"));
        return (new FeedHttp("test", h), h);
    }
}
