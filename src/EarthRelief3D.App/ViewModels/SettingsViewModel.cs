using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EarthRelief3D.App.Curated;
using EarthRelief3D.App.Presentation;
using EarthRelief3D.App.Settings;
using EarthRelief3D.Core.Curated;
using EarthRelief3D.Core.Help;
using GlobeCore.Storage;

namespace EarthRelief3D.App.ViewModels;

/// <summary>One state in "Your state (optional)": its two-letter code (null for "Not chosen") and its full name.</summary>
public sealed record StateChoice(string? Code, string Name);

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
/// The Settings flyout (the gear at the end of the layer strip). Plan 3's group, "Formal determinations by", is built
/// from <c>data/curated/governments.json</c>: only governments curated completely are offered (C15), on by default (P3-5).
/// The choice is saved in the settings and applied to the <see cref="CuratedStore"/> at once. Plan 4 adds "Distances"
/// (P4-15): kilometres (the default) or miles, saved and announced by <see cref="UnitsChanged"/> so the open panel and the
/// arc labels follow at once.
/// </summary>
public sealed partial class SettingsViewModel : ObservableObject
{
    public const string DeterminationsGroup = "Formal determinations by";
    /// <summary>Under the group's header.</summary>
    public const string DeterminationsNote = "Court judgments and UN findings are always shown. A government is offered only when every formal determination it has made within the stated scope is listed.";

    /// <summary>The header of the unit choice.</summary>
    public const string DistancesGroup = "Distances";
    /// <summary>Under the group's header.</summary>
    public const string DistancesNote = "Distances to the nearest hubs are straight-line, over the globe's surface.";

    /// <summary>The header of the country choice (P4-15): only countries with a profile are listed.</summary>
    public const string CountryGroup = "Your country";
    public const string CountryNote = "The countries whose charities and officials the app lists. Takes effect the next time the app starts.";

    /// <summary>The header of the optional state choice (P4-15).</summary>
    public const string StateGroup = "Your state (optional)";
    /// <summary>Under the group's header: the app keeps a state, never an address or ZIP code.</summary>
    public const string StateNote = "Used only to list your two senators. The app never asks for an address or ZIP code.";
    public const string NotChosenText = "Not chosen";

    private readonly ISettingsStore<AppSettings> _settings;
    private readonly CuratedStore _curated;
    private DistanceUnit _units;
    private string? _userState;

    [ObservableProperty] private bool _isOpen;

    /// <param name="help">The help catalog: its profiles give "Your country", its senators give "Your state"; null: both lists are empty.</param>
    public SettingsViewModel(ISettingsStore<AppSettings> settings, CuratedStore curated, HelpTables? help = null)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _curated = curated ?? throw new ArgumentNullException(nameof(curated));
        Countries = help?.AvailableCountries ?? [];
        States =
        [
            new StateChoice(null, NotChosenText),
            .. (help?.Senators.Select(s => s.State).Distinct(StringComparer.Ordinal) ?? [])
                .Select(code => (Code: code, Name: UsStates.Name(code))).Where(s => s.Name is not null)
                .OrderBy(s => s.Name, StringComparer.Ordinal).Select(s => new StateChoice(s.Code, s.Name!)),
        ];
        var savedState = settings.Load().UserState;
        _userState = States.Any(s => s.Code == savedState) ? savedState : null;
        var offered = curated.Tables.OfferedGovernments;
        var saved = settings.Load().DeterminationGovernments;
        var on = saved is null ? offered.Select(g => g.Iso3).ToHashSet(StringComparer.OrdinalIgnoreCase)
            : saved.ToHashSet(StringComparer.OrdinalIgnoreCase);
        Governments = [.. offered.Select(g => new GovernmentChoice(this, g, on.Contains(g.Iso3)))];
        curated.SetGovernments(Governments.Where(g => g.IsChecked).Select(g => g.Iso3));
        _units = settings.Load().Units;
    }

    /// <summary>The countries that have a profile, by name (today the United States).</summary>
    public IReadOnlyList<AvailableCountry> Countries { get; }

    /// <summary>The country chosen: the saved one when it has a profile, else the first listed (null: none listed).</summary>
    public AvailableCountry? SelectedCountry
    {
        get => Countries.FirstOrDefault(c => c.Iso2 == UserCountry) ?? Countries.FirstOrDefault();
        set
        {
            if (value is null || value.Iso2 == UserCountry) return;
            _settings.Save(_settings.Load() with { UserCountry = value.Iso2 });
            OnPropertyChanged();
            OnPropertyChanged(nameof(UserCountry));
        }
    }

    /// <summary>The ISO2 code of the country chosen ("US" by default).</summary>
    public string UserCountry => _settings.Load().UserCountry;

    /// <summary>"Not chosen", then the states the senators list has, by full name.</summary>
    public IReadOnlyList<StateChoice> States { get; }

    /// <summary>The state chosen, or "Not chosen" (the first entry).</summary>
    public StateChoice SelectedState
    {
        get => States.First(s => s.Code == _userState);
        set => UserState = value?.Code;
    }

    /// <summary>The state's two-letter code, or null (the default: not chosen). Saved, and announced by <see cref="StateChanged"/>.</summary>
    public string? UserState
    {
        get => _userState;
        set
        {
            var code = States.Any(s => s.Code == value) ? value : null;
            if (_userState == code) return;
            _userState = code;
            _settings.Save(_settings.Load() with { UserState = code });
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedState));
            StateChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>The state was changed (and saved): the open panel writes its senators again.</summary>
    public event EventHandler? StateChanged;

    /// <summary>The unit distances are shown in (kilometres by default).</summary>
    public DistanceUnit Units
    {
        get => _units;
        set
        {
            if (_units == value) return;
            _units = value;
            _settings.Save(_settings.Load() with { Units = value });
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsKilometres));
            OnPropertyChanged(nameof(IsMiles));
            UnitsChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>The "Kilometres" radio button. Setting it false does nothing: a radio button leaves its state when the other is chosen.</summary>
    public bool IsKilometres
    {
        get => _units == DistanceUnit.Kilometres;
        set { if (value) Units = DistanceUnit.Kilometres; }
    }

    /// <summary>The "Miles" radio button.</summary>
    public bool IsMiles
    {
        get => _units == DistanceUnit.Miles;
        set { if (value) Units = DistanceUnit.Miles; }
    }

    /// <summary>The unit was changed (and saved): the panel's rows and the arc labels are written again.</summary>
    public event EventHandler? UnitsChanged;

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
