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
        Assert.Equal([MarkerLayer.Conflict, MarkerLayer.Outbreaks, MarkerLayer.Displacement], vm.ShownMarkers);   // today's default

        Toggle(vm, "Power plants").IsChecked = true;
        Assert.True(vm.IsShown(MarkerLayer.PowerPlants));
        Assert.True(vm.ShowConflict);
        Assert.True(vm.IsShown(MarkerLayer.Conflict));
        Assert.Equal(ShadingLayer.Food, vm.Shading);   // a marker layer does not touch the shading
        Assert.Equal([MarkerLayer.Conflict, MarkerLayer.Outbreaks, MarkerLayer.Displacement, MarkerLayer.PowerPlants], settings.Current.Markers);

        vm.ShowConflict = false;
        Assert.True(vm.IsShown(MarkerLayer.PowerPlants));
        Assert.False(Toggle(vm, "Armed conflict").IsChecked);
        Assert.False(settings.Current.ShowConflict);
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
            Assert.Equal([MarkerLayer.Outbreaks, MarkerLayer.Displacement], s.EffectiveMarkers);
            var vm = new LayerStripViewModel(new SettingsStore(root, new ListLog()));
            Assert.Equal(ShadingLayer.Food, vm.Shading);
            Assert.False(vm.ShowConflict);
            Assert.False(vm.IsExpanded);

            File.WriteAllText(root.SettingsFile, """{ "ShowFood": false }""");
            Assert.Equal(ShadingLayer.None, new SettingsStore(root, new ListLog()).Load().EffectiveShading);
            Assert.Equal(ShadingLayer.None, new LayerStripViewModel(new SettingsStore(root, new ListLog())).Shading);
        }
        finally { Directory.Delete(root.Root, recursive: true); }
    }

    [Fact]
    public void TheStripHasFourGroupsAndEveryLayerOnce()
    {
        var vm = new LayerStripViewModel(new MemorySettingsStore());
        Assert.Equal(["Crises", "Needs", "Resources"], vm.Groups.Select(g => g.Name));
        Assert.Equal(["Getting help in"], vm.ComingGroups.Select(g => g.Name));
        Assert.Equal(["Armed conflict", "Food crisis", "Disease outbreaks", "People forced from home", "Genocide and mass atrocities", "Prison camps"],
            vm.Groups[0].Toggles.Select(t => t.Label));
        Assert.Equal(["Water", "Sanitation", "Health care", "Hunger", "Shelter need"], vm.Groups[1].Toggles.Select(t => t.Label));
        Assert.Equal(["Oil production", "Water stress", "Power plants", "Oil and gas fields", "Steel plants"], vm.Groups[2].Toggles.Select(t => t.Label));
        // Every shading layer and every marker layer has exactly one toggle (R2: all of them exist from Task 2).
        var toggles = vm.Groups.SelectMany(g => g.Toggles).ToList();
        Assert.Equal(Enum.GetValues<ShadingLayer>().Length - 1 + Enum.GetValues<MarkerLayer>().Length, toggles.Count);
        Assert.Equal(toggles.Count, toggles.Select(t => t.Id).Distinct().Count());
        Assert.All(toggles, t =>
        {
            Assert.False(string.IsNullOrWhiteSpace(t.Symbol));
            Assert.False(string.IsNullOrWhiteSpace(t.Tooltip));
        });
        Assert.Equal(8, toggles.Count(t => t.IsShading));
        Assert.Equal(LegendViewModel.FoodTooltip, Toggle(vm, "Food crisis").Tooltip);
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
