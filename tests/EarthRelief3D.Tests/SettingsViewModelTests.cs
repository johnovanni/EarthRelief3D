using EarthRelief3D.App.Curated;
using EarthRelief3D.App.Settings;
using EarthRelief3D.App.ViewModels;
using EarthRelief3D.Core.Curated;
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
}
