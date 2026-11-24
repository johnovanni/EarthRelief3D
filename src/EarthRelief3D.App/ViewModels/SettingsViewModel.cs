using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EarthRelief3D.App.Curated;
using EarthRelief3D.App.Settings;
using EarthRelief3D.Core.Curated;
using GlobeCore.Storage;

namespace EarthRelief3D.App.ViewModels;

/// <summary>One government in "Formal determinations by": a check box whose change is saved and applied at once.</summary>
public sealed class GovernmentChoice : ObservableObject
{
    private readonly SettingsViewModel _owner;
    private bool _isChecked;

    internal GovernmentChoice(SettingsViewModel owner, Government g, bool isChecked)
    {
        _owner = owner;
        Iso3 = g.Iso3;
        Name = g.Name;
        Note = (g.Scope is { Length: > 0 } s ? "Listed: " + s : "All its formal determinations are listed") + $"; checked {DesignationText.Date(g.CheckedOn)}";
        _isChecked = isChecked;
    }

    public string Iso3 { get; }
    public string Name { get; }

    /// <summary>What the choice covers and when the list was checked.</summary>
    public string Note { get; }

    public bool IsChecked
    {
        get => _isChecked;
        set
        {
            if (!SetProperty(ref _isChecked, value)) return;
            _owner.Apply();
        }
    }
}

/// <summary>
/// The Settings flyout (the gear at the end of the layer strip). Plan 3 has one group, "Formal determinations by", built
/// from <c>data/curated/governments.json</c>: only governments curated completely are offered (C15), on by default (P3-5).
/// The choice is saved in the settings and applied to the <see cref="CuratedStore"/> at once.
/// </summary>
public sealed partial class SettingsViewModel : ObservableObject
{
    public const string DeterminationsGroup = "Formal determinations by";
    /// <summary>Under the group's header.</summary>
    public const string DeterminationsNote = "Court judgments and UN findings are always shown. A government is offered only when every formal determination it has made within the stated scope is listed.";

    private readonly ISettingsStore<AppSettings> _settings;
    private readonly CuratedStore _curated;

    [ObservableProperty] private bool _isOpen;

    public SettingsViewModel(ISettingsStore<AppSettings> settings, CuratedStore curated)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _curated = curated ?? throw new ArgumentNullException(nameof(curated));
        var offered = curated.Tables.OfferedGovernments;
        var saved = settings.Load().DeterminationGovernments;
        var on = saved is null ? offered.Select(g => g.Iso3).ToHashSet(StringComparer.OrdinalIgnoreCase)
            : saved.ToHashSet(StringComparer.OrdinalIgnoreCase);
        Governments = [.. offered.Select(g => new GovernmentChoice(this, g, on.Contains(g.Iso3)))];
        curated.SetGovernments(Governments.Where(g => g.IsChecked).Select(g => g.Iso3));
    }

    /// <summary>The governments offered (C15), in the file's order.</summary>
    public IReadOnlyList<GovernmentChoice> Governments { get; }

    public bool HasGovernments => Governments.Count > 0;

    [RelayCommand]
    private void Toggle() => IsOpen = !IsOpen;

    [RelayCommand]
    private void Close() => IsOpen = false;

    /// <summary>A check box changed: save the choice and show it.</summary>
    internal void Apply()
    {
        var chosen = Governments.Where(g => g.IsChecked).Select(g => g.Iso3).ToList();
        _settings.Save(_settings.Load() with { DeterminationGovernments = chosen });
        _curated.SetGovernments(chosen);
    }
}
