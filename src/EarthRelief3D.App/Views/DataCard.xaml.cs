using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace EarthRelief3D.App.Views;

/// <summary>
/// The Data card (centre). DataContext: DataViewModel. When it appears it scrolls to the top and takes the keyboard focus, so Tab
/// reaches its close button and links at once. Its links open through the view model (the user's browser, https only).
/// </summary>
public partial class DataCard : UserControl
{
    public DataCard()
    {
        InitializeComponent();
        IsVisibleChanged += OnIsVisibleChanged;
    }

    private void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is not true) return;
        // After layout, so the card is visible and can take the focus.
        Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () =>
        {
            if (!IsVisible) return;
            Scroller.ScrollToTop();
            Keyboard.Focus(CloseButton);
        });
    }
}
