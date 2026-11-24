using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using EarthRelief3D.App.Presentation;
using GlobeCore.Logging;
using GlobeCore.Storage;

namespace EarthRelief3D.App.Settings;

/// <summary>
/// What the app remembers between runs (settings.json in the data folder). By default the food shading and the three
/// Plan 2 crisis markers are on, as in Plan 2.
/// <para>
/// Plan 3 keeps the four Plan 2 flags, so a Plan 2 file still reads: with no <see cref="Shading"/> in the file,
/// <see cref="ShowFood"/> gives it (food or none); with no <see cref="Markers"/>, the three marker flags give them. A
/// save writes both, kept in step, so an older copy of the app still reads the file.
/// </para>
/// </summary>
public sealed record AppSettings
{
    public bool ShowConflict { get; init; } = true;
    public bool ShowFood { get; init; } = true;
    public bool ShowOutbreaks { get; init; } = true;
    public bool ShowDisplacement { get; init; } = true;
    /// <summary>The layer strip shows text labels beside its icons.</summary>
    public bool LayerStripExpanded { get; init; } = true;

    /// <summary>The one shading shown (P3-6); null in a Plan 2 file (then <see cref="ShowFood"/> says).</summary>
    public ShadingLayer? Shading { get; init; }

    /// <summary>The marker layers shown (they stack); null in a Plan 2 file (then the three Plan 2 flags say).</summary>
    public IReadOnlyList<MarkerLayer>? Markers { get; init; }

    /// <summary>
    /// The governments whose formal determinations are shown (Settings, "Formal determinations by"; C15), by ISO3; null
    /// (no choice saved yet): every government offered, which in Plan 3 is the United States (P3-5, on by default).
    /// </summary>
    public IReadOnlyList<string>? DeterminationGovernments { get; init; }

    /// <summary>
    /// The user's open or closed choice for each collapsible group of the strip ("Needs", "Resources"; ruling R20), by
    /// name; null, or a group missing, when the user has not chosen (then the group starts open only if one of its layers
    /// is on). A Plan 2 file has none.
    /// </summary>
    public IReadOnlyDictionary<string, bool>? OpenGroups { get; init; }

    /// <summary>The unit distances are shown in (Settings, "Distances"; P4-15): kilometres, or miles. A file without it reads as kilometres.</summary>
    public DistanceUnit Units { get; init; } = DistanceUnit.Kilometres;

    /// <summary>The country whose help catalog the panel shows (Settings, "Your country"; P4-15): the ISO2 code of a country that has a profile. "US" by default.</summary>
    public string UserCountry { get; init; } = "US";

    /// <summary>The user's state (Settings, "Your state (optional)"; P4-15), by two-letter code, for listing their two senators; null: not chosen. Not personal data: a state, not an address.</summary>
    public string? UserState { get; init; }

    /// <summary>The shading to show: <see cref="Shading"/>, or for a Plan 2 file food when <see cref="ShowFood"/>, else none.</summary>
    [JsonIgnore]
    public ShadingLayer EffectiveShading => Shading ?? (ShowFood ? ShadingLayer.Food : ShadingLayer.None);

    /// <summary>
    /// The marker layers to show, in enum order: <see cref="Markers"/> as saved, or, in a file with no list (a fresh start, or
    /// a Plan 2 file), the Plan 2 layers its three flags say plus the layers on by default after them: the lines to the nearest
    /// hubs (Plan 4, R3). A file that lists its markers keeps its own list, so a Plan 3 file reads with the lines off until the
    /// user turns them on.
    /// </summary>
    [JsonIgnore]
    public IReadOnlyList<MarkerLayer> EffectiveMarkers => Markers is { } m
        ? [.. m.Distinct().Order()]
        : [.. new[] { (MarkerLayer.Conflict, ShowConflict), (MarkerLayer.Outbreaks, ShowOutbreaks), (MarkerLayer.Displacement, ShowDisplacement) }
            .Where(x => x.Item2).Select(x => x.Item1), MarkerLayer.HubLines];
}

/// <summary>Reads and writes <see cref="AppSettings"/>; defaults when the file is missing or unreadable (logged).</summary>
public sealed class SettingsStore(AppDataRoot root, IAppLog log) : ISettingsStore<AppSettings>
{
    // Layers are written by name, so a later reordering of the enums cannot change a saved choice.
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true, Converters = { new JsonStringEnumConverter() } };

    public AppSettings Load()
    {
        if (!File.Exists(root.SettingsFile)) return new AppSettings();
        try
        {
            return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(root.SettingsFile), Options) ?? new AppSettings();
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            log.Warn("Settings file unreadable; using defaults", ex);
            return new AppSettings();
        }
    }

    /// <summary>Never throws for I/O failures (logged).</summary>
    public void Save(AppSettings s)
    {
        if (!AtomicFile.TryWriteAllText(root.SettingsFile, JsonSerializer.Serialize(s, Options), out var error))
            log.Warn("Could not save settings", error);
    }
}
