using System.Text.Json;
using AstroCore.Earth;
using GlobeCore.Earth;
using GlobeCore.Logging;
using GlobeCore.Regions;

namespace EarthRelief3D.Core.Feeds;

/// <summary>
/// The app's boundary data files (<c>data/</c>, beside the program): the rules (<c>boundary-rules.json</c>), the probes
/// (<c>boundary-probes.json</c>), the UCDP country strings (<c>ucdp-country-names.json</c>), and the validator GlobeCore
/// runs on every downloaded set. The aliases (<c>country-aliases.json</c>) stay with <see cref="CountryResolver"/>: they
/// are this app's name matching, not boundary policy.
/// </summary>
public static class BoundaryData
{
    public const string RulesFile = "boundary-rules.json";
    public const string ProbesFile = "boundary-probes.json";
    public const string UcdpNamesFile = "ucdp-country-names.json";

    public static string DataDir { get; } = Path.Combine(AppContext.BaseDirectory, "data");

    private static readonly Lazy<BoundaryRules> DefaultRules = new(() => ReadRules(Path.Combine(DataDir, RulesFile)));

    /// <summary>The rules shipped beside the program, read once.</summary>
    public static BoundaryRules Rules => DefaultRules.Value;

    /// <summary>Reads a rules file (every rule needs a reason).</summary>
    public static BoundaryRules ReadRules(string path)
    {
        using var s = File.OpenRead(path);
        return BoundaryRules.Parse(s);
    }

    public static IReadOnlyList<(GeoPosition Point, string ExpectedId)> ReadProbes(string path)
    {
        using var s = File.OpenRead(path);
        return BoundaryValidator.ReadProbes(s);
    }

    /// <summary>Reads <c>ucdp-country-names.json</c> (<c>{"names": [...]}</c>).</summary>
    public static IReadOnlyList<string> ReadUcdpNames(string path)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(path), new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
        if (!doc.RootElement.TryGetProperty("names", out var list) || list.ValueKind != JsonValueKind.Array)
            throw new InvalidDataException($"{path}: expected {{\"names\": [...]}}");
        var names = list.EnumerateArray().Select(e => e.GetString()).ToList();
        if (names.Count == 0 || names.Any(string.IsNullOrWhiteSpace)) throw new InvalidDataException($"{path}: needs a non-empty list of names");
        return names!;
    }

    /// <summary>
    /// The names that no longer resolve in <paramref name="regions"/> with the aliases applied: an alias whose country is
    /// gone, an alias that now resolves to another country, and a source country string that resolves to nothing.
    /// </summary>
    public static IReadOnlyList<string> UnresolvedNames(RegionIndex regions, IReadOnlyDictionary<string, string> aliases,
        IEnumerable<string> sourceNames)
    {
        var index = CountryIndex.From(regions);
        var lost = new List<string>();
        var usable = new Dictionary<string, string>();
        foreach (var (name, iso3) in aliases)
        {
            if (index.ByIso3(iso3) is null) lost.Add($"{name} (country {iso3} is gone)");
            else usable[name] = iso3;
        }
        index = index.WithAliases(usable);
        foreach (var (name, iso3) in usable)
        {
            var c = index.Resolve(name);
            if (c is null) lost.Add(name);
            else if (!string.Equals(c.Iso3, iso3, StringComparison.OrdinalIgnoreCase)) lost.Add($"{name} (now {c.Iso3}, not {iso3})");
        }
        foreach (var name in sourceNames)
            if (index.Resolve(name) is null) lost.Add(name);
        return lost;
    }

    /// <summary>The validator for downloaded sets: the probes, and the name hook over the aliases and the UCDP names.</summary>
    public static BoundaryValidator CreateValidator(BoundaryRules rules, string dataDir, IReadOnlyDictionary<string, string>? aliases = null)
    {
        aliases ??= CountryResolver.ReadAliases(Path.Combine(dataDir, CountryResolver.AliasesFile));
        var probes = ReadProbes(Path.Combine(dataDir, ProbesFile));
        var names = ReadUcdpNames(Path.Combine(dataDir, UcdpNamesFile));
        int bundled = NaturalEarthBoundaries.LoadBundled(rules).Regions.All.Count;
        return new BoundaryValidator(probes, idx => UnresolvedNames(idx, aliases, names), bundled);
    }

    /// <summary>
    /// The live boundaries: the newest stored set that is whole and passes validation, else the bundled set, both with
    /// the rules applied. <paramref name="cacheDir"/> is the app's cache folder; the sets live under <c>boundaries</c>.
    /// </summary>
    public static (CurrentBoundaries Current, BoundaryStore Store, BoundaryValidator Validator, BoundaryRules Rules) Open(
        string dataDir, string cacheDir, IAppLog log)
    {
        var rules = ReadRules(Path.Combine(dataDir, RulesFile));
        var validator = CreateValidator(rules, dataDir);
        var store = new BoundaryStore(Path.Combine(cacheDir, "boundaries"));
        // GlobeCore deletes a remembered rejection (rejected.json) in this folder after every successful check; it logs a warning
        // when the folder is missing, so it exists from the start.
        try { Directory.CreateDirectory(store.CacheDir); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { log.Warn("Could not create the boundaries cache folder", ex); }
        var current = new CurrentBoundaries(NaturalEarthBoundaries.Load(rules, store, validator, log), log);
        log.Info($"Boundaries in use: {current.Set.Describe()}");
        return (current, store, validator, rules);
    }
}
