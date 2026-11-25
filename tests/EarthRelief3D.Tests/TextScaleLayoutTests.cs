using System.Windows;
using EarthRelief3D.App.Presentation;

namespace EarthRelief3D.Tests;

/// <summary>Text size (C8; P5-8): the cards' sizes at 100%, 125% and 150%, from the same numbers the window uses.</summary>
public class TextScaleLayoutTests
{
    [Fact]
    public void PanelWidthIsCapped()
    {
        Assert.Equal([1.0, 1.25, 1.5], TextScaleLayout.Choices);
        Assert.Equal(420, TextScaleLayout.PanelBaseWidth);
        Assert.Equal(420, TextScaleLayout.PanelWidth(1200, 1.0));
        Assert.Equal(540, TextScaleLayout.PanelWidth(1200, 1.5), 6);    // 0.45 of the window, not 630
        Assert.Equal(450, TextScaleLayout.PanelWidth(1000, 1.25), 6);   // 0.45 of the window, not 525
    }

    public static TheoryData<double, double, double, bool, bool> Cases()
    {
        var d = new TheoryData<double, double, double, bool, bool>();
        foreach (var s in TextScaleLayout.Choices)
            foreach (var (w, h) in new[] { (1000.0, 760.0), (1200.0, 760.0) })
                foreach (var legendOpen in new[] { false, true })
                    d.Add(s, w, h, legendOpen, true);
        return d;
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void CardsDoNotOverlapAt150PercentIn1000Wide(double scale, double width, double height, bool legendOpen, bool explainerOpen)
    {
        var window = new Size(width, height);
        var r = TextScaleLayout.CardBounds(window, scale, panelOpen: true, explainerOpen: explainerOpen, legendOpen: legendOpen);
        string[] names = ["strip", "legend", "panel", "explainer", "sources"];
        Assert.Equal(5, r.Length);
        var all = new Rect(0, 0, width, height);
        for (int i = 0; i < r.Length; i++)
        {
            Assert.False(r[i].IsEmpty, names[i] + " is shown");
            Assert.True(r[i].Width > 0 && r[i].Height > 0, $"{names[i]} has a size at {scale}");
            Assert.True(all.Contains(r[i]), $"{names[i]} {r[i]} lies inside the {width}x{height} window at {scale}");
            for (int j = i + 1; j < r.Length; j++)
                Assert.False(r[i].IntersectsWith(r[j]), $"{names[i]} {r[i]} covers {names[j]} {r[j]} at {scale}, {width}x{height}, legend {(legendOpen ? "open" : "closed")}");
        }
    }

    [Fact]
    public void AClosedCardTakesNoRoomAndTheExplainerSharesTheColumnWhenTheSideIsTooNarrow()
    {
        var narrow = new Size(1000, 760);
        var closed = TextScaleLayout.CardBounds(narrow, 1.5, panelOpen: false, explainerOpen: false, legendOpen: false);
        Assert.True(closed[2].IsEmpty);
        Assert.True(closed[3].IsEmpty);
        // 150% in the narrowest window leaves under 240 beside the panel: the explainer goes under the panel.
        var both = TextScaleLayout.CardBounds(narrow, 1.5, panelOpen: true, explainerOpen: true, legendOpen: false);
        Assert.True(both[3].Top >= both[2].Bottom);
        Assert.Equal(both[2].Left, both[3].Left, 6);
        // At 100% in a wide window it stays beside the panel, as before.
        var wide = TextScaleLayout.CardBounds(new Size(1200, 760), 1.0, panelOpen: true, explainerOpen: true, legendOpen: false);
        Assert.True(wide[3].Right <= wide[2].Left);
    }

    [Theory]
    [InlineData(1.0, 1.0)]
    [InlineData(1.25, 1.25)]
    [InlineData(1.5, 1.5)]
    [InlineData(2.0, 1.0)]
    [InlineData(1.3, 1.0)]
    [InlineData(0.0, 1.0)]
    [InlineData(double.NaN, 1.0)]
    public void OnlyTheThreeChoicesAreKept(double saved, double expected) => Assert.Equal(expected, TextScaleLayout.Normalise(saved));

    [Fact]
    public void CardScaleScalesTheInsideNotTheCard() => StaAsync.Run(async () =>
    {
        var text = new System.Windows.Controls.Border { Width = 100, Height = 40 };
        var card = new System.Windows.Controls.Border { Child = text, Margin = new Thickness(12) };
        card.InvalidateMeasure();
        card.Measure(new Size(2000, 2000));
        Assert.Equal(new Size(124, 64), card.DesiredSize);

        Assert.True(EarthRelief3D.App.Views.CardScale.Apply(card, 1.5));
        card.InvalidateMeasure();
        card.Measure(new Size(2000, 2000));
        Assert.Equal(new Size(100 * 1.5 + 24, 40 * 1.5 + 24), card.DesiredSize);   // the margin is the card's own: not scaled

        Assert.True(EarthRelief3D.App.Views.CardScale.Apply(card, 1.0));
        card.InvalidateMeasure();
        card.Measure(new Size(2000, 2000));
        Assert.Equal(new Size(124, 64), card.DesiredSize);
        Assert.False(EarthRelief3D.App.Views.CardScale.Apply(new System.Windows.Controls.TextBlock(), 1.5));   // nothing inside to scale
        await Task.CompletedTask;
    });

    /// <summary>The real window, not the model: at each size, in the narrowest window, the cards that are open do not cover one another.</summary>
    [Theory]
    [InlineData(1.0, 1000, 760)]
    [InlineData(1.25, 1000, 760)]
    [InlineData(1.5, 1000, 760)]
    [InlineData(1.5, 1200, 760)]
    public void TheRealCardsDoNotOverlapEither(double scale, double width, double height) => StaAsync.Run(async () =>
    {
        var store = await SampleStore.LoadAsync();
        var countries = EarthRelief3D.Core.Feeds.CountryResolver.LoadDefault();
        var settings = new MemorySettingsStore();
        var vm = new EarthRelief3D.App.ViewModels.MainViewModel(store, settings, iso => countries.ByIso3(iso)?.Name, dispatcher: null, boundaries: countries.Boundaries,
            curated: new EarthRelief3D.App.Curated.CuratedStore(EarthRelief3D.Core.Curated.CuratedTables.Load(System.IO.Path.Combine(AppContext.BaseDirectory, "data", "curated")), showDrafts: true));
        var window = new EarthRelief3D.App.MainWindow(vm, store, countries, new FixedClock(SampleStore.Now), new ListLog());
        var root = (FrameworkElement)window.Content;
        vm.Settings.TextScale = scale;
        Assert.Equal(scale, settings.Current.TextScale);
        vm.Panel.Show("SDN", CrisisLayer.Food);
        Assert.True(vm.Panel.Explainer.Open(EarthRelief3D.App.ViewModels.CountryPanelViewModel.FoodExplainerId));
        foreach (var legendOpen in new[] { false, true })
        {
            vm.Legend.IsOpen = legendOpen;
            for (int pass = 0; pass < 4; pass++)   // each pass can set limits that change a card's size
            {
                root.Measure(new Size(width, height));
                root.Arrange(new Rect(0, 0, width, height));
                root.UpdateLayout();
            }
            string[] names = ["Strip", "LegendCard", "Panel", "ExplainerCard", "StatusCard"];
            var rects = names.Select(n =>
            {
                var card = (FrameworkElement)window.FindName(n);
                return card.TransformToAncestor(root).TransformBounds(new Rect(card.RenderSize));
            }).ToArray();
            var all = new Rect(0, 0, width, height);
            for (int i = 0; i < rects.Length; i++)
            {
                Assert.True(rects[i].Width > 20 && rects[i].Height > 10, $"{names[i]} {rects[i]} has a size");
                Assert.True(all.Contains(new Rect(rects[i].X + 0.5, rects[i].Y + 0.5, Math.Max(0, rects[i].Width - 1), Math.Max(0, rects[i].Height - 1))),
                    $"{names[i]} {rects[i]} lies inside the {width}x{height} window at {scale}");
                for (int j = i + 1; j < rects.Length; j++)
                {
                    var overlap = Rect.Intersect(rects[i], rects[j]);
                    Assert.True(overlap.IsEmpty || overlap.Width < 1 || overlap.Height < 1,
                        $"{names[i]} {rects[i]} covers {names[j]} {rects[j]} at {scale}, {width}x{height}, legend {(legendOpen ? "open" : "closed")}");
                }
            }
        }
    });

    /// <summary>
    /// The update banner (P5-19) sits at the top centre, between the layer strip and the panel; where that room is too narrow it
    /// goes under the panel's header, in the panel's column. It never covers the strip or the panel's header.
    /// </summary>
    [Theory]
    [InlineData(1.0)]
    [InlineData(1.25)]
    [InlineData(1.5)]
    public void TheUpdateBannerNeverCoversTheStripOrThePanelsHeader(double scale)
    {
        foreach (var width in new[] { 1000.0, 1200.0, 1600.0 })
            foreach (var panelOpen in new[] { false, true })
                foreach (var stripW in new[] { 0.0, 200 * scale, TextScaleLayout.StripBaseWidth * scale })
                {
                    var window = new Size(width, 760);
                    double gap = TextScaleLayout.Gap, headerBottom = gap + 44 * scale;
                    var b = TextScaleLayout.BannerBounds(window, scale, stripW, panelOpen, headerBottom);
                    string at = $"{width} wide, {scale}, strip {stripW}, panel {(panelOpen ? "open" : "closed")}: banner {b}";
                    var strip = new Rect(gap, gap, Math.Max(1, stripW), 760 * TextScaleLayout.StripShare);
                    double panelW = TextScaleLayout.PanelWidth(width, scale);
                    var header = panelOpen ? new Rect(width - gap - panelW, gap, panelW, headerBottom - gap) : Rect.Empty;
                    var banner = new Rect(b.Left, b.Top, b.Width, 1);   // its height is its text's: only its top edge is fixed
                    Assert.True(b.Width >= TextScaleLayout.BannerMinBaseWidth * scale || (panelOpen && b.Width == panelW), at);
                    Assert.True(b.Width <= TextScaleLayout.BannerBaseWidth * scale + 0.001, at);
                    Assert.True(b.Left >= 0 && b.Right <= width, at);
                    Assert.False(banner.IntersectsWith(strip) && b.Top < strip.Bottom, "covers the strip: " + at);
                    if (panelOpen) Assert.False(new Rect(b.Left, b.Top, b.Width, 1).IntersectsWith(header) || (b.Top < header.Bottom && b.Right > header.Left), "covers the panel's header: " + at);
                }
        // Room enough: the window's centre.
        var wide = TextScaleLayout.BannerBounds(new Size(1600, 760), 1.0, 320, panelOpen: false, 56);
        Assert.Equal(800, wide.Left + wide.Width / 2, 6);
        Assert.Equal(TextScaleLayout.Gap, wide.Top);
    }
}
