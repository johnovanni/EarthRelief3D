using System.IO;
using EarthRelief3D.Core.Feeds;
using GlobeCore.Earth;
using GlobeCore.Feeds;
using GlobeCore.Logging;

namespace EarthRelief3D.App.Diagnostics;

/// <summary>
/// The <c>--offline-samples &lt;dir&gt;</c> diagnostic: every feed request is answered from the sample files in
/// <c>tests\Samples</c> (captured 2026-11-22; the WHO file is synthetic), so the app can be checked without a network.
/// Anything not mapped gets a 404, which the feeds treat as a failure.
/// </summary>
public static class OfflineSamples
{
    /// <summary>The sample files the handler reads, all of which must be in the folder.</summary>
    public static IReadOnlyList<string> Files { get; } =
    [
        "ucdp_downloads.html", "ucdp_candidate_monthly.csv", "ucdp_candidate_quarterly.csv",
        "hdx_ipc_package.json", "ipc_national_wide_latest.csv", "unhcr_population_coo.json", "who_don_synthetic.json",
    ];

    /// <summary>The year of the UNHCR sample (the feed asks for the clock's year first).</summary>
    public const int UnhcrSampleYear = 2026;

    // The CSV links the UCDP downloads page held when the samples were captured (tests\Samples\README.md).
    public const string UcdpMonthlyUrl = "https://ucdp.uu.se/downloads/candidateged/GEDEvent_v26_0_10.csv";
    public const string UcdpQuarterlyUrl = "https://ucdp.uu.se/downloads/candidateged/GEDEvent_v26_01_26_09.csv";
    public const string IpcCsvUrl = "https://data.humdata.org/dataset/7a7e7428-b8d7-4d2e-91d3-19100500e016/resource/2e4f7475-105b-4fae-81f7-7c32076096b6/download/ipc_global_national_wide_latest.csv";
    public const string WhoUrlPrefix = "https://www.who.int/api/news/diseaseoutbreaknews";

    /// <summary>What GitHub's "latest release" answer holds for the Borders feed in an offline run: the bundled edition, so no update.</summary>
    public const string BordersLatestReleaseJson = "{\"tag_name\":\"v5.1.2\",\"published_at\":\"2022-05-13T00:00:00Z\"}";

    /// <summary>The prefix of the temporary data folders under %TEMP% (one per offline run, deleted on exit).</summary>
    public const string TempFolderPrefix = "EarthRelief3D-offline-";

    /// <summary>The sample files missing from <paramref name="dir"/> (empty when all are there).</summary>
    public static IReadOnlyList<string> Missing(string dir) => Files.Where(f => !File.Exists(Path.Combine(dir, f))).ToList();

    /// <summary>
    /// Deletes offline data folders in <paramref name="tempDir"/> last written more than a day before
    /// <paramref name="nowUtc"/>: left behind when a run was killed. Returns how many were deleted; failures are logged.
    /// </summary>
    public static int SweepStaleFolders(string tempDir, DateTime nowUtc, IAppLog log)
    {
        int deleted = 0;
        try
        {
            if (!Directory.Exists(tempDir)) return 0;
            foreach (var dir in Directory.EnumerateDirectories(tempDir, TempFolderPrefix + "*"))
            {
                try
                {
                    if (nowUtc - Directory.GetLastWriteTimeUtc(dir) <= TimeSpan.FromDays(1)) continue;
                    Directory.Delete(dir, recursive: true);
                    deleted++;
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    log.Warn($"Could not delete the old offline data folder {dir}", ex);
                }
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            log.Warn("Could not look for old offline data folders", ex);
        }
        if (deleted > 0) log.Info($"Deleted {deleted} old offline data folder(s)");
        return deleted;
    }

    /// <summary>
    /// A handler mapping each feed's live URL to the sample file in <paramref name="dir"/> (read on each request). The
    /// URLs are the ones the feed classes use; OfflineSamplesTests runs every feed through this handler, so a URL that
    /// drifts fails a test.
    /// </summary>
    public static SampleHttpHandler CreateHandler(string dir)
    {
        var h = new SampleHttpHandler();
        void Map(string url, string file) => h.Map(url, Path.Combine(dir, file));

        // UCDP: the downloads page, then the two CSV files it links (the longest matching prefix wins).
        Map(UcdpCandidateFeed.PageUrl.ToString(), "ucdp_downloads.html");
        Map(UcdpMonthlyUrl, "ucdp_candidate_monthly.csv");
        Map(UcdpQuarterlyUrl, "ucdp_candidate_quarterly.csv");
        // IPC: the HDX package, then the resource it names.
        Map(IpcNationalFeed.PackageUrl.ToString(), "hdx_ipc_package.json");
        Map(IpcCsvUrl, "ipc_national_wide_latest.csv");
        // WHO: the synthetic sample answers the list request, whatever its query.
        Map(WhoUrlPrefix, "who_don_synthetic.json");
        // UNHCR: only the sample's year has items; any other year is a 404.
        Map(UnhcrDisplacementFeed.UrlFor(UnhcrSampleYear).ToString(), "unhcr_population_coo.json");
        // Borders: the release check answers "latest is v5.1.2" (the edition the program ships), so nothing is downloaded and
        // the dot shows the same state in every run. The text is inline: there is no sample file for it.
        h.MapText(NaturalEarthBoundarySource.LatestReleaseUrl.ToString(), BordersLatestReleaseJson);
        return h;
    }
}
