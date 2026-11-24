using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Threading;
using EarthRelief3D.App.ViewModels;
using GlobeCore.Wpf.Services;

namespace EarthRelief3D.App.Views;

/// <summary>
/// The explainer card (left of the country panel). DataContext: ExplainerViewModel. When it opens (or shows another
/// explainer) it scrolls to the top and takes the keyboard focus, so Tab reaches its close button and source links at once.
/// </summary>
public partial class ExplainerCard : UserControl
{
    private ExplainerViewModel? _vm;

    public ExplainerCard()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    /// <summary>Opens the source links; the real shell by default.</summary>
    public ILinkOpener Links { get; set; } = new ShellLinkOpener();

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (_vm is not null) _vm.PropertyChanged -= OnVmChanged;
        _vm = DataContext as ExplainerViewModel;
        if (_vm is not null) _vm.PropertyChanged += OnVmChanged;
    }

    private void OnVmChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(ExplainerViewModel.Id)) return;
        // After layout, so the card is visible and can take the focus.
        Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () =>
        {
            if (_vm is not { IsOpen: true }) return;
            Scroller.ScrollToTop();
            Keyboard.Focus(CloseButton);
        });
    }

    private void OnSourceLinkClick(object sender, RoutedEventArgs e)
    {
        if (sender is Hyperlink { Tag: Uri link }) CountryPanel.OpenInBrowser(link, Links);
    }
}
