using System.IO;
using EarthRelief3D.App.ViewModels;
using EarthRelief3D.Core.Feeds;
using EarthRelief3D.Core.Store;

namespace EarthRelief3D.Tests;

/// <summary>Task 6 in the panel: FAO's food supply under "Basic needs", and the "Resources" section (EIA oil, WRI water stress).</summary>
public class ResourcesPanelTests
{
    private static readonly DateTime Now = new(2026, 11, 23, 12, 0, 0, DateTimeKind.Utc);

    /// <summary>The committed extracts, loaded as the app loads them.</summary>
    private static IndicatorStore Bundled()
    {
        var store = new IndicatorStore();
        foreach (var (name, folder) in App.Services.AppServices.BundledDatasets)
            store.ReplaceAll(BundledDataset.Load(name, Path.Combine(BundledDataset.DatasetsDir, folder), null, new ListLog()).Series);
        return store;
    }

    private static CountryPanelViewModel Panel(IndicatorStore ind) =>
        new(new CrisisStore(), iso => CountryResolver.LoadDefault().ByIso3(iso)?.Name, nowUtc: () => Now, indicators: ind);

    private static PanelSection Section(CountryPanelViewModel p, string heading) => Assert.Single(p.Sections, s => s.Heading == heading);

    [Fact]
    public void BasicNeedsShowsFoodSupply()
    {
        var panel = Panel(Bundled());
        panel.Show("HTI");
        var basic = Section(panel, CountryPanelViewModel.BasicNeedsHeading);
        // After the three WHO GHO rows: FAO's 3-year average, with its years and source.
        Assert.Equal(("Food supply", "dietary energy adequacy 84%, 2023–2025, FAO"), basic.Rows[3]);

        panel.Show("GRL");   // Greenland: not in FAO's file
        Assert.Equal(("Food supply", "No figure from FAO"), Section(panel, CountryPanelViewModel.BasicNeedsHeading).Rows[3]);

        var empty = Panel(new IndicatorStore());
        empty.Show("HTI");
        Assert.Equal(("Food supply", "Data not loaded yet"), Section(empty, CountryPanelViewModel.BasicNeedsHeading).Rows[3]);
    }

    [Fact]
    public void ResourcesSectionShowsOilAndWaterStress()
    {
        var panel = Panel(Bundled());
        panel.Show("USA");
        var res = Section(panel, CountryPanelViewModel.ResourcesHeading);
        Assert.Equal(
            [("Crude oil production", "13,662 thousand barrels per day (2025, US EIA)"),
             ("Water stress", "Medium - High (20-40%) (2019 baseline, WRI Aqueduct)")],
            res.Rows);
        Assert.Equal("Source: U.S. Energy Information Administration, estimate year 2025; World Resources Institute, estimate year 2019 · "
            + BundledDataset.StatusNote, res.SourceLine);
        Assert.Equal(["U.S. Energy Information Administration", "World Resources Institute"], res.Links!.Select(l => l.Text));
        Assert.DoesNotContain("as of", res.SourceLine);

        // WRI's "no data" is no figure; a country EIA does not list says so too.
        panel.Show("GRL");
        var grl = Section(panel, CountryPanelViewModel.ResourcesHeading);
        Assert.Equal("No figure from WRI Aqueduct", grl.Rows[1].Value);
        Assert.True(panel.HasNoCrisis);   // resources are not crises
    }

    [Fact]
    public void TheBundledShadingsHaveTheirLegends()
    {
        var layers = new App.ViewModels.LayerStripViewModel(new MemorySettingsStore());
        var legend = new LegendViewModel(new CrisisStore(), layers, nowUtc: () => Now, indicators: Bundled());

        layers.Shading = App.Presentation.ShadingLayer.Hunger;
        var hunger = Assert.Single(legend.Entries);
        // Each country's latest 3-year average, as ranges (fix round 1): North Korea's is 2017-2019, most end in 2025.
        Assert.Equal("Prevalence of undernourishment (share of people, 3-year average), 2017–2019 to 2023–2025 (each country's latest average)",
            hunger.Measures);
        Assert.Equal("Food and Agriculture Organization of the United Nations (FAO), FAOSTAT: Suite of Food Security Indicators · 3-year averages, latest ending 2025", hunger.SourceLine);

        layers.Shading = App.Presentation.ShadingLayer.OilProduction;
        var oil = Assert.Single(legend.Entries);
        Assert.Equal("Crude oil production, including lease condensate, in thousand barrels per day, 2025", oil.Measures);
        Assert.StartsWith("U.S. Energy Information Administration, International Energy Statistics", oil.SourceLine);

        layers.Shading = App.Presentation.ShadingLayer.WaterStress;
        var water = Assert.Single(legend.Entries);
        Assert.Equal("Baseline water stress (WRI Aqueduct 4.0 category; baseline period 1979 to 2019), 2019", water.Measures);
        Assert.EndsWith("· estimate year 2019", water.SourceLine);
        // WRI's own caution travels with the layer (docs/data-sources.md §10).
        Assert.Equal(LegendViewModel.WaterStressNote, water.Note);
        Assert.Contains("not been validated", water.Note);
    }

    [Fact]
    public void TheAridKeyShowsOnlyWhenTheDataHasIt()
    {
        // Ruling R17b: Aqueduct 4.0 has no country in "Arid and Low Water Use" (cat -1), so its grey key is not shown.
        var layers = new App.ViewModels.LayerStripViewModel(new MemorySettingsStore());
        var store = Bundled();
        var legend = new LegendViewModel(new CrisisStore(), layers, nowUtc: () => Now, indicators: store);
        layers.Shading = App.Presentation.ShadingLayer.WaterStress;
        var keys = Assert.Single(legend.Entries).Keys.Select(k => k.Label).ToList();
        Assert.DoesNotContain("Arid and Low Water Use", keys);
        Assert.Equal(["Low (<10%)", "Low - Medium (10-20%)", "Medium - High (20-40%)", "High (40-80%)", "Extremely High (>80%)",
            LegendViewModel.NoFigureKeyLabel], keys);

        // A country in that category brings the key back, in its place and colour.
        var bws = store.Snapshot.Get("wri.bws")!;
        var arid = bws.Values[0] with { Iso3 = "ESH", Value = -1, Qualifier = "Arid and Low Water Use" };
        store.Replace("wri.bws", bws with { Values = [.. bws.Values.Where(v => v.Iso3 != "ESH"), arid] });
        var withArid = Assert.Single(legend.Entries).Keys;
        Assert.Equal("Arid and Low Water Use", withArid[0].Label);
        Assert.Equal(App.Presentation.ShadingScales.AridColor, withArid[0].Color);
    }

    [Fact]
    public void ResourcesSectionSaysWhenNothingIsLoaded()
    {
        var panel = Panel(new IndicatorStore());
        panel.Show("SAU");
        var res = Section(panel, CountryPanelViewModel.ResourcesHeading);
        Assert.All(res.Rows, r => Assert.Equal("Data not loaded yet", r.Value));
        Assert.Equal("Source: US EIA; WRI Aqueduct · Data not loaded yet · " + BundledDataset.StatusNote, res.SourceLine);
    }
}
