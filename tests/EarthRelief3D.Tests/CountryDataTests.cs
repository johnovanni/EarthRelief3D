using System.IO;
using System.Text.Json;
using EarthRelief3D.Core.Feeds;
using GlobeCore.Earth;

namespace EarthRelief3D.Tests;

public class CountryDataTests
{
    private static string Data(string name) => Path.Combine(AppContext.BaseDirectory, "data", name);

    [Fact]
    public void EveryAliasIsKnown()
    {
        var index = CountryResolver.LoadDefault().Index;
        var aliases = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(Data("country-aliases.json")))!;
        Assert.NotEmpty(aliases);
        foreach (var (alias, iso3) in aliases) Assert.True(index.ByIso3(iso3) is not null, $"alias \"{alias}\" -> unknown {iso3}");
    }

    [Fact]
    public void EveryUcdpTerritoryIsKnownAndHasAReason()
    {
        var index = CountryResolver.LoadDefault().Index;
        var territories = CountryResolver.ReadTerritories(Data("ucdp-territories.json"));
        Assert.Contains("PSE", territories);
        foreach (var iso3 in territories) Assert.True(index.ByIso3(iso3) is not null, $"UCDP territory {iso3} unknown");
        var path = Path.Combine(Path.GetTempPath(), $"territories-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, """{"PSE": {"reason": " "}}""");
        try { Assert.Throws<InvalidDataException>(() => CountryResolver.ReadTerritories(path)); }
        finally { File.Delete(path); }
    }

    [Fact]
    public void UnitsAreAppliedAndNamesStillResolve()
    {
        var r = CountryResolver.LoadDefault();
        Assert.Null(r.ByIso3("SOL"));
        Assert.Equal("SOM", r.Resolve("Somaliland")?.Iso3);
        Assert.Equal("COD", r.Resolve("Democratic Republic of the Congo")?.Iso3);
        Assert.Equal("COG", r.Resolve("Congo")?.Iso3);
        Assert.Equal("PSE", r.Resolve("occupied Palestinian territory")?.Iso3);
        Assert.Equal("COD", r.Resolve("DR Congo (Zaire)")?.Iso3);
    }

    [Theory]
    [InlineData("Guinea", "GIN")]
    [InlineData("Guinea-Bissau", "GNB")]
    [InlineData("Equatorial Guinea", "GNQ")]
    [InlineData("Niger", "NER")]
    [InlineData("Nigeria", "NGA")]
    [InlineData("Sudan", "SDN")]
    [InlineData("South Sudan", "SSD")]
    public void SimilarNamesResolveToTheirOwnCountry(string name, string iso3)
    {
        var r = CountryResolver.LoadDefault();
        Assert.Equal(iso3, r.Resolve(name)?.Iso3);
    }
}
