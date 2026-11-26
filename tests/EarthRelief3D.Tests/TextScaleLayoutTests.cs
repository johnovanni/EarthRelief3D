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

    /// <summary>A tall update banner at 100% (four lines and a row of buttons), times the text size, for the model (R13).</summary>
    internal const double BannerTestBaseHeight = 130;

    public static TheoryData<double, double, double, bool, bool, bool> Cases()
    {
        var d = new TheoryData<double, double, double, bool, bool, bool>();
        foreach (var s in TextScaleLayout.Choices)
            foreach (var (w, h) in new[] { (1000.0, 760.0), (1200.0, 760.0) })
                foreach (var legendOpen in new[] { false, true })
                    foreach (var banner in new[] { false, true })
                        d.Add(s, w, h, legendOpen, true, banner);
        return d;
    }

    /// <summary>The model: no card covers another, with or without the update banner (R13: it covers no card).</summary>
    [Theory]
    [MemberData(nameof(Cases))]
    public void CardsDoNotOverlapAt150PercentIn1000Wide(double scale, double width, double height, bool legendOpen, bool explainerOpen, bool banner)
    {
        var window = new Size(width, height);
        var r = TextScaleLayout.CardBounds(window, scale, panelOpen: true, explainerOpen: explainerOpen, legendOpen: legendOpen,
            bannerHeight: banner ? BannerTestBaseHeight * scale : 0);
        string[] names = ["strip", "legend", "panel", "explainer", "sources", "banner"];
        Assert.Equal(6, r.Length);
        Assert.Equal(!banner, r[5].IsEmpty);
        var all = new Rect(0, 0, width, height);
        for (int i = 0; i < r.Length; i++)
        {
            if (i == 5 && !banner) continue;
            Assert.False(r[i].IsEmpty, names[i] + " is shown");
            Assert.True(r[i].Width > 0 && r[i].Height > 0, $"{names[i]} has a size at {scale}");
            Assert.True(all.Contains(r[i]), $"{names[i]} {r[i]} lies inside the {width}x{height} window at {scale}");
            for (int j = i + 1; j < r.Length; j++)
                Assert.False(r[i].IntersectsWith(r[j]), $"{names[i]} {r[i]} covers {names[j]} {r[j]} at {scale}, {width}x{height}, legend {(legendOpen ? "open" : "closed")}, banner {banner}");
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
        // The banner (R13): from the strip's right edge to the window's right edge, at the top; the panel and the explainer start below it.
        var withBanner = TextScaleLayout.CardBounds(new Size(1200, 760), 1.0, panelOpen: true, explainerOpen: true, legendOpen: false, bannerHeight: 100);
        Assert.Equal(new Rect(TextScaleLayout.StripBaseWidth + 2 * TextScaleLayout.Gap, TextScaleLayout.Gap,
            1200 - TextScaleLayout.Gap - (TextScaleLayout.StripBaseWidth + 2 * TextScaleLayout.Gap), 100), withBanner[5]);
        Assert.Equal(withBanner[5].Bottom + TextScaleLayout.Gap, withBanner[2].Top);
        Assert.Equal(withBanner[5].Bottom + TextScaleLayout.Gap, withBanner[3].Top);
        Assert.Equal(0, TextScaleLayout.BannerOffset(0));
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
    [InlineData(1.0, 1000, 760, true)]
    [InlineData(1.25, 1000, 760, true)]
    [InlineData(1.5, 1000, 760, true)]
    [InlineData(1.0, 1200, 760, true)]
    [InlineData(1.5, 1200, 760, true)]
    public void TheRealCardsDoNotOverlapEither(double scale, double width, double height, bool banner = false) => StaAsync.Run(async () =>
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
        // R13: with the update banner's offer showing (the longest text), it covers no card.
        if (banner)
            vm.Updates.ShowForPicture(EarthRelief3D.Core.Updates.UpdateCheck.Parse(Samples.Text("github_release_latest_newer.json")),
                EarthRelief3D.App.ViewModels.UpdateBannerState.Offer);
        foreach (var legendOpen in new[] { false, true })
        {
            vm.Legend.IsOpen = legendOpen;
            for (int pass = 0; pass < 4; pass++)   // each pass can set limits that change a card's size
            {
                root.Measure(new Size(width, height));
                root.Arrange(new Rect(0, 0, width, height));
                root.UpdateLayout();
            }
            string[] names = banner ? ["Strip", "LegendCard", "Panel", "ExplainerCard", "StatusCard", "UpdateBannerView"]
                : ["Strip", "LegendCard", "Panel", "ExplainerCard", "StatusCard"];
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
                        $"{names[i]} {rects[i]} covers {names[j]} {rects[j]} at {scale}, {width}x{height}, legend {(legendOpen ? "open" : "closed")}, banner {banner}");
                }
            }
        }
    });
}
