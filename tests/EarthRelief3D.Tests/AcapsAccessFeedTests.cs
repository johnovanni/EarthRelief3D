using System.IO;
using System.Text.Json;
using EarthRelief3D.Core.Feeds;
using EarthRelief3D.Core.Model;
using EarthRelief3D.Core.Store;
using EarthRelief3D.DataTool;
using GlobeCore.Feeds;

namespace EarthRelief3D.Tests;

/// <summary>The ACAPS access feed (Plan 4 Task 2) over the synthetic workbook in ACAPS's layout (ruling P4-2).</summary>
public class AcapsAccessFeedTests
{
    private static readonly DateTime Now = new(2026, 11, 24, 12, 0, 0, DateTimeKind.Utc);
    private const string ResourceBase = "https://data.humdata.org/dataset/x/resource/y/download/";
    private const string Dec2025 = "acaps-humanitarian-access-dataset_dec_2025.xlsx";

    private static AcapsAccessFeed Make(FeedHttp http, IndicatorStore ind, CrisisStore? crises = null, ListLog? log = null) =>
        new(http, ind, crises ?? new CrisisStore(), new FixedClock(Now), log ?? new ListLog());

    private static async Task<(IndicatorStore Ind, CrisisStore Crises)> Loaded()
    {
        var ind = new IndicatorStore();
        var crises = new CrisisStore();
        Assert.Equal(FeedRunResult.Ok, await Make(Samples.AcapsHttp().Http, ind, crises).RunOnceAsync(default));
        return (ind, crises);
    }

    private static string Package(params (string File, string Modified)[] resources) =>
        JsonSerializer.Serialize(new
        {
            success = true,
            result = new
            {
                resources = resources.Select(r => new
                {
                    name = r.File,
                    format = "XLSX",
                    last_modified = r.Modified,
                    url = ResourceBase + r.File,
                }),
            },
        });

    /// <summary>The synthetic workbook, with the csv sheet's rows changed by <paramref name="edit"/>.</summary>
    private static string Workbook(Func<IReadOnlyList<object[]>, IReadOnlyList<object[]>> edit)
    {
        var sheets = XlsxWriter.ReadSpec(File.ReadAllText(Samples.Path("acaps_access_synthetic.json")));
        var bytes = XlsxWriter.Write([.. sheets.Select(s => s.Name == "csv" ? (s.Name, edit(s.Rows)) : s)]);
        var path = Path.Combine(Path.GetTempPath(), "er3d-acaps-" + Guid.NewGuid().ToString("N") + ".xlsx");
        File.WriteAllBytes(path, bytes);
        return path;
    }

    private static FeedHttp HttpWithWorkbook(string path)
    {
        var h = new SampleHttpHandler();
        h.MapText(AcapsAccessFeed.PackageUrl.ToString(), Package((Dec2025, "2026-08-03T13:01:20")));
        h.Map(ResourceBase + Dec2025, path);
        return new FeedHttp("test", h);
    }

    [Fact]
    public async Task ReadsCountryLevelRows()
    {
        var (ind, _) = await Loaded();
        var snap = ind.Snapshot;
        // 14 synthetic crises: one is not country level (AFG007), one has a code the boundary set lacks (XXK).
        Assert.Equal(12, snap.Access.Count);
        Assert.DoesNotContain(snap.Access, a => a.Iso3 == "XXK");
        Assert.Equal(snap.Access.Count, snap.Access.Select(a => a.Iso3).Distinct().Count());

        var sdn = snap.AccessOf("SDN")!;
        Assert.Equal(5, sdn.Score);
        Assert.Equal([5, 5, 5], sdn.Pillars);
        Assert.Equal(Enumerable.Range(1, 9), sdn.Indicators.Select(i => i.Number));
        Assert.Equal([3, 3, 3, 3, 3, 3, 3, 2, 2], sdn.Indicators.Select(i => i.Score));
        Assert.Equal("Denial of existence of needs", sdn.Indicators[0].Name);
        Assert.Equal("Impediments to entry into the country", sdn.Indicators[2].Name);   // "3.Impediments..." has no space
        Assert.Equal("Deteriorating", sdn.Trend);
        Assert.Equal(0.12, sdn.InformationGap);
        Assert.Equal("December 2025 edition", sdn.Edition);

        // The "No" row (AFG007, an earthquake) must not replace Afghanistan's country-level row.
        Assert.Equal(4, snap.AccessOf("AFG")!.Score);

        var series = snap.Get(AcapsAccessFeed.ScoreSeriesId)!;
        Assert.Equal("acaps.access", series.Id);
        Assert.Equal(snap.Access.Select(a => a.Iso3).Order(StringComparer.Ordinal), series.Values.Select(v => v.Iso3));
        Assert.Equal(5, series.For("SDN")!.Value);
        Assert.Equal(2025, series.For("SDN")!.Year);
    }

    [Fact]
    public async Task ScoreZeroIsKeptAsARealFigure()
    {
        // R5: 0 is "No constraints", a real figure; both the overall score and an indicator's 0 are kept.
        var path = Workbook(rows => [.. rows.Select((r, i) => i > 0 && (string)r[1] == "SDN001" ? [.. r.Select((c, j) => j == 5 ? 0.0 : c)] : r)]);
        var ind = new IndicatorStore();
        await Make(HttpWithWorkbook(path), ind).RunOnceAsync(default);
        Assert.Equal(0, ind.Snapshot.AccessOf("SDN")!.Score);
        Assert.Equal(0, ind.Snapshot.Get("acaps.access")!.For("SDN")!.Value);
        Assert.Equal(0, ind.Snapshot.AccessOf("UKR")!.Indicators[0].Score);
    }

    [Fact]
    public void EditionFromFileName()
    {
        Assert.Equal("December 2025 edition", AcapsAccessFeed.EditionFromFileName("acaps-humanitarian-access-dataset_dec_2025.xlsx"));
        Assert.Equal("July 2026 edition", AcapsAccessFeed.EditionFromFileName("acaps-humanitarian-access-dataset_jul_2026.xlsx"));
        // The names ACAPS actually uses (ruling R9): full month, a stray hyphen, a capital.
        Assert.Equal("July 2026 edition", AcapsAccessFeed.EditionFromFileName("acaps-humanitarian-access-dataset-july_2026.xlsx"));
        Assert.Equal("December 2024 edition", AcapsAccessFeed.EditionFromFileName("acaps-humanitarian-access-dataset_dec-_2024.xlsx"));
        Assert.Equal("December 2025 edition", AcapsAccessFeed.EditionFromFileName("acaps-humanitarian-access-dataset-December_2025.xlsx"));
        // "sept" is a common four-letter short form of September (m11).
        Assert.Equal("September 2026 edition", AcapsAccessFeed.EditionFromFileName("acaps-humanitarian-access-dataset_sept_2026.xlsx"));
        Assert.Equal("September 2026 edition", AcapsAccessFeed.EditionFromFileName("acaps-humanitarian-access-dataset_sep_2026.xlsx"));
        Assert.Throws<FeedFormatException>(() => AcapsAccessFeed.EditionFromFileName("acaps-humanitarian-access-dataset.xlsx"));
        Assert.Throws<FeedFormatException>(() => AcapsAccessFeed.EditionFromFileName("acaps-humanitarian-access-dataset_xyz_2025.xlsx"));
    }

    [Fact]
    public async Task CitationNamesBothLicences()
    {
        var (ind, _) = await Loaded();
        var c = ind.Snapshot.AccessOf("SDN")!.Citation;
        Assert.Contains("CC BY", c.Licence);
        Assert.Contains("CC BY-NC-ND 4.0", c.Licence);
        Assert.Equal(CitationDate.Release, c.DateMeaning);
        Assert.Equal("December 2025 edition", c.DateText);
        Assert.Equal("ACAPS, via HDX", c.Publisher);
        Assert.Equal("Humanitarian Access dataset", c.Dataset);
        Assert.Equal(new DateTime(2025, 12, 31, 0, 0, 0, DateTimeKind.Utc), c.AsOfUtc);
        Assert.Equal(Now, c.RetrievedUtc);
        Assert.Equal(AcapsAccessFeed.PageUrl, c.Url);
    }

    [Fact]
    public async Task NewestEditionIsUsed()
    {
        // The older edition has the later last_modified (as on HDX in 2026): the file name decides (ruling R4).
        var h = new SampleHttpHandler();
        h.MapText(AcapsAccessFeed.PackageUrl.ToString(), Package(
            ("acaps-humanitarian-access-dataset_jul_2025.xlsx", "2026-08-03T13:01:43"),
            (Dec2025, "2026-08-03T13:01:20"),
            ("acaps-humanitarian-access-dataset-latest.xlsx", "2026-08-03T13:01:50")));   // unparsable name: skipped
        h.Map(ResourceBase + Dec2025, Samples.Path("acaps_access_synthetic.xlsx"));
        var ind = new IndicatorStore();
        var log = new ListLog();
        await Make(new FeedHttp("test", h), ind, log: log).RunOnceAsync(default);
        Assert.Equal("December 2025 edition", ind.Snapshot.AccessOf("SDN")!.Edition);
        Assert.DoesNotContain(h.Requests, r => r.Contains("jul_2025") || r.Contains("july_2025"));
        Assert.Contains(log.Lines, l => l.StartsWith("WARN", StringComparison.Ordinal) && l.Contains("dataset-latest.xlsx"));   // skipped and warned
    }

    [Fact]
    public async Task NoParsableEditionIsFormatChanged()
    {
        var h = new SampleHttpHandler();
        h.MapText(AcapsAccessFeed.PackageUrl.ToString(), Package(("acaps-humanitarian-access-dataset-latest.xlsx", "2026-08-03T13:01:50")));
        var ex = await Assert.ThrowsAsync<FeedFormatException>(() => Make(new FeedHttp("test", h), new IndicatorStore()).RunOnceAsync(default));
        Assert.StartsWith("format changed:", ex.Message);
    }

    [Fact]
    public async Task ReshapedWorkbookIsFormatChanged()
    {
        var path = Workbook(rows => [.. rows.Select(r => r.Where((_, j) => j != 5).ToArray())]);   // no ACCESS column
        var (ind, _) = await Loaded();
        var before = ind.Snapshot;
        var ex = await Assert.ThrowsAsync<FeedFormatException>(() => Make(HttpWithWorkbook(path), ind).RunOnceAsync(default));
        Assert.StartsWith("format changed:", ex.Message);
        Assert.Contains("ACCESS", ex.Message);
        Assert.Same(before, ind.Snapshot);   // the earlier data is kept
    }

    [Fact]
    public async Task UnknownIsoIsUnplaced()
    {
        var (_, crises) = await Loaded();
        var u = Assert.Single(crises.Snapshot.Unplaced, x => x.Source == "ACAPS");
        Assert.Contains("XXK", u.Title);
        Assert.Contains("boundary", u.Reason);
        Assert.True(crises.Snapshot.CheckedUtc.ContainsKey("ACAPS"));
    }

    [Fact]
    public async Task FailureKeepsCache()
    {
        var (ind, _) = await Loaded();
        var before = ind.Snapshot;
        var feed = Make(new FeedHttp("test", new SampleHttpHandler()), ind);   // every request answers 404
        await Assert.ThrowsAnyAsync<Exception>(() => feed.RunOnceAsync(default));
        Assert.Same(before, ind.Snapshot);
        Assert.Equal(12, ind.Snapshot.Access.Count);
        Assert.Equal(TimeSpan.FromDays(7), feed.Interval);
        Assert.Equal("ACAPS", feed.Name);
    }

    [Fact]
    public async Task ASecondIdenticalRunRaisesNoEvent()
    {
        var (ind, _) = await Loaded();
        var raised = 0;
        ind.Changed += (_, _) => raised++;
        await Make(Samples.AcapsHttp().Http, ind).RunOnceAsync(default);
        Assert.Equal(0, raised);
    }
}
