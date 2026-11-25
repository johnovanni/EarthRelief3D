using System.Windows.Media;
using EarthRelief3D.App.Diagnostics;
using EarthRelief3D.App.Presentation;
using EarthRelief3D.App.Settings;
using EarthRelief3D.App.ViewModels;
using EarthRelief3D.App.Curated;

namespace EarthRelief3D.Tests;

/// <summary>Plan 5 Task 6: the Welcome card shows once, never in a diagnostic run, and every way of closing it marks it shown.</summary>
public sealed class WelcomeViewModelTests
{
    private static (WelcomeViewModel Vm, MemorySettingsStore Store, CardHost Cards, SettingsViewModel Settings) Make(AppSettings? initial = null)
    {
        var store = new MemorySettingsStore(initial);
        var settings = new SettingsViewModel(store, CuratedStore.Empty());
        var cards = new CardHost();
        var vm = new WelcomeViewModel(store, settings, cards);
        cards.Register(AppCard.Welcome);
        return (vm, store, cards, settings);
    }

    [Fact]
    public void ShownOnlyOnFirstRun()
    {
        var normal = LaunchOptions.Parse([]);
        Assert.True(WelcomeViewModel.ShouldShow(new AppSettings(), normal));
        Assert.False(WelcomeViewModel.ShouldShow(new AppSettings { WelcomeShown = true }, normal));
    }

    [Fact]
    public void NotShownInDiagnosticOrOfflineRuns()
    {
        var fresh = new AppSettings();
        Assert.False(WelcomeViewModel.ShouldShow(fresh, LaunchOptions.Parse(["--offline-samples", "x"])));
        Assert.False(WelcomeViewModel.ShouldShow(fresh, LaunchOptions.Parse(["--snapshot", "a.png"])));
        Assert.False(WelcomeViewModel.ShouldShow(fresh, LaunchOptions.Parse(["--snapshot-scenes", "s.json", "--snapshot-out", "o"])));
    }

    [Fact]
    public void Plan4SettingsFileShowsItOnce()
    {
        // A Plan 4 file has no WelcomeShown: it reads as not shown, then the flag is saved and the file shows it no more.
        var old = System.Text.Json.JsonSerializer.Deserialize<AppSettings>("""{ "ShowConflict": false, "UserState": "OH" }""")!;
        Assert.False(old.WelcomeShown);
        Assert.True(WelcomeViewModel.ShouldShow(old, LaunchOptions.Parse([])));
        var (vm, store, cards, _) = Make(old);
        cards.Show(AppCard.Welcome);
        vm.StartCommand.Execute(null);
        Assert.True(store.Current.WelcomeShown);
        Assert.False(WelcomeViewModel.ShouldShow(store.Current, LaunchOptions.Parse([])));
    }

    [Fact]
    public void EveryWayOfClosingMarksItShownOnce()
    {
        foreach (var close in new Action<WelcomeViewModel, CardHost>[]
        {
            (vm, _) => vm.StartCommand.Execute(null),
            (_, cards) => cards.CloseTop(),
            (vm, _) => vm.OpenSettingsCommand.Execute(null),
        })
        {
            var initial = new AppSettings { ShowConflict = false, UserState = "OH", TextScale = 1.25, LegendOpen = true };
            var (vm, store, cards, _) = Make(initial);
            cards.Show(AppCard.Welcome);
            Assert.Equal(0, store.Saves);
            close(vm, cards);
            Assert.Equal(AppCard.None, cards.Open);
            Assert.Equal(1, store.Saves);
            Assert.Equal(initial with { WelcomeShown = true }, store.Current);
            // Opened again (About's "Show the welcome again") and closed again: nothing more is saved.
            cards.Show(AppCard.Welcome);
            cards.CloseTop();
            Assert.Equal(1, store.Saves);
        }
    }

    [Fact]
    public void KeysFollowLayerStyle()
    {
        var (vm, _, _, _) = Make();
        Assert.Equal(7, vm.Keys.Count);
        void Check(int i, string symbol, Color color) { Assert.Equal(symbol, vm.Keys[i].Symbol); Assert.Equal(color, vm.Keys[i].Color); }
        Check(0, LayerStyle.Symbol(MarkerLayer.Conflict), LayerStyle.SymbolColor(MarkerLayer.Conflict));
        for (int p = 3; p <= 5; p++)
        {
            Check(p - 2, LayerStyle.Symbol(ShadingLayer.Food), LayerStyle.IpcPhaseColor(p));
            Assert.Contains(LayerStyle.IpcPhaseLabel(p), vm.Keys[p - 2].Text);
        }
        Check(4, LayerStyle.Symbol(MarkerLayer.Outbreaks), LayerStyle.SymbolColor(MarkerLayer.Outbreaks));
        Check(5, LayerStyle.Symbol(MarkerLayer.Displacement), LayerStyle.SymbolColor(MarkerLayer.Displacement));
        Check(6, LayerStyle.Symbol(MarkerLayer.HubLines), LayerStyle.SymbolColor(MarkerLayer.HubLines));
        // Each key names its layer in the catalog's words.
        Assert.Contains(LayerCatalog.Label(MarkerLayer.Conflict), vm.Keys[0].Text);
        Assert.Contains(LayerCatalog.Label(MarkerLayer.Outbreaks), vm.Keys[4].Text);
        Assert.Contains(LayerCatalog.Label(MarkerLayer.Displacement), vm.Keys[5].Text);
        Assert.Contains(LayerCatalog.Label(MarkerLayer.HubLines), vm.Keys[6].Text);
        // These are exactly the layers on by default.
        var defaults = new AppSettings();
        Assert.Equal(ShadingLayer.Food, defaults.EffectiveShading);
        Assert.Equal(LayerCatalog.DefaultMarkers, defaults.EffectiveMarkers);
    }

    [Fact]
    public void TextSizeChoiceChangesTheSetting()
    {
        var (vm, store, _, settings) = Make();
        Assert.Equal(1.0, vm.TextScale);
        vm.TextScale = 1.5;
        Assert.Equal(1.5, settings.TextScale);
        Assert.Equal(1.5, store.Current.TextScale);
        settings.TextScale = 1.25;
        Assert.Equal(1.25, vm.TextScale);
        var changed = new List<string?>();
        vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName);
        settings.TextScale = 1.0;
        Assert.Contains(nameof(WelcomeViewModel.TextScale), changed);
        Assert.True(vm.IsScale100);
    }

    [Fact]
    public void OpenSettingsOpensTheFlyout()
    {
        var (vm, _, cards, settings) = Make();
        cards.Show(AppCard.Welcome);
        Assert.False(settings.IsOpen);
        vm.OpenSettingsCommand.Execute(null);
        Assert.True(settings.IsOpen);
        Assert.Equal(AppCard.None, cards.Open);
    }

    [Fact]
    public void CopyIsTheOwnersWording()
    {
        Assert.Equal("Welcome to EarthRelief3D", WelcomeViewModel.TitleText);
        Assert.Equal(2, WelcomeViewModel.Intro.Count);
        Assert.StartsWith("This globe shows where people are suffering", WelcomeViewModel.Intro[0]);
        Assert.All(WelcomeViewModel.Intro.Concat([WelcomeViewModel.KeysNote, WelcomeViewModel.PanelText, WelcomeViewModel.StateText]), p => Assert.DoesNotContain("click here", p, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CardIsAtMost620WideAtOneHundredPercentAndFitsTheWindow()
    {
        Assert.Equal(620, TextScaleLayout.WelcomeCardMax(1600, 900, 1.0).Width);
        Assert.Equal(930, TextScaleLayout.WelcomeCardMax(1600, 900, 1.5).Width);
        var narrow = TextScaleLayout.WelcomeCardMax(1000, 760, 1.5);
        Assert.Equal(930, narrow.Width);
        Assert.True(TextScaleLayout.WelcomeCardMax(800, 760, 1.5).Width <= 800 - 24);
        Assert.Equal(736, narrow.Height);
    }
}
