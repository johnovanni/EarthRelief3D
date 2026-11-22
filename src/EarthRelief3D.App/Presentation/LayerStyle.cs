using System.Windows.Media;

namespace EarthRelief3D.App.Presentation;

/// <summary>The four crisis layers of Plan 2, in the order the strip, the legend and the panel list them.</summary>
public enum CrisisLayer { Conflict, Food, Outbreaks, Displacement }

/// <summary>The second cue after colour (C8): each layer has its own shape.</summary>
public enum MarkerShape { Dot, Area, Cross, Ring }

/// <summary>Colours, shapes and legend symbols of the crisis layers. Colour-blind safe, with shape as a second cue (C8).</summary>
public static class LayerStyle
{
    // ---------------------------------------------------------------- IPC phase colours
    //
    // The IPC's own Acute Food Insecurity phase colours (the IPC asks for them on every map of IPC data):
    //   Phase 1 Minimal    #CDFACD  (205, 250, 205)
    //   Phase 2 Stressed   #FAE61E  (250, 230,  30)
    //   Phase 3 Crisis     #E67800  (230, 120,   0)
    //   Phase 4 Emergency  #C80000  (200,   0,   0)
    //   Phase 5 Famine     #640000  (100,   0,   0)
    // Sources (IPC):
    //   https://www.ipcinfo.org/fileadmin/user_upload/ipcinfo/docs/communication_tools/IPC_Icons_guidelines_2023.pdf
    //   https://www.ipcinfo.org/ipc-manual-interactive/ipc-acute-food-insecurity-protocols/function-3-communicate-for-action/protocol-32-adhere-to-mapping-standards/en/
    //   (IPC Technical Manual 3.1, Protocol 3.2 "Adhere to mapping standards"; also the IPC Mapping Guidelines for Acute
    //   Food Insecurity, https://library.ipcinfo.org/ .../IPC_Acute_Food_Insecurity_Mapping_Guidelines.pdf)
    // Checked 2026-11-22. ipcinfo.org refuses automated downloads (HTTP 403), so the values were read from the search
    // index's text of those two IPC documents, and confirmed against FEWS NET's brand page, which states it uses the IPC
    // colours: https://help.fews.net/brand/data-visualization-colors (fetched directly; same five values).
    private static readonly Color[] IpcPhaseColors =
    [
        Color.FromRgb(0xCD, 0xFA, 0xCD),   // Phase 1 Minimal
        Color.FromRgb(0xFA, 0xE6, 0x1E),   // Phase 2 Stressed
        Color.FromRgb(0xE6, 0x78, 0x00),   // Phase 3 Crisis
        Color.FromRgb(0xC8, 0x00, 0x00),   // Phase 4 Emergency
        Color.FromRgb(0x64, 0x00, 0x00),   // Phase 5 Famine
    ];

    /// <summary>The IPC's colour for <paramref name="phase"/> (1 to 5).</summary>
    public static Color IpcPhaseColor(int phase) =>
        phase is >= 1 and <= 5 ? IpcPhaseColors[phase - 1] : throw new ArgumentOutOfRangeException(nameof(phase), phase, "IPC phases are 1 to 5.");

    /// <summary>The IPC's short phase name (the legend writes "Phase 5 Famine"; the data says "Catastrophe/Famine" for households).</summary>
    public static string IpcPhaseName(int phase) => phase switch
    {
        1 => "Minimal",
        2 => "Stressed",
        3 => "Crisis",
        4 => "Emergency",
        5 => "Famine",
        _ => throw new ArgumentOutOfRangeException(nameof(phase), phase, "IPC phases are 1 to 5."),
    };

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

    public static SolidColorBrush Frozen(Color c)
    {
        var b = new SolidColorBrush(c);
        b.Freeze();
        return b;
    }

    public static MarkerShape Shape(CrisisLayer layer) => layer switch
    {
        CrisisLayer.Conflict => MarkerShape.Dot,
        CrisisLayer.Food => MarkerShape.Area,
        CrisisLayer.Outbreaks => MarkerShape.Cross,
        CrisisLayer.Displacement => MarkerShape.Ring,
        _ => throw new ArgumentOutOfRangeException(nameof(layer)),
    };

    /// <summary>The legend's and the strip's symbol for the layer's shape.</summary>
    public static string Symbol(CrisisLayer layer) => Shape(layer) switch
    {
        MarkerShape.Dot => "●",
        MarkerShape.Area => "▰",
        MarkerShape.Cross => "✚",
        MarkerShape.Ring => "◯",
        _ => throw new ArgumentOutOfRangeException(nameof(layer)),
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
}
