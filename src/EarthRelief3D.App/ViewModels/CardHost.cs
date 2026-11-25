using CommunityToolkit.Mvvm.ComponentModel;

namespace EarthRelief3D.App.ViewModels;

/// <summary>The cards that open over the globe, one at a time (Plan 5): the first-run Welcome, About and Data.</summary>
public enum AppCard { None, Welcome, About, Data }

/// <summary>
/// Owns which card is open: at most one at a time. A card's view model is registered once, when the app builds it
/// (<see cref="Register"/>); the card's view shows itself from its own flag (<see cref="WelcomeOpen"/>, <see cref="AboutOpen"/>,
/// <see cref="DataOpen"/>), so a later task adds its card without changing this class. Showing a card that is not registered is
/// an error with a plain message (a scene naming it fails with it). <see cref="CloseTop"/> is what Esc does first.
/// </summary>
public sealed class CardHost : ObservableObject
{
    private readonly Dictionary<AppCard, Action?> _registered = [];
    private AppCard _open;

    /// <summary>The card showing now (<see cref="AppCard.None"/> when none is).</summary>
    public AppCard Open
    {
        get => _open;
        private set
        {
            if (!SetProperty(ref _open, value)) return;
            OnPropertyChanged(nameof(WelcomeOpen));
            OnPropertyChanged(nameof(AboutOpen));
            OnPropertyChanged(nameof(DataOpen));
        }
    }

    public bool WelcomeOpen => _open == AppCard.Welcome;
    public bool AboutOpen => _open == AppCard.About;
    public bool DataOpen => _open == AppCard.Data;

    /// <summary>Makes <paramref name="card"/> available. <paramref name="onOpened"/> runs each time it is shown (to refresh what it lists).</summary>
    public void Register(AppCard card, Action? onOpened = null)
    {
        if (card == AppCard.None) throw new ArgumentException("None is not a card.", nameof(card));
        _registered[card] = onOpened;
    }

    /// <summary>True for <see cref="AppCard.None"/> and for a card that has been registered.</summary>
    public bool IsAvailable(AppCard card) => card == AppCard.None || _registered.ContainsKey(card);

    /// <summary>Shows <paramref name="card"/>, closing the one that was open; <see cref="AppCard.None"/> closes. Throws when the card is not registered.</summary>
    public void Show(AppCard card)
    {
        if (!IsAvailable(card)) throw new InvalidOperationException($"card '{card.ToString().ToLowerInvariant()}' is not available");
        if (card == _open) return;
        Open = card;
        if (card != AppCard.None) _registered[card]?.Invoke();
    }

    /// <summary>Closes the open card (Esc): false when none was open, so Esc goes on to the explainer and the panel.</summary>
    public bool CloseTop()
    {
        if (_open == AppCard.None) return false;
        Open = AppCard.None;
        return true;
    }
}
