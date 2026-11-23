using System.Windows.Media;

namespace EarthRelief3D.App.Presentation;

/// <summary>The four crisis layers of Plan 2, in the order the strip, the legend and the panel list them.</summary>
public enum CrisisLayer { Conflict, Food, Outbreaks, Displacement }

/// <summary>
/// The country shadings (Plan 3, P3-6): one shows at a time across all groups. Food is the IPC shading of Plan 2 (R17);
/// the others are country values on a sequential scale (P3-7, <see cref="ShadingScales"/>).
/// </summary>
public enum ShadingLayer { None, Food, Water, Sanitation, Health, Hunger, ShelterNeed, OilProduction, WaterStress }

/// <summary>The marker layers (Plan 3, P3-6): any number show at once (they stack).</summary>
public enum MarkerLayer { Conflict, Outbreaks, Displacement, Atrocities, Camps, PowerPlants, OilGasFields, SteelPlants }

/// <summary>The second cue after colour (C8): each layer has its own shape.</summary>
/// <remarks>
/// Plan 3 adds the hollow triangle (atrocities, Task 5), the filled square (prison camps), the disc with a dark edge
/// (power plants, filled by fuel group in Task 7: the edge keeps it apart from conflict's plain dots when both are on),
/// the diamond (oil and gas fields) and the square outline (steel plants).
/// </remarks>
public enum MarkerShape { Dot, Area, Cross, Ring, TriangleOutline, Square, EdgedDisc, Diamond, SquareOutline }

/// <summary>Colours, shapes and legend symbols of the crisis layers. Colour-blind safe, with shape as a second cue (C8).</summary>
public static class LayerStyle
{
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<Color, SolidColorBrush> BrushCache = new();

    /// <summary>One frozen brush per colour, made once and shared (frozen brushes are safe on any thread).</summary>
    public static SolidColorBrush Frozen(Color c) => BrushCache.GetOrAdd(c, static k =>
    {
        var b = new SolidColorBrush(k);
        b.Freeze();
        return b;
    });

    // ---------------------------------------------------------------- IPC phase colours
    //
    // The IPC's own Acute Food Insecurity phase colours (the IPC asks for them on every map of IPC data):
    //   Phase 1 Minimal             #CDFACD  (205, 250, 205)
    //   Phase 2 Stressed            #FAE61E  (250, 230,  30)
    //   Phase 3 Crisis              #E67800  (230, 120,   0)
    //   Phase 4 Emergency           #C80000  (200,   0,   0)
    //   Phase 5 Catastrophe/Famine  #640000  (100,   0,   0)
    // Verified source (fetched directly on 2026-11-22; FEWS NET states it uses the IPC colours; the same five values):
    //   https://help.fews.net/brand/data-visualization-colors
    // The IPC's own documents give the same values. ipcinfo.org refuses automated downloads (HTTP 403), so these were read
    // through a search engine's text of them, not opened:
    //   https://www.ipcinfo.org/fileadmin/user_upload/ipcinfo/docs/communication_tools/IPC_Icons_guidelines_2023.pdf
    //   https://www.ipcinfo.org/ipc-manual-interactive/ipc-acute-food-insecurity-protocols/function-3-communicate-for-action/protocol-32-adhere-to-mapping-standards/en/
    //   https://library.ipcinfo.org/Acute%20Food%20Insecurity/IPC%20Thematic%20Material/7.%20Communication/Guidance%20Notes%20on%20Communication/IPC_Acute_Food_Insecurity_Mapping_Guidelines.pdf
    private static readonly Color[] IpcPhaseColors =
    [
        Color.FromRgb(0xCD, 0xFA, 0xCD),   // Phase 1 Minimal
        Color.FromRgb(0xFA, 0xE6, 0x1E),   // Phase 2 Stressed
        Color.FromRgb(0xE6, 0x78, 0x00),   // Phase 3 Crisis
        Color.FromRgb(0xC8, 0x00, 0x00),   // Phase 4 Emergency
        Color.FromRgb(0x64, 0x00, 0x00),   // Phase 5 Catastrophe/Famine
    ];

    /// <summary>The IPC's colour for <paramref name="phase"/> (1 to 5).</summary>
    public static Color IpcPhaseColor(int phase) =>
        phase is >= 1 and <= 5 ? IpcPhaseColors[phase - 1] : throw new ArgumentOutOfRangeException(nameof(phase), phase, "IPC phases are 1 to 5.");

    /// <summary>The IPC's phase name, one wording everywhere (legend, tooltip, panel): Phase 5 is "Catastrophe/Famine".</summary>
    public static string IpcPhaseName(int phase) => phase switch
    {
        1 => "Minimal",
        2 => "Stressed",
        3 => "Crisis",
        4 => "Emergency",
        5 => "Catastrophe/Famine",
        _ => throw new ArgumentOutOfRangeException(nameof(phase), phase, "IPC phases are 1 to 5."),
    };

    /// <summary>"Phase 4 Emergency", "Phase 5 Catastrophe/Famine".</summary>
    public static string IpcPhaseLabel(int phase) => $"Phase {phase} {IpcPhaseName(phase)}";

    // ---------------------------------------------------------------- markers
    //
    // The marker colours keep clear of the IPC's yellow-to-dark-red scale, and each reads by lightness and shape alone.

    /// <summary>
    /// Conflict dots by the age of the event (newest first): one hue, reddish purple (Okabe–Ito #CC79A7), from light to
    /// dark, so the order reads by lightness alone for any colour vision.
    /// </summary>
    public static IReadOnlyList<Color> ConflictAgeColors { get; } =
    [
        Color.FromRgb(0xF5, 0xB8, 0xE0),   // the last 3 months
        Color.FromRgb(0xCC, 0x79, 0xA7),   // 3 to 6 months before
        Color.FromRgb(0x84, 0x4A, 0x6C),   // older
    ];

    public static IReadOnlyList<string> ConflictAgeLabels { get; } = ["last 3 months", "3–6 months earlier", "older"];

    /// <summary>Outbreak crosses: Okabe–Ito sky blue.</summary>
    public static Color OutbreakColor { get; } = Color.FromRgb(0x56, 0xB4, 0xE9);

    /// <summary>Displacement rings: near white, readable over the IPC shading and the dark globe.</summary>
    public static Color DisplacementColor { get; } = Color.FromRgb(0xF0, 0xF0, 0xF0);

    /// <summary>Country labels.</summary>
    public static Color LabelColor { get; } = Color.FromRgb(0xF0, 0xF0, 0xF0);

    // Frozen brushes of SymbolColor, for the views.
    public static SolidColorBrush ConflictBrush { get; } = Frozen(SymbolColor(CrisisLayer.Conflict));
    public static SolidColorBrush FoodBrush { get; } = Frozen(SymbolColor(CrisisLayer.Food));
    public static SolidColorBrush OutbreakBrush { get; } = Frozen(SymbolColor(CrisisLayer.Outbreaks));
    public static SolidColorBrush DisplacementBrush { get; } = Frozen(SymbolColor(CrisisLayer.Displacement));

    public static MarkerShape Shape(CrisisLayer layer) => layer switch
    {
        CrisisLayer.Conflict => MarkerShape.Dot,
        CrisisLayer.Food => MarkerShape.Area,
        CrisisLayer.Outbreaks => MarkerShape.Cross,
        CrisisLayer.Displacement => MarkerShape.Ring,
        _ => throw new ArgumentOutOfRangeException(nameof(layer)),
    };

    /// <summary>The legend's and the strip's symbol for the layer's shape.</summary>
    public static string Symbol(CrisisLayer layer) => Symbol(Shape(layer));

    /// <summary>The legend's and the strip's symbol for a shape.</summary>
    public static string Symbol(MarkerShape shape) => shape switch
    {
        MarkerShape.Dot => "●",
        MarkerShape.Area => "▰",
        MarkerShape.Cross => "✚",
        MarkerShape.Ring => "◯",
        MarkerShape.TriangleOutline => "△",
        MarkerShape.Square => "■",
        MarkerShape.EdgedDisc => "◉",
        MarkerShape.Diamond => "◆",
        MarkerShape.SquareOutline => "□",
        _ => throw new ArgumentOutOfRangeException(nameof(shape)),
    };

    /// <summary>The colour the legend draws the layer's symbol in.</summary>
    public static Color SymbolColor(CrisisLayer layer) => layer switch
    {
        CrisisLayer.Conflict => ConflictAgeColors[1],
        CrisisLayer.Food => IpcPhaseColor(3),
        CrisisLayer.Outbreaks => OutbreakColor,
        CrisisLayer.Displacement => DisplacementColor,
        _ => throw new ArgumentOutOfRangeException(nameof(layer)),
    };

    // ---------------------------------------------------------------- Plan 3: marker layers and shadings
    //
    // Okabe–Ito colours, as the Plan 2 markers, each kept to one layer: yellow for prison camps, orange for power plants
    // (Task 7 colours them by fuel group; this is the legend's default), vermillion for oil and gas fields, bluish green
    // for steel plants. Atrocities: #F2F2F2 with a dark edge (Task 5).

    /// <summary>Atrocity glyphs (Task 5): near white, with a dark edge.</summary>
    public static Color AtrocityColor { get; } = Color.FromRgb(0xF2, 0xF2, 0xF2);
    public static Color CampColor { get; } = Color.FromRgb(0xF0, 0xE4, 0x42);
    public static Color PowerPlantColor { get; } = Color.FromRgb(0xE6, 0x9F, 0x00);
    public static Color OilGasColor { get; } = Color.FromRgb(0xD5, 0x5E, 0x00);
    public static Color SteelColor { get; } = Color.FromRgb(0x00, 0x9E, 0x73);
    /// <summary>The dark edge of an edged disc (power plants) and of the atrocity triangle.</summary>
    public static Color MarkerEdgeColor { get; } = Color.FromRgb(0x20, 0x20, 0x20);

    /// <summary>The marker layer that stands for a Plan 2 crisis layer (food is a shading, not a marker).</summary>
    public static MarkerLayer? MarkerOf(CrisisLayer layer) => layer switch
    {
        CrisisLayer.Conflict => MarkerLayer.Conflict,
        CrisisLayer.Outbreaks => MarkerLayer.Outbreaks,
        CrisisLayer.Displacement => MarkerLayer.Displacement,
        _ => null,
    };

    public static MarkerShape Shape(MarkerLayer layer) => layer switch
    {
        MarkerLayer.Conflict => Shape(CrisisLayer.Conflict),
        MarkerLayer.Outbreaks => Shape(CrisisLayer.Outbreaks),
        MarkerLayer.Displacement => Shape(CrisisLayer.Displacement),
        MarkerLayer.Atrocities => MarkerShape.TriangleOutline,
        MarkerLayer.Camps => MarkerShape.Square,
        MarkerLayer.PowerPlants => MarkerShape.EdgedDisc,
        MarkerLayer.OilGasFields => MarkerShape.Diamond,
        MarkerLayer.SteelPlants => MarkerShape.SquareOutline,
        _ => throw new ArgumentOutOfRangeException(nameof(layer)),
    };

    public static string Symbol(MarkerLayer layer) => Symbol(Shape(layer));

    /// <summary>The marker layer's colour: its markers', and the legend's and strip's symbol.</summary>
    public static Color SymbolColor(MarkerLayer layer) => layer switch
    {
        MarkerLayer.Conflict => SymbolColor(CrisisLayer.Conflict),
        MarkerLayer.Outbreaks => SymbolColor(CrisisLayer.Outbreaks),
        MarkerLayer.Displacement => SymbolColor(CrisisLayer.Displacement),
        MarkerLayer.Atrocities => AtrocityColor,
        MarkerLayer.Camps => CampColor,
        MarkerLayer.PowerPlants => PowerPlantColor,
        MarkerLayer.OilGasFields => OilGasColor,
        MarkerLayer.SteelPlants => SteelColor,
        _ => throw new ArgumentOutOfRangeException(nameof(layer)),
    };

    /// <summary>Every shading is an area ("▰").</summary>
    public static string Symbol(ShadingLayer layer) => layer == ShadingLayer.None
        ? throw new ArgumentOutOfRangeException(nameof(layer)) : Symbol(MarkerShape.Area);

    /// <summary>The shading's symbol colour: IPC Phase 3 for food, otherwise its scale's fourth class (the group's colours).</summary>
    public static Color SymbolColor(ShadingLayer layer) => layer switch
    {
        ShadingLayer.None => throw new ArgumentOutOfRangeException(nameof(layer)),
        ShadingLayer.Food => SymbolColor(CrisisLayer.Food),
        // Water stress has the arid grey as its class 0, so its classes are shifted by one: its Colors[4] is YlGnBu's
        // fourth colour (#2C7FB8), the colour the other shadings take from Colors[3].
        ShadingLayer.WaterStress => ShadingScales.For(layer).Colors[4],
        _ => ShadingScales.For(layer).Colors[3],
    };
}
