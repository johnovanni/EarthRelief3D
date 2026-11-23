using System.IO;
using EarthRelief3D.Core.Feeds;
using EarthRelief3D.Core.Model;
using EarthRelief3D.Core.Store;
using GlobeCore.Feeds;
using GlobeCore.Logging;
using GlobeCore.Storage;

namespace EarthRelief3D.Tests;

/// <summary>Ruling R12: the snapshot records when each source last ran successfully, even when it found nothing.</summary>
public sealed class CheckedUtcTests : IDisposable
{
    private static readonly DateTime T = TestData.T;
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "er3d-checked-" + Guid.NewGuid().ToString("N"));
    private CrisisCache Cache() => new(new AppDataRoot(_dir), new NullLog());
    public void Dispose() { try { Directory.Delete(_dir, true); } catch (IOException) { } }

    [Fact]
    public void StoreRecordsEachSourceCheckAndRaisesChanged()
    {
        var store = new CrisisStore();
        Assert.Empty(store.Snapshot.CheckedUtc);
        int changes = 0;
        store.Changed += (_, _) => changes++;

        Assert.True(store.MarkChecked("WHO", T));
        Assert.Equal(T, store.Snapshot.CheckedUtc["WHO"]);
        Assert.Equal(1, changes);
        Assert.False(store.MarkChecked("WHO", T));   // the same time again: no change, no event
        Assert.Equal(1, changes);
        store.MarkChecked("UCDP", T.AddHours(1));
        store.ReplaceKind(CrisisKind.ArmedConflict, [TestData.Conflict()]);
        Assert.Equal(2, store.Snapshot.CheckedUtc.Count);   // other writes keep the checks
        Assert.Equal(T.AddHours(1), store.Snapshot.CheckedUtc["UCDP"]);
    }

    [Fact]
    public void SnapshotEqualityIncludesTheChecks()
    {
        var a = TestData.Snap();
        var b = TestData.Snap() with { CheckedUtc = new Dictionary<string, DateTime> { ["IPC"] = T } };
        var c = TestData.Snap() with { CheckedUtc = new Dictionary<string, DateTime> { ["IPC"] = T } };
        Assert.NotEqual(a, b);
        Assert.Equal(b, c);
    }

    [Fact]
    public void CacheRoundTripsTheChecks()
    {
        var snap = TestData.Snap() with { CheckedUtc = new Dictionary<string, DateTime> { ["UCDP"] = T, ["WHO"] = T.AddMinutes(5) } };
        Assert.True(Cache().Save(snap));
        var loaded = Cache().Load();
        Assert.Equal(snap, loaded);
        Assert.Equal(DateTimeKind.Utc, loaded!.CheckedUtc["WHO"].Kind);
    }

    [Fact]
    public void SchemaOneCacheIsDiscarded()
    {
        Assert.Equal(4, CrisisCache.SchemaVersion);
        var file = Cache().FilePath;
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllText(file, """{"SchemaVersion":1,"Crises":[],"Displacement":[],"Unplaced":[]}""");
        Assert.Null(Cache().Load());
        Assert.False(File.Exists(file));
    }

    [Fact]
    public async Task FeedsMarkCheckedOnSuccessEvenWhenTheyFindNothing()
    {
        var h = new SampleHttpHandler();
        h.MapText("https://www.who.int/api/news/diseaseoutbreaknews", """{"value":[]}""");
        var store = new CrisisStore();
        var now = new DateTime(2026, 11, 22, 6, 0, 0, DateTimeKind.Utc);
        var who = new WhoOutbreakFeed(new FeedHttp("test", h), store, new FixedClock(now), new ListLog());
        Assert.Equal(FeedRunResult.Ok, await who.RunOnceAsync(default));
        Assert.Empty(store.Snapshot.Crises);
        Assert.Equal(now, store.Snapshot.CheckedUtc["WHO"]);

        // A failed run records nothing.
        var ipc = new IpcNationalFeed(new FeedHttp("test", new SampleHttpHandler()), store, new FixedClock(now), new ListLog());
        await Assert.ThrowsAnyAsync<Exception>(() => ipc.RunOnceAsync(default));
        Assert.False(store.Snapshot.CheckedUtc.ContainsKey("IPC"));
    }

    [Fact]
    public async Task EveryFeedMarksItsName()
    {
        var store = await SampleStore.LoadAsync();
        Assert.Equal(["IPC", "UCDP", "UNHCR", "WHO"], store.Snapshot.CheckedUtc.Keys.Order());
        Assert.All(store.Snapshot.CheckedUtc.Values, t => Assert.Equal(SampleStore.Now, t));
    }
}
