using System.IO;
using System.Text.Json;
using GlobeCore.Logging;
using GlobeCore.Storage;

namespace EarthRelief3D.App.Settings;

/// <summary>What the app remembers between runs (settings.json in the data folder). Every crisis layer is on by default.</summary>
public sealed record AppSettings
{
    public bool ShowConflict { get; init; } = true;
    public bool ShowFood { get; init; } = true;
    public bool ShowOutbreaks { get; init; } = true;
    public bool ShowDisplacement { get; init; } = true;
    /// <summary>The layer strip shows text labels beside its icons.</summary>
    public bool LayerStripExpanded { get; init; } = true;
}

/// <summary>Reads and writes <see cref="AppSettings"/>; defaults when the file is missing or unreadable (logged).</summary>
public sealed class SettingsStore(AppDataRoot root, IAppLog log) : ISettingsStore<AppSettings>
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

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
