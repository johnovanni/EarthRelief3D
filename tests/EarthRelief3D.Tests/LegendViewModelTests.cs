using EarthRelief3D.Core.Feeds;
using EarthRelief3D.App.Layers;
using EarthRelief3D.App.Presentation;
using EarthRelief3D.App.Settings;
using EarthRelief3D.App.ViewModels;
using EarthRelief3D.Core.Model;
using EarthRelief3D.Core.Store;

namespace EarthRelief3D.Tests;

public class LegendViewModelTests
{
    private static readonly DateTime T = TestData.T;

    private static SourceCitation Cite(string pub, DateTime asOf, string? version = null, string dataset = "Data") =>
        new(pub, dataset, version, new Uri("https://example.org/"), "CC BY 4.0", pub, asOf, T);

    private static CrisisSnapshot Snapshot()
    {
        var conflictOld = TestData.Conflict("ucdp-1") with { Citations = [Cite("UCDP", T.AddDays(-40))] };
        var conflictNew = TestData.Conflict("ucdp-2") with { CountryIso3 = "UKR", Citations = [Cite("UCDP", T.AddDays(-16))] };
        // IPC as-of is the period END, in the future here: the legend uses the newest analysis date (Version) instead.
        var foodA = TestData.Food() with { Citations = [Cite("IPC", T.AddDays(200), "Mar 2026")] };
        var foodB = TestData.Food() with { Id = "ipc-SDN", CountryIso3 = "SDN", Citations = [Cite("IPC", T.AddDays(100), "Sep 2026")] };
        var outA = TestData.Outbreak() with { Citations = [Cite("WHO", T.AddDays(-30))] };
        var outB = TestData.Outbreak() with { Id = "who-2", CountryIso3 = "KEN", Citations = [Cite("WHO", T.AddDays(-4))] };
        var disp = TestData.Disp() with { Citation = Cite("UNHCR", new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc), "2026", "Mid-year 2026 figures") };
        var unplaced = new UnplacedReport("WHO", "Glimmer fever - Atlantis", "country name not recognised: Atlantis", Cite("WHO", T.AddDays(-2)));
        return new CrisisSnapshot([conflictOld, conflictNew, foodA, foodB, outA, outB], [disp], [unplaced])
        {
            CheckedUtc = new Dictionary<string, DateTime> { ["UCDP"] = T, ["IPC"] = T, ["WHO"] = T, ["UNHCR"] = T },
        };
    }

    private static (LegendViewModel Legend, LayerStripViewModel Layers, CrisisStore Store) Make(CrisisSnapshot? s = null)
    {
        var store = new CrisisStore();
        if (s is not null) store.Load(s);
        var layers = new LayerStripViewModel(new MemorySettingsStore());
        return (new LegendViewModel(store, layers, nowUtc: () => T), layers, store);
    }

    [Fact]
    public void LegendShowsNewestAsOfPerLayer()
    {
        var (legend, _, _) = Make(Snapshot());

        Assert.Equal([CrisisLayer.Conflict, CrisisLayer.Food, CrisisLayer.Outbreaks, CrisisLayer.Displacement],
            legend.Lines.Select(l => l.Layer));
        LegendLine Line(CrisisLayer k) => legend.Lines.Single(l => l.Layer == k);
        Assert.Equal("as of " + T.AddDays(-16).ToString("d MMM yyyy", System.Globalization.CultureInfo.InvariantCulture), Line(CrisisLayer.Conflict).AsOf);
        Assert.Equal("as of Sep 2026", Line(CrisisLayer.Food).AsOf);
        Assert.Equal("as of 18 Nov 2026", Line(CrisisLayer.Outbreaks).AsOf);
        Assert.Equal("as of 30 Jun 2026", Line(CrisisLayer.Displacement).AsOf);
        Assert.Contains("Mid-year 2026 figures", Line(CrisisLayer.Displacement).Source);
        // Every line: a symbol, what it measures, a source.
        Assert.All(legend.Lines, l =>
        {
            Assert.False(string.IsNullOrWhiteSpace(l.Symbol));
            Assert.False(string.IsNullOrWhiteSpace(l.Measures));
            Assert.False(string.IsNullOrWhiteSpace(l.Source));
        });
        // The food legend names each phase it shows, with the phase number.
        Assert.Equal(["Phase 3 Crisis", "Phase 4 Emergency", "Phase 5 Catastrophe/Famine", LegendViewModel.OutlineKeyLabel, LegendViewModel.FaintKeyLabel],
            Line(CrisisLayer.Food).Keys.Select(k => k.Label));
        // Brushes are made once per colour, not on every read.
        Assert.Same(Line(CrisisLayer.Food).Brush, Line(CrisisLayer.Food).Brush);
        Assert.Same(Line(CrisisLayer.Food).Keys[0].Brush, Line(CrisisLayer.Food).Keys[0].Brush);
    }

    [Fact]
    public void HiddenLayerHasNoLineAndShowingItBringsItBack()
    {
        var (legend, layers, _) = Make(Snapshot());
        layers.ShowFood = false;
        Assert.DoesNotContain(legend.Lines, l => l.Layer == CrisisLayer.Food);
        layers.ShowFood = true;
        Assert.Contains(legend.Lines, l => l.Layer == CrisisLayer.Food);
    }

    [Fact]
    public void UnplacedReportsAreListedInTheFlyout()
    {
        var (legend, _, _) = Make(Snapshot());
        var u = Assert.Single(legend.Unplaced);
        Assert.Equal("Glimmer fever - Atlantis", u.Title);
        Assert.Contains("country name not recognised: Atlantis", u.Detail);
        Assert.Contains("WHO", u.Detail);
        Assert.Contains("20 Nov 2026", u.Detail);
        Assert.Equal("Not placed on the map (1)", legend.UnplacedHeader);
        Assert.True(legend.HasUnplaced);
    }

    [Fact]
    public void EmptyStoreSaysNotYetCheckedAndFollowsTheStore()
    {
        var (legend, _, store) = Make();
        Assert.All(legend.Lines, l => Assert.Equal(LegendViewModel.NotChecked, l.AsOf));
        Assert.Equal("not yet checked", LegendViewModel.NotChecked);
        Assert.False(legend.HasUnplaced);
        store.Load(Snapshot());   // no dispatcher: at once
        Assert.Equal("as of Sep 2026", legend.Lines.Single(l => l.Layer == CrisisLayer.Food).AsOf);
    }

    [Fact]
    public void UnplacedDatesFollowTheCitationsDateMeaning()
    {
        // Task 3's carry: a GHO code is an estimate year (never "31 Dec 2024"), an HNO code a file load date (never a bare date).
        var gho = new UnplacedReport("WHO GHO", "XKX", "country code not recognised: XKX",
            Cite("World Health Organization", new DateTime(2024, 12, 31, 0, 0, 0, DateTimeKind.Utc)) with { DateMeaning = CitationDate.EstimateYear });
        var hno = new UnplacedReport("UN HNO", "ZZZ", "country code not recognised: ZZZ",
            Cite("OCHA", new DateTime(2026, 2, 13, 0, 0, 0, DateTimeKind.Utc)) with { DateMeaning = CitationDate.FileLoaded });
        var who = new UnplacedReport("WHO", "Glimmer fever - Atlantis", "country name not recognised: Atlantis", Cite("WHO", T.AddDays(-2)));
        var (legend, _, _) = Make(new CrisisSnapshot([], [], [gho, hno, who]));
        string Detail(string title) => legend.Unplaced.Single(u => u.Title == title).Detail;
        Assert.Equal("WHO GHO · country code not recognised: XKX · estimate year 2024", Detail("XKX"));
        Assert.Equal("UN HNO · country code not recognised: ZZZ · file loaded 13 Feb 2026", Detail("ZZZ"));
        Assert.Equal("WHO · country name not recognised: Atlantis · as of 20 Nov 2026", Detail("Glimmer fever - Atlantis"));
        Assert.DoesNotContain(legend.Unplaced, u => u.Detail.Contains("31 Dec 2024"));
    }

    [Fact]
    public void CheckedSourceWithNothingSaysChecked()
    {
        var (legend, _, store) = Make();
        store.MarkChecked("WHO", T);
        store.MarkChecked("UCDP", T);
        Assert.Equal("no reports in the last 90 days · checked 22 Nov 2026", legend.Lines.Single(l => l.Layer == CrisisLayer.Outbreaks).AsOf);
        Assert.Equal("none recorded · checked 22 Nov 2026", legend.Lines.Single(l => l.Layer == CrisisLayer.Conflict).AsOf);
        Assert.Equal(LegendViewModel.NotChecked, legend.Lines.Single(l => l.Layer == CrisisLayer.Food).AsOf);
    }

    [Fact]
    public void FutureUnhcrDateShowsTheReleaseNotAnAsOf()
    {
        var future = TestData.Disp() with
        {
            Citation = Cite("UNHCR", new DateTime(2027, 6, 30, 0, 0, 0, DateTimeKind.Utc), "2027", "Mid-year 2027 figures"),
        };
        var (legend, _, store) = Make();
        store.ReplaceDisplacement([future]);
        var line = legend.Lines.Single(l => l.Layer == CrisisLayer.Displacement);
        Assert.Equal("UNHCR, Mid-year 2027 figures", line.Source);
        Assert.Equal("retrieved 22 Nov 2026", line.AsOf);
        Assert.DoesNotContain("as of", line.SourceLine);
    }

    // ---------------------------------------------------------------- Plan 3: shading and the new marker layers

    [Fact]
    public void ShadingLegendNamesMeasureYearClassesAndSource()
    {
        var indicators = new IndicatorStore();
        indicators.Replace("gho.water", CountryValueLayerTests.Water(
            new CountryValue("SDN", 40, 2024, CountryValueLayerTests.Gho), new CountryValue("ISL", 0, 2024, CountryValueLayerTests.Gho)));
        var layers = new LayerStripViewModel(new MemorySettingsStore());
        layers.Show(EarthRelief3D.App.Presentation.MarkerLayer.HubLines, false);   // on by default (R3): its own legend line is tested in HubPanelTests
        var legend = new LegendViewModel(new CrisisStore(), layers, nowUtc: () => T, indicators: indicators);
        Assert.Empty(legend.Entries);   // food is the shading: its line is the crisis line

        layers.Shading = ShadingLayer.Water;
        Assert.DoesNotContain(legend.Lines, l => l.Layer == CrisisLayer.Food);   // only the shading that is on
        var e = Assert.Single(legend.Entries);
        Assert.Equal("water", e.Id);
        Assert.Equal("Share of people without at least basic drinking water, 2024", e.Measures);
        var scale = ShadingScales.For(ShadingLayer.Water);
        Assert.Equal([.. scale.ClassLabels, LegendViewModel.NoFigureKeyLabel], e.Keys.Select(k => k.Label));
        Assert.Equal("no figure: not shaded", LegendViewModel.NoFigureKeyLabel);
        Assert.True(e.Keys[^1].IsNone);
        Assert.Equal(scale.Colors, e.Keys.Take(5).Select(k => k.Color));
        Assert.Equal("World Health Organization, Global Health Observatory · as of 23 Jan 2026", e.SourceLine);

        // Mixed years say so; a change in the store reaches the legend.
        indicators.Replace("gho.water", CountryValueLayerTests.Water(
            new CountryValue("SDN", 40, 2022, CountryValueLayerTests.Gho), new CountryValue("ISL", 0, 2024, CountryValueLayerTests.Gho)));
        Assert.Equal("Share of people without at least basic drinking water, 2022–2024 (each country's latest year)", Assert.Single(legend.Entries).Measures);

        layers.ShowFood = true;
        Assert.Empty(legend.Entries);
        Assert.Contains(legend.Lines, l => l.Layer == CrisisLayer.Food);
    }

    [Fact]
    public void AccessLine()
    {
        var c = new SourceCitation(AcapsAccessFeed.Publisher, AcapsAccessFeed.Dataset, "December 2025", AcapsAccessFeed.PageUrl, AcapsAccessFeed.Licence,
            "ACAPS", new DateTime(2025, 12, 31, 0, 0, 0, DateTimeKind.Utc), T, CitationDate.Release) { DateText = "December 2025 edition" };
        var indicators = new IndicatorStore();
        var layers = new LayerStripViewModel(new MemorySettingsStore());
        layers.Show(EarthRelief3D.App.Presentation.MarkerLayer.HubLines, false);   // on by default (R3): its own legend line is tested in HubPanelTests
        var legend = new LegendViewModel(new CrisisStore(), layers, nowUtc: () => T, indicators: indicators);
        layers.Shading = ShadingLayer.Access;

        // Before the feed has run: the layer's own wording and "Data not loaded yet" (R2).
        var before = Assert.Single(legend.Entries);
        Assert.Equal("Humanitarian access constraints (ACAPS score 0–5) · Data not loaded yet", $"{before.Measures} · {before.AsOf}");

        indicators.Replace(AcapsAccessFeed.ScoreSeriesId, new IndicatorSeries(AcapsAccessFeed.ScoreSeriesId, AcapsAccessFeed.ScoreMeasures, "score (0 to 5)",
            [new CountryValue("SDN", 5, 2025, c), new CountryValue("ISL", 0, 2025, c)]));
        var e = Assert.Single(legend.Entries);
        Assert.Equal("Humanitarian access constraints (ACAPS score 0–5) · December 2025 edition", $"{e.Measures} · {e.AsOf}");
        Assert.Equal("ACAPS, via HDX, Humanitarian Access dataset · December 2025 edition", e.SourceLine);
        var scale = ShadingScales.For(ShadingLayer.Access);
        Assert.Equal([.. scale.ClassLabels, LegendViewModel.NoFigureKeyLabel], e.Keys.Select(k => k.Label));
        Assert.Equal(scale.Colors, e.Keys.Take(6).Select(k => k.Color));
    }

    [Fact]
    public void ShadingYearIsFromEachCountrysNewestFigure()
    {
        // Sudan has 2022 and 2024; the map shows 2024, so the legend says 2024, not 2022–2024. A value with no figure
        // (below the first break) does not count either.
        var indicators = new IndicatorStore();
        indicators.Replace("gho.water", CountryValueLayerTests.Water(
            new CountryValue("SDN", 60, 2022, CountryValueLayerTests.Gho with { AsOfUtc = T.AddYears(-3) }),
            new CountryValue("SDN", 40, 2024, CountryValueLayerTests.Gho),
            new CountryValue("ISL", 0, 2024, CountryValueLayerTests.Gho),
            new CountryValue("TCD", -1, 2019, CountryValueLayerTests.Gho)));
        var layers = new LayerStripViewModel(new MemorySettingsStore());
        layers.Show(EarthRelief3D.App.Presentation.MarkerLayer.HubLines, false);   // on by default (R3): its own legend line is tested in HubPanelTests
        var legend = new LegendViewModel(new CrisisStore(), layers, nowUtc: () => T, indicators: indicators);
        layers.Shading = ShadingLayer.Water;
        Assert.Equal("Share of people without at least basic drinking water, 2024", Assert.Single(legend.Entries).Measures);
    }

    [Fact]
    public async Task ShelterSaysFileLoadedAndGhoSaysEstimateYearNeverAsOf()
    {
        var indicators = new IndicatorStore();
        var crises = new CrisisStore();
        var clock = new FixedClock(T);
        await new HnoFeed(Samples.HnoHttp().Http, indicators, crises, clock, new ListLog()).RunOnceAsync(default);
        await new GhoFeed(Samples.GhoHttp().Http, indicators, crises, clock, new ListLog()).RunOnceAsync(default);
        var layers = new LayerStripViewModel(new MemorySettingsStore());
        layers.Show(EarthRelief3D.App.Presentation.MarkerLayer.HubLines, false);   // on by default (R3): its own legend line is tested in HubPanelTests
        var legend = new LegendViewModel(new CrisisStore(), layers, nowUtc: () => T, indicators: indicators);

        layers.Shading = ShadingLayer.ShelterNeed;
        var shelter = Assert.Single(legend.Entries);
        Assert.Equal("file loaded 13 Feb 2026", shelter.AsOf);
        Assert.DoesNotContain("as of", shelter.SourceLine);

        layers.Shading = ShadingLayer.Water;
        var water = Assert.Single(legend.Entries);
        Assert.Equal("estimate year 2024", water.AsOf);
        Assert.DoesNotContain("as of", water.SourceLine);
    }

    [Fact]
    public void LayersWithoutDataSayNotLoadedYet()
    {
        var layers = new LayerStripViewModel(new MemorySettingsStore());
        layers.Show(EarthRelief3D.App.Presentation.MarkerLayer.HubLines, false);   // on by default (R3): its own legend line is tested in HubPanelTests
        var legend = new LegendViewModel(new CrisisStore(), layers, nowUtc: () => T);
        layers.Shading = ShadingLayer.Hunger;
        layers.Show(MarkerLayer.PowerPlants, true);
        layers.Show(MarkerLayer.Atrocities, true);

        Assert.Equal(["atrocities", "hunger", "power"], legend.Entries.Select(e => e.Id));
        Assert.All(legend.Entries, e =>
        {
            Assert.Equal(LegendViewModel.NotLoaded, e.AsOf);
            Assert.Empty(e.Keys);
            Assert.False(string.IsNullOrWhiteSpace(e.Measures));
            Assert.False(string.IsNullOrWhiteSpace(e.Source));
        });
        Assert.Equal("Data not loaded yet", LegendViewModel.NotLoaded);
    }

    [Fact]
    public void AMarkerLayerWithDataNamesItsSourceAndDate()
    {
        var markers = new MarkerDataStore();
        var layers = new LayerStripViewModel(new MemorySettingsStore());
        layers.Show(EarthRelief3D.App.Presentation.MarkerLayer.HubLines, false);   // on by default (R3): its own legend line is tested in HubPanelTests
        var legend = new LegendViewModel(new CrisisStore(), layers, nowUtc: () => T, markerData: markers);
        layers.Show(MarkerLayer.Camps, true);
        Assert.Equal(LegendViewModel.NotLoaded, Assert.Single(legend.Entries).AsOf);

        var cite = Cite("HRNK", new DateTime(2024, 5, 1, 0, 0, 0, DateTimeKind.Utc), dataset: "Imagery reports");
        markers.Replace(MarkerLayer.Camps, new PointSet([], cite, "documented sites with published coordinates"));
        var e = Assert.Single(legend.Entries);
        Assert.Equal("HRNK, Imagery reports · as of 1 May 2024", e.SourceLine);
        Assert.Equal("documented sites with published coordinates", e.Note);
    }
}
