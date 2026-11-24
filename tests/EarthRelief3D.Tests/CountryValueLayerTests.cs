using AstroCore.Earth;
using EarthRelief3D.App.Layers;
using EarthRelief3D.App.Presentation;
using EarthRelief3D.Core.Feeds;
using EarthRelief3D.Core.Model;
using GlobeCore.Imaging;

namespace EarthRelief3D.Tests;

/// <summary>Country-value shading (P3-7): a country with a value is filled in its class colour; one without is not, and says so.</summary>
public class CountryValueLayerTests
{
    internal static readonly SourceCitation Gho = new("World Health Organization", "Global Health Observatory", "WSH_WATER_BASIC",
        new Uri("https://ghoapi.azureedge.net/api/WSH_WATER_BASIC"), "CC BY 4.0", "World Health Organization",
        new DateTime(2026, 1, 23, 0, 0, 0, DateTimeKind.Utc), TestData.T);

    internal static readonly SourceCitation Fao = new("FAO", "Suite of Food Security Indicators", "2025",
        new Uri("https://www.fao.org/faostat/en/#data/FS"), "CC BY 4.0", "FAO", new DateTime(2025, 7, 28, 0, 0, 0, DateTimeKind.Utc), TestData.T);

    internal static IndicatorSeries Water(params CountryValue[] values) =>
        new("gho.water", "Share of people without at least basic drinking water", "%", values);

    private static readonly ShadingScale WaterScale = ShadingScales.For(ShadingLayer.Water);

    private static int Alpha(Bgra32Image image, GeoPosition at)
    {
        int x = (int)((at.LongitudeDeg + 180) / 360 * image.Width), y = (int)((90 - at.LatitudeDeg) / 180 * image.Height);
        return image.Pixels[(y * image.Width + x) * 4 + 3];
    }

    [Fact]
    public void MissingValueIsNotShadedAndSaysSo() => StaAsync.Sta(() =>
    {
        var countries = CountryResolver.LoadDefault().Index;
        var series = Water(new CountryValue("SDN", 40, 2024, Gho));
        var plan = CountryValueLayer.Plan(series, WaterScale);
        Assert.Equal([new ShadedCountry("SDN", WaterScale.Colors[3])], plan);

        var image = CountryValueLayer.Rasterize(plan, countries, 1440, 720);
        // Sudan is filled like the food layer's fill (the 35% fill raised by FillBoost, about 60%); Iceland gets no pixels.
        Assert.InRange(Alpha(image, countries.ByIso3("SDN")!.LabelPoint), 140, 170);
        var iceland = countries.ByIso3("ISL")!;
        Assert.Equal(0, Alpha(image, iceland.LabelPoint));
        Assert.DoesNotContain(plan, p => p.Iso3 == "ISL");

        var tooltip = CountryValueLayer.Tooltip(ShadingLayer.Water, "Iceland", "ISL", series, WaterScale);
        Assert.Contains("No figure from", tooltip);
        Assert.Equal("Iceland · No figure from WHO GHO for the share of people without at least basic drinking water: not shaded", tooltip);
    });

    [Fact]
    public void TooltipNamesMeasureYearAndSource()
    {
        var series = Water(new CountryValue("ISL", 0.0, 2024, Gho), new CountryValue("SDN", 39.84, 2022, Gho));
        Assert.Equal("Iceland · Share of people without at least basic drinking water, 2024: 0.0% · WHO GHO",
            CountryValueLayer.Tooltip(ShadingLayer.Water, "Iceland", "ISL", series, WaterScale));
        Assert.Equal("Sudan · Share of people without at least basic drinking water, 2022: 39.8% · WHO GHO",
            CountryValueLayer.Tooltip(ShadingLayer.Water, "Sudan", "sdn", series, WaterScale));
    }

    [Fact]
    public void AQualifiedValueIsShownAsTheSourceWroteItAndStillFilled()
    {
        // Ruling R3: FAO writes "<2.5"; it is stored as 2.5 with the qualifier, shown as "under 2.5%", and filled (class 0).
        var scale = ShadingScales.For(ShadingLayer.Hunger);
        var series = new IndicatorSeries("fao.undernourishment", "Prevalence of undernourishment (3-year average)", "%",
            [new CountryValue("ISL", 2.5, 2023, Fao, "<2.5")]);
        Assert.Equal("Iceland · Prevalence of undernourishment (3-year average), 2023: under 2.5% · FAO",
            CountryValueLayer.Tooltip(ShadingLayer.Hunger, "Iceland", "ISL", series, scale));
        Assert.Equal([new ShadedCountry("ISL", scale.Colors[0])], CountryValueLayer.Plan(series, scale));
    }

    [Fact]
    public void ACategoryIsNamedNotNumbered()
    {
        var scale = ShadingScales.For(ShadingLayer.WaterStress);
        // I3: a modelled baseline says its period and publication, never a year appended to the measure.
        var cite = Gho with { Publisher = "World Resources Institute", Dataset = "Aqueduct 4.0 country rankings",
            DateMeaning = CitationDate.Baseline, DateText = "baseline 1979–2019 (published 2023)" };
        var series = new IndicatorSeries("wri.bws", "Baseline water stress", "category",
            [new CountryValue("IRQ", 4, 2019, cite), new CountryValue("EGY", -1, 2019, cite, "Arid and Low Water Use")]);
        Assert.Equal("Iraq · Baseline water stress: Extremely High (>80%) · WRI Aqueduct, baseline 1979–2019 (published 2023)",
            CountryValueLayer.Tooltip(ShadingLayer.WaterStress, "Iraq", "IRQ", series, scale));
        Assert.EndsWith(": Arid and Low Water Use · WRI Aqueduct, baseline 1979–2019 (published 2023)", CountryValueLayer.Tooltip(ShadingLayer.WaterStress, "Egypt", "EGY", series, scale));
    }

    [Fact]
    public void AValueBelowTheFirstBreakIsNoFigure() => StaAsync.Sta(() =>
    {
        // WRI writes -9999 for "no data"; a slightly negative 100 − x is no figure either: no pixels, and the tooltip says so.
        var countries = CountryResolver.LoadDefault().Index;
        var scale = ShadingScales.For(ShadingLayer.WaterStress);
        var cite = Gho with { Publisher = "World Resources Institute", Dataset = "Aqueduct 4.0 country rankings" };
        var series = new IndicatorSeries("wri.bws", "Baseline water stress", "category",
            [new CountryValue("ISL", -9999, 2019, cite), new CountryValue("IRQ", 4, 2019, cite)]);
        var plan = CountryValueLayer.Plan(series, scale);
        Assert.Equal([new ShadedCountry("IRQ", scale.Colors[5])], plan);
        var image = CountryValueLayer.Rasterize(plan, countries, 1440, 720);
        Assert.Equal(0, Alpha(image, countries.ByIso3("ISL")!.LabelPoint));
        Assert.True(Alpha(image, countries.ByIso3("IRQ")!.LabelPoint) > 0);
        Assert.Equal("Iceland · No figure from WRI Aqueduct for the baseline water stress: not shaded",
            CountryValueLayer.Tooltip(ShadingLayer.WaterStress, "Iceland", "ISL", series, scale));
        Assert.False(CountryValueLayer.HasFigure(series.For("ISL")!, scale));

        // A qualifier does not make a figure: a -9999 row labelled by the source is still no figure.
        var labelled = series with { Values = [new CountryValue("ISL", -9999, 2019, cite, "No data")] };
        Assert.Empty(CountryValueLayer.Plan(labelled, scale));
        Assert.Equal(0, Alpha(CountryValueLayer.Rasterize(CountryValueLayer.Plan(labelled, scale), countries, 1440, 720),
            countries.ByIso3("ISL")!.LabelPoint));
        Assert.Equal("Iceland · No figure from WRI Aqueduct for the baseline water stress: not shaded",
            CountryValueLayer.Tooltip(ShadingLayer.WaterStress, "Iceland", "ISL", labelled, scale));
        Assert.False(CountryValueLayer.HasFigure(labelled.Values[0], scale));

        var water = Water(new CountryValue("SDN", -0.2, 2024, Gho));
        Assert.Empty(CountryValueLayer.Plan(water, WaterScale));
        Assert.Contains("No figure from WHO GHO", CountryValueLayer.Tooltip(ShadingLayer.Water, "Sudan", "SDN", water, WaterScale));
    });

    [Fact]
    public void LargeNumbersAreGroupedWithTheirUnit()
    {
        var scale = ShadingScales.For(ShadingLayer.OilProduction);
        var series = new IndicatorSeries("eia.crudeOil", "Crude oil production, including lease condensate", "thousand barrels per day",
            [new CountryValue("SAU", 9_612.4, 2024, Gho), new CountryValue("GHA", 47.2, 2024, Gho)]);
        Assert.Contains(": 9,612 thousand barrels per day · ", CountryValueLayer.Tooltip(ShadingLayer.OilProduction, "Saudi Arabia", "SAU", series, scale));
        Assert.Contains(": 47.2 thousand barrels per day · ", CountryValueLayer.Tooltip(ShadingLayer.OilProduction, "Ghana", "GHA", series, scale));
    }

    [Fact]
    public void NewestYearPerCountryIsShownAndNothingWithoutData()
    {
        var series = Water(new CountryValue("SDN", 60, 2022, Gho), new CountryValue("SDN", 40, 2024, Gho), new CountryValue("TCD", double.NaN, 2024, Gho));
        Assert.Equal([new ShadedCountry("SDN", WaterScale.Colors[3])], CountryValueLayer.Plan(series, WaterScale));
        Assert.Empty(CountryValueLayer.Plan(null, WaterScale));   // R2: data not loaded yet, nothing drawn
        Assert.Equal(40, series.For("sdn")!.Value);
        Assert.Contains("No figure from", CountryValueLayer.Tooltip(ShadingLayer.Water, "Chad", "TCD", series, WaterScale));
        Assert.Null(CountryValueLayer.Tooltip(ShadingLayer.Water, "Chad", "TCD", null, WaterScale));   // not loaded: the borders answer
    }

    [Fact]
    public void BuildKeyChangesWithTheColoursAndTheBoundaries()
    {
        var a = new ShadedCountry("SDN", WaterScale.Colors[3]);
        Assert.Equal(CountryValueLayer.BuildKey(1, [a]), CountryValueLayer.BuildKey(1, [a with { }]));
        Assert.NotEqual(CountryValueLayer.BuildKey(1, [a]), CountryValueLayer.BuildKey(1, [a with { Color = WaterScale.Colors[4] }]));
        Assert.NotEqual(CountryValueLayer.BuildKey(1, [a]), CountryValueLayer.BuildKey(2, [a]));
    }
}
