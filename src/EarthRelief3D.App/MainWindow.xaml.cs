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
        foreach (var card in new UIElement[] { Strip, Panel, LegendCard, StatusCard }) Scene.HoverBlockers.Add(card);
        // The legend never grows over the layer strip above it: it scrolls when the window is short.
        SizeChanged += (_, _) => FitLegend();
        Strip.SizeChanged += (_, _) => FitLegend();
    }

    private void FitLegend()
    {
        double above = Strip.ActualHeight + Strip.Margin.Top + Strip.Margin.Bottom + LegendCard.Margin.Top + LegendCard.Margin.Bottom;
        LegendCard.MaxHeight = Math.Max(120, Root.ActualHeight - above);
    }

    public SceneController Scene { get; }
}
