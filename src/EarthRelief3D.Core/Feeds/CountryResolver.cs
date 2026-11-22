using System.Text.Json;
using GlobeCore.Earth;

namespace EarthRelief3D.Core.Feeds;

/// <summary>
/// Country names as the sources spell them (UCDP's "DR Congo (Zaire)", ...) to Natural Earth countries. Wraps
/// <see cref="CountryIndex.WithAliases"/> with the alias list in <c>data/country-aliases.json</c> (alias to ISO3).
/// </summary>
public sealed class CountryResolver
{
    private static readonly Lazy<CountryResolver> DefaultResolver = new(() =>
        Load(Path.Combine(AppContext.BaseDirectory, "data", "country-aliases.json")));

    public CountryIndex Index { get; }

    public CountryResolver(CountryIndex baseIndex, IReadOnlyDictionary<string, string> aliasToIso3)
    {
        foreach (var (alias, iso3) in aliasToIso3)
            if (baseIndex.ByIso3(iso3) is null) throw new InvalidDataException($"country alias \"{alias}\" points at unknown ISO3 \"{iso3}\"");
        Index = baseIndex.WithAliases(aliasToIso3);
    }

    public static CountryResolver Load(string aliasesPath)
    {
        // A missing file is a packaging bug; an empty list would silently drop conflicts, so fail loudly.
        var aliases = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(aliasesPath))
            ?? throw new InvalidDataException($"{aliasesPath} is empty");
        return new CountryResolver(CountryIndex.LoadNaturalEarth(), aliases);
    }

    /// <summary>The list shipped beside the program (<c>data/country-aliases.json</c>), loaded once.</summary>
    public static CountryResolver LoadDefault() => DefaultResolver.Value;

    public Country? Resolve(string name) => Index.Resolve(name);

    public Country? ByIso3(string iso3) => Index.ByIso3(iso3);
}
