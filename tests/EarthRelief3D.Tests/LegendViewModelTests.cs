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
}
