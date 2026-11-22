using System.Text.RegularExpressions;
using EarthRelief3D.App.Presentation;
using EarthRelief3D.App.ViewModels;
using EarthRelief3D.Core.Feeds;
using EarthRelief3D.Core.Model;
using EarthRelief3D.Core.Store;

namespace EarthRelief3D.Tests;

public class CountryPanelViewModelTests
{
    // "Source: <publisher>, <dataset> · ..." with a d MMM yyyy date somewhere after the dot.
    private static readonly Regex SourceLineShape = new(@"^Source: [^,]+(, .+)? · .*\b\d{1,2} [A-Z][a-z]{2} \d{4}\b");

    private static CountryPanelViewModel Panel(CrisisStore store) =>
        new(store, iso => CountryResolver.LoadDefault().ByIso3(iso)?.Name);

    [Fact]
    public async Task SudanShowsEveryCrisisInOrder()
    {
        var store = await SampleStore.LoadAsync();
        var panel = Panel(store);

        panel.Show("SDN");

        Assert.True(panel.IsOpen);
        Assert.Equal("Sudan", panel.CountryName);
        Assert.False(panel.HasNoCrisis);
        Assert.Equal(
            [CountryPanelViewModel.ConflictHeading, CountryPanelViewModel.FoodHeading, CountryPanelViewModel.OutbreakHeading,
             CountryPanelViewModel.DisplacementHeading],
            panel.Sections.Select(s => s.Heading));

        var conflict = panel.Sections[0];
        var sudanConflicts = store.Snapshot.ByCountry("SDN").OfType<ConflictCrisis>().ToList();
        Assert.NotEmpty(sudanConflicts);
        // The UCDP text is shown as is ("N deaths since <Mon yyyy> (UCDP)"); never "began".
        foreach (var c in sudanConflicts) Assert.Contains(conflict.Rows, r => r.Value == c.Severity.Text);
        Assert.DoesNotContain(conflict.Rows, r => r.Label.Contains("began", StringComparison.OrdinalIgnoreCase)
                                                  || r.Value.Contains("began", StringComparison.OrdinalIgnoreCase));

        var food = panel.Sections[1];
        var ipc = store.Snapshot.ByCountry("SDN").OfType<FoodCrisis>().Single();
        Assert.Contains(food.Rows, r => r.Value.StartsWith($"Phase {ipc.HighestPhase} ", StringComparison.Ordinal));
        // Phase3PlusShare is a fraction: 0.32 is shown as 32%.
        var pct = Math.Round(ipc.Phase3PlusShare * 100).ToString("0", System.Globalization.CultureInfo.InvariantCulture) + "%";
        Assert.Contains(food.Rows, r => r.Value.Contains(pct, StringComparison.Ordinal));
        Assert.Contains("IPC analysis period", food.SourceLine);
        Assert.Contains($"(analysed {ipc.Citations[0].Version})", food.SourceLine);
        Assert.DoesNotContain("as of", food.SourceLine);

        var outbreak = panel.Sections[2];
        Assert.Contains(outbreak.Rows, r => r.Value == "Pellwick fever");
        Assert.Equal(new Uri("https://www.who.int/emergencies/disease-outbreak-news/item/2026-DON990"), outbreak.Link);

        var displaced = panel.Sections[3];
        var unhcr = store.Snapshot.Displacement.Single(d => d.CountryIso3 == "SDN");
        Assert.Contains(displaced.Rows, r => r.Value == unhcr.Idps.ToString("N0", System.Globalization.CultureInfo.InvariantCulture));
        Assert.Contains("Mid-year 2026 figures", displaced.SourceLine);
        Assert.Contains("as of 30 Jun 2026", displaced.SourceLine);
    }

    [Fact]
    public async Task NoCrisisCountryListsSourcesChecked()
    {
        var store = await SampleStore.LoadAsync();
        var panel = Panel(store);

        panel.Show("ISL");

        Assert.True(panel.IsOpen);
        Assert.Equal("Iceland", panel.CountryName);
        Assert.True(panel.HasNoCrisis);
        Assert.Equal("No crisis recorded by these sources", CountryPanelViewModel.NoCrisisText);
        Assert.DoesNotContain(panel.Sections, s => s.Heading is CountryPanelViewModel.ConflictHeading
            or CountryPanelViewModel.FoodHeading or CountryPanelViewModel.OutbreakHeading);
        // Every source, with its date.
        Assert.Equal(4, panel.SourcesChecked.Count);
        Assert.Collection(panel.SourcesChecked,
            s => Assert.StartsWith("UCDP", s),
            s => Assert.StartsWith("IPC", s),
            s => Assert.StartsWith("WHO", s),
            s => Assert.StartsWith("UNHCR", s));
        Assert.All(panel.SourcesChecked, s => Assert.Matches(@"\b([A-Z][a-z]{2} \d{4})\b", s));
        Assert.Contains("as of 30 Jun 2026", panel.SourcesChecked[3]);
    }

    [Fact]
    public async Task EverySectionHasASourceLineWithDate()
    {
        var store = await SampleStore.LoadAsync();
        var panel = Panel(store);
        var countries = store.Snapshot.Crises.Select(c => c.CountryIso3)
            .Concat(store.Snapshot.Displacement.Select(d => d.CountryIso3)).Distinct().ToList();
        Assert.True(countries.Count > 20);
        foreach (var iso in countries)
        {
            panel.Show(iso);
            Assert.NotEmpty(panel.Sections);
            foreach (var s in panel.Sections)
            {
                Assert.Matches(SourceLineShape, s.SourceLine);
                Assert.NotNull(s.Link);
                Assert.NotEmpty(s.Rows);
            }
        }
    }

    [Fact]
    public void StaleFoodAnalysisIsListedButIsNoCrisis()
    {
        var stale = TestData.Food() with
        {
            CountryIso3 = "AGO", PeriodFrom = new DateOnly(2021, 1, 1), PeriodTo = new DateOnly(2021, 6, 30), IsStale = true,
        };
        var store = new CrisisStore();
        store.ReplaceKind(CrisisKind.FoodCrisis, [stale]);
        var panel = new CountryPanelViewModel(store, _ => "Angola");

        panel.Show("AGO");

        Assert.True(panel.HasNoCrisis);
        var s = Assert.Single(panel.Sections);
        Assert.Equal(CountryPanelViewModel.PastFoodHeading, s.Heading);
        Assert.Contains(s.Rows, r => r.Label == "Last IPC analysis" && r.Value == "1 Jan 2021–30 Jun 2021");
    }

    [Fact]
    public void CloseAndFocusAndStoreRefresh()
    {
        var store = new CrisisStore();
        store.Load(TestData.Snap());
        var panel = new CountryPanelViewModel(store, iso => iso == "SDN" ? "Sudan" : iso);

        panel.Show("SDN", CrisisLayer.Conflict);
        Assert.Equal(0, panel.FocusedSectionIndex);
        Assert.Single(panel.Sections);

        // A store change while open refreshes the panel (no dispatcher: at once).
        store.ReplaceKind(CrisisKind.FoodCrisis, [TestData.Food() with { CountryIso3 = "SDN" }]);
        Assert.Equal(2, panel.Sections.Count);

        panel.CloseCommand.Execute(null);
        Assert.False(panel.IsOpen);
        Assert.Empty(panel.Sections);
    }

    [Fact]
    public void HoverTextNamesSourceAndDate()
    {
        var c = TestData.Conflict();
        Assert.Equal("Sudan · armed conflict · 30 deaths · as of 22 Nov 2026", CrisisText.ConflictHover("Sudan", c));
        var f = TestData.Food();
        Assert.Equal("Somalia · food crisis · Phase 4 (Emergency) · IPC analysis period 1 Apr 2026–31 Dec 2026 (analysed 26.0.9)",
            CrisisText.FoodHover("Somalia", f));
        Assert.DoesNotContain("as of", CrisisText.FoodHover("Somalia", f));
    }
}
