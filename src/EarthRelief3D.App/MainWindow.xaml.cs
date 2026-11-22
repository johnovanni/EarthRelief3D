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
    }

    public SceneController Scene { get; }
}
