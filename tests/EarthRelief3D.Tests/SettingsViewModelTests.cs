using EarthRelief3D.App.Curated;
using EarthRelief3D.App.Settings;
using EarthRelief3D.App.ViewModels;
using EarthRelief3D.Core.Curated;
using EarthRelief3D.Core.Feeds;
using EarthRelief3D.Core.Help;
using static EarthRelief3D.Tests.CuratedTablesTests;

namespace EarthRelief3D.Tests;

/// <summary>The Settings flyout (P3-5, C15): "Formal determinations by", built from governments.json and saved.</summary>
public class SettingsViewModelTests
{
    [Fact]
    public void DeterminationChoiceIsSaved()
    {
        var fra = new Government("FRA", "French government", false, new DateOnly(2026, 11, 23));
        var tables = Tables([Row("us", "SDN", AuthorityType.GovernmentDetermination, gov: "USA", continuing: Still(new DateOnly(2026, 3, 10)))],
            g: [Usa, fra]);
        var settings = new MemorySettingsStore();
        var curated = new CuratedStore(tables, showDrafts: false);
        var vm = new SettingsViewModel(settings, curated);

        Assert.Equal(SettingsViewModel.DeterminationsGroup, "Formal determinations by");
        // Only the completely curated government is offered (C15), on by default (P3-5).
        var us = Assert.Single(vm.Governments);
        Assert.Equal("USA", us.Iso3);
        Assert.Equal("United States government", us.Name);
        Assert.True(us.IsChecked);
        Assert.Contains("USA", curated.GovernmentsOn);
        Assert.Single(curated.Visible);

        var changes = 0;
        curated.Changed += (_, _) => changes++;
        us.IsChecked = false;

        Assert.Equal(1, changes);
        Assert.Empty(curated.Visible);
        Assert.NotNull(settings.Current.DeterminationGovernments);
        Assert.Empty(settings.Current.DeterminationGovernments!);
        // The other settings are kept.
        Assert.True(settings.Current.ShowFood);

        // A new run reads the choice back.
        var curated2 = new CuratedStore(tables, showDrafts: false);
        var vm2 = new SettingsViewModel(settings, curated2);
        Assert.False(Assert.Single(vm2.Governments).IsChecked);
        Assert.Empty(curated2.GovernmentsOn);
        Assert.Empty(curated2.Visible);

        vm2.Governments[0].IsChecked = true;
        Assert.Equal(["USA"], settings.Current.DeterminationGovernments);
        Assert.Single(curated2.Visible);
    }

    [Fact]
    public void FlyoutOpensAndCloses()
    {
        var vm = new SettingsViewModel(new MemorySettingsStore(), new CuratedStore(Tables(), showDrafts: false));
        Assert.False(vm.IsOpen);
        vm.ToggleCommand.Execute(null);
        Assert.True(vm.IsOpen);
        vm.CloseCommand.Execute(null);
        Assert.False(vm.IsOpen);
        // Each choice says what it covers and when it was checked.
        Assert.Contains("23 Nov 2026", vm.Governments[0].Note);
    }

    [Fact]
    public void AnUnknownSavedGovernmentIsIgnored()
    {
        // A settings file may name a government no longer offered; it is not applied (C15).
        var settings = new MemorySettingsStore(new AppSettings { DeterminationGovernments = ["FRA"] });
        var curated = new CuratedStore(Tables(), showDrafts: false);
        var vm = new SettingsViewModel(settings, curated);
        Assert.False(vm.Governments.Single().IsChecked);
        Assert.Empty(curated.GovernmentsOn);
    }
    [Fact]
    public void DistancesSaved()
    {
        var settings = new MemorySettingsStore();
        var vm = new SettingsViewModel(settings, CuratedStore.Empty());
        // Kilometres by default (P4-15), as two radio choices.
        Assert.Equal("Distances", SettingsViewModel.DistancesGroup);
        Assert.Equal(EarthRelief3D.App.Presentation.DistanceUnit.Kilometres, vm.Units);
        Assert.True(vm.IsKilometres);
        Assert.False(vm.IsMiles);

        var changes = 0;
        vm.UnitsChanged += (_, _) => changes++;
        vm.IsMiles = true;
        Assert.Equal(EarthRelief3D.App.Presentation.DistanceUnit.Miles, vm.Units);
        Assert.False(vm.IsKilometres);
        Assert.Equal(1, changes);
        Assert.Equal(EarthRelief3D.App.Presentation.DistanceUnit.Miles, settings.Current.Units);
        Assert.True(settings.Current.ShowFood);   // the other settings are kept

        // A new run reads it back; choosing the other radio goes back.
        var again = new SettingsViewModel(settings, CuratedStore.Empty());
        Assert.True(again.IsMiles);
        again.IsKilometres = true;
        Assert.Equal(EarthRelief3D.App.Presentation.DistanceUnit.Kilometres, settings.Current.Units);
        // A radio button being unchecked is not a choice, and the choice already made changes nothing.
        vm.IsKilometres = false;
        vm.IsMiles = true;
        Assert.Equal(1, changes);
    }

    private static HelpTables RealHelp() => HelpTables.Load(System.IO.Path.Combine(AppContext.BaseDirectory, "data"), "US");

    [Fact]
    public void CountryListsOnlyProfiles()
    {
        // "Your country" lists only countries that have a profile.json: today the United States (P4-15).
        var vm = new SettingsViewModel(new MemorySettingsStore(), CuratedStore.Empty(), RealHelp());
        Assert.Equal("Your country", SettingsViewModel.CountryGroup);
        var us = Assert.Single(vm.Countries);
        Assert.Equal(("US", "United States"), (us.Iso2, us.Name));
        Assert.Equal("US", vm.UserCountry);
        Assert.Same(us, vm.SelectedCountry);

        // With no help tables the list is empty and nothing breaks.
        var none = new SettingsViewModel(new MemorySettingsStore(), CuratedStore.Empty());
        Assert.Empty(none.Countries);
        Assert.Single(none.States);
    }

    [Fact]
    public void StateDefaultsToNotChosen()
    {
        var settings = new MemorySettingsStore();
        var vm = new SettingsViewModel(settings, CuratedStore.Empty(), RealHelp());
        Assert.Equal("Your state (optional)", SettingsViewModel.StateGroup);
        Assert.Null(vm.UserState);
        Assert.Null(settings.Current.UserState);
        // "Not chosen" first, then the 50 states with their full names, sorted by name.
        Assert.Equal(51, vm.States.Count);
        Assert.Equal((null, "Not chosen"), (vm.States[0].Code, vm.States[0].Name));
        Assert.Same(vm.States[0], vm.SelectedState);
        var names = vm.States.Skip(1).Select(s => s.Name).ToList();
        Assert.Equal(names.Order(StringComparer.Ordinal), names);
        Assert.Contains(vm.States, s => s is { Code: "MD", Name: "Maryland" });
        Assert.Equal("Alabama", vm.States[1].Name);
        Assert.Equal("Wyoming", vm.States[^1].Name);
    }

    [Fact]
    public void StateSaved()
    {
        var settings = new MemorySettingsStore();
        var vm = new SettingsViewModel(settings, CuratedStore.Empty(), RealHelp());
        var changes = 0;
        vm.StateChanged += (_, _) => changes++;

        vm.SelectedState = vm.States.Single(s => s.Code == "WI");
        Assert.Equal("WI", vm.UserState);
        Assert.Equal("WI", settings.Current.UserState);
        Assert.Equal(1, changes);
        Assert.True(settings.Current.ShowFood);   // the other settings are kept

        // A new run reads it back; "Not chosen" goes back to none.
        var again = new SettingsViewModel(settings, CuratedStore.Empty(), RealHelp());
        Assert.Equal("WI", again.UserState);
        Assert.Equal("Wisconsin", again.SelectedState.Name);
        again.SelectedState = again.States[0];
        Assert.Null(settings.Current.UserState);

        // A saved state the senators list lacks reads as not chosen.
        var odd = new SettingsViewModel(new MemorySettingsStore(new AppSettings { UserState = "ZZ" }), CuratedStore.Empty(), RealHelp());
        Assert.Null(odd.UserState);
        // Setting the same state again changes nothing.
        vm.UserState = "WI";
        Assert.Equal(1, changes);
    }

    [Fact]
    public void CountryAndStateDefaults()
    {
        var s = new AppSettings();
        Assert.Equal("US", s.UserCountry);
        Assert.Null(s.UserState);
    }
}
