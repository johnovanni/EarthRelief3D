using System.Text.Json;
using AstroCore.Earth;
using GlobeCore.Earth;

namespace EarthRelief3D.Core.Feeds;

/// <summary>
/// Country names as the sources spell them (UCDP's "DR Congo (Zaire)", ...) to Natural Earth countries. Wraps
/// <see cref="CountryIndex.WithUnits"/> and <see cref="CountryIndex.WithAliases"/> with <c>data/country-units.json</c>
/// (units merged into another unit) and <c>data/country-aliases.json</c> (alias to ISO3).
/// </summary>
public sealed class CountryResolver
{
    private static readonly Lazy<CountryResolver> DefaultResolver = new(() =>
        Load(Path.Combine(AppContext.BaseDirectory, "data", "country-aliases.json"),
            Path.Combine(AppContext.BaseDirectory, "data", "country-units.json")));

    public CountryIndex Index { get; }

    /// <param name="unitIntoIso3">Units merged into another unit (C16), e.g. SOL into SOM, applied before the aliases.</param>
    public CountryResolver(CountryIndex baseIndex, IReadOnlyDictionary<string, string> aliasToIso3,
        IReadOnlyDictionary<string, string>? unitIntoIso3 = null)
    {
        if (unitIntoIso3 is { Count: > 0 })
        {
            foreach (var (unit, into) in unitIntoIso3)
                if (baseIndex.ByIso3(unit) is null || baseIndex.ByIso3(into) is null)
                    throw new InvalidDataException($"country unit \"{unit}\" into \"{into}\": unknown ISO3");
            baseIndex = baseIndex.WithUnits(unitIntoIso3);
        }
        foreach (var (alias, iso3) in aliasToIso3)
            if (baseIndex.ByIso3(iso3) is null) throw new InvalidDataException($"country alias \"{alias}\" points at unknown ISO3 \"{iso3}\"");
        Index = baseIndex.WithAliases(aliasToIso3);
    }

    /// <summary>
    /// Loads the alias list (<c>{"name": "ISO3"}</c>) and, when given, the units list
    /// (<c>{"SOL": {"into": "SOM", "reason": "..."}}</c>; every entry must say why it is merged).
    /// </summary>
    public static CountryResolver Load(string aliasesPath, string? unitsPath = null)
    {
        // A missing file is a packaging bug; an empty list would silently drop conflicts, so fail loudly.
        var aliases = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(aliasesPath))
            ?? throw new InvalidDataException($"{aliasesPath} is empty");
        return new CountryResolver(CountryIndex.LoadNaturalEarth(), aliases, unitsPath is null ? null : ReadUnits(unitsPath));
    }

    /// <summary>Reads <c>country-units.json</c> into unit to target ISO3, rejecting entries without a target or a reason.</summary>
    public static Dictionary<string, string> ReadUnits(string unitsPath)
    {
        var raw = JsonSerializer.Deserialize<Dictionary<string, UnitEntry>>(File.ReadAllText(unitsPath), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidDataException($"{unitsPath} is empty");
        foreach (var (unit, e) in raw)
            if (string.IsNullOrWhiteSpace(e.Into) || string.IsNullOrWhiteSpace(e.Reason))
                throw new InvalidDataException($"country unit \"{unit}\" needs \"into\" and \"reason\"");
        return raw.ToDictionary(kv => kv.Key, kv => kv.Value.Into!);
    }

    private sealed record UnitEntry(string? Into, string? Reason);

    /// <summary>
    /// Reads <c>data/ucdp-territories.json</c> (<c>{"PSE": {"reason": "..."}}</c>): units that UCDP's country list does not
    /// have, where an event's coordinates override UCDP's country name (ruling R14b). Every entry must say why.
    /// </summary>
    public static IReadOnlySet<string> ReadTerritories(string path)
    {
        var raw = JsonSerializer.Deserialize<Dictionary<string, TerritoryEntry>>(File.ReadAllText(path), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidDataException($"{path} is empty");
        foreach (var (iso3, e) in raw)
            if (e is null || string.IsNullOrWhiteSpace(e.Reason)) throw new InvalidDataException($"UCDP territory \"{iso3}\" needs a \"reason\"");
        return raw.Keys.Select(k => k.Trim().ToUpperInvariant()).ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private sealed record TerritoryEntry(string? Reason);

    private static readonly Lazy<IReadOnlySet<string>> DefaultTerritories = new(() =>
        ReadTerritories(Path.Combine(AppContext.BaseDirectory, "data", "ucdp-territories.json")));

    /// <summary>The UCDP territories list shipped beside the program (<c>data/ucdp-territories.json</c>), loaded once.</summary>
    public static IReadOnlySet<string> LoadDefaultUcdpTerritories() => DefaultTerritories.Value;

    /// <summary>The lists shipped beside the program (<c>data/</c>), loaded once.</summary>
    public static CountryResolver LoadDefault() => DefaultResolver.Value;

    public Country? Resolve(string name) => Index.Resolve(name);

    /// <summary>The country (units applied) that contains the point, or null over the sea.</summary>
    public Country? Find(GeoPosition p) => Index.Find(p);

    public Country? ByIso3(string iso3) => Index.ByIso3(iso3);
}
