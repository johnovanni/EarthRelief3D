using System.Text.Json;
using System.Text.Json.Serialization;
using EarthRelief3D.Core.Model;
using GlobeCore.Logging;
using GlobeCore.Storage;

namespace EarthRelief3D.Core.Store;

/// <summary>On-disk copy of the last snapshot (<c>cache/crises.json</c>), so a restart offline still shows the last known crises.</summary>
public sealed class CrisisCache(AppDataRoot root, IAppLog log)
{
    /// <summary>Raised whenever the saved shape or meaning of the records changes; an older file is discarded.
    /// 2: the snapshot's per-source check times (ruling R12).</summary>
    public const int SchemaVersion = 2;

    private sealed record Dto(int SchemaVersion, List<Crisis> Crises, List<DisplacementFigure> Displacement, List<UnplacedReport> Unplaced,
        Dictionary<string, DateTime>? CheckedUtc);

    private static readonly JsonSerializerOptions Options = new()
    {
        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
        Converters = { new JsonStringEnumConverter() },
    };

    public string FilePath => Path.Combine(root.CacheDir, "crises.json");

    /// <summary>Null when missing. A wrong schema version, corrupt JSON or records with nulls the model forbids: file deleted, logged, treated as missing.</summary>
    public CrisisSnapshot? Load()
    {
        var file = FilePath;
        if (!File.Exists(file)) return null;
        try
        {
            var dto = JsonSerializer.Deserialize<Dto>(File.ReadAllText(file), Options) ?? throw new JsonException("empty cache");
            if (dto.SchemaVersion != SchemaVersion)
            {
                log.Warn($"Crisis cache has schema version {dto.SchemaVersion}, not {SchemaVersion}; deleting it");
                Delete(file);
                return null;
            }
            if (Problem(dto) is { } problem) throw new JsonException("invalid record: " + problem);
            return new CrisisSnapshot(dto.Crises, dto.Displacement, dto.Unplaced)
            {
                CheckedUtc = new Dictionary<string, DateTime>(dto.CheckedUtc ?? [], StringComparer.Ordinal),
            };
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException or ArgumentException or FormatException or InvalidOperationException or NotSupportedException)
        {
            log.Warn("Crisis cache unreadable; deleting it", ex);
            Delete(file);
            return null;
        }
    }

    /// <summary>False (with a logged warning) if the file could not be written; never throws for I/O failures.</summary>
    public bool Save(CrisisSnapshot s)
    {
        var dto = new Dto(SchemaVersion, [.. s.Crises], [.. s.Displacement], [.. s.Unplaced],
            new Dictionary<string, DateTime>(s.CheckedUtc, StringComparer.Ordinal));
        if (AtomicFile.TryWriteAllText(FilePath, JsonSerializer.Serialize(dto, Options), out var error)) return true;
        log.Warn("Could not save crisis cache", error);
        return false;
    }

    private static void Delete(string file)
    {
        try { File.Delete(file); } catch (Exception) { }
    }

    // JSON can hold nulls the types forbid.
    private static string? Problem(Dto d)
    {
        if (d.Crises is null || d.Displacement is null || d.Unplaced is null) return "missing lists";
        foreach (var c in d.Crises)
        {
            if (c is null) return "null crisis";
            if (c.Id is null || c.CountryIso3 is null || c.Title is null || c.Severity is null || c.Severity.ScaleName is null
                || c.Severity.Text is null || c.Citations is null || c.Citations.Any(CitationProblem)) return $"crisis {c.Id}: base fields";
            if (c is ConflictCrisis k && (k.SideA is null || k.SideB is null || k.Events is null || k.Events.Contains(null!) || k.DeathsByMonth is null))
                return $"conflict {c.Id}: fields";
            if (c is OutbreakCrisis o && (o.Disease is null || o.DonId is null || o.ReportUrl is null)) return $"outbreak {c.Id}: fields";
            if (c.Trend is { Window: null }) return $"crisis {c.Id}: trend";
        }
        foreach (var f in d.Displacement)
            if (f is null || f.CountryIso3 is null || CitationProblem(f.Citation)) return "displacement figure";
        foreach (var u in d.Unplaced)
            if (u is null || u.Source is null || u.Title is null || u.Reason is null || CitationProblem(u.Citation)) return "unplaced report";
        return null;
    }

    private static bool CitationProblem(SourceCitation? c) =>
        c is null || c.Publisher is null || c.Dataset is null || c.Url is null || c.Licence is null || c.Attribution is null;
}
