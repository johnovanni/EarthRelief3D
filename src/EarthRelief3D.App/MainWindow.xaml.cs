using System.ComponentModel;
using System.Windows;
using EarthRelief3D.App.Presentation;
using EarthRelief3D.App.Views;
using EarthRelief3D.App.Controllers;
using EarthRelief3D.App.ViewModels;
using EarthRelief3D.Core.Feeds;
using EarthRelief3D.Core.Store;
using GlobeCore.Logging;
using GlobeCore.Time;

namespace EarthRelief3D.App;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel vm, CrisisStore store, CountryResolver countries, IClock clock, IAppLog? log = null)
    {
        InitializeComponent();
        DataContext = vm;
        Scene = new SceneController(Viewport, Overlay, vm, this, store, countries, clock, log);
        // No hover tooltips from the globe while the mouse is over a card.
        foreach (var card in new UIElement[] { Strip, Panel, ExplainerCard, LegendCard, StatusCard, DataCardView, AboutCardView }) Scene.HoverBlockers.Add(card);
        // Text size (C8, P5-8): every card is scaled from Settings, and sized from TextScaleLayout so none covers another.
        // A new card (Welcome, About, Data) goes in this list and in FitCards.
        _cards = [Strip, LegendCard, Panel, ExplainerCard, StatusCard, DataCardView, AboutCardView];
        ApplyTextScale();
        vm.Settings.TextScaleChanged += (_, _) => { ApplyTextScale(); FitCards(); };
        SizeChanged += (_, _) => FitCards();
        Root.SizeChanged += (_, _) => FitCards();
        // What each card's size depends on: the strip's width and height, the legend's and panel's height, the Sources card.
        Strip.SizeChanged += (_, _) => FitCards();
        LegendCard.SizeChanged += (_, _) => FitCards();   // opening or closing the legend changes what room the strip has
        Panel.SizeChanged += (_, _) => FitCards();
        StatusCard.SizeChanged += (_, _) => FitCards();
        // Opening or closing a card changes the room the others have: fit at once, not at the next size change (review m11).
        void OnOpenChanged(object? sender, PropertyChangedEventArgs e) { if (e.PropertyName == "IsOpen") FitCards(); }
        vm.Legend.PropertyChanged += OnOpenChanged;
        vm.Panel.PropertyChanged += OnOpenChanged;
        vm.Explainer.PropertyChanged += OnOpenChanged;
        vm.Cards.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(CardHost.Open)) FitCards(); };
        DataCardView.SizeChanged += (_, _) => FitCards();
        AboutCardView.SizeChanged += (_, _) => FitCards();
    }

    /// <summary>The "Data" button: opens the Data card, or closes it when it is open.</summary>
    private void OnDataButtonClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel vm) return;
        if (vm.Cards.Open == AppCard.Data) vm.Cards.CloseTop();
        else vm.Cards.Show(AppCard.Data);
    }

    private readonly FrameworkElement[] _cards;

    /// <summary>Puts the Settings text size on every card (<see cref="CardScale"/>).</summary>
    private void ApplyTextScale()
    {
        double scale = (DataContext as MainViewModel)?.Settings.TextScale ?? 1.0;
        foreach (var card in _cards) CardScale.Apply(card, scale);
    }

    /// <summary>The share of the window's height the layer strip may take before it scrolls (Plan 3's groups are long).</summary>
    public const double StripShare = TextScaleLayout.StripShare;

    /// <summary>The share of the window's height an open legend may take before it scrolls (P5-24).</summary>
    public const double LegendShare = TextScaleLayout.LegendShare;

    /// <summary>
    /// Sizes the cards from the window, the text size and what is open, with <see cref="TextScaleLayout"/>'s rules (the model the
    /// tests check): the strip scrolls when the window is short and never wider than its limit; the legend never grows over the
    /// strip above it, or under the panel; the panel and the explainer stay above the Sources card; the explainer stays right of
    /// the strip and left of the panel, or goes under the panel when the room beside it is too narrow.
    /// </summary>
    private void FitCards()
    {
        if (DataContext is not MainViewModel vm || Root.ActualWidth <= 0 || Root.ActualHeight <= 0) return;
        double w = Root.ActualWidth, h = Root.ActualHeight, scale = vm.Settings.TextScale;
        double sourcesW = StatusCard.ActualWidth, sourcesH = StatusCard.ActualHeight;

        SetMax(Strip, FrameworkElement.MaxWidthProperty, TextScaleLayout.StripBaseWidth * scale);
        double stripW = Strip.ActualWidth;
        bool explainerUnder = vm.Explainer.IsOpen && TextScaleLayout.IsStacked(w, scale, stripW);
        bool columnOpen = vm.Panel.IsOpen || explainerUnder;

        // The panel: its width from the text size; its height leaves the Sources card (and the explainer, when under it) its room.
        SetMax(Panel, FrameworkElement.WidthProperty, TextScaleLayout.PanelWidth(w, scale));
        SetMax(Panel, FrameworkElement.MaxHeightProperty, TextScaleLayout.PanelMaxHeight(h, sourcesH, explainerUnder));

        // The strip and the legend: a closed legend is only its header and the UN note, so the strip may take all the rest of the height.
        bool legendOpen = vm.Legend.IsOpen;
        SetMax(Strip, FrameworkElement.MaxHeightProperty, TextScaleLayout.StripMaxHeight(h, legendOpen, LegendCard.ActualHeight));
        SetMax(LegendCard, FrameworkElement.MaxWidthProperty, TextScaleLayout.LegendMaxWidth(w, scale, columnOpen, sourcesW));
        // Open: at most half the window's height, and never over the strip. Closed: no cap (it is two or three lines).
        SetMax(LegendCard, FrameworkElement.MaxHeightProperty, legendOpen ? TextScaleLayout.LegendMaxHeight(h, Strip.ActualHeight) : double.PositiveInfinity);

        // The Data card: centred, 900 x 640 at 100% at most, the text size times that, never over the window's edge less 24 (TextScaleLayout).
        var data = TextScaleLayout.DataCardMax(w, h, scale);
        SetMax(DataCardView, FrameworkElement.MaxWidthProperty, data.Width);
        SetMax(DataCardView, FrameworkElement.MaxHeightProperty, data.Height);
        // The About card has the same room as the Data card (they are never open together).
        SetMax(AboutCardView, FrameworkElement.MaxWidthProperty, data.Width);
        SetMax(AboutCardView, FrameworkElement.MaxHeightProperty, data.Height);

        // The explainer: beside the panel, or under it.
        var b = TextScaleLayout.ExplainerBounds(new Size(w, h), scale, stripW, vm.Panel.IsOpen, Panel.ActualHeight, LegendCard.ActualHeight, sourcesH);
        SetMax(ExplainerCard, FrameworkElement.MaxWidthProperty, Math.Max(1, b.Width));
        SetMax(ExplainerCard, FrameworkElement.MaxHeightProperty, Math.Max(1, b.Height));
        var margin = new Thickness(b.Left, b.Top, Math.Max(0, w - b.Right), 0);
        var m = ExplainerCard.Margin;
        if (Math.Abs(m.Left - margin.Left) > 0.5 || Math.Abs(m.Top - margin.Top) > 0.5 || Math.Abs(m.Right - margin.Right) > 0.5 || m.Bottom != 0)
            ExplainerCard.Margin = margin;
    }

    /// <summary>Sets a size property only when it changes (a change sets off the card's size changed, which fits again).</summary>
    private static void SetMax(FrameworkElement card, DependencyProperty property, double value)
    {
        double now = (double)card.GetValue(property);
        if (double.IsPositiveInfinity(now) && double.IsPositiveInfinity(value)) return;
        if (double.IsNaN(now) ? !double.IsNaN(value) : !(Math.Abs(now - value) <= 0.5)) card.SetValue(property, value);
    }

    public SceneController Scene { get; }
}
