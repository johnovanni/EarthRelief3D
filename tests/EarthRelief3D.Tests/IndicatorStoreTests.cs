using System.IO;
using System.Text.Json.Nodes;
using EarthRelief3D.Core.Model;
using EarthRelief3D.Core.Store;
using GlobeCore.Storage;

namespace EarthRelief3D.Tests;

/// <summary>The store of country-value series, and its on-disk copy (its own schema version 1).</summary>
public class IndicatorStoreTests
{
    private static IndicatorSeries Water(double sdn = 40) => CountryValueLayerTests.Water(
        new CountryValue("SDN", sdn, 2024, CountryValueLayerTests.Gho), new CountryValue("ISL", 0, 2024, CountryValueLayerTests.Gho));

    [Fact]
    public void ReplaceKeepsEachSeriesByIdAndSkipsNoChange()
    {
        var store = new IndicatorStore();
        int changes = 0;
        store.Changed += (_, _) => changes++;
        Assert.Null(store.Snapshot.Get("gho.water"));

        Assert.True(store.Replace("gho.water", Water()));
        Assert.Equal(1, changes);
        Assert.Equal(40, store.Snapshot.Get("gho.water")!.For("SDN")!.Value);
        Assert.False(store.Replace("gho.water", Water()));   // equal by value: no event
        Assert.Equal(1, changes);
        Assert.True(store.Replace("gho.water", Water(41)));
        Assert.Equal(2, changes);

        var other = Water() with { Id = "gho.sanitation", Measures = "Share of people without at least basic sanitation" };
        Assert.True(store.Replace("gho.sanitation", other));
        Assert.Equal(41, store.Snapshot.Get("gho.water")!.For("SDN")!.Value);   // other series untouched
        Assert.Equal(2, store.Snapshot.Series.Count);
        Assert.Throws<ArgumentException>(() => store.Replace("gho.uhc", other));   // the id must match the series
    }

    [Fact]
    public void SeriesCompareByValue()
    {
        Assert.Equal(Water(), Water());
        Assert.NotEqual(Water(), Water(41));
        Assert.Equal(new IndicatorSnapshot(new Dictionary<string, IndicatorSeries> { ["gho.water"] = Water() }),
            new IndicatorSnapshot(new Dictionary<string, IndicatorSeries> { ["gho.water"] = Water() }));
    }

    [Fact]
    public void CacheRoundTripsAndDropsAnotherSchemaVersion()
    {
        var root = new AppDataRoot(Path.Combine(Path.GetTempPath(), "er3d-ind-" + Guid.NewGuid().ToString("N")));
        try
        {
            Directory.CreateDirectory(root.CacheDir);
            var log = new ListLog();
            var cache = new IndicatorCache(root, log);
            Assert.Null(cache.Load());
            var store = new IndicatorStore();
            store.Replace("gho.water", Water());
            store.Replace("fao.undernourishment", new IndicatorSeries("fao.undernourishment", "Prevalence of undernourishment", "%",
                [new CountryValue("ISL", 2.5, 2023, CountryValueLayerTests.Fao, "<2.5")]));
            Assert.True(cache.Save(store.Snapshot));
            Assert.Equal(1, IndicatorCache.SchemaVersion);
            Assert.EndsWith(Path.Combine("cache", "indicators.json"), cache.FilePath);

            var loaded = cache.Load();
            Assert.Equal(store.Snapshot, loaded);
            Assert.Equal("<2.5", loaded!.Get("fao.undernourishment")!.Values[0].Qualifier);

            File.WriteAllText(cache.FilePath, File.ReadAllText(cache.FilePath).Replace("\"SchemaVersion\":1", "\"SchemaVersion\":0"));
            Assert.Null(cache.Load());
            Assert.False(File.Exists(cache.FilePath));
            Assert.Contains(log.Lines, l => l.Contains("schema version 0"));

            File.WriteAllText(cache.FilePath, """{"SchemaVersion":1,"Series":[{"Id":"x","Measures":null,"Unit":"%","Values":[]}]}""");
            Assert.Null(cache.Load());   // a null the model forbids: discarded
        }
        finally { Directory.Delete(root.Root, recursive: true); }
    }

    [Fact]
    public void OldCacheWithoutDateMeaningLoadsAsAsOf()
    {
        // indicators.json written before Task 3 has no DateMeaning on its citations: they load as AsOf (the default).
        var root = new AppDataRoot(Path.Combine(Path.GetTempPath(), "er3d-ind-" + Guid.NewGuid().ToString("N")));
        try
        {
            Directory.CreateDirectory(root.CacheDir);
            var cache = new IndicatorCache(root, new ListLog());
            var store = new IndicatorStore();
            store.Replace("gho.water", Water());
            Assert.True(cache.Save(store.Snapshot));
            var json = JsonNode.Parse(File.ReadAllText(cache.FilePath))!;
            int removed = 0;
            foreach (var v in json["Series"]![0]!["Values"]!.AsArray())
                if (v!["Citation"]!.AsObject().Remove("DateMeaning")) removed++;
            Assert.Equal(2, removed);   // the field is written today, so this file really is an old one now
            File.WriteAllText(cache.FilePath, json.ToJsonString());
            Assert.DoesNotContain("DateMeaning", File.ReadAllText(cache.FilePath));

            var loaded = cache.Load();
            Assert.NotNull(loaded);
            Assert.All(loaded.Get("gho.water")!.Values, v => Assert.Equal(CitationDate.AsOf, v.Citation.DateMeaning));
            Assert.Equal(store.Snapshot, loaded);
        }
        finally { Directory.Delete(root.Root, recursive: true); }
    }
}
