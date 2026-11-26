using System.IO;
using EarthRelief3D.Core.Feeds;
using GlobeCore.Feeds;
using GlobeCore.Logging;
using GlobeCore.Time;

namespace EarthRelief3D.Tests;

internal sealed class FixedClock(DateTime now) : IClock { public DateTime UtcNow { get; set; } = now; }

/// <summary>
/// Records log lines. Feeds run side by side on pool threads (the scheduler starts every due feed at once), so writes are
/// locked: a plain List.Add from several threads can lose a line (it did, in the v1.0.1 release build's HdxFirewallTests).
/// Read <see cref="Lines"/> after the runs have finished.
/// </summary>
internal sealed class ListLog : IAppLog
{
    public List<string> Lines { get; } = [];
    private void Add(string line) { lock (Lines) Lines.Add(line); }
    public void Info(string m) => Add("INFO " + m);
    public void Warn(string m, Exception? e = null) => Add("WARN " + m);
    public void Error(string m, Exception? e = null) => Add("ERROR " + m);
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

    public const string AcapsXlsxDec2025 = "https://data.humdata.org/dataset/b0e0bf77-29a3-46ee-9f78-1830e59c43d1/resource/2b5fed9e-83d9-410e-b590-934e5be5dda3/download/acaps-humanitarian-access-dataset_dec_2025.xlsx";
    public const string FtsCsv = "https://data.humdata.org/dataset/b2bbb33c-2cfb-4809-8dd3-6bbdc080cbb9/resource/b3232da8-f1e4-41ab-9642-b22dae10a1d7/download/fts_requirements_funding_global.csv";

    /// <summary>The ACAPS package (the real one, trimmed to two editions) and the synthetic December 2025 workbook.</summary>
    public static (FeedHttp Http, SampleHttpHandler Handler) AcapsHttp()
    {
        var h = new SampleHttpHandler();
        h.Map(AcapsAccessFeed.PackageUrl.ToString(), Path("hdx_acaps_package.json"));
        h.Map(AcapsXlsxDec2025, Path("acaps_access_synthetic.xlsx"));
        return (new FeedHttp("test", h), h);
    }

    /// <summary>The FTS package and its requirements-and-funding CSV (real rows, trimmed).</summary>
    public static (FeedHttp Http, SampleHttpHandler Handler) FtsHttp()
    {
        var h = new SampleHttpHandler();
        h.Map(FtsFundingFeed.PackageUrl.ToString(), Path("hdx_fts_package.json"));
        h.Map(FtsCsv, Path("fts_requirements_funding_global.csv"));
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
