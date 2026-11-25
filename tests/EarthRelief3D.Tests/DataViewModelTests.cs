using System.ComponentModel;
using System.IO;
using EarthRelief3D.App.Curated;
using EarthRelief3D.App.Layers;
using EarthRelief3D.App.Presentation;
using EarthRelief3D.App.Services;
using EarthRelief3D.App.ViewModels;
using EarthRelief3D.Core.Curated;
using EarthRelief3D.Core.Feeds;
using EarthRelief3D.Core.Help;
using EarthRelief3D.Core.Model;
using EarthRelief3D.Core.Sources;
using EarthRelief3D.Core.Store;
using GlobeCore.Wpf.Services;

namespace EarthRelief3D.Tests;

/// <summary>Plan 5 Task 4: the Data card's rows (every source with its use, date, licence and credit) and the card host.</summary>
public sealed class DataViewModelTests
{
    private static readonly DateTime T = TestData.T;   // 22 Nov 2026

    private static string DataDir => Path.Combine(Repo.Root(), "data");
    private static SourceRegistry Real() => SourceRegistry.Load(DataDir);

    /// <summary>The bundled extracts' citations as the app collects them at start-up (folder name to the newest citation).</summary>
    internal static Dictionary<string, SourceCitation> RealDatasets()
    {
        var log = new ListLog();
        var datasets = new Dictionary<string, SourceCitation>(StringComparer.Ordinal);
        foreach (var d in AppServices.BundledDatasets)
            if (BundledDataset.Load(d.Name, Path.Combine(BundledDataset.DatasetsDir, d.Folder), null, log).Newest is { } c) datasets[d.Folder] = c;
        foreach (var (_, folder) in ResourceMarkers.Datasets.Concat(HubMarkers.Datasets))
            if (BundledPoints.Load("test", Path.Combine(BundledDataset.DatasetsDir, folder), null, log).Citation is { } c) datasets[folder] = c;
        return datasets;
    }

    private static SourceCitation Cite(string pub, DateTime asOf, string? version = null, string dataset = "Data") =>
        new(pub, dataset, version, new Uri("https://example.org/"), "CC BY 4.0", pub, asOf, T);

    private static SourceAsOfInputs Inputs(CrisisSnapshot? crises = null, IndicatorSnapshot? indicators = null, IReadOnlyDictionary<string, SourceCitation>? datasets = null,
        bool realTables = true) =>
        new(crises ?? CrisisSnapshot.Empty, indicators ?? IndicatorSnapshot.Empty, datasets ?? new Dictionary<string, SourceCitation>(),
            realTables ? new CuratedStore(CuratedTables.Load(Path.Combine(DataDir, "curated")), false) : CuratedStore.Empty(),
            realTables ? HelpTables.Load(DataDir, "US") : HelpTables.Empty, null);

    private static DataViewModel Make(SourceAsOfInputs inputs, ILinkOpener? links = null) =>
        new(Real(), () => inputs, () => T, links ?? new RecordingOpener());

    private static DataRow RowOf(DataViewModel vm, string id) => vm.Rows[Real().Sources.ToList().FindIndex(s => s.Id == id)];

    private sealed class RecordingOpener : ILinkOpener
    {
        public List<string> Opened { get; } = [];
        public void Open(string url) => Opened.Add(url);
    }

    private sealed class ThrowingOpener : ILinkOpener
    {
        public void Open(string url) => throw new Win32Exception("no browser");
    }

    private static CrisisSnapshot UcdpSnapshot(DateTime asOf, DateTime? checkedAt)
    {
        var conflict = TestData.Conflict("ucdp-x") with { Citations = [Cite("UCDP", asOf)] };
        return new CrisisSnapshot([conflict], [], [])
        {
            CheckedUtc = checkedAt is { } t ? new Dictionary<string, DateTime> { ["UCDP"] = t } : new Dictionary<string, DateTime>(),
        };
    }

    [Fact]
    public void OneRowPerSource()
    {
        var vm = Make(Inputs(datasets: RealDatasets()));
        var registry = Real();
        Assert.Equal(27, vm.Rows.Count);
        Assert.Equal(registry.Sources.Select(s => s.Name), vm.Rows.Select(r => r.Name));
        Assert.Equal("Data sources", vm.Heading);
        Assert.All(vm.Rows, r => { Assert.NotEmpty(r.AsOf); Assert.NotEmpty(r.Use); Assert.NotEmpty(r.Credit); Assert.Equal("https", r.Page.Scheme); });
        Assert.Contains("Every fact in EarthRelief3D comes from one of these sources.", vm.Intro);
    }

    [Fact]
    public void UseLabelsArePlain()
    {
        var vm = Make(Inputs(datasets: RealDatasets()));
        var registry = Real();
        string Expected(SourceUse u) => u switch
        {
            SourceUse.Live => "fetched by the app", SourceUse.Bundled => "included with the app",
            SourceUse.Curated => "written for the app, each entry cited", _ => "links only",
        };
        for (var i = 0; i < registry.Sources.Count; i++) Assert.Equal(Expected(registry.Sources[i].Use), vm.Rows[i].Use);
    }

    [Fact]
    public void LiveRowMatchesTheLegend()
    {
        var snap = UcdpSnapshot(T.AddDays(-16), T);
        var legend = SourceDates.For(CrisisLayer.Conflict, snap, T).LegendDate;
        var row = RowOf(Make(Inputs(snap)), "ucdp");
        Assert.StartsWith(legend, row.AsOf);
        Assert.Equal(legend + " · checked 22 Nov 2026", row.AsOf);
    }

    [Fact]
    public void LegendAndDataCardShareOneFunction()
    {
        var snap = UcdpSnapshot(T.AddDays(-16), null);
        var summary = SourceDates.For(CrisisLayer.Conflict, snap, T);
        Assert.Equal(SourceAsOf.LegendDate(summary), summary.LegendDate);
        // Never checked, but with data: the date is not shown as current.
        Assert.Equal(summary.LegendDate + " · not yet checked", RowOf(Make(Inputs(snap)), "ucdp").AsOf);
    }

    [Fact]
    public void BundledRowUsesTheManifest()
    {
        var vm = Make(Inputs(datasets: RealDatasets()));
        Assert.Equal("September 2026 release (file V2)", RowOf(vm, "gem-gipt").AsOf);
        Assert.StartsWith("3-year averages, latest ending 2025", RowOf(vm, "faostat").AsOf);
        Assert.Equal("as of 5 Nov 2026", RowOf(vm, "nga-wpi").AsOf);
        Assert.Equal("baseline 1979–2019 (published 2023)", RowOf(vm, "aqueduct").AsOf);
        Assert.Equal("year 2025", RowOf(vm, "eia").AsOf);
    }

    [Fact]
    public void BordersNameTheirEdition()
    {
        var set = new GlobeCore.Regions.BoundarySet("Natural Earth 5.1.2 (2022-05-13)", new DateOnly(2022, 5, 13), null!, [])
        {
            RuleDescriptions = ["Crimea shown as part of Ukraine"],
        };
        var inputs = Inputs() with { Borders = set };
        Assert.Equal("Natural Earth 5.1.2 (2022-05-13)", RowOf(Make(inputs), "natural-earth").AsOf);
        Assert.Equal("Data not loaded yet", RowOf(Make(Inputs()), "natural-earth").AsOf);
    }

    [Fact]
    public void NotLoadedSaysSo()
    {
        var vm = Make(Inputs());
        Assert.Equal("not yet checked", RowOf(vm, "acaps").AsOf);
        Assert.Equal("not yet checked", RowOf(vm, "fts").AsOf);
        Assert.Equal("not yet checked", RowOf(vm, "who-gho").AsOf);
        // A dataset folder that did not load.
        Assert.Equal("Data not loaded yet", RowOf(vm, "faostat").AsOf);
        Assert.Equal("Data not loaded yet", RowOf(vm, "gem-gipt").AsOf);
        Assert.Equal("Data not loaded yet", RowOf(vm, "nga-wpi").AsOf);
        // A source checked that found nothing says so, with the check.
        var checkedOnly = new CrisisSnapshot([], [], []) { CheckedUtc = new Dictionary<string, DateTime> { ["FTS"] = T.AddDays(-1) } };
        Assert.Equal("no figures · checked 21 Nov 2026", RowOf(Make(Inputs(checkedOnly)), "fts").AsOf);
    }

    [Fact]
    public void FailedFeedKeepsCachedDate()
    {
        // The last run failed, so the check time is the last success, two days ago; the cached data keeps its own date.
        var snap = UcdpSnapshot(T.AddDays(-10), T.AddDays(-2));
        var row = RowOf(Make(Inputs(snap)), "ucdp");
        Assert.Equal(CrisisText.AsOf(T.AddDays(-10)) + " · checked 20 Nov 2026", row.AsOf);
    }

    [Fact]
    public void CuratedRowGivesNewestReview()
    {
        var vm = Make(Inputs());
        Assert.Equal("each entry gives its own date; last reviewed 25 Nov 2026", RowOf(vm, "curated").AsOf);
        Assert.StartsWith("each entry gives its own date; last reviewed ", RowOf(vm, "unhrd").AsOf);
        Assert.StartsWith("each entry gives its own date; last reviewed ", RowOf(vm, "ocha-pooled-funds").AsOf);
        // No table loaded: say so.
        Assert.Equal("Data not loaded yet", RowOf(Make(Inputs(realTables: false)), "curated").AsOf);
    }

    [Fact]
    public void LinksOnlyRowsShowTheirCheckDay()
    {
        var registry = Real();
        var vm = Make(Inputs());
        foreach (var s in registry.Sources.Where(s => s.Use == SourceUse.LinksOnly))
            Assert.Equal("links only; checked " + CrisisText.Date(s.CheckedOn), RowOf(vm, s.Id).AsOf);
    }

    [Fact]
    public void CreditCarriesItsDate()
    {
        var cite = Cite("OCHA Financial Tracking Service (FTS), via HDX", new DateTime(2026, 11, 20, 0, 0, 0, DateTimeKind.Utc));
        var appeal = new AppealFunding("SDN", 1, "Sudan HRP 2026", "HRP", 2026, 1_000_000, 400_000, 40, false, cite);
        var indicators = IndicatorSnapshot.Empty with { Appeals = [appeal] };
        var row = RowOf(Make(Inputs(indicators: indicators)), "fts");
        Assert.Equal("Financial Tracking Service (FTS), 20 Nov 2026, https://fts.unocha.org", row.Credit);
        Assert.StartsWith("as of 20 Nov 2026", row.AsOf);
        // No data: the date and its comma go, never a raw placeholder.
        var empty = RowOf(Make(Inputs()), "fts").Credit;
        Assert.Equal("Financial Tracking Service (FTS), https://fts.unocha.org", empty);
        Assert.All(Make(Inputs(datasets: RealDatasets())).Rows, r => Assert.DoesNotContain("{date}", r.Credit));
    }

    [Fact]
    public void IrsRowGivesTheCharityChecks()
    {
        var real = Inputs();
        var newest = real.Help.Charities.Where(c => c.Legal is not null).Max(c => c.Legal!.CheckedOn);
        var row = RowOf(Make(real), "irs");
        Assert.Equal("curated", Real().Sources.Single(x => x.Id == "irs").Use.ToString().ToLowerInvariant());
        Assert.Equal("each charity gives its own Publication 78 check date; latest checked " + CrisisText.Date(newest), row.AsOf);
        Assert.Equal("written for the app, each entry cited", row.Use);
        // No charity loaded: it says what applies.
        Assert.StartsWith("no charity is listed yet", RowOf(Make(Inputs(realTables: false)), "irs").AsOf);
        // The evaluators stay links only: their ratings are never copied.
        Assert.Equal("links only", RowOf(Make(real), "charity-evaluators").Use);
    }

    [Fact]
    public void EiaCreditFollowsThePublishedDayNotTheDownload()
    {
        // Published in October, downloaded in December: the credit says October.
        var published = new SourceCitation("EIA", "INTL", null, new Uri("https://example.org/"), "Public domain", "EIA",
            new DateTime(2025, 12, 31, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 12, 3, 0, 0, 0, DateTimeKind.Utc), CitationDate.Year)
        { PublishedUtc = new DateTime(2026, 10, 19, 0, 0, 0, DateTimeKind.Utc) };
        var row = RowOf(Make(Inputs(datasets: new Dictionary<string, SourceCitation> { ["eia-crude-oil"] = published })), "eia");
        Assert.Equal("Source: U.S. Energy Information Administration (Oct 2026)", row.Credit);
        // No published day known: the date is dropped, never replaced by the download day.
        var unknown = published with { PublishedUtc = null };
        var bare = RowOf(Make(Inputs(datasets: new Dictionary<string, SourceCitation> { ["eia-crude-oil"] = unknown })), "eia");
        Assert.Equal("Source: U.S. Energy Information Administration", bare.Credit);
        // The real extract carries its manifest's published day.
        Assert.Equal("Source: U.S. Energy Information Administration (Nov 2026)", RowOf(Make(Inputs(datasets: RealDatasets())), "eia").Credit);
    }

    [Fact]
    public void LicenceLinksAreCarriedOver()
    {
        var vm = Make(Inputs());
        var registry = Real();
        for (var i = 0; i < registry.Sources.Count; i++)
        {
            Assert.Equal(registry.Sources[i].LicenceUrl, vm.Rows[i].LicenceUrl);
            Assert.Equal(registry.Sources[i].Licence, vm.Rows[i].Licence);
            Assert.Equal(registry.Sources[i].Page, vm.Rows[i].Page);
        }
    }

    [Fact]
    public void LinkOpensInTheBrowser()
    {
        var opener = new RecordingOpener();
        var vm = Make(Inputs(), opener);
        vm.OpenLinkCommand.Execute(new Uri("https://www.fao.org/faostat/en/#data/FS"));
        Assert.Single(opener.Opened);
        Assert.Equal("", vm.Message);
    }

    [Fact]
    public void LinkThatFailsToOpenShowsAMessage()
    {
        var vm = Make(Inputs(), new ThrowingOpener());
        vm.OpenLinkCommand.Execute(new Uri("https://www.fao.org/faostat/en/#data/FS"));
        Assert.Contains("could not be opened", vm.Message);
        Assert.Contains("https://www.fao.org/faostat/en/#data/FS", vm.Message);
    }

    [Fact]
    public void OnlyHttpsLinksOpen()
    {
        var opener = new RecordingOpener();
        var vm = Make(Inputs(), opener);
        vm.OpenLinkCommand.Execute(new Uri("http://example.org/"));
        vm.OpenLinkCommand.Execute(new Uri("file:///C:/Windows/notepad.exe"));
        Assert.Empty(opener.Opened);
        Assert.NotEmpty(vm.Message);
    }

    [Fact]
    public void UnreadableSourceListSaysSo()
    {
        var vm = new DataViewModel(new SourceRegistry([]), () => Inputs(), () => T, new RecordingOpener());
        Assert.Empty(vm.Rows);
        Assert.Equal("The source list could not be read; see the log", vm.Message);
    }

    [Fact]
    public void RefreshFollowsTheData()
    {
        var inputs = Inputs();
        var vm = new DataViewModel(Real(), () => inputs, () => T, new RecordingOpener());
        Assert.Equal("not yet checked", RowOf(vm, "ucdp").AsOf);
        inputs = Inputs(UcdpSnapshot(T.AddDays(-3), T));
        vm.Refresh();
        Assert.StartsWith("as of", RowOf(vm, "ucdp").AsOf);
    }

    [Fact]
    public void MainViewModelOwnsTheCardAndRefreshesItOnStoreChanges()
    {
        var store = new CrisisStore();
        var main = new MainViewModel(store, new MemorySettingsStore(), _ => null, null, () => T, registry: Real(), datasets: RealDatasets());
        Assert.Equal(27, main.Registry.Sources.Count);
        Assert.Equal(27, main.Data.Rows.Count);
        Assert.Equal("not yet checked", RowOf(main.Data, "ucdp").AsOf);
        main.Cards.Show(AppCard.Data);
        Assert.Equal(AppCard.Data, main.Cards.Open);
        store.Load(UcdpSnapshot(T.AddDays(-3), T));   // a feed finished while the card is open
        Assert.StartsWith("as of", RowOf(main.Data, "ucdp").AsOf);
        Assert.True(main.Cards.CloseTop());
        Assert.Equal(AppCard.None, main.Cards.Open);
    }
}

public sealed class CardHostTests
{
    private static CardHost Host() => new();

    [Fact]
    public void OneCardAtATime()
    {
        var host = Host();
        host.Register(AppCard.Welcome);
        host.Register(AppCard.Data);
        Assert.Equal(AppCard.None, host.Open);
        host.Show(AppCard.Welcome);
        Assert.True(host.WelcomeOpen);
        host.Show(AppCard.Data);
        Assert.Equal(AppCard.Data, host.Open);
        Assert.False(host.WelcomeOpen);
        Assert.True(host.DataOpen);
        Assert.False(host.AboutOpen);
    }

    [Fact]
    public void EscClosesTheCardBeforeTheExplainer()
    {
        var host = Host();
        host.Register(AppCard.Data);
        Assert.False(host.CloseTop());   // nothing open: Esc goes on to the explainer, then the panel
        host.Show(AppCard.Data);
        Assert.True(host.CloseTop());    // a card is open: Esc closes it first
        Assert.Equal(AppCard.None, host.Open);
        Assert.False(host.CloseTop());
    }

    [Fact]
    public void ACardThatIsNotRegisteredIsNotAvailable()
    {
        var host = Host();
        Assert.False(host.IsAvailable(AppCard.About));
        Assert.True(host.IsAvailable(AppCard.None));
        var ex = Assert.Throws<InvalidOperationException>(() => host.Show(AppCard.About));
        Assert.Equal("card 'about' is not available", ex.Message);
        Assert.Equal(AppCard.None, host.Open);
        host.Register(AppCard.About);
        host.Show(AppCard.About);
        Assert.True(host.AboutOpen);
    }

    [Fact]
    public void OpeningACardTellsItsOwner()
    {
        var host = Host();
        int opened = 0;
        host.Register(AppCard.Data, () => opened++);
        host.Show(AppCard.Data);
        host.Show(AppCard.Data);   // already open: nothing new
        Assert.Equal(1, opened);
    }

    [Fact]
    public void ChangesAreAnnounced()
    {
        var host = Host();
        host.Register(AppCard.Data);
        var names = new List<string?>();
        host.PropertyChanged += (_, e) => names.Add(e.PropertyName);
        host.Show(AppCard.Data);
        Assert.Contains(nameof(CardHost.Open), names);
        Assert.Contains(nameof(CardHost.DataOpen), names);
    }

    [Fact]
    public void DataCardFitsTheWindow()
    {
        // At most 900 x 640 at 100%, bigger with the text size, never more than the window less 24.
        Assert.Equal(new System.Windows.Size(900, 640), TextScaleLayout.DataCardMax(1600, 1000, 1.0));
        Assert.Equal(new System.Windows.Size(1000 - 24, 760 - 24), TextScaleLayout.DataCardMax(1000, 760, 1.5));
        Assert.Equal(new System.Windows.Size(900, 600 - 24), TextScaleLayout.DataCardMax(1000, 600, 1.0));
    }
}
