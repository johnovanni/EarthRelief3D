using System.Text.Json;
using EarthRelief3D.Core.Feeds;
using GlobeCore.Earth;

namespace EarthRelief3D.DataTool;

/// <summary>
/// Source codes to the app's ISO3 codes, always through <see cref="CountryResolver"/> (the boundary set's units: C16,
/// Somaliland in Somalia). M49 numeric codes (FAOSTAT) map through Natural Earth's <c>UN_A3</c> field (ruling P3-11),
/// read from GlobeCore's bundled countries file (<see cref="EarthAssets"/>, read only). Where Natural Earth 5.1.2 has no
/// <c>UN_A3</c> (Norway is -99, Taiwan -099), its <c>ISO_N3_EH</c> is used: ISO 3166 numeric, the same number as M49.
/// </summary>
public sealed class CountryCodes
{
    private readonly CountryResolver _resolver;
    private readonly Dictionary<string, string> _byM49 = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _byIso2 = new(StringComparer.Ordinal);

    public CountryCodes(CountryResolver resolver)
    {
        _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        using var s = EarthAssets.Open(EarthAssets.CountriesFile);
        using var doc = JsonDocument.Parse(s);
        var iso2Fallback = new List<(string Code, string Iso3)>();
        foreach (var f in doc.RootElement.GetProperty("features").EnumerateArray())
        {
            var p = f.GetProperty("properties");
            // The region id as GlobeCore's NaturalEarthBoundaries makes it: ISO_A3_EH, or ADM0_A3 where that is -99.
            var iso = Str(p, "ISO_A3_EH");
            var id = iso.Length == 0 || iso == "-99" ? Str(p, "ADM0_A3") : iso;
            var m49 = Numeric(Str(p, "UN_A3")) ?? Numeric(Str(p, "ISO_N3_EH"));
            if (_resolver.ByIso3(id)?.Iso3 is not { } placed) continue;
            // ISO2 (OurAirports): ISO_A2 where it has a code, else ISO_A2_EH (Norway, France, Kosovo). Two Australian
            // territories share "AU" under _EH: the unit with its own ISO_A2 wins.
            var a2 = Str(p, "ISO_A2");
            if (IsIso2(a2)) _byIso2.TryAdd(a2, placed);
            else if (IsIso2(Str(p, "ISO_A2_EH"))) iso2Fallback.Add((Str(p, "ISO_A2_EH"), placed));
            if (m49 is null) continue;
            _byM49.TryAdd(m49, placed);   // no two units share a code in 5.1.2 (checked 2026-11-23)
        }
        foreach (var (code, iso3) in iso2Fallback) _byIso2.TryAdd(code, iso3);
    }

    /// <summary>The bundled boundaries with the rules and aliases of <c>data\</c> beside the program.</summary>
    public static CountryCodes Default { get; } = new(CountryResolver.LoadDefault());

    /// <summary>"'004", "004" or "4" to the country's ISO3, or null when no unit has that M49 code.</summary>
    public string? FromM49(string code)
    {
        var digits = code.Trim().TrimStart('\'').Trim();
        if (digits.Length == 0 || !digits.All(char.IsAsciiDigit) || digits.Length > 3) return null;
        return _byM49.GetValueOrDefault(digits.PadLeft(3, '0'));
    }

    /// <summary>The boundary set's unit for an ISO3 code, or null.</summary>
    public string? FromIso3(string code) => _resolver.ByIso3(code.Trim())?.Iso3;

    /// <summary>The boundary set's unit for a two-letter ISO 3166 code ("SD" to "SDN"; case does not matter), or null.</summary>
    public string? FromIso2(string code) => _byIso2.GetValueOrDefault(code.Trim().ToUpperInvariant());

    /// <summary>The unit for a country name in any spelling the resolver knows, or null.</summary>
    public string? FromName(string name) => _resolver.Resolve(name)?.Iso3;

    private static bool IsIso2(string v) => v.Length == 2 && v.All(char.IsAsciiLetterUpper);

    private static string? Numeric(string v) => v.Length == 3 && v.All(char.IsAsciiDigit) ? v : null;

    private static string Str(JsonElement p, string key) =>
        p.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() ?? "" : "";
}
