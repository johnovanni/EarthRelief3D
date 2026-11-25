using System.ComponentModel;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EarthRelief3D.App.Diagnostics;
using EarthRelief3D.App.Presentation;
using EarthRelief3D.App.Settings;
using GlobeCore.Storage;

namespace EarthRelief3D.App.ViewModels;

/// <summary>One line of "What the marks mean": a symbol in its colour, and what it stands for.</summary>
public sealed record WelcomeKey(string Symbol, Color Color, string Text)
{
    /// <summary>The colour as a frozen brush, for the card.</summary>
    public SolidColorBrush Brush => LayerStyle.Frozen(Color);
}

/// <summary>
/// The first-run Welcome card (Plan 5 Task 6): what the globe shows, what the marks mean, where the sources are, the text size and
/// the optional state. Shown once; every way of closing it (Start, Esc, Settings) marks it shown, saved once through the settings
/// store (the app's is the debounced saver), keeping every other setting. Never shown in a diagnostic or offline run.
/// </summary>
public sealed partial class WelcomeViewModel : ObservableObject
{
    public const string TitleText = "Welcome to EarthRelief3D";
    public const string KeysHeading = "What the marks mean";
    public const string TextSizeHeading = "Text size";

    public static IReadOnlyList<string> Intro { get; } =
    [
        "This globe shows where people are suffering, what they lack, why help is hard to get in, and one small thing you can do about it.",
        "Every fact comes with its source and its date. The legend at the bottom left (open it from its header) names them for each layer, the dots at the bottom right show whether each source is up to date, and Data lists every source.",
    ];

    public const string KeysNote = "Shapes as well as colours tell the layers apart. More layers are in the strip on the left; only one shaded layer shows at a time.";
    public const string PanelText = "Click a country to open its panel: what is happening there, how hard it is for help to get in, and Your one small step: vetted ways to give, and official people to contact.";
    public const string StateText = "If you like, choose your state in Settings so the panel can list your two senators. EarthRelief3D never asks for your address.";

    private readonly ISettingsStore<AppSettings> _store;
    private readonly SettingsViewModel _settings;
    private readonly CardHost _cards;
    private bool _marked;

    public WelcomeViewModel(ISettingsStore<AppSettings> store, SettingsViewModel settings, CardHost cards)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _cards = cards ?? throw new ArgumentNullException(nameof(cards));
        // The layers on by default, in the strip's order: conflict, food (one key per IPC phase 3 to 5), outbreaks, displacement, the hub lines.
        Keys =
        [
            Key(MarkerLayer.Conflict),
            .. Enumerable.Range(3, 3).Select(p => new WelcomeKey(LayerStyle.Symbol(ShadingLayer.Food), LayerStyle.IpcPhaseColor(p), "Food: " + LayerStyle.IpcPhaseLabel(p))),
            Key(MarkerLayer.Outbreaks),
            Key(MarkerLayer.Displacement),
            Key(MarkerLayer.HubLines),
        ];
        _settings.TextScaleChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(TextScale));
            OnPropertyChanged(nameof(IsScale100));
            OnPropertyChanged(nameof(IsScale125));
            OnPropertyChanged(nameof(IsScale150));
        };
        // Closing in any way (Start, Esc, another card replacing it) marks the card shown.
        _cards.PropertyChanged += OnCardsChanged;
    }

    private static WelcomeKey Key(MarkerLayer layer) => new(LayerStyle.Symbol(layer), LayerStyle.SymbolColor(layer), LayerCatalog.Label(layer));

    /// <summary>Only the very first run, and never a diagnostic or offline run (a picture run must not be held up by a card).</summary>
    public static bool ShouldShow(AppSettings settings, LaunchOptions options) =>
        !settings.WelcomeShown && !options.IsDiagnosticRun;

    public string Title => TitleText;
    public IReadOnlyList<string> Paragraphs => Intro;
    public string Heading => KeysHeading;
    public string Note => KeysNote;
    public string Panel => PanelText;
    public string State => StateText;
    public string SizeHeading => TextSizeHeading;

    /// <summary>The marks of the layers that are on by default.</summary>
    public IReadOnlyList<WelcomeKey> Keys { get; }

    /// <summary>The text size (1.0, 1.25 or 1.5), the same setting as Settings' "Text size".</summary>
    public double TextScale
    {
        get => _settings.TextScale;
        set => _settings.TextScale = value;
    }

    public bool IsScale100 { get => _settings.IsScale100; set => _settings.IsScale100 = value; }
    public bool IsScale125 { get => _settings.IsScale125; set => _settings.IsScale125 = value; }
    public bool IsScale150 { get => _settings.IsScale150; set => _settings.IsScale150 = value; }

    private void OnCardsChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CardHost.Open) && !_cards.WelcomeOpen && _wasOpen) Close();
        _wasOpen = _cards.WelcomeOpen;
    }

    private bool _wasOpen;

    /// <summary>Marks the Welcome as shown (once), keeping every other setting.</summary>
    public void Close()
    {
        if (_marked) return;
        _marked = true;
        var now = _store.Load();
        if (!now.WelcomeShown) _store.Save(now with { WelcomeShown = true });
    }

    /// <summary>"Start": closes the card.</summary>
    [RelayCommand]
    private void Start()
    {
        if (_cards.WelcomeOpen) _cards.CloseTop();
        Close();
    }

    /// <summary>"Settings": closes the card and opens the Settings flyout.</summary>
    [RelayCommand]
    private void OpenSettings()
    {
        if (_cards.WelcomeOpen) _cards.CloseTop();
        Close();
        _settings.IsOpen = true;
    }
}
