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
        File.WriteAllText(file, """{"SchemaVersion":1,"Crises":null,"Displacement":[],"Unplaced":[]}""");
        Assert.Null(Cache().Load());
        File.WriteAllText(file, """{"SchemaVersion":1,"Crises":[null],"Displacement":[],"Unplaced":[]}""");
        Assert.Null(Cache().Load());
    }
}
