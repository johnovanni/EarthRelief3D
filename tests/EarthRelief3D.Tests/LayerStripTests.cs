using System.IO;
using EarthRelief3D.App.Presentation;
using EarthRelief3D.App.Settings;
using EarthRelief3D.App.ViewModels;
using GlobeCore.Storage;

namespace EarthRelief3D.Tests;

/// <summary>The strip's four groups, one shading at a time (P3-6), stacking markers, and the saved choice.</summary>
public class LayerStripTests
{
    private static LayerToggle Toggle(LayerStripViewModel vm, string label) =>
        vm.Groups.SelectMany(g => g.Toggles).Single(t => t.Label == label);

    private static AppDataRoot TempRoot() => new(Path.Combine(Path.GetTempPath(), "er3d-strip-" + Guid.NewGuid().ToString("N")));

    [Fact]
    public void OneShadingAtATime()
    {
        var vm = new LayerStripViewModel(new MemorySettingsStore());
        Assert.Equal(ShadingLayer.Food, vm.Shading);   // today's default
        Assert.True(vm.ShowFood);
        var changed = new List<string?>();
        var food = Toggle(vm, "Food crisis");
        var foodChanged = 0;
        food.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(LayerToggle.IsChecked)) foodChanged++; };
        vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        var water = Toggle(vm, "Water");
        water.IsChecked = true;

        Assert.Equal(ShadingLayer.Water, vm.Shading);
        Assert.False(vm.ShowFood);
        Assert.False(food.IsChecked);
        Assert.True(water.IsChecked);
        // The strip shows the change: the food toggle is told it unchecked.
        Assert.Contains(nameof(LayerStripViewModel.ShowFood), changed);
        Assert.Contains(nameof(LayerStripViewModel.Shading), changed);
        Assert.True(foodChanged > 0);
        // Only one shading toggle is ever checked.
        Assert.Single(vm.Groups.SelectMany(g => g.Toggles), t => t.IsShading && t.IsChecked);

        Toggle(vm, "Oil production").IsChecked = true;   // across groups too
        Assert.Equal(ShadingLayer.OilProduction, vm.Shading);
        Assert.False(water.IsChecked);

        vm.ShowFood = true;   // the old property still works
        Assert.Equal(ShadingLayer.Food, vm.Shading);
        Assert.True(food.IsChecked);
        food.IsChecked = false;   // turning the shown one off leaves no shading
        Assert.Equal(ShadingLayer.None, vm.Shading);
        Assert.DoesNotContain(vm.Groups.SelectMany(g => g.Toggles), t => t.IsShading && t.IsChecked);
    }

    [Fact]
    public void ShadingChoiceIsSaved()
    {
        var root = TempRoot();
        try
        {
            Directory.CreateDirectory(root.Root);
            var store = new SettingsStore(root, new ListLog());
            var vm = new LayerStripViewModel(store);
            Toggle(vm, "Water").IsChecked = true;

            var reloaded = new SettingsStore(root, new ListLog()).Load();
            Assert.Equal(ShadingLayer.Water, reloaded.Shading);
            Assert.False(reloaded.ShowFood);   // kept in step for older copies of the app
            var again = new LayerStripViewModel(new SettingsStore(root, new ListLog()));
            Assert.Equal(ShadingLayer.Water, again.Shading);
            Assert.True(Toggle(again, "Water").IsChecked);
            Assert.False(again.ShowFood);
        }
        finally { Directory.Delete(root.Root, recursive: true); }
    }

    [Fact]
    public void MarkersStack()
    {
        var settings = new MemorySettingsStore();
        var vm = new LayerStripViewModel(settings);
        Assert.Equal([MarkerLayer.Conflict, MarkerLayer.Outbreaks, MarkerLayer.Displacement, MarkerLayer.HubLines], vm.ShownMarkers);   // today's default, and the hub lines (R3)

        Toggle(vm, "Power plants").IsChecked = true;
        Assert.True(vm.IsShown(MarkerLayer.PowerPlants));
        Assert.True(vm.ShowConflict);
        Assert.True(vm.IsShown(MarkerLayer.Conflict));
        Assert.Equal(ShadingLayer.Food, vm.Shading);   // a marker layer does not touch the shading
        Assert.Equal([MarkerLayer.Conflict, MarkerLayer.Outbreaks, MarkerLayer.Displacement, MarkerLayer.PowerPlants, MarkerLayer.HubLines], settings.Current.Markers);

        vm.ShowConflict = false;
        Assert.True(vm.IsShown(MarkerLayer.PowerPlants));
        Assert.False(Toggle(vm, "Armed conflict").IsChecked);
        Assert.False(settings.Current.ShowConflict);
    }

    [Fact]
    public void HeaderClosesTheWholeStrip()
    {
        var vm = new LayerStripViewModel(new MemorySettingsStore());
        Assert.True(vm.IsOpen);
        Assert.Equal(4, vm.VisibleGroups.Count);
        Assert.All(vm.VisibleGroups, g => Assert.NotEmpty(g.Toggles));
        vm.IsOpen = false;
        Assert.Empty(vm.VisibleGroups);   // no group, so no toggle, is shown
        Assert.Equal("Show or hide the list of layers", LayerStripViewModel.HeaderTooltip);
        vm.IsOpen = true;
        Assert.Equal(4, vm.VisibleGroups.Count);
    }

    [Fact]
    public void OpenOrClosedIsSaved()
    {
        var store = new MemorySettingsStore();
        var vm = new LayerStripViewModel(store);
        vm.IsOpen = false;
        Assert.False(store.Current.LayerStripExpanded);
        Assert.False(new LayerStripViewModel(store).IsOpen);
        vm.IsOpen = true;
        Assert.True(store.Current.LayerStripExpanded);
        Assert.True(new LayerStripViewModel(store).IsOpen);
    }

    [Fact]
    public void OldFileWithLabelsHiddenReadsAsClosed()
    {
        var store = new MemorySettingsStore(new AppSettings { LayerStripExpanded = false });
        Assert.False(new LayerStripViewModel(store).IsOpen);
    }

    [Fact]
    public void LabelsAlwaysShowWhenOpen()
    {
        // There is no labels-hidden state any more, and every toggle has a label and a symbol.
        Assert.Null(typeof(LayerStripViewModel).GetProperty("IsExpanded"));
        var vm = new LayerStripViewModel(new MemorySettingsStore());
        Assert.All(vm.Groups.SelectMany(g => g.Toggles), t =>
        {
            Assert.False(string.IsNullOrWhiteSpace(t.Label));
            Assert.False(string.IsNullOrWhiteSpace(t.Symbol));
        });
    }

    [Fact]
    public void APlan2SettingsFileIsReadWithShowFoodAsTheShading()
    {
        var root = TempRoot();
        try
        {
            Directory.CreateDirectory(root.Root);
            File.WriteAllText(root.SettingsFile, """
                { "ShowConflict": false, "ShowFood": true, "ShowOutbreaks": true, "ShowDisplacement": true, "LayerStripExpanded": false }
                """);
            var s = new SettingsStore(root, new ListLog()).Load();
            Assert.Null(s.Shading);
            Assert.Equal(ShadingLayer.Food, s.EffectiveShading);
            Assert.Equal([MarkerLayer.Outbreaks, MarkerLayer.Displacement, MarkerLayer.HubLines], s.EffectiveMarkers);   // a file with no marker list gets the lines to the nearest hubs on (R3)
            var vm = new LayerStripViewModel(new SettingsStore(root, new ListLog()));
            Assert.Equal(ShadingLayer.Food, vm.Shading);
            Assert.False(vm.ShowConflict);
            Assert.False(vm.IsOpen);   // the old name's false reads as closed (P5-23)
            Assert.Null(s.OpenGroups);   // no group choice in a Plan 2 file: the start rule decides
            Assert.False(vm.Groups.Single(g => g.Name == "Needs").IsOpen);

            File.WriteAllText(root.SettingsFile, """{ "ShowFood": false }""");
            Assert.Equal(ShadingLayer.None, new SettingsStore(root, new ListLog()).Load().EffectiveShading);
            Assert.Equal(ShadingLayer.None, new LayerStripViewModel(new SettingsStore(root, new ListLog())).Shading);
        }
        finally { Directory.Delete(root.Root, recursive: true); }
    }

    [Fact]
    public void GettingHelpGroupReplacesPlaceholder()
    {
        var vm = new LayerStripViewModel(new MemorySettingsStore());
        Assert.Equal(["Crises", "Needs", "Resources", "Getting help in"], vm.Groups.Select(g => g.Name));
        Assert.Equal(LayerStripViewModel.GettingHelpGroup, vm.Groups[3].Name);
        Assert.Equal(["Access constraints", "UN depots", "Airports", "Ports", "Lines to nearest hubs"], vm.Groups[3].Toggles.Select(t => t.Label));
        Assert.True(vm.Groups[3].IsCollapsible);
        Assert.True(vm.Groups[3].IsOpen);   // the lines to the nearest hubs are on by default (R3), so the group starts open: R20
        Assert.Null(typeof(LayerStripViewModel).GetProperty("ComingGroups"));   // the header-only placeholder is gone
    }

    [Fact]
    public void GettingHelpGroupHoldsHubs()
    {
        var vm = new LayerStripViewModel(new MemorySettingsStore());
        var group = Group(vm, LayerStripViewModel.GettingHelpGroup);
        // The Access shading first, then the three hub markers (P4-5, off by default), then the toggle for the lines to the
        // nearest hubs (R3, on by default).
        Assert.Equal(["access", "un-depots", "airports", "ports", "hub-lines"], group.Toggles.Select(t => t.Id));
        Assert.Equal([true, false, false, false, false], group.Toggles.Select(t => t.IsShading));
        Assert.All(group.Toggles.Skip(1).Take(3), t => Assert.False(t.IsChecked));
        Assert.True(group.Toggles[4].IsChecked);
        Assert.Equal(["★", "⬡", "◇", "⌒"], group.Toggles.Skip(1).Select(t => t.Symbol));
        Assert.All(group.Toggles.Skip(1), t => Assert.False(string.IsNullOrWhiteSpace(t.Tooltip)));
        Assert.Equal([MarkerLayer.HubLines], vm.ShownMarkers.Intersect(LayerCatalog.HelpMarkers));
        Assert.True(group.AnyOn);

        // Markers stack with a shading and with each other; turning one on opens the group at the next start (R20).
        Toggle(vm, "Airports").IsChecked = true;
        Toggle(vm, "Ports").IsChecked = true;
        Toggle(vm, "Water").IsChecked = true;
        Assert.Equal([MarkerLayer.Airports, MarkerLayer.Ports, MarkerLayer.HubLines], vm.ShownMarkers.Where(LayerCatalog.HelpMarkers.Contains));
        Assert.Equal(ShadingLayer.Water, vm.Shading);
        Assert.True(group.AnyOn);
        Assert.Contains("OurAirports", Toggle(vm, "Airports").Tooltip, StringComparison.Ordinal);
    }

    [Fact]
    public void HubMarkersAreSavedByNameAndComeBack()
    {
        var store = new MemorySettingsStore();
        var vm = new LayerStripViewModel(store);
        vm.Show(MarkerLayer.UnDepots, true);
        vm.Show(MarkerLayer.Airports, true);
        Assert.Contains(MarkerLayer.UnDepots, store.Current.Markers!);
        var again = new LayerStripViewModel(store);
        Assert.Equal([MarkerLayer.UnDepots, MarkerLayer.Airports, MarkerLayer.HubLines], again.ShownMarkers.Where(LayerCatalog.HelpMarkers.Contains));
        Assert.True(Group(again, LayerStripViewModel.GettingHelpGroup).IsOpen);   // a layer is on: the group starts open
    }

    [Fact]
    public void AccessIsOneOfTheShadings()
    {
        var vm = new LayerStripViewModel(new MemorySettingsStore());
        Toggle(vm, "Water").IsChecked = true;
        Toggle(vm, "Access constraints").IsChecked = true;
        Assert.Equal(ShadingLayer.Access, vm.Shading);
        Assert.False(Toggle(vm, "Water").IsChecked);
        Assert.Single(vm.Groups.SelectMany(g => g.Toggles), t => t.IsShading && t.IsChecked);
        Assert.True(vm.Groups[3].AnyOn);
        Assert.Equal("access", Toggle(vm, "Access constraints").Id);
    }

    [Fact]
    public void TheStripHasFourGroupsAndEveryLayerOnce()
    {
        var vm = new LayerStripViewModel(new MemorySettingsStore());
        Assert.Equal(["Crises", "Needs", "Resources", "Getting help in"], vm.Groups.Select(g => g.Name));
        Assert.Equal(["Armed conflict", "Food crisis", "Disease outbreaks", "People forced from home", "Genocide and mass atrocities", "Prison camps"],
            vm.Groups[0].Toggles.Select(t => t.Label));
        Assert.Equal(["Water", "Sanitation", "Health care", "Hunger", "Shelter need"], vm.Groups[1].Toggles.Select(t => t.Label));
        Assert.Equal(["Oil production", "Water stress", "Power plants", "Renewable power plants", "Oil and gas fields", "Steel plants"], vm.Groups[2].Toggles.Select(t => t.Label));
        Assert.Equal(["Access constraints", "UN depots", "Airports", "Ports", "Lines to nearest hubs"], vm.Groups[3].Toggles.Select(t => t.Label));
        // Every shading layer and every marker layer has exactly one toggle (R2: all of them exist from Task 2).
        var toggles = vm.Groups.SelectMany(g => g.Toggles).ToList();
        Assert.Equal(Enum.GetValues<ShadingLayer>().Length - 1 + Enum.GetValues<MarkerLayer>().Length, toggles.Count);
        Assert.Equal(toggles.Count, toggles.Select(t => t.Id).Distinct().Count());
        Assert.All(toggles, t =>
        {
            Assert.False(string.IsNullOrWhiteSpace(t.Symbol));
            Assert.False(string.IsNullOrWhiteSpace(t.Tooltip));
        });
        Assert.Equal(9, toggles.Count(t => t.IsShading));
        Assert.Equal(LegendViewModel.FoodTooltip, Toggle(vm, "Food crisis").Tooltip);
    }

    private static LayerGroup Group(LayerStripViewModel vm, string name) => vm.Groups.Single(g => g.Name == name);

    [Fact]
    public void AGroupStartsOpenOnlyWhenOneOfItsLayersIsOn()
    {
        // Today's default: food and the Plan 2 markers, all in Crises. Needs and Resources start closed; Crises always open.
        var vm = new LayerStripViewModel(new MemorySettingsStore());
        Assert.False(Group(vm, "Crises").IsCollapsible);
        Assert.True(Group(vm, "Crises").IsOpen);
        Assert.True(Group(vm, "Needs").IsCollapsible);
        Assert.False(Group(vm, "Needs").IsOpen);
        Assert.True(Group(vm, "Resources").IsCollapsible);
        Assert.False(Group(vm, "Resources").IsOpen);

        // An active shading opens its group.
        var water = new LayerStripViewModel(new MemorySettingsStore(new AppSettings { Shading = ShadingLayer.Water }));
        Assert.True(Group(water, "Needs").IsOpen);
        Assert.False(Group(water, "Resources").IsOpen);
        var oil = new LayerStripViewModel(new MemorySettingsStore(new AppSettings { Shading = ShadingLayer.OilProduction }));
        Assert.False(Group(oil, "Needs").IsOpen);
        Assert.True(Group(oil, "Resources").IsOpen);
        // So does a shown marker layer.
        var power = new LayerStripViewModel(new MemorySettingsStore(new AppSettings { Markers = [MarkerLayer.PowerPlants] }));
        Assert.True(Group(power, "Resources").IsOpen);
        Assert.False(Group(power, "Needs").IsOpen);
    }

    [Fact]
    public void AGroupNeverClosesByItself()
    {
        var vm = new LayerStripViewModel(new MemorySettingsStore(new AppSettings { Shading = ShadingLayer.Water }));
        var needs = Group(vm, "Needs");
        Assert.True(needs.IsOpen);
        Toggle(vm, "Food crisis").IsChecked = true;   // the water shading goes off: Needs stays open
        Assert.Equal(ShadingLayer.Food, vm.Shading);
        Assert.True(needs.IsOpen);
        vm.ShowFood = false;
        Assert.True(needs.IsOpen);
        Toggle(vm, "Power plants").IsChecked = true;
        Toggle(vm, "Power plants").IsChecked = false;
        Assert.True(needs.IsOpen);
    }

    [Fact]
    public void TheOpenOrClosedChoiceIsSaved()
    {
        var root = TempRoot();
        try
        {
            Directory.CreateDirectory(root.Root);
            var vm = new LayerStripViewModel(new SettingsStore(root, new ListLog()));
            Assert.False(Group(vm, "Resources").IsOpen);
            Group(vm, "Resources").IsOpen = true;   // the user opens it with nothing on
            Assert.True(new SettingsStore(root, new ListLog()).Load().OpenGroups![LayerStripViewModel.ResourcesGroup]);

            var again = new LayerStripViewModel(new SettingsStore(root, new ListLog()));
            Assert.True(Group(again, "Resources").IsOpen);
            Assert.False(Group(again, "Needs").IsOpen);
            Group(again, "Resources").IsOpen = false;
            Assert.False(new LayerStripViewModel(new SettingsStore(root, new ListLog())).Groups.Single(g => g.Name == "Resources").IsOpen);

            // A closed choice does not hide an active layer at start: the group opens to show its toggle.
            var store = new SettingsStore(root, new ListLog());
            var vm3 = new LayerStripViewModel(store);
            Toggle(vm3, "Oil production").IsChecked = true;
            Assert.True(new LayerStripViewModel(new SettingsStore(root, new ListLog())).Groups.Single(g => g.Name == "Resources").IsOpen);

            // Crises is never stored (it cannot close).
            Assert.False(new SettingsStore(root, new ListLog()).Load().OpenGroups!.ContainsKey(LayerStripViewModel.CrisesGroup));
        }
        finally { Directory.Delete(root.Root, recursive: true); }
    }

    [Fact]
    public void ASceneOpensTheGroupsOfItsLayersAndClosesTheOthers()
    {
        var settings = new MemorySettingsStore();
        var vm = new LayerStripViewModel(settings);
        vm.SetLayers(ShadingLayer.OilProduction, []);
        Assert.True(Group(vm, "Resources").IsOpen);
        Assert.False(Group(vm, "Needs").IsOpen);
        vm.SetLayers(ShadingLayer.Water, [MarkerLayer.Camps]);
        Assert.True(Group(vm, "Needs").IsOpen);
        Assert.False(Group(vm, "Resources").IsOpen);
        // A scene is not the user's choice: nothing is saved for the groups.
        Assert.Null(settings.Current.OpenGroups);
    }

    [Fact]
    public void SetLayersAppliesASceneAtOnce()
    {
        var vm = new LayerStripViewModel(new MemorySettingsStore());
        vm.SetLayers(ShadingLayer.Water, [MarkerLayer.Camps]);
        Assert.Equal(ShadingLayer.Water, vm.Shading);
        Assert.Equal([MarkerLayer.Camps], vm.ShownMarkers);
        Assert.False(vm.ShowConflict || vm.ShowFood || vm.ShowOutbreaks || vm.ShowDisplacement);
    }
}
