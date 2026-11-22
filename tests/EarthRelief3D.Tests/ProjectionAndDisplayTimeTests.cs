using System.Globalization;
using System.IO;
using EarthRelief3D.App.Layers;
using EarthRelief3D.App.Presentation;
using EarthRelief3D.App.Settings;
using EarthRelief3D.App.ViewModels;
using EarthRelief3D.Core.Feeds;
using EarthRelief3D.Core.Model;
using EarthRelief3D.Core.Store;

namespace EarthRelief3D.Tests;

/// <summary>
/// IPC rows with only a projection (final review I1), the people in each IPC phase (ruling R16), and the time windows
/// judged at display time (WHO 90 days, IPC 12 months), all from the samples where the samples have them.
/// </summary>
public class ProjectionAndDisplayTimeTests
{
    private static readonly DateTime Now = SampleStore.Now;

    private static CountryPanelViewModel Panel(CrisisStore store, DateTime? now = null) =>
        new(store, iso => CountryResolver.LoadDefault().ByIso3(iso)?.Name, nowUtc: () => now ?? Now);

    private static Dictionary<string, string> IpcRow(string iso3)
    {
        var rows = CsvReader.Read(new StringReader(Samples.Text("ipc_national_wide_latest.csv"))).ToList();
        var header = rows[0];
        var row = rows.Skip(1).Single(r => r[Array.IndexOf(header, "Country")] == iso3);
        return header.Zip(row).ToDictionary(p => p.First, p => p.Second);
    }

    private static string D(string isoDate) => CrisisText.Date(DateOnly.Parse(isoDate, CultureInfo.InvariantCulture));

    private static string N(string number) => CrisisText.Number(long.Parse(number, CultureInfo.InvariantCulture));

    // ---------------------------------------------------------------- I1: projection only

    [Fact]
    public async Task GuatemalaWithOnlyASecondProjectionIsNotNoCrisis()
    {
        var store = await SampleStore.LoadAsync();
        var gtm = IpcRow("GTM");
        Assert.Equal("", gtm["Current from"]);
        var panel = Panel(store);

        panel.Show("GTM");

        Assert.False(panel.HasNoCrisis);
        Assert.NotEqual(CountryPanelViewModel.NoCrisisText, panel.Headline);
        Assert.Equal(CountryPanelViewModel.ProjectionOnlyText, panel.Headline);
        Assert.True(panel.ShowsSourcesChecked);
        var food = Assert.Single(panel.Sections, s => s.Heading == CountryPanelViewModel.ProjectionFoodHeading);
        var expected = $"{gtm["Date of analysis"]}: projection only ({D(gtm["Second projection from"])}–{D(gtm["Second projection to"])}), no current-period figures";
        Assert.Contains(food.Rows, r => r.Label == "IPC analysis" && r.Value == expected);
        Assert.Contains(food.Rows, r => r.Label == "Projected in Phase 3 or higher" && r.Value.StartsWith(N(gtm["Phase 3+ number second projection"]) + " (", StringComparison.Ordinal)
                                        && r.Value.EndsWith("; second projection)", StringComparison.Ordinal));
        Assert.Contains("IPC projection period", food.SourceLine);
        Assert.Contains(panel.SourcesChecked, l => l.StartsWith("IPC (food crises) · projection only (", StringComparison.Ordinal));
        Assert.DoesNotContain(panel.SourcesChecked, l => l.StartsWith("IPC (food crises) · none recorded", StringComparison.Ordinal));
    }

    [Fact]
    public async Task GuatemalaHoverSaysProjectionOnly()
    {
        var s = (await SampleStore.LoadAsync()).Snapshot;
        var gtm = IpcRow("GTM");
        var hover = CrisisText.CountryHover("Guatemala", "GTM", s, Now);
        Assert.Equal($"Guatemala · IPC: projection only (analysed {gtm["Date of analysis"]}) · no crisis recorded by UCDP or WHO · checked 22 Nov 2026 · click for details", hover);
        Assert.DoesNotContain("no crisis recorded by these sources", hover);
    }

    [Theory]
    [InlineData("HND")]
    [InlineData("SSD")]
    [InlineData("HTI")]
    [InlineData("CAF")]
    [InlineData("NGA")]
    public async Task EveryProjectionOnlyCountryHasAFoodSection(string iso3)
    {
        var store = await SampleStore.LoadAsync();
        var panel = Panel(store);
        panel.Show(iso3);
        Assert.Contains(panel.Sections, s => s.Heading == CountryPanelViewModel.ProjectionFoodHeading);
        Assert.NotEqual(CountryPanelViewModel.NoCrisisText, panel.Headline);
        Assert.Contains("IPC: projection only", CrisisText.CountryHover(iso3, iso3, store.Snapshot, Now));
    }

    [Fact]
    public async Task NigeriaShowsItsOutbreakAndItsFirstProjection()
    {
        var store = await SampleStore.LoadAsync();
        var nga = IpcRow("NGA");
        var panel = Panel(store);

        panel.Show("NGA");

        Assert.Equal([CountryPanelViewModel.ProjectionFoodHeading, CountryPanelViewModel.OutbreakHeading],
            panel.Sections.Where(s => s.Heading != CountryPanelViewModel.DisplacementHeading).Select(s => s.Heading));
        Assert.False(panel.HasNoCrisis);
        Assert.False(panel.ShowsSourcesChecked);   // the outbreak is a crisis
        var food = panel.Sections.Single(s => s.Heading == CountryPanelViewModel.ProjectionFoodHeading);
        Assert.Contains(food.Rows, r => r.Value.EndsWith("; first projection)", StringComparison.Ordinal)
                                        && r.Value.StartsWith(N(nga["Phase 3+ number first projection"]), StringComparison.Ordinal));
        var hover = CrisisText.CountryHover("Nigeria", "NGA", store.Snapshot, Now);
        Assert.Contains($"IPC: projection only (analysed {nga["Date of analysis"]})", hover);
        Assert.StartsWith("Nigeria · disease outbreak · ", hover);
    }

    [Fact]
    public async Task ProjectionOnlyIsNeitherShadedNorInTheLegendDate()
    {
        var s = (await SampleStore.LoadAsync()).Snapshot;
        var plan = FoodImageLayer.Plan(s.Crises);
        foreach (var iso in new[] { "CAF", "GTM", "HND", "HTI", "NGA", "SSD" }) Assert.DoesNotContain(plan, p => p.Iso3 == iso);
        Assert.Contains(plan, p => p.Iso3 == "SDN");
        // The legend's date is the newest "Date of analysis" of the rows the layer shades.
        var newest = s.Crises.OfType<FoodCrisis>().Where(f => !f.ProjectionOnly).Select(f => f.Citations[0].Version)
            .MaxBy(SourceDates.AnalysisDate);
        Assert.Equal("as of " + newest, SourceDates.For(CrisisLayer.Food, s, Now).DateText);
    }

    // ---------------------------------------------------------------- R16: people in each phase

    [Fact]
    public async Task SudanFoodSectionAndTooltipShowThePeopleInEachPhase()
    {
        var store = await SampleStore.LoadAsync();
        var sdn = IpcRow("SDN");
        var panel = Panel(store);
        panel.Show("SDN");
        var food = panel.Sections.Single(s => s.Heading == CountryPanelViewModel.FoodHeading);
        Assert.Contains(food.Rows, r => r.Label == "In Phase 3 Crisis" && r.Value == N(sdn["Phase 3 number current"]) + " people");
        Assert.Contains(food.Rows, r => r.Label == "In Phase 4 Emergency" && r.Value == N(sdn["Phase 4 number current"]) + " people");
        Assert.Contains(food.Rows, r => r.Label == "In Phase 5 Catastrophe/Famine" && r.Value == N(sdn["Phase 5 number current"]) + " people");

        var ipc = store.Snapshot.ByCountry("SDN").OfType<FoodCrisis>().Single();
        var tip = CrisisText.FoodHover("Sudan", ipc);
        Assert.Contains($"people in Phase 3 Crisis {N(sdn["Phase 3 number current"])}, Phase 4 Emergency {N(sdn["Phase 4 number current"])}, "
                        + $"Phase 5 Catastrophe/Famine {N(sdn["Phase 5 number current"])}", tip);
    }

    // ---------------------------------------------------------------- time windows judged at display time

    private static CrisisStore OldCacheStore(DateTime now)
    {
        // As if loaded from a cache written long ago: the feeds judged these current then.
        var oldReport = TestData.Outbreak() with { PublishedUtc = now.AddDays(-91), Citations = [TestData.Cite("WHO") with { AsOfUtc = now.AddDays(-91) }] };
        var oldFood = TestData.Food() with { PeriodFrom = new DateOnly(2025, 1, 1), PeriodTo = DateOnly.FromDateTime(now).AddMonths(-13), IsStale = false };
        var store = new CrisisStore();
        store.Load(new CrisisSnapshot([oldReport, oldFood], [], [])
        {
            CheckedUtc = SourceDates.SourceNames.ToDictionary(n => n, _ => now.AddDays(-100)),
        });
        return store;
    }

    [Fact]
    public void SnapshotAsSeenAtDropsOldReportsAndMarksOldAnalysesStale()
    {
        var store = OldCacheStore(Now);
        var seen = store.Snapshot.AsSeenAt(Now);
        Assert.Empty(seen.Crises.OfType<OutbreakCrisis>());
        Assert.True(seen.Crises.OfType<FoodCrisis>().Single().IsStale);
        Assert.Equal(store.Snapshot.CheckedUtc, seen.CheckedUtc);
        // Nothing aged: the same instance.
        var fresh = TestData.Snap();
        Assert.Same(fresh, fresh.AsSeenAt(TestData.T));
        // 90 days exactly is still inside the window.
        Assert.True((TestData.Outbreak() with { PublishedUtc = Now.AddDays(-90) }).IsCurrentAt(Now));
    }

    [Fact]
    public void OldCachedReportsAndAnalysesAreNotShownAsCurrent()
    {
        var store = OldCacheStore(Now);
        var cod = Panel(store);
        cod.Show("COD");
        Assert.DoesNotContain(cod.Sections, s => s.Heading == CountryPanelViewModel.OutbreakHeading);
        Assert.Equal(CountryPanelViewModel.NoCrisisText, cod.Headline);

        var som = Panel(store);
        som.Show("SOM");
        Assert.Equal(CountryPanelViewModel.PastFoodHeading, Assert.Single(som.Sections).Heading);
        Assert.True(som.HasNoCrisis);

        Assert.Empty(FoodImageLayer.Plan(store.Snapshot.AsSeenAt(Now).Crises));
        Assert.StartsWith("Somalia · no crisis recorded by these sources", CrisisText.CountryHover("Somalia", "SOM", store.Snapshot, Now));

        var legend = new LegendViewModel(store, new LayerStripViewModel(new MemorySettingsStore()), nowUtc: () => Now);
        var outbreaks = legend.Lines.Single(l => l.Layer == CrisisLayer.Outbreaks);
        Assert.StartsWith("no reports in the last 90 days", outbreaks.AsOf);
    }
}
