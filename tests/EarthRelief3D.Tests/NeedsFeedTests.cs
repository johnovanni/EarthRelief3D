using System.IO;
using System.Text.Json;
using EarthRelief3D.Core.Feeds;
using EarthRelief3D.Core.Store;

namespace EarthRelief3D.Tests;

/// <summary>The two needs samples against the boundary set (C16): every code is placed or listed as not placed.</summary>
public class NeedsFeedTests
{
    private static readonly DateTime Now = new(2026, 11, 23, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task CodesResolveToBoundaryUnits()
    {
        var ind = new IndicatorStore();
        var crises = new CrisisStore();
        var countries = CountryResolver.LoadDefault();
        var clock = new FixedClock(Now);
        await new GhoFeed(Samples.GhoHttp().Http, ind, crises, clock, new ListLog(), countries).RunOnceAsync(default);
        await new HnoFeed(Samples.HnoHttp().Http, ind, crises, clock, new ListLog(), countries).RunOnceAsync(default);

        // Every ISO3 the GHO sample holds (country rows) and every one in the HNO CSV.
        var ghoCodes = new HashSet<string>();
        foreach (var file in new[] { "gho_wsh_water_basic.json", "gho_wsh_sanitation_basic.json", "gho_uhc_index.json" })
            foreach (var row in JsonDocument.Parse(Samples.Text(file)).RootElement.GetProperty("value").EnumerateArray())
                if (row.GetProperty("SpatialDimType").GetString() == "COUNTRY") ghoCodes.Add(row.GetProperty("SpatialDim").GetString()!);
        var hnoCodes = CsvReader.Read(File.ReadAllBytes(Samples.Path("hpc_hno_2026.csv"))).Skip(1).Select(r => r[0]).ToHashSet();
        Assert.Equal(15, ghoCodes.Count);
        Assert.Equal(20, hnoCodes.Count);

        var placedGho = ind.Snapshot.Get("gho.water")!.Values.Select(v => v.Iso3).ToHashSet();
        var placedHno = ind.Snapshot.Needs.Select(n => n.Iso3).ToHashSet();
        var unplaced = crises.Snapshot.Unplaced.Select(u => u.Title).ToList();

        foreach (var iso in ghoCodes)
            Assert.True(countries.ByIso3(iso) is not null ? placedGho.Contains(iso) : unplaced.Any(t => t.Contains(iso)),
                $"GHO {iso} is neither placed nor listed as not placed");
        foreach (var iso in hnoCodes)
            Assert.True(countries.ByIso3(iso) is not null ? placedHno.Contains(iso) : unplaced.Any(t => t.Contains(iso)),
                $"HNO {iso} is neither placed nor listed as not placed");

        // Both samples are made of real countries, so nothing is left out.
        Assert.Empty(unplaced);
        // C16: Palestine and Somalia are the units the boundary set has.
        Assert.Contains("PSE", placedGho);
        Assert.Contains("SOM", placedHno);
    }

    [Fact]
    public async Task NeedsRowsSurviveTheCacheAndAnOlderFileWithoutThemStillLoads()
    {
        var ind = new IndicatorStore();
        await new HnoFeed(Samples.HnoHttp().Http, ind, new CrisisStore(), new FixedClock(Now), new ListLog()).RunOnceAsync(default);
        var root = new GlobeCore.Storage.AppDataRoot(Path.Combine(Path.GetTempPath(), "er3d-needs-" + Guid.NewGuid().ToString("N")));
        try
        {
            Directory.CreateDirectory(root.CacheDir);
            var cache = new IndicatorCache(root, new ListLog());
            Assert.True(cache.Save(ind.Snapshot));
            var loaded = cache.Load()!;
            Assert.Equal(ind.Snapshot, loaded);
            Assert.Equal("GHO Estimates", loaded.NeedsOf("YEM").Single().Basis);

            // A file written before Task 3 has no Needs: valid, empty list.
            File.WriteAllText(cache.FilePath, "{\"SchemaVersion\":1,\"Series\":[]}");
            Assert.Empty(cache.Load()!.Needs);
        }
        finally { try { Directory.Delete(root.CacheDir, true); } catch (IOException) { } }
    }
}
