using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace EarthRelief3D.App.Views;

/// <summary>
/// The Welcome card (centre). DataContext: WelcomeViewModel. When it appears it scrolls to the top and takes the keyboard focus on
/// its Start button, so Enter starts and Tab reaches the rest. Esc closes it (SceneController).
/// </summary>
public partial class WelcomeCard : UserControl
{
    public WelcomeCard()
    {
        InitializeComponent();
        IsVisibleChanged += OnIsVisibleChanged;
    }

    private void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is not true) return;
        Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () =>
        {
            if (!IsVisible) return;
            Scroller.ScrollToTop();
            Keyboard.Focus(StartButton);
        });
    }
}
