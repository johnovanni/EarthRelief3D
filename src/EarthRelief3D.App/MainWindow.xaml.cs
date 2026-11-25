using System.Windows;
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
        foreach (var card in new UIElement[] { Strip, Panel, ExplainerCard, LegendCard, StatusCard }) Scene.HoverBlockers.Add(card);
        // The legend never grows over the layer strip above it: it scrolls when the window is short.
        SizeChanged += (_, _) => FitLegend();
        Strip.SizeChanged += (_, _) => FitLegend();
        LegendCard.SizeChanged += (_, _) => FitLegend();   // opening or closing the legend changes what room the strip has
        // The explainer card never covers the layer strip: its left edge stays right of the strip, and it shrinks to fit.
        Root.SizeChanged += (_, _) => FitExplainer();
        Strip.SizeChanged += (_, _) => FitExplainer();
    }

    private void FitExplainer()
    {
        var m = ExplainerCard.Margin;
        double left = Strip.ActualWidth + Strip.Margin.Left + Strip.Margin.Right;
        if (Math.Abs(m.Left - left) > 0.5) ExplainerCard.Margin = new Thickness(left, m.Top, m.Right, m.Bottom);
    }

    /// <summary>The share of the window's height the layer strip may take before it scrolls (Plan 3's groups are long).</summary>
    public const double StripShare = 0.62;

    /// <summary>The share of the window's height an open legend may take before it scrolls (P5-24).</summary>
    public const double LegendShare = 0.5;

    private void FitLegend()
    {
        bool legendOpen = (DataContext as MainViewModel)?.Legend.IsOpen ?? false;
        double legendMargins = LegendCard.Margin.Top + LegendCard.Margin.Bottom, stripMargins = Strip.Margin.Top + Strip.Margin.Bottom;
        // A closed legend is only its header and the UN note: the strip may take all the rest of the height, so it scrolls less.
        double stripMax = legendOpen
            ? Root.ActualHeight * StripShare - stripMargins
            : Root.ActualHeight - stripMargins - legendMargins - LegendCard.ActualHeight;
        Strip.MaxHeight = Math.Max(200, stripMax);
        double above = Strip.ActualHeight + stripMargins + legendMargins;
        double room = Root.ActualHeight - above;
        // Open: at most half the window's height, and never over the strip. Closed: no cap (it is two or three lines).
        LegendCard.MaxHeight = legendOpen ? Math.Max(120, Math.Min(Root.ActualHeight * LegendShare - legendMargins, room)) : double.PositiveInfinity;
    }

    public SceneController Scene { get; }
}
