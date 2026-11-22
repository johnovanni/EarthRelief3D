using System.IO;
using EarthRelief3D.Core.Model;
using EarthRelief3D.Core.Store;
using GlobeCore.Logging;
using GlobeCore.Storage;

namespace EarthRelief3D.Tests;

public sealed class CrisisCacheTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "er3d-cache-" + Guid.NewGuid().ToString("N"));
    private CrisisCache Cache() => new(new AppDataRoot(_dir), new NullLog());
    public void Dispose() { try { Directory.Delete(_dir, true); } catch (IOException) { } }

    [Fact]
    public void CacheRoundTripsEveryCrisisKind()
    {
        var snap = TestData.Snap();
        Assert.True(Cache().Save(snap));
        var loaded = Cache().Load();
        Assert.NotNull(loaded);
        Assert.Equal(snap, loaded);
        Assert.Equal([typeof(ConflictCrisis), typeof(FoodCrisis), typeof(OutbreakCrisis)],
            loaded!.Crises.Select(c => c.GetType()).ToArray());
        Assert.Equal(DateTimeKind.Utc, loaded.Crises[0].Began!.Value.Kind);
    }

    [Fact]
    public void LockedCacheIsKeptNotDeleted()
    {
        // An antivirus scan (or another process) can hold the file open without sharing: that is not a bad cache.
        Assert.True(Cache().Save(TestData.Snap()));
        var file = Cache().FilePath;
        using (new FileStream(file, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            Assert.Null(Cache().Load());
        Assert.True(File.Exists(file));
        Assert.Equal(TestData.Snap(), Cache().Load());   // readable again once the lock is gone
    }

    [Fact]
    public void FoodPhaseCountsAndProjectionOnlyRoundTrip()
    {
        var food = TestData.Food() with { Phase3People = 900_000, Phase4People = 280_000, Phase5People = 20_000 };
        var projection = TestData.Food() with
        {
            Id = "ipc-GTM", CountryIso3 = "GTM", HighestPhase = 0, Band = 0, ProjectionOnly = true, ProjectionName = "second projection",
        };
        var snap = new CrisisSnapshot([food, projection], [], []);
        Assert.True(Cache().Save(snap));
        var loaded = Cache().Load()!;
        Assert.Equal(snap, loaded);
        var g = loaded.Crises.OfType<FoodCrisis>().Single(f => f.CountryIso3 == "GTM");
        Assert.True(g.ProjectionOnly);
        Assert.Equal("second projection", g.ProjectionName);
        Assert.Equal(20_000, loaded.Crises.OfType<FoodCrisis>().Single(f => f.CountryIso3 == "SOM").Phase5People);
    }

    [Fact]
    public void RestartOfflineShowsCachedCrises()
    {
        var first = new CrisisStore();
        first.Load(TestData.Snap());
        Assert.True(Cache().Save(first.Snapshot));
        var second = new CrisisStore();   // new process, no network
        second.Load(Cache().Load()!);
        Assert.Equal(first.Snapshot, second.Snapshot);
        Assert.Equal(3, second.Snapshot.Crises.Count);
    }

    [Fact]
    public void WrongSchemaIsIgnored()
    {
        var file = Cache().FilePath;
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllText(file, """{"SchemaVersion":99,"Crises":[],"Displacement":[],"Unplaced":[]}""");
        Assert.Null(Cache().Load());
        Assert.False(File.Exists(file));
    }

    [Fact]
    public void MissingCorruptAndNullFieldFilesAreTreatedAsMissing()
    {
        Assert.Null(Cache().Load());
        var file = Cache().FilePath;
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllText(file, "{not json");
        Assert.Null(Cache().Load());
        File.WriteAllText(file, """{"SchemaVersion":3,"Crises":null,"Displacement":[],"Unplaced":[]}""");
        Assert.Null(Cache().Load());
        File.WriteAllText(file, """{"SchemaVersion":3,"Crises":[null],"Displacement":[],"Unplaced":[]}""");
        Assert.Null(Cache().Load());
    }

    [Theory]
    [InlineData("""{"SchemaVersion":3,"Crises":[{"Id":"x"}],"Displacement":[],"Unplaced":[]}""")]
    [InlineData("""{"SchemaVersion":3,"Crises":[{"$type":"zzz","Id":"x"}],"Displacement":[],"Unplaced":[]}""")]
    public void BadDiscriminatorLoadsAsEmptyAndDeletesFile(string json)
    {
        var file = Cache().FilePath;
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllText(file, json);
        Assert.Null(Cache().Load());
        Assert.False(File.Exists(file));
    }
}
