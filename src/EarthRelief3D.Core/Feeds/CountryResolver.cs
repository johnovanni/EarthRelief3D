using System.Text.Json;
using AstroCore.Earth;
using GlobeCore.Earth;
using GlobeCore.Regions;

namespace EarthRelief3D.Core.Feeds;

/// <summary>
/// Country names as the sources spell them (UCDP's "DR Congo (Zaire)", ...) to Natural Earth countries. Its index is the
/// live boundary set (<see cref="CurrentBoundaries"/>, with the rules of <c>data/boundary-rules.json</c> applied: Crimea
/// in Ukraine, Somaliland in Somalia) plus <c>data/country-aliases.json</c> (alias to ISO3). When the boundaries are
/// swapped it builds the new index and raises <see cref="Changed"/>; <see cref="Index"/> always gives the newest.
/// </summary>
public sealed class CountryResolver
{
    public const string AliasesFile = "country-aliases.json";

    private static readonly Lazy<CountryResolver> DefaultResolver = new(() =>
        new CountryResolver(new CurrentBoundaries(NaturalEarthBoundaries.LoadBundled(BoundaryData.Rules)),
            ReadAliases(Path.Combine(BoundaryData.DataDir, AliasesFile))));

    private readonly IReadOnlyDictionary<string, string> _aliases;
    private CountryIndex _index;

    /// <param name="boundaries">The live set: the index follows its swaps.</param>
    /// <param name="aliasToIso3">Names (any spelling) to ISO3, applied on every set.</param>
    public CountryResolver(CurrentBoundaries boundaries, IReadOnlyDictionary<string, string> aliasToIso3)
    {
        Boundaries = boundaries ?? throw new ArgumentNullException(nameof(boundaries));
        _aliases = aliasToIso3 ?? throw new ArgumentNullException(nameof(aliasToIso3));
        _index = Build(boundaries.Set);
        boundaries.Changed += OnBoundariesChanged;
    }

    public CurrentBoundaries Boundaries { get; }

    /// <summary>The current countries (units applied). Take it once per operation.</summary>
    public CountryIndex Index => Volatile.Read(ref _index);

    /// <summary>Raised, on the thread that swapped the boundaries, after <see cref="Index"/> has been rebuilt.</summary>
    public event EventHandler? Changed;

    private CountryIndex Build(BoundarySet set)
    {
        var index = CountryIndex.From(set);
        foreach (var (alias, iso3) in _aliases)
            if (index.ByIso3(iso3) is null) throw new InvalidDataException($"country alias \"{alias}\" points at unknown ISO3 \"{iso3}\"");
        return index.WithAliases(_aliases);
    }

    private void OnBoundariesChanged(object? sender, BoundariesChangedEventArgs e)
    {
        Volatile.Write(ref _index, Build(e.New));
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Reads the alias list (<c>{"name": "ISO3"}</c>); a missing or empty file is a packaging bug, so it fails loudly.</summary>
    public static Dictionary<string, string> ReadAliases(string path) =>
        JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(path))
            ?? throw new InvalidDataException($"{path} is empty");

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

    /// <summary>The bundled boundaries with the rules and aliases shipped beside the program (<c>data/</c>), loaded once.</summary>
    public static CountryResolver LoadDefault() => DefaultResolver.Value;

    public Country? Resolve(string name) => Index.Resolve(name);

    /// <summary>The country (units applied) that contains the point, or null over the sea.</summary>
    public Country? Find(GeoPosition p) => Index.Find(p);

    public Country? ByIso3(string iso3) => Index.ByIso3(iso3);
}
