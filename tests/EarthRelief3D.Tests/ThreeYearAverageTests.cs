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
/// Task 6 fix round 1: FAO's figures are 3-year averages, so every place that shows them names the range
/// (<see cref="CitationDate.ThreeYearAverage"/>), never a single year that a user could read as the figure's year.
/// </summary>
public class ThreeYearAverageTests
{
    private static readonly DateTime Now = new(2026, 11, 23, 12, 0, 0, DateTimeKind.Utc);

    private static BundledDatasetLoad Fao() =>
        BundledDataset.Load("FAO", Path.Combine(BundledDataset.DatasetsDir, "fao-food-security"), null, new ListLog());

    private static IndicatorStore Store()
    {
        var store = new IndicatorStore();
        store.ReplaceAll(Fao().Series);
        return store;
    }

    private static SourceCitation Average(int lastYear) => new("FAO", "Suite", null, new Uri("https://www.fao.org/faostat/en/#data/FS"),
        "CC BY 4.0", "FAO", new DateTime(lastYear, 12, 31, 0, 0, 0, DateTimeKind.Utc), Now, CitationDate.ThreeYearAverage);

    [Fact]
    public void TheDateOfAnAverageIsItsRange()
    {
        Assert.Equal("average 2020–2022", CrisisText.DataDate(Average(2022), Now));
        Assert.Equal("3-year averages, latest ending 2025", CrisisText.SeriesDate(Average(2025), Now));
        // Other kinds keep their wording.
        var gho = Average(2024) with { DateMeaning = CitationDate.EstimateYear };
        Assert.Equal(CrisisText.DataDate(gho, Now), CrisisText.SeriesDate(gho, Now));
    }

    [Fact]
    public void TheFaoExtractIsReadAsAverages()
    {
        var load = Fao();
        Assert.True(load.Loaded, load.Problem);
        Assert.All(load.Series.SelectMany(s => s.Values), v => Assert.Equal(CitationDate.ThreeYearAverage, v.Citation.DateMeaning));
    }

    [Fact]
    public void SudansHoverShowsItsOwnRangeNeverTheLatest()
    {
        var series = Store().Snapshot.Get("fao.undernourishment");
        var text = CountryValueLayer.Tooltip(ShadingLayer.Hunger, "Sudan", "SDN", series, ShadingScales.For(ShadingLayer.Hunger));
        Assert.Equal("Sudan · Prevalence of undernourishment (share of people, 3-year average): 10.9%, average 2020–2022 · FAO", text);
        Assert.DoesNotContain("2025", text);

        var haiti = CountryValueLayer.Tooltip(ShadingLayer.Hunger, "Haiti", "HTI", series, ShadingScales.For(ShadingLayer.Hunger));
        Assert.EndsWith(": 51.4%, average 2023–2025 · FAO", haiti);
    }

    [Fact]
    public void TheHungerLegendSaysAveragesAndThatSomeAreOlder()
    {
        var layers = new LayerStripViewModel(new MemorySettingsStore());
        layers.Show(EarthRelief3D.App.Presentation.MarkerLayer.HubLines, false);   // on by default (R3): its own legend line is tested in HubPanelTests
        var legend = new LegendViewModel(new CrisisStore(), layers, nowUtc: () => Now, indicators: Store());
        layers.Shading = ShadingLayer.Hunger;
        var e = Assert.Single(legend.Entries);

        // North Korea's latest average is 2017-2019, Sudan's 2020-2022; most end in 2025.
        Assert.Equal("Prevalence of undernourishment (share of people, 3-year average), 2017–2019 to 2023–2025 (each country's latest average)", e.Measures);
        Assert.Equal("Food and Agriculture Organization of the United Nations (FAO), FAOSTAT: Suite of Food Security Indicators · 3-year averages, latest ending 2025",
            e.SourceLine);
        Assert.DoesNotContain("estimate year", e.SourceLine);
        Assert.Equal("These figures are 3-year averages. Some countries' latest average is older (the oldest ends in 2019); "
            + "hover over a country to see its own range.", e.Note);
    }

    [Fact]
    public void TheStatusDotSaysAverages()
    {
        var status = BundledDataset.Status(Fao(), c => CrisisText.SeriesDate(c, Now));
        Assert.Equal("3-year averages, latest ending 2025 · bundled; updated with the app", status.Note);
    }

    [Fact]
    public void ThePanelShowsTheRange()
    {
        var panel = new CountryPanelViewModel(new CrisisStore(), _ => "Sudan", nowUtc: () => Now, indicators: Store());
        panel.Show("SDN");
        var food = Assert.Single(panel.Sections, s => s.Heading == CountryPanelViewModel.BasicNeedsHeading).Rows[3];
        Assert.Equal("Food supply", food.Label);
        Assert.Matches(@"^dietary energy adequacy \d+%, \d{4}–\d{4}, FAO$", food.Value);
    }

    [Fact]
    public void APeriodThatDisagreesWithTheYearIsRejected()
    {
        using var dir = new TempDir();
        var src = Path.Combine(BundledDataset.DatasetsDir, "fao-food-security");
        var folder = dir.File("fao");
        Directory.CreateDirectory(folder);
        File.Copy(Path.Combine(src, BundledDataset.ManifestFile), Path.Combine(folder, BundledDataset.ManifestFile));
        File.WriteAllText(Path.Combine(folder, BundledDataset.DataFile),
            "series,iso3,year,value,qualifier,period,m49,area,flag\nfao.undernourishment,SDN,2022,10.9,,2019-2022,729,Sudan,E\n");
        var log = new ListLog();
        var load = BundledDataset.Load("FAO", folder, null, log);
        Assert.False(load.Loaded);
        Assert.Contains("2019-2022", load.Problem);
    }
}
