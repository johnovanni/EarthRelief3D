using System.IO;
using EarthRelief3D.App.Diagnostics;
using EarthRelief3D.App.ViewModels;

namespace EarthRelief3D.Tests;

/// <summary>Plan 5 Task 4: the scene field "card" opens a card (or the Settings flyout) for one picture.</summary>
public partial class SnapshotScenesTests
{
    private static IReadOnlyList<SnapshotScene> Read(string json)
    {
        var path = Path.Combine(Path.GetTempPath(), "er3d-scenes-" + Guid.NewGuid().ToString("N") + ".json");
        File.WriteAllText(path, json);
        try { return SnapshotScenes.Load(path); }
        finally { File.Delete(path); }
    }

    [Fact]
    public void CardFieldIsRead()
    {
        var scenes = Read("""
            {"scenes":[
              {"name":"a","displayTimeUtc":"2026-11-22T12:00:00Z"},
              {"name":"b","displayTimeUtc":"2026-11-22T12:00:00Z","card":"welcome"},
              {"name":"c","displayTimeUtc":"2026-11-22T12:00:00Z","card":"about"},
              {"name":"d","displayTimeUtc":"2026-11-22T12:00:00Z","card":"Data"},
              {"name":"e","displayTimeUtc":"2026-11-22T12:00:00Z","card":"settings"}]}
            """);
        Assert.Null(scenes[0].Card);
        Assert.Equal(AppCard.None, scenes[0].AppCard);
        Assert.Equal(AppCard.Welcome, scenes[1].AppCard);
        Assert.Equal(AppCard.About, scenes[2].AppCard);
        Assert.Equal(AppCard.Data, scenes[3].AppCard);
        Assert.Equal("data", scenes[3].Card);
        Assert.False(scenes[3].OpensSettings);
        Assert.Equal("settings", scenes[4].Card);
        Assert.True(scenes[4].OpensSettings);
        Assert.Equal(AppCard.None, scenes[4].AppCard);
    }

    [Theory]
    [InlineData("\"help\"")]
    [InlineData("\"\"")]
    [InlineData("3")]
    [InlineData("true")]
    public void UnknownCardIsRefused(string value)
    {
        var json = "{\"scenes\":[{\"name\":\"a\",\"displayTimeUtc\":\"2026-11-22T12:00:00Z\",\"card\":" + value + "}]}";
        var ex = Assert.Throws<FormatException>(() => Read(json));
        Assert.Contains("Scene a: card is \"welcome\", \"about\", \"data\" or \"settings\".", ex.Message);
    }
}
