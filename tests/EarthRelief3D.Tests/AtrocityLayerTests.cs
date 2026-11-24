using System.Windows.Media.Media3D;
using EarthRelief3D.App.Curated;
using EarthRelief3D.App.Layers;
using EarthRelief3D.App.Presentation;
using EarthRelief3D.App.ViewModels;
using EarthRelief3D.Core.Curated;
using EarthRelief3D.Core.Feeds;
using EarthRelief3D.Core.Store;
using GlobeCore.Wpf.Layers;
using GlobeCore.Wpf.Rendering;
using static EarthRelief3D.Tests.CuratedTablesTests;

namespace EarthRelief3D.Tests;

/// <summary>
/// The atrocities layer (C14, P3-4, C15): a glyph only while a statement from the last 12 months says the situation
/// continues, otherwise a panel history row; a government's determinations only while that government is chosen; drafts
/// only with <c>--show-drafts</c>, and then labelled.
/// </summary>
public class AtrocityLayerTests
{
    private static readonly CountryResolver Countries = CountryResolver.LoadDefault();

    // Looking straight down on Myanmar's label point from 3 Earth radii.
    private static (SceneContext Ctx, Projector Projector) ViewOn(string iso3)
    {
        var p = Countries.ByIso3(iso3)!.LabelPoint;
        var eye = MarkerMeshes.Above(p, 3);
        var camera = new PerspectiveCamera(eye, new Point3D(0, 0, 0) - eye, new Vector3D(0, 0, 1), 45);
        var projector = new Projector(camera, 800, 600);
        return (new SceneContext(camera, projector, Now), projector);
    }

    private static AtrocityLayer Layer(CuratedStore store) => new(store, Countries, () => Now);

    private static CountryPanelViewModel Panel(CuratedStore store) =>
        new(new CrisisStore(), iso => Countries.ByIso3(iso)?.Name, null, () => Now, curated: store);

    [Fact]
    public void PastDesignationIsHistoryNotMarker() => StaAsync.Sta(() =>
    {
        var past = Row("past", "RWA", AuthorityType.CourtJudgment);
        var stale = Row("stale", "KHM", AuthorityType.CourtJudgment, continuing: Still(new DateOnly(2025, 1, 1)));
        var store = new CuratedStore(Tables([past, stale]), showDrafts: false);
        var layer = Layer(store);
        layer.Update(ViewOn("RWA").Ctx);
        Assert.Equal(0, layer.MarkerCount);

        var panel = Panel(store);
        panel.Show("RWA");
        Assert.DoesNotContain(panel.Sections, s => s.Heading == CountryPanelViewModel.AtrocityHeading);
        var history = Assert.Single(panel.Sections, s => s.Heading == CountryPanelViewModel.AtrocityHistoryHeading);
        Assert.Contains(history.Rows, r => r.Label.Contains("Court judgment: genocide"));
        Assert.Contains(history.Links!, l => l.Url == past.DocumentUrl);
        // A past designation is not a current crisis.
        Assert.True(panel.HasNoCrisis);

        // A statement within 12 months puts the country on the map, and the panel lists it (with the statement) at the top.
        var current = Row("now", "MMR", continuing: Still(new DateOnly(2026, 7, 13)));
        store.Replace(Tables([past, stale, current]));
        var (ctx, projector) = ViewOn("MMR");
        layer.Update(ctx);
        Assert.Equal(1, layer.MarkerCount);
        Assert.True(projector.TryProject(MarkerMeshes.Above(Countries.ByIso3("MMR")!.LabelPoint, AtrocityLayer.Height), out var at));
        var hit = layer.HitTest(at, projector, hover: true);
        Assert.NotNull(hit);
        Assert.Equal("Myanmar · UN body finding: genocide (Test, 2020)", hit.Tooltip);
        var pick = Assert.IsType<CountryPick>(hit.Item);
        Assert.Equal("MMR", pick.Iso3);
        Assert.Equal(MarkerLayer.Atrocities, pick.Marker);
        panel.Show("MMR", marker: MarkerLayer.Atrocities);
        var section = Assert.Single(panel.Sections, s => s.Heading == CountryPanelViewModel.AtrocityHeading);
        Assert.Equal(panel.Sections.ToList().IndexOf(section), panel.FocusedSectionIndex);
        Assert.False(panel.HasNoCrisis);   // a current designation is a crisis (C11)
        Assert.Contains(section.Rows, r => r.Value.Contains("The situation continues.") && r.Value.Contains("13 Jul 2026"));
    });

    [Fact]
    public void NotEstablishedIsNeverAGlyph() => StaAsync.Sta(() =>
    {
        // Validation refuses a continuing statement on it; the layer would not draw one anyway.
        var hrv = Row("hrv", "HRV", AuthorityType.CourtJudgment) with { Outcome = FindingOutcome.NotEstablished };
        var store = new CuratedStore(Tables([hrv]), showDrafts: false);
        var layer = Layer(store);
        layer.Update(ViewOn("HRV").Ctx);
        Assert.Equal(0, layer.MarkerCount);
        var panel = Panel(store);
        panel.Show("HRV");
        var history = Assert.Single(panel.Sections, s => s.Heading == CountryPanelViewModel.AtrocityHistoryHeading);
        Assert.Contains(history.Rows, r => r.Label == "Court judgment: genocide not established");
        // "Not established" is not a crisis.
        Assert.True(panel.HasNoCrisis);
    });

    [Fact]
    public void DeterminationHiddenWhenItsGovernmentIsOff() => StaAsync.Sta(() =>
    {
        var us = Row("us", "SDN", AuthorityType.GovernmentDetermination, gov: "USA", continuing: Still(new DateOnly(2026, 3, 10)));
        var store = new CuratedStore(Tables([us]), showDrafts: false);
        var layer = Layer(store);
        var ctx = ViewOn("SDN").Ctx;
        layer.Update(ctx);
        Assert.Equal(1, layer.MarkerCount);

        store.SetGovernments([]);
        layer.Update(ctx);
        Assert.Equal(0, layer.MarkerCount);
        var panel = Panel(store);
        panel.Show("SDN");
        Assert.DoesNotContain(panel.Sections, s => s.Heading.StartsWith("Genocide", StringComparison.Ordinal));

        store.SetGovernments(["USA"]);
        layer.Update(ctx);
        Assert.Equal(1, layer.MarkerCount);
        panel.Show("SDN");
        Assert.Contains(panel.Sections, s => s.Heading == CountryPanelViewModel.AtrocityHeading
            && s.Rows.Any(r => r.Label.Contains("United States government determination")));
    });

    [Fact]
    public void DraftsHiddenUnlessShowDrafts() => StaAsync.Sta(() =>
    {
        var draft = Row("draft", "MMR", status: Review.Draft, continuing: Still(new DateOnly(2026, 7, 13)));
        var camp = Camp(status: Review.Draft);
        var hidden = new CuratedStore(Tables([draft], [camp]), showDrafts: false);
        var layer = Layer(hidden);
        layer.Update(ViewOn("MMR").Ctx);
        Assert.Equal(0, layer.MarkerCount);
        Assert.Empty(CampMarkers.For(hidden).Items);
        var panel = Panel(hidden);
        panel.Show("MMR");
        Assert.DoesNotContain(panel.Sections, s => s.Heading.StartsWith("Genocide", StringComparison.Ordinal));
        panel.Show("PRK");
        Assert.DoesNotContain(panel.Sections, s => s.Heading == CountryPanelViewModel.CampsHeading);

        var shown = new CuratedStore(Tables([draft], [camp]), showDrafts: true);
        var layer2 = Layer(shown);
        layer2.Update(ViewOn("MMR").Ctx);
        Assert.Equal(1, layer2.MarkerCount);
        Assert.Single(CampMarkers.For(shown).Items);
    });

    [Fact]
    public void DraftsAreLabelled() => StaAsync.Sta(() =>
    {
        var draft = Row("draft", "MMR", status: Review.Draft, continuing: Still(new DateOnly(2026, 7, 13)));
        var camp = Camp(status: Review.Draft);
        var store = new CuratedStore(Tables([draft], [camp]), showDrafts: true);
        var layer = Layer(store);
        var (ctx, projector) = ViewOn("MMR");
        layer.Update(ctx);
        Assert.True(projector.TryProject(MarkerMeshes.Above(Countries.ByIso3("MMR")!.LabelPoint, AtrocityLayer.Height), out var at));
        var hit = layer.HitTest(at, projector, hover: true)!;
        Assert.Contains(DesignationText.DraftLabel, hit.Tooltip);
        Assert.Equal("Draft: not yet reviewed", DesignationText.DraftLabel);

        var panel = Panel(store);
        panel.Show("MMR");
        Assert.All(panel.Sections.Single(s => s.Heading == CountryPanelViewModel.AtrocityHeading).Rows.Where(r => r.Label.Contains("finding")),
            r => Assert.StartsWith(DesignationText.DraftLabel, r.Label, StringComparison.Ordinal));
        panel.Show("PRK");
        var camps = panel.Sections.Single(s => s.Heading == CountryPanelViewModel.CampsHeading);
        Assert.All(camps.Rows, r => Assert.StartsWith(DesignationText.DraftLabel, r.Label, StringComparison.Ordinal));
        Assert.All(CampMarkers.For(store).Items, i => Assert.StartsWith(DesignationText.DraftLabel, i.Tooltip, StringComparison.Ordinal));

        // An approved row carries no draft label.
        var approved = new CuratedStore(Tables([Row("ok", "MMR", continuing: Still(new DateOnly(2026, 7, 13)))]), showDrafts: true);
        var l3 = Layer(approved);
        l3.Update(ctx);
        Assert.DoesNotContain(DesignationText.DraftLabel, l3.HitTest(at, projector, hover: true)!.Tooltip);
    });

    [Fact]
    public void LegendSaysWhatIsShownAndWhy()
    {
        var settings = new MemorySettingsStore(new App.Settings.AppSettings { Markers = [MarkerLayer.Atrocities, MarkerLayer.Camps] });
        var draft = Row("draft", "MMR", status: Review.Draft, continuing: Still(new DateOnly(2026, 7, 13)));
        var camp = Camp(status: Review.Draft);

        // A normal run: nothing approved yet, and the legend says why the layers are empty (P3-4).
        var normal = new MainViewModel(new CrisisStore(), settings, iso => Countries.ByIso3(iso)?.Name, null, () => Now,
            curated: new CuratedStore(Tables([draft], [camp]), showDrafts: false));
        var atrocities = normal.Legend.Entries.Single(e => e.Id == "atrocities");
        Assert.Equal(LegendViewModel.NoneApprovedNote, atrocities.Note);
        Assert.Equal("checked 23 Nov 2026", atrocities.AsOf);
        var camps = normal.Legend.Entries.Single(e => e.Id == "camps");
        Assert.Equal(CampMarkers.NoneApprovedNote, camps.Note);
        Assert.Empty(normal.MarkerData.Items(MarkerLayer.Camps));

        // A --show-drafts run: one triangle, the camp square, both labelled as drafts.
        var drafts = new MainViewModel(new CrisisStore(), settings, iso => Countries.ByIso3(iso)?.Name, null, () => Now,
            curated: new CuratedStore(Tables([draft], [camp]), showDrafts: true));
        var a2 = drafts.Legend.Entries.Single(e => e.Id == "atrocities");
        Assert.Contains("(1 country)", a2.Note);
        Assert.Contains(CampMarkers.DraftsNote, a2.Note);
        Assert.Equal(CampMarkers.DraftsNote, drafts.Legend.Entries.Single(e => e.Id == "camps").Note);
        Assert.Single(drafts.MarkerData.Items(MarkerLayer.Camps));
        Assert.Equal("PRK", drafts.MarkerData.Items(MarkerLayer.Camps)[0].CountryIso3);
    }

    [Fact]
    public void ShowDraftsIsADiagnosticOption()
    {
        Assert.False(App.Diagnostics.LaunchOptions.Parse([]).ShowDrafts);
        var o = App.Diagnostics.LaunchOptions.Parse(["--show-drafts", "--open-panel", "MMR"]);
        Assert.True(o.ShowDrafts);
        Assert.Equal("MMR", o.OpenPanel);
        Assert.Null(o.Error);
    }

    [Fact]
    public void GlyphIsAHollowTriangleWithADarkEdge() => StaAsync.Sta(() =>
    {
        var store = new CuratedStore(Tables([Row("now", "MMR", continuing: Still(new DateOnly(2026, 7, 13)))]), showDrafts: false);
        var layer = Layer(store);
        layer.Update(ViewOn("MMR").Ctx);
        var group = Assert.IsType<Model3DGroup>(layer.Root.Content);
        static System.Windows.Media.Color ColorOf(Model3D m) =>
            ((System.Windows.Media.SolidColorBrush)((DiffuseMaterial)((GeometryModel3D)m).Material).Brush).Color;
        Assert.Equal(2, group.Children.Count);
        Assert.Equal(LayerStyle.MarkerEdgeColor, ColorOf(group.Children[0]));   // the dark edge first, under
        Assert.Equal(LayerStyle.AtrocityColor, ColorOf(group.Children[1]));
        Assert.Equal(System.Windows.Media.Color.FromRgb(0xF2, 0xF2, 0xF2), LayerStyle.AtrocityColor);
        Assert.Equal(MarkerShape.TriangleOutline, LayerStyle.Shape(MarkerLayer.Atrocities));
        // No other marker layer uses the hollow triangle (C8: shape as the second cue).
        Assert.Single(Enum.GetValues<MarkerLayer>(), m => LayerStyle.Shape(m) == MarkerShape.TriangleOutline);
        Assert.Equal(MarkerShape.Square, LayerStyle.Shape(MarkerLayer.Camps));
    });
}
