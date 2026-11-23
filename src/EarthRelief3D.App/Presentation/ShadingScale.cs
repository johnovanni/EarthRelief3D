using System.Windows.Media;

namespace EarthRelief3D.App.Presentation;

/// <summary>
/// A class scale for country shading (P3-7). <see cref="Breaks"/> are lower bounds in rising order: class i runs from
/// Breaks[i] up to (not including) Breaks[i + 1], and the last class has no upper bound. A value below the first break
/// (or not a number) has no class and is not shaded.
/// </summary>
public sealed record ShadingScale(IReadOnlyList<double> Breaks, IReadOnlyList<Color> Colors, IReadOnlyList<string> ClassLabels)
{
    /// <summary>
    /// True when the values are a source's categories (WRI's water-stress categories): the tooltip names the category
    /// (the value's own label if it has one, else <see cref="ClassLabels"/>), never the number.
    /// </summary>
    public bool Categorical { get; init; }

    /// <summary>The class of <paramref name="value"/>, or null when it is below the first break or not a number.</summary>
    public int? ClassOf(double value)
    {
        if (double.IsNaN(value)) return null;
        for (int i = Breaks.Count - 1; i >= 0; i--)
            if (value >= Breaks[i]) return i;
        return null;
    }

    /// <summary>The colour <paramref name="value"/> is filled in, or null when it is not shaded.</summary>
    public Color? ColorOf(double value) => ClassOf(value) is { } c ? Colors[c] : null;
}

/// <summary>The scales of the shading layers (P3-7; Hunger per ruling R3). Food is not here: it keeps the IPC colours (R17).</summary>
public static class ShadingScales
{
    /// <summary>Needs: ColorBrewer BuPu, 5 classes (colour-blind safe, unlike any IPC colour).</summary>
    public static IReadOnlyList<Color> Needs { get; } = [Rgb(0xEDF8FB), Rgb(0xB3CDE3), Rgb(0x8C96C6), Rgb(0x8856A7), Rgb(0x810F7C)];

    /// <summary>Resources: ColorBrewer YlGnBu, 5 classes.</summary>
    public static IReadOnlyList<Color> Resources { get; } = [Rgb(0xFFFFCC), Rgb(0xA1DAB4), Rgb(0x41B6C4), Rgb(0x2C7FB8), Rgb(0x253494)];

    /// <summary>WRI's "Arid and Low Water Use" category: grey, so it never reads as low or high stress.</summary>
    public static Color AridColor { get; } = Rgb(0xBDBDBD);

    private static readonly string[] PercentLabels5 = ["under 5%", "5–15%", "15–30%", "30–50%", "50% or more"];

    private static readonly ShadingScale WaterScale = new([0, 5, 15, 30, 50], Needs, PercentLabels5);
    private static readonly ShadingScale SanitationScale = new([0, 5, 15, 30, 50], Needs, PercentLabels5);
    private static readonly ShadingScale HealthScale = new([0, 20, 40, 60, 80], Needs, ["under 20", "20–40", "40–60", "60–80", "80 or more"]);
    // FAO Hunger Map classes (ruling R3): every country with a figure is filled; FAO's "<2.5" falls in the first class.
    private static readonly ShadingScale HungerScale = new([0, 5, 15, 25, 35], Needs, ["under 5%", "5–15%", "15–25%", "25–35%", "35% or more"]);
    private static readonly ShadingScale ShelterScale = new([0, 5, 15, 30, 50], Needs, PercentLabels5);
    private static readonly ShadingScale OilScale = new([0, 10, 100, 1000, 5000], Resources,
        ["under 10", "10–100", "100–1,000", "1,000–5,000", "5,000 or more"]);
    // WRI Aqueduct 4.0 baseline water stress (bws) categories: cat -1 is arid and low water use, 0 to 4 low to extremely
    // high; -9999 (no data) has no class. The labels are WRI's (the dataset's `label` column); Task 6 checks them against
    // the file it extracts.
    private static readonly ShadingScale WaterStressScale = new([-1, 0, 1, 2, 3, 4], [AridColor, .. Resources],
        ["Arid and Low Water Use", "Low (<10%)", "Low - Medium (10-20%)", "Medium - High (20-40%)", "High (40-80%)", "Extremely High (>80%)"])
    {
        Categorical = true,
    };

    public static ShadingScale For(ShadingLayer layer) => layer switch
    {
        ShadingLayer.Water => WaterScale,
        ShadingLayer.Sanitation => SanitationScale,
        ShadingLayer.Health => HealthScale,
        ShadingLayer.Hunger => HungerScale,
        ShadingLayer.ShelterNeed => ShelterScale,
        ShadingLayer.OilProduction => OilScale,
        ShadingLayer.WaterStress => WaterStressScale,
        _ => throw new ArgumentOutOfRangeException(nameof(layer), layer, "Only the country-value shadings have a scale (food keeps the IPC colours)."),
    };

    private static Color Rgb(int rgb) => Color.FromRgb((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb);
}
