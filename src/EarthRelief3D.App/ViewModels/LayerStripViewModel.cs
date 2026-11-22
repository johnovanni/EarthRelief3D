using CommunityToolkit.Mvvm.ComponentModel;
using EarthRelief3D.App.Presentation;
using EarthRelief3D.App.Settings;
using GlobeCore.Storage;

namespace EarthRelief3D.App.ViewModels;

/// <summary>A group of layers the strip names but cannot show yet.</summary>
public sealed record ComingGroup(string Name, string Note);

/// <summary>
/// The layer strip: the Crises group with its four toggles, then the Needs, Resources and Getting help in group headers,
/// which say they are coming in a later version and do nothing. Starts from the saved settings; any change is saved.
/// </summary>
public sealed partial class LayerStripViewModel : ObservableObject
{
    public const string CrisesGroup = "Crises";
    public const string ComingNote = "coming in a later version";

    private readonly ISettingsStore<AppSettings> _store;

    [ObservableProperty] private bool _showConflict;
    [ObservableProperty] private bool _showFood;
    [ObservableProperty] private bool _showOutbreaks;
    [ObservableProperty] private bool _showDisplacement;
    /// <summary>The strip shows text labels beside its icons.</summary>
    [ObservableProperty] private bool _isExpanded;

    public LayerStripViewModel(ISettingsStore<AppSettings> store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        var s = store.Load();
        _showConflict = s.ShowConflict;
        _showFood = s.ShowFood;
        _showOutbreaks = s.ShowOutbreaks;
        _showDisplacement = s.ShowDisplacement;
        _isExpanded = s.LayerStripExpanded;
    }

    /// <summary>The groups after Crises (spec §6), shown as headers only in Plan 2.</summary>
    public IReadOnlyList<ComingGroup> ComingGroups { get; } =
        [new("Needs", ComingNote), new("Resources", ComingNote), new("Getting help in", ComingNote)];

    // The strip's labels and symbols, so the view and the legend use the same ones.
    public string ConflictSymbol => LayerStyle.Symbol(CrisisLayer.Conflict);
    public string FoodSymbol => LayerStyle.Symbol(CrisisLayer.Food);
    public string OutbreaksSymbol => LayerStyle.Symbol(CrisisLayer.Outbreaks);
    public string DisplacementSymbol => LayerStyle.Symbol(CrisisLayer.Displacement);

    public bool IsShown(CrisisLayer layer) => layer switch
    {
        CrisisLayer.Conflict => ShowConflict,
        CrisisLayer.Food => ShowFood,
        CrisisLayer.Outbreaks => ShowOutbreaks,
        CrisisLayer.Displacement => ShowDisplacement,
        _ => false,
    };

    /// <summary>The property name that carries <paramref name="layer"/>'s toggle.</summary>
    public static string PropertyFor(CrisisLayer layer) => layer switch
    {
        CrisisLayer.Conflict => nameof(ShowConflict),
        CrisisLayer.Food => nameof(ShowFood),
        CrisisLayer.Outbreaks => nameof(ShowOutbreaks),
        CrisisLayer.Displacement => nameof(ShowDisplacement),
        _ => throw new ArgumentOutOfRangeException(nameof(layer)),
    };

    partial void OnShowConflictChanged(bool value) => Save();
    partial void OnShowFoodChanged(bool value) => Save();
    partial void OnShowOutbreaksChanged(bool value) => Save();
    partial void OnShowDisplacementChanged(bool value) => Save();
    partial void OnIsExpandedChanged(bool value) => Save();

    private void Save() => _store.Save(_store.Load() with
    {
        ShowConflict = ShowConflict,
        ShowFood = ShowFood,
        ShowOutbreaks = ShowOutbreaks,
        ShowDisplacement = ShowDisplacement,
        LayerStripExpanded = IsExpanded,
    });
}
