using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using EarthRelief3D.App.Presentation;
using EarthRelief3D.App.Settings;
using GlobeCore.Storage;

namespace EarthRelief3D.App.ViewModels;

/// <summary>One layer's toggle in the strip: its symbol in the layer's colour, its label, its tooltip, and whether it is on.</summary>
/// <remarks>
/// A shading toggle (<see cref="IsShading"/>) is radio-style within the one-shading rule (P3-6): turning it on turns the
/// shading that was on off, and that toggle is told it changed, so the strip shows it unchecking. Turning the shading that
/// is on off leaves none.
/// </remarks>
public sealed class LayerToggle : ObservableObject
{
    private readonly Func<bool> _get;
    private readonly Action<bool> _set;

    internal LayerToggle(string id, string label, string symbol, Color color, string tooltip, bool isShading, Func<bool> get, Action<bool> set)
    {
        Id = id;
        Label = label;
        Symbol = symbol;
        Brush = LayerStyle.Frozen(color);
        Tooltip = tooltip;
        IsShading = isShading;
        _get = get;
        _set = set;
    }

    /// <summary>The layer's name in scenes and on the globe ("water", "power").</summary>
    public string Id { get; }
    public string Label { get; }
    public string Symbol { get; }
    public SolidColorBrush Brush { get; }
    public string Tooltip { get; }
    public bool IsShading { get; }

    public bool IsChecked
    {
        get => _get();
        set { if (value != _get()) _set(value); }
    }

    internal void Refresh() => OnPropertyChanged(nameof(IsChecked));
}

/// <summary>
/// A group of the strip (spec §6) with its toggles. A collapsible group (Needs, Resources; ruling R20) opens and closes from
/// its header; Crises is always open.
/// </summary>
public sealed class LayerGroup : ObservableObject
{
    private readonly Action<LayerGroup>? _userChose;
    private bool _isOpen;

    /// <param name="note">A short line under the group's name (the one-shading rule), or null.</param>
    /// <param name="userChose">Called when the user opens or closes the group (to save the choice); null: not collapsible.</param>
    internal LayerGroup(string name, IReadOnlyList<LayerToggle> toggles, string? note = null, Action<LayerGroup>? userChose = null)
    {
        Name = name;
        Toggles = toggles;
        Note = note;
        _userChose = userChose;
        _isOpen = true;
    }

    public string Name { get; }
    public IReadOnlyList<LayerToggle> Toggles { get; }
    public string? Note { get; }
    public bool HasNote => Note is not null;
    public bool IsCollapsible => _userChose is not null;

    /// <summary>The header's tooltip.</summary>
    public string HeaderTooltip => $"Show or hide the {Name} layers";

    /// <summary>The group's toggles are shown. Set from the header (the user's choice, saved); a non-collapsible group stays open.</summary>
    public bool IsOpen
    {
        get => _isOpen;
        set
        {
            if (_userChose is null || !SetProperty(ref _isOpen, value)) return;
            _userChose(this);
        }
    }

    /// <summary>True when one of the group's layers is on.</summary>
    public bool AnyOn => Toggles.Any(t => t.IsChecked);

    /// <summary>Opens or closes the group without recording a choice (the start rule, a snapshot scene).</summary>
    internal void SetOpenQuietly(bool open)
    {
        if (_userChose is not null) SetProperty(ref _isOpen, open, nameof(IsOpen));
    }
}

/// <summary>
/// The layer strip: the Crises, Needs, Resources and Getting help in groups with their toggles. One shading shows at a time
/// across all groups; marker layers stack (P3-6).
/// Starts from the saved settings; any change is saved.
/// </summary>
public sealed partial class LayerStripViewModel : ObservableObject
{
    public const string CrisesGroup = "Crises";
    public const string NeedsGroup = "Needs";
    public const string ResourcesGroup = "Resources";
    /// <summary>The Plan 4 group: ACAPS access constraints, then the UN depot, airport and port markers (off by default; P4-5).</summary>
    public const string GettingHelpGroup = "Getting help in";
    /// <summary>Under the Needs and Resources headers.</summary>
    public const string ShadingNote = "▰ one shading at a time";

    private readonly ISettingsStore<AppSettings> _store;
    private readonly SortedSet<MarkerLayer> _markers;
    private readonly List<LayerToggle> _toggles = [];

    /// <summary>The one shading shown (P3-6); <see cref="ShadingLayer.None"/> for none.</summary>
    [ObservableProperty] private ShadingLayer _shading;
    /// <summary>The strip shows text labels beside its icons.</summary>
    [ObservableProperty] private bool _isExpanded;

    public LayerStripViewModel(ISettingsStore<AppSettings> store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        var s = store.Load();
        _shading = s.EffectiveShading;
        _markers = [.. s.EffectiveMarkers];
        _isExpanded = s.LayerStripExpanded;

        LayerToggle Marker(MarkerLayer m) => Add(new LayerToggle(LayerCatalog.Id(m), LayerCatalog.Label(m), LayerStyle.Symbol(m),
            LayerStyle.SymbolColor(m), LayerCatalog.Tooltip(m), false, () => IsShown(m), on => Show(m, on)));
        LayerToggle Shade(ShadingLayer l) => Add(new LayerToggle(LayerCatalog.Id(l), LayerCatalog.Label(l), LayerStyle.Symbol(l),
            LayerStyle.SymbolColor(l), l == ShadingLayer.Food ? LegendViewModel.FoodTooltip : LayerCatalog.Tooltip(l), true,
            () => Shading == l, on => Show(l, on)));
        Groups =
        [
            new LayerGroup(CrisesGroup,
            [
                Marker(MarkerLayer.Conflict), Shade(ShadingLayer.Food), Marker(MarkerLayer.Outbreaks), Marker(MarkerLayer.Displacement),
                .. LayerCatalog.CrisisMarkersAdded.Select(Marker),
            ]),
            new LayerGroup(NeedsGroup, [.. LayerCatalog.NeedsShadings.Select(Shade)], ShadingNote, OnGroupChosen),
            new LayerGroup(ResourcesGroup, [.. LayerCatalog.ResourceShadings.Select(Shade), .. LayerCatalog.ResourceMarkers.Select(Marker)],
                ShadingNote, OnGroupChosen),
            new LayerGroup(GettingHelpGroup, [.. LayerCatalog.HelpShadings.Select(Shade), .. LayerCatalog.HelpMarkers.Select(Marker)], ShadingNote, OnGroupChosen),
        ];

        // R20: a group starts open when one of its layers is on (its active toggle is never hidden at start), otherwise as
        // the user last left it, otherwise closed. After that it changes only when the user opens or closes it.
        _openChoices = s.OpenGroups is { } saved ? new Dictionary<string, bool>(saved, StringComparer.Ordinal) : null;
        foreach (var g in Groups.Where(g => g.IsCollapsible))
            g.SetOpenQuietly(g.AnyOn || (_openChoices?.GetValueOrDefault(g.Name) ?? false));
    }

    // The user's open or closed choices, by group name; null until the user first opens or closes a group.
    private Dictionary<string, bool>? _openChoices;

    private void OnGroupChosen(LayerGroup g)
    {
        (_openChoices ??= new(StringComparer.Ordinal))[g.Name] = g.IsOpen;
        Save();
    }

    private LayerToggle Add(LayerToggle t)
    {
        _toggles.Add(t);
        return t;
    }

    /// <summary>The Crises, Needs, Resources and Getting help in groups (spec §6), in strip order.</summary>
    public IReadOnlyList<LayerGroup> Groups { get; }

    /// <summary>The Settings flyout opened by the gear at the end of the strip (P3-5); null: no gear.</summary>
    public SettingsViewModel? Settings
    {
        get => _settings;
        set
        {
            if (SetProperty(ref _settings, value)) OnPropertyChanged(nameof(HasSettings));
        }
    }

    private SettingsViewModel? _settings;

    public bool HasSettings => _settings is not null;


    /// <summary>The marker layers shown, in enum order.</summary>
    public IReadOnlyList<MarkerLayer> ShownMarkers => [.. _markers];

    // The Plan 2 toggles, kept as properties: the scene, the labels, the legend and the snapshot scenes use them.
    public bool ShowConflict { get => IsShown(MarkerLayer.Conflict); set => Show(MarkerLayer.Conflict, value); }
    public bool ShowOutbreaks { get => IsShown(MarkerLayer.Outbreaks); set => Show(MarkerLayer.Outbreaks, value); }
    public bool ShowDisplacement { get => IsShown(MarkerLayer.Displacement); set => Show(MarkerLayer.Displacement, value); }

    /// <summary>The food shading: on sets <see cref="Shading"/> to food (another shading goes off); off leaves no shading.</summary>
    public bool ShowFood { get => Shading == ShadingLayer.Food; set => Show(ShadingLayer.Food, value); }

    // The strip's labels and symbols, so the view and the legend use the same ones.
    public string ConflictSymbol => LayerStyle.Symbol(CrisisLayer.Conflict);
    public string FoodSymbol => LayerStyle.Symbol(CrisisLayer.Food);
    public string OutbreaksSymbol => LayerStyle.Symbol(CrisisLayer.Outbreaks);
    public string DisplacementSymbol => LayerStyle.Symbol(CrisisLayer.Displacement);

    public bool IsShown(CrisisLayer layer) => layer == CrisisLayer.Food ? ShowFood
        : LayerStyle.MarkerOf(layer) is { } m && IsShown(m);

    public bool IsShown(MarkerLayer layer) => _markers.Contains(layer);

    public bool IsShown(ShadingLayer layer) => layer != ShadingLayer.None && Shading == layer;

    /// <summary>Shows or hides a marker layer; the others stay as they are (markers stack).</summary>
    public void Show(MarkerLayer layer, bool on)
    {
        if (!(on ? _markers.Add(layer) : _markers.Remove(layer))) return;
        if (PropertyForMarker(layer) is { } legacy) OnPropertyChanged(legacy);
        OnPropertyChanged(nameof(ShownMarkers));
        RefreshToggles();
        Save();
    }

    /// <summary>Turns a shading on (the one shown goes off) or, if it is the one shown, off (none is shown).</summary>
    public void Show(ShadingLayer layer, bool on)
    {
        if (on) Shading = layer;
        else if (Shading == layer) Shading = ShadingLayer.None;
    }

    /// <summary>
    /// Sets the shading and exactly these marker layers at once (a snapshot scene), and opens the groups as a start would
    /// with them: a collapsible group is open exactly when one of its layers is on. That is not the user's choice, so it is
    /// not saved.
    /// </summary>
    public void SetLayers(ShadingLayer shading, IEnumerable<MarkerLayer> markers)
    {
        var wanted = new SortedSet<MarkerLayer>(markers);
        foreach (var m in Enum.GetValues<MarkerLayer>()) Show(m, wanted.Contains(m));
        Shading = shading;
        foreach (var g in Groups.Where(g => g.IsCollapsible)) g.SetOpenQuietly(g.AnyOn);
    }

    /// <summary>The property name that carries <paramref name="layer"/>'s toggle.</summary>
    public static string PropertyFor(CrisisLayer layer) => layer switch
    {
        CrisisLayer.Conflict => nameof(ShowConflict),
        CrisisLayer.Food => nameof(ShowFood),
        CrisisLayer.Outbreaks => nameof(ShowOutbreaks),
        CrisisLayer.Displacement => nameof(ShowDisplacement),
        _ => throw new ArgumentOutOfRangeException(nameof(layer)),
    };

    private static string? PropertyForMarker(MarkerLayer layer) => layer switch
    {
        MarkerLayer.Conflict => nameof(ShowConflict),
        MarkerLayer.Outbreaks => nameof(ShowOutbreaks),
        MarkerLayer.Displacement => nameof(ShowDisplacement),
        _ => null,
    };

    /// <summary>True for a property change that may change what the globe shows (the shading or any marker layer).</summary>
    public static bool IsLayerProperty(string? name) => name is nameof(Shading) or nameof(ShownMarkers) or nameof(ShowConflict)
        or nameof(ShowFood) or nameof(ShowOutbreaks) or nameof(ShowDisplacement);

    partial void OnShadingChanged(ShadingLayer oldValue, ShadingLayer newValue)
    {
        if ((oldValue == ShadingLayer.Food) != (newValue == ShadingLayer.Food)) OnPropertyChanged(nameof(ShowFood));
        RefreshToggles();
        Save();
    }

    partial void OnIsExpandedChanged(bool value) => Save();

    private void RefreshToggles()
    {
        foreach (var t in _toggles) t.Refresh();
    }

    private void Save()
    {
        var markers = ShownMarkers;
        var saved = _store.Load();
        _store.Save(saved with
        {
            Shading = Shading,
            Markers = markers,
            // The Plan 2 flags, kept in step for older copies of the app.
            ShowConflict = ShowConflict,
            ShowFood = ShowFood,
            ShowOutbreaks = ShowOutbreaks,
            ShowDisplacement = ShowDisplacement,
            LayerStripExpanded = IsExpanded,
            OpenGroups = _openChoices is null ? saved.OpenGroups : new Dictionary<string, bool>(_openChoices, StringComparer.Ordinal),
        });
    }
}
