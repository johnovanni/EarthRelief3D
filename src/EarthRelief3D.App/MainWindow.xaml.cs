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

    private void FitLegend()
    {
        Strip.MaxHeight = Math.Max(200, Root.ActualHeight * StripShare - Strip.Margin.Top - Strip.Margin.Bottom);
        double above = Strip.ActualHeight + Strip.Margin.Top + Strip.Margin.Bottom + LegendCard.Margin.Top + LegendCard.Margin.Bottom;
        LegendCard.MaxHeight = Math.Max(120, Root.ActualHeight - above);
    }

    public SceneController Scene { get; }
}
