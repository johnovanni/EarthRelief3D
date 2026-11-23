using System.Text.Json;
using System.Text.Json.Serialization;
using EarthRelief3D.Core.Model;
using GlobeCore.Logging;
using GlobeCore.Storage;

namespace EarthRelief3D.Core.Store;

/// <summary>
/// On-disk copy of the last country-value series (<c>cache/indicators.json</c>), so a restart offline still shows them,
/// with their dates. Mirrors <see cref="CrisisCache"/>, with its own schema version.
/// </summary>
public sealed class IndicatorCache(AppDataRoot root, IAppLog log)
{
    /// <summary>Raised whenever the saved shape or meaning of the series changes; an older file is discarded.</summary>
    public const int SchemaVersion = 1;

    private sealed record Dto(int SchemaVersion, List<IndicatorSeries> Series);

    private static readonly JsonSerializerOptions Options = new()
    {
        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
    };

    public string FilePath => Path.Combine(root.CacheDir, "indicators.json");

    /// <summary>
    /// Null when missing. A wrong schema version, corrupt JSON or records with nulls the model forbids: file deleted,
    /// logged, treated as missing. A file that cannot be read just now (locked, or access denied) is logged and kept.
    /// </summary>
    public IndicatorSnapshot? Load()
    {
        var file = FilePath;
        if (!File.Exists(file)) return null;
        string text;
        try { text = File.ReadAllText(file); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            log.Warn("Indicator cache could not be read now; keeping it for the next start", ex);
            return null;
        }
        try
        {
            var dto = JsonSerializer.Deserialize<Dto>(text, Options) ?? throw new JsonException("empty cache");
            if (dto.SchemaVersion != SchemaVersion)
            {
                log.Warn($"Indicator cache has schema version {dto.SchemaVersion}, not {SchemaVersion}; deleting it");
                Delete(file);
                return null;
            }
            if (Problem(dto) is { } problem) throw new JsonException("invalid record: " + problem);
            var series = new Dictionary<string, IndicatorSeries>(StringComparer.Ordinal);
            foreach (var s in dto.Series) series[s.Id] = s;
            return new IndicatorSnapshot(series);
        }
        catch (Exception ex) when (ex is JsonException or ArgumentException or FormatException or InvalidOperationException or NotSupportedException)
        {
            log.Warn("Indicator cache malformed; deleting it", ex);
            Delete(file);
            return null;
        }
    }

    /// <summary>False (with a logged warning) if the file could not be written; never throws for I/O failures.</summary>
    public bool Save(IndicatorSnapshot s)
    {
        var dto = new Dto(SchemaVersion, [.. s.Series.Values.OrderBy(x => x.Id, StringComparer.Ordinal)]);
        if (AtomicFile.TryWriteAllText(FilePath, JsonSerializer.Serialize(dto, Options), out var error)) return true;
        log.Warn("Could not save indicator cache", error);
        return false;
    }

    private static void Delete(string file)
    {
        try { File.Delete(file); } catch (Exception) { }
    }

    // JSON can hold nulls the types forbid.
    private static string? Problem(Dto d)
    {
        if (d.Series is null) return "missing series";
        foreach (var s in d.Series)
        {
            if (s is null || s.Id is null || s.Measures is null || s.Unit is null || s.Values is null) return $"series {s?.Id}: fields";
            foreach (var v in s.Values)
                if (v is null || v.Iso3 is null || CitationProblem(v.Citation)) return $"series {s.Id}: value";
        }
        return null;
    }

    private static bool CitationProblem(SourceCitation? c) =>
        c is null || c.Publisher is null || c.Dataset is null || c.Url is null || c.Licence is null || c.Attribution is null;
}
