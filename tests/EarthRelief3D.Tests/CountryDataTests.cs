using System.IO;
using System.Text.Json;
using EarthRelief3D.Core.Feeds;
using GlobeCore.Earth;

namespace EarthRelief3D.Tests;

public class CountryDataTests
{
    private static string Data(string name) => Path.Combine(AppContext.BaseDirectory, "data", name);

    [Fact]
    public void EveryAliasAndUnitIsKnown()
    {
        var index = CountryIndex.LoadNaturalEarth();
        var aliases = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(Data("country-aliases.json")))!;
        Assert.NotEmpty(aliases);
        foreach (var (alias, iso3) in aliases) Assert.True(index.ByIso3(iso3) is not null, $"alias \"{alias}\" -> unknown {iso3}");

        var units = CountryResolver.ReadUnits(Data("country-units.json"));
        Assert.Equal("SOM", units["SOL"]);
        foreach (var (unit, into) in units)
        {
            Assert.True(index.ByIso3(unit) is not null, $"unit {unit} unknown");
            Assert.True(index.ByIso3(into) is not null, $"unit target {into} unknown");
        }
    }

    [Fact]
    public void UnitsAreAppliedAndNamesStillResolve()
    {
        var r = CountryResolver.Load(Data("country-aliases.json"), Data("country-units.json"));
        Assert.Null(r.ByIso3("SOL"));
        Assert.Equal("SOM", r.Resolve("Somaliland")?.Iso3);
        Assert.Equal("COD", r.Resolve("Democratic Republic of the Congo")?.Iso3);
        Assert.Equal("COG", r.Resolve("Congo")?.Iso3);
        Assert.Equal("PSE", r.Resolve("occupied Palestinian territory")?.Iso3);
        Assert.Equal("COD", r.Resolve("DR Congo (Zaire)")?.Iso3);
    }

    [Fact]
    public void UnitWithoutAReasonIsRejected()
    {
        var path = Path.Combine(Path.GetTempPath(), $"units-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, "{\"SOL\": {\"into\": \"SOM\"}}");
        try { Assert.Throws<InvalidDataException>(() => CountryResolver.ReadUnits(path)); }
        finally { File.Delete(path); }
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
        var r = CountryResolver.Load(Data("country-aliases.json"), Data("country-units.json"));
        Assert.Equal(iso3, r.Resolve(name)?.Iso3);
    }
}
