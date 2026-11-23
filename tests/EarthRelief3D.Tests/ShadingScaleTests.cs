using System.Windows.Media;
using EarthRelief3D.App.Presentation;

namespace EarthRelief3D.Tests;

/// <summary>The shading scales of P3-7 (breaks are lower bounds; ruling R3 for Hunger).</summary>
public class ShadingScaleTests
{
    private static readonly ShadingLayer[] Shadings =
        [.. Enum.GetValues<ShadingLayer>().Where(l => l is not ShadingLayer.None and not ShadingLayer.Food)];

    [Fact]
    public void ClassOfUsesLowerBounds()
    {
        var water = ShadingScales.For(ShadingLayer.Water);
        Assert.Equal([0, 5, 15, 30, 50], water.Breaks);
        Assert.Equal(0, water.ClassOf(4.9));
        Assert.Equal(1, water.ClassOf(5));
        Assert.Equal(3, water.ClassOf(49.9));
        Assert.Equal(4, water.ClassOf(50));
        Assert.Equal(0, water.ClassOf(0));
        Assert.Equal(4, water.ClassOf(100));
        Assert.Null(water.ClassOf(-0.1));   // below the first break: not shaded
        Assert.Null(water.ClassOf(double.NaN));
        Assert.Equal(water.Colors[3], water.ColorOf(49.9));
        Assert.Null(water.ColorOf(-1));
    }

    [Fact]
    public void HungerUsesTheFaoHungerMapClasses()
    {
        // Ruling R3: FAO's five classes, so every country with a figure is filled ("<2.5" arrives as 2.5).
        var hunger = ShadingScales.For(ShadingLayer.Hunger);
        Assert.Equal([0, 5, 15, 25, 35], hunger.Breaks);
        Assert.Equal(["under 5%", "5–15%", "15–25%", "25–35%", "35% or more"], hunger.ClassLabels);
        Assert.Equal(0, hunger.ClassOf(2.5));
        Assert.Equal(0, hunger.ClassOf(4.9));
        Assert.Equal(1, hunger.ClassOf(5));
        Assert.Equal(2, hunger.ClassOf(15));
        Assert.Equal(3, hunger.ClassOf(34.9));
        Assert.Equal(4, hunger.ClassOf(35));
    }

    [Fact]
    public void TheOtherFixedBreaksAreThePlansTable()
    {
        Assert.Equal([0, 5, 15, 30, 50], ShadingScales.For(ShadingLayer.Sanitation).Breaks);
        Assert.Equal([0, 20, 40, 60, 80], ShadingScales.For(ShadingLayer.Health).Breaks);
        Assert.Equal([0, 5, 15, 30, 50], ShadingScales.For(ShadingLayer.ShelterNeed).Breaks);
        Assert.Equal([0, 10, 100, 1000, 5000], ShadingScales.For(ShadingLayer.OilProduction).Breaks);
        Assert.Equal(["under 5%", "5–15%", "15–30%", "30–50%", "50% or more"], ShadingScales.For(ShadingLayer.Water).ClassLabels);
        Assert.Equal(["under 10", "10–100", "100–1,000", "1,000–5,000", "5,000 or more"], ShadingScales.For(ShadingLayer.OilProduction).ClassLabels);
    }

    [Fact]
    public void EachGroupHasItsColourBrewerScale()
    {
        Color[] buPu = [Rgb(0xEDF8FB), Rgb(0xB3CDE3), Rgb(0x8C96C6), Rgb(0x8856A7), Rgb(0x810F7C)];
        Color[] ylGnBu = [Rgb(0xFFFFCC), Rgb(0xA1DAB4), Rgb(0x41B6C4), Rgb(0x2C7FB8), Rgb(0x253494)];
        foreach (var l in new[] { ShadingLayer.Water, ShadingLayer.Sanitation, ShadingLayer.Health, ShadingLayer.Hunger, ShadingLayer.ShelterNeed })
            Assert.Equal(buPu, ShadingScales.For(l).Colors);
        Assert.Equal(ylGnBu, ShadingScales.For(ShadingLayer.OilProduction).Colors);
        // Never the IPC colours (P3-7).
        var ipc = Enumerable.Range(1, 5).Select(LayerStyle.IpcPhaseColor).ToHashSet();
        Assert.All(Shadings, l => Assert.DoesNotContain(ShadingScales.For(l).Colors, ipc.Contains));
    }

    [Fact]
    public void WaterStressUsesWriCategoriesWithAridInGrey()
    {
        var bws = ShadingScales.For(ShadingLayer.WaterStress);
        Assert.True(bws.Categorical);
        Assert.Equal([-1, 0, 1, 2, 3, 4], bws.Breaks);
        Assert.Equal(0, bws.ClassOf(-1));
        Assert.Equal(Rgb(0xBDBDBD), bws.Colors[0]);
        Assert.Equal("Arid and Low Water Use", bws.ClassLabels[0]);
        Assert.Equal(1, bws.ClassOf(0));
        Assert.Equal(5, bws.ClassOf(4));
        Assert.Equal([Rgb(0xFFFFCC), Rgb(0xA1DAB4), Rgb(0x41B6C4), Rgb(0x2C7FB8), Rgb(0x253494)], bws.Colors.Skip(1));
        Assert.Null(bws.ClassOf(-9999));   // WRI's "no data"
    }

    [Fact]
    public void EveryScaleHasOneColourAndLabelPerBreakInRisingOrder()
    {
        Assert.All(Shadings, l =>
        {
            var s = ShadingScales.For(l);
            Assert.Equal(s.Breaks.Count, s.Colors.Count);
            Assert.Equal(s.Breaks.Count, s.ClassLabels.Count);
            Assert.True(s.Breaks.Zip(s.Breaks.Skip(1)).All(p => p.First < p.Second));
            Assert.All(s.ClassLabels, t => Assert.False(string.IsNullOrWhiteSpace(t)));
        });
        Assert.Throws<ArgumentOutOfRangeException>(() => ShadingScales.For(ShadingLayer.None));
        Assert.Throws<ArgumentOutOfRangeException>(() => ShadingScales.For(ShadingLayer.Food));   // IPC's own colours (R17)
    }

    private static Color Rgb(int rgb) => Color.FromRgb((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb);
}
