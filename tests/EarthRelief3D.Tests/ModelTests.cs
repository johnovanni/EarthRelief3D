using EarthRelief3D.Core.Model;
using EarthRelief3D.Core.Store;

namespace EarthRelief3D.Tests;

public class ModelTests
{
    [Fact]
    public void CrisesCompareByValueIncludingLists()
    {
        Assert.Equal(TestData.Conflict(), TestData.Conflict());
        Assert.Equal(TestData.Conflict().GetHashCode(), TestData.Conflict().GetHashCode());
        Assert.NotEqual(TestData.Conflict(), TestData.Conflict() with { SideB = "C" });
        Assert.NotEqual(TestData.Conflict(), TestData.Conflict() with { Events = [] });
        Assert.NotEqual(TestData.Conflict(), TestData.Conflict() with { DeathsByMonth = new Dictionary<DateOnly, int>() });
        Assert.Equal(TestData.Food(), TestData.Food());
        Assert.Equal(TestData.Outbreak(), TestData.Outbreak());
        Assert.NotEqual<Crisis>(TestData.Food(), TestData.Outbreak());
        Assert.Equal(TestData.Snap(), TestData.Snap());
    }

    [Fact]
    public void StoreIgnoresIdenticalWritesAndKeepsOtherKinds()
    {
        var store = new CrisisStore();
        var raised = 0;
        store.Changed += (_, _) => raised++;
        Assert.True(store.ReplaceKind(CrisisKind.ArmedConflict, [TestData.Conflict()]));
        Assert.False(store.ReplaceKind(CrisisKind.ArmedConflict, [TestData.Conflict()]));
        Assert.True(store.ReplaceKind(CrisisKind.Outbreak, [TestData.Outbreak()]));
        Assert.True(store.ReplaceKind(CrisisKind.ArmedConflict, []));
        Assert.Single(store.Snapshot.Crises);
        Assert.True(store.ReplaceDisplacement([TestData.Disp()]));
        Assert.False(store.ReplaceDisplacement([TestData.Disp()]));
        Assert.True(store.ReplaceUnplaced("WHO", [TestData.Unplaced()]));
        Assert.False(store.ReplaceUnplaced("WHO", [TestData.Unplaced()]));
        Assert.True(store.ReplaceUnplaced("WHO", []));
        Assert.Equal(6, raised);
        Assert.Single(store.Snapshot.ByCountry("cod"));
        Assert.Empty(store.Snapshot.ByCountry("SDN"));
    }

    [Fact]
    public void LoadReplacesEverythingOnceAndNotWhenEqual()
    {
        var store = new CrisisStore();
        Assert.True(store.Load(TestData.Snap()));
        Assert.False(store.Load(TestData.Snap()));
        Assert.Equal(TestData.Snap(), store.Snapshot);
    }
}
