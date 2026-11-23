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

    private static readonly DateTime Now = SampleStore.Now;

    private static CountryPanelViewModel Panel(CrisisStore store) =>
        new(store, iso => CountryResolver.LoadDefault().ByIso3(iso)?.Name, nowUtc: () => Now);

    private static CrisisStore AllChecked(CrisisStore? store = null)
    {
        store ??= new CrisisStore();
        foreach (var n in SourceDates.SourceNames) store.MarkChecked(n, Now);
        return store;
    }

    /// <summary>The IPC fixture with a real-looking "Date of analysis".</summary>
    private static FoodCrisis Food() => TestData.Food() with { Citations = [TestData.Food().Citations[0] with { Version = "Sep 2026" }] };

    [Fact]
    public async Task SudanShowsEveryCrisisInOrder()
    {
        var store = await SampleStore.LoadAsync();
        var panel = Panel(store);

        panel.Show("SDN");

        Assert.True(panel.IsOpen);
        Assert.Equal("Sudan", panel.CountryName);
        Assert.False(panel.HasNoCrisis);
        Assert.False(panel.HasHeadline);   // every source has been checked
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
        // R17: the 20% rule's phase, not the highest phase with people (which is the outline).
        Assert.Contains(food.Rows, r => r.Label == "Country shading" && r.Value == LayerStyle.IpcPhaseLabel(3) + " (IPC's 20% rule)");
        Assert.Contains(food.Rows, r => r.Label == "Outline" && r.Value.StartsWith(LayerStyle.IpcPhaseLabel(ipc.HighestPhase), StringComparison.Ordinal));
        Assert.Equal("Phase 5 Catastrophe/Famine", LayerStyle.IpcPhaseLabel(5));
        // The share of Phase 3 or higher from the exact counts (the file's 0.41 is rounded), to one decimal.
        Assert.Contains(food.Rows, r => r.Value.Contains("(41.0% of the people analysed)", StringComparison.Ordinal));
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
        Assert.Equal(CountryPanelViewModel.NoCrisisText, panel.Headline);
        Assert.DoesNotContain(panel.Sections, s => s.Heading is CountryPanelViewModel.ConflictHeading
            or CountryPanelViewModel.FoodHeading or CountryPanelViewModel.OutbreakHeading);
        // Every source, with its date.
        Assert.Equal(4, panel.SourcesChecked.Count);
        Assert.Collection(panel.SourcesChecked,
            s => Assert.StartsWith("UCDP", s),
            s => Assert.StartsWith("IPC", s),
            s => Assert.StartsWith("WHO", s),
            s => Assert.StartsWith("UNHCR", s));
        // Each source with the date it was checked.
        Assert.All(panel.SourcesChecked, s => Assert.EndsWith("checked 22 Nov 2026", s));
        Assert.Equal("UCDP (armed conflict) · none recorded · checked 22 Nov 2026", panel.SourcesChecked[0]);
        Assert.Equal("WHO (disease outbreaks) · no reports in the last 90 days · checked 22 Nov 2026", panel.SourcesChecked[2]);
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
        var store = AllChecked();
        store.ReplaceKind(CrisisKind.FoodCrisis, [stale]);
        var panel = new CountryPanelViewModel(store, _ => "Angola", nowUtc: () => Now);

        panel.Show("AGO");

        Assert.True(panel.HasNoCrisis);
        var s = Assert.Single(panel.Sections);
        Assert.Equal(CountryPanelViewModel.PastFoodHeading, s.Heading);
        Assert.Contains(s.Rows, r => r.Label == "Last IPC analysis" && r.Value == "1 Jan 2021–30 Jun 2021");
        Assert.Equal(CountryPanelViewModel.NoCrisisText, panel.Headline);
        Assert.Contains(panel.SourcesChecked, l => l.StartsWith("IPC (food crises) · no current crisis (last analysis 1 Jan 2021–30 Jun 2021)", StringComparison.Ordinal));
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
        store.ReplaceKind(CrisisKind.FoodCrisis, [Food() with { CountryIso3 = "SDN" }]);
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
        var food = Food() with { Phase3People = 900_000, Phase4People = 300_000, Phase5People = 0 };
        // No people analysed in the fixture: the phase sum (1,200,000) is the denominator, and 25% are in Phase 4 (R17).
        Assert.Equal("Somalia · food crisis · shaded Phase 4 Emergency (20% rule) · people in Phase 3 Crisis 900,000, Phase 4 Emergency 300,000, "
                     + "Phase 5 Catastrophe/Famine 0 · IPC analysis period 1 Apr 2026–31 Dec 2026 (analysed Sep 2026)",
            CrisisText.FoodHover("Somalia", food));
        Assert.DoesNotContain("as of", CrisisText.FoodHover("Somalia", Food()));
    }

    [Fact]
    public async Task CountryHoverNamesSourcesAndDates()
    {
        var s = (await SampleStore.LoadAsync()).Snapshot;
        var sudan = CrisisText.CountryHover("Sudan", "SDN", s, Now);
        var newestUcdp = s.ByCountry("SDN").OfType<ConflictCrisis>().SelectMany(x => x.Citations).Max(x => x.AsOfUtc);
        var ipcVersion = s.ByCountry("SDN").OfType<FoodCrisis>().Single().Citations[0].Version;
        Assert.Equal($"Sudan · armed conflict, food crisis, disease outbreak · UCDP (as of {CrisisText.Date(newestUcdp)}), "
                     + $"IPC (analysed {ipcVersion}), WHO (reported 15 Nov 2026) · click for details", sudan);
        Assert.Equal("Iceland · no crisis recorded by these sources · checked 22 Nov 2026 · click for details",
            CrisisText.CountryHover("Iceland", "ISL", s, Now));

        var empty = CrisisSnapshot.Empty;
        Assert.Equal("Iceland · not yet checked: UCDP, IPC, WHO, UNHCR · click for details", CrisisText.CountryHover("Iceland", "ISL", empty, Now));
        var partly = empty with { CheckedUtc = new Dictionary<string, DateTime> { ["UCDP"] = Now, ["WHO"] = Now } };
        Assert.Equal("Iceland · none recorded by UCDP, WHO · checked 22 Nov 2026 · not yet checked: IPC, UNHCR · click for details",
            CrisisText.CountryHover("Iceland", "ISL", partly, Now));
    }

    [Fact]
    public void EmptyStoreSaysNotYetChecked()
    {
        var panel = new CountryPanelViewModel(new CrisisStore(), _ => "Iceland", nowUtc: () => Now);
        panel.Show("ISL");

        Assert.True(panel.HasNoCrisis);
        Assert.True(panel.HasHeadline);
        Assert.Equal("Not yet checked: UCDP, IPC, WHO, UNHCR", panel.Headline);
        Assert.All(panel.SourcesChecked, l => Assert.EndsWith("· not yet checked", l));
    }

    [Fact]
    public void WhoCheckedWithNothingSaysChecked()
    {
        var store = new CrisisStore();
        store.MarkChecked("WHO", Now);
        var panel = new CountryPanelViewModel(store, _ => "Iceland", nowUtc: () => Now);
        panel.Show("ISL");

        Assert.Equal("Not yet checked: UCDP, IPC, UNHCR", panel.Headline);
        Assert.Equal("WHO (disease outbreaks) · no reports in the last 90 days · checked 22 Nov 2026", panel.SourcesChecked[2]);

        // With a crisis, the sources not yet checked are still named.
        store.ReplaceKind(CrisisKind.ArmedConflict, [TestData.Conflict() with { CountryIso3 = "ISL" }]);
        Assert.False(panel.HasNoCrisis);
        Assert.Equal("Not yet checked: UCDP, IPC, UNHCR", panel.Headline);
    }

    [Fact]
    public void FutureUnhcrDateShowsTheReleaseWithoutAsOf()
    {
        // UNHCR's current-year figures are "as of 30 June"; before that date there is no as-of date to show.
        var cite = TestData.Disp().Citation with { Dataset = "Mid-year 2027 figures", AsOfUtc = new DateTime(2027, 6, 30, 0, 0, 0, DateTimeKind.Utc) };
        var figure = TestData.Disp() with { Citation = cite };
        var store = AllChecked();
        store.ReplaceDisplacement([figure]);
        var panel = new CountryPanelViewModel(store, _ => "Syria", nowUtc: () => Now);
        panel.Show("SYR");

        var s = Assert.Single(panel.Sections);
        Assert.Equal($"Source: UNHCR, Mid-year 2027 figures · retrieved {CrisisText.Date(cite.RetrievedUtc)}", s.SourceLine);
        var hover = CrisisText.DisplacementHover("Syria", figure, Now);
        Assert.Contains("UNHCR, Mid-year 2027 figures", hover);
        Assert.DoesNotContain("as of", hover);
        Assert.Contains(panel.SourcesChecked, l => l == "UNHCR (people forced from home) · Mid-year 2027 figures · checked 22 Nov 2026");
    }
}
