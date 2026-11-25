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

    [Fact]
    public void CharitiesFocusIsAccepted()
    {
        var scenes = Read("""{"scenes":[{"name":"a","displayTimeUtc":"2026-11-22T12:00:00Z","panel":"SDN","panelFocus":"Charities"}]}""");
        Assert.Equal(PanelFocus.Charities, scenes[0].FocusSection);
        Assert.Contains("charities", SnapshotScene.PanelFocusNames);
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

    /// <summary>P5-22: the scene field "update" shows the update banner in one state, filled from the synthetic newer release.</summary>
    [Fact]
    public void UpdateFieldIsRead()
    {
        var scenes = Read("""
            {"scenes":[
              {"name":"a","displayTimeUtc":"2026-11-22T12:00:00Z"},
              {"name":"b","displayTimeUtc":"2026-11-22T12:00:00Z","update":"offer"},
              {"name":"c","displayTimeUtc":"2026-11-22T12:00:00Z","update":"Downloading"},
              {"name":"d","displayTimeUtc":"2026-11-22T12:00:00Z","update":"ready"},
              {"name":"e","displayTimeUtc":"2026-11-22T12:00:00Z","update":"failed"},
              {"name":"f","displayTimeUtc":"2026-11-22T12:00:00Z","update":"stale","card":"about"}]}
            """);
        Assert.Equal(new string?[] { null, "offer", "downloading", "ready", "failed", "stale" }, scenes.Select(s => s.Update));
        Assert.Equal(
            [UpdateBannerState.Hidden, UpdateBannerState.Offer, UpdateBannerState.Downloading, UpdateBannerState.Ready,
             UpdateBannerState.Failed, UpdateBannerState.Stale],
            scenes.Select(s => s.UpdateState).ToArray());
        Assert.False(scenes[0].ShowsUpdate);
        Assert.True(scenes[1].ShowsUpdate);
        Assert.Equal("github_release_latest_newer.json", SnapshotScenes.UpdateSampleName);

        // Each state's banner as a picture shows it, from the sample (no network).
        var release = EarthRelief3D.Core.Updates.UpdateCheck.Parse(File.ReadAllText(Samples.Path(SnapshotScenes.UpdateSampleName)));
        var banner = new UpdateBannerViewModel(new GlobeCore.Wpf.Services.ShellLinkOpener(), new Version(1, 0, 0));
        banner.ShowForPicture(release, UpdateBannerState.Offer);
        Assert.StartsWith("A new version of EarthRelief3D is available: 1.1.0 (released 3 Feb 2027). You have 1.0.0.", banner.Text);
        banner.ShowForPicture(release, UpdateBannerState.Downloading);
        Assert.Equal("Downloading EarthRelief3D 1.1.0: 12 of 53 MB", banner.Text);
        Assert.Equal("12 of 53 MB", banner.ProgressText);
        banner.ShowForPicture(release, UpdateBannerState.Ready);
        Assert.StartsWith("Ready to install 1.1.0.", banner.Text);
        banner.ShowForPicture(release, UpdateBannerState.Failed);
        Assert.StartsWith("The update could not be used: ", banner.Text);
        Assert.True(banner.ShowOpenDownloadPage);
        banner.ShowForPicture(release, UpdateBannerState.Stale);
        Assert.StartsWith("EarthRelief3D has not been able to check", banner.Text);
        banner.ShowForPicture(release, UpdateBannerState.Hidden);
        Assert.False(banner.IsVisible);
    }

    [Theory]
    [InlineData("\"install\"")]
    [InlineData("\"hidden\"")]
    [InlineData("\"\"")]
    [InlineData("1")]
    [InlineData("true")]
    public void UnknownUpdateStateIsRefused(string value)
    {
        var json = "{\"scenes\":[{\"name\":\"a\",\"displayTimeUtc\":\"2026-11-22T12:00:00Z\",\"update\":" + value + "}]}";
        var ex = Assert.Throws<FormatException>(() => Read(json));
        Assert.Contains("Scene a: update is \"offer\", \"downloading\", \"ready\", \"failed\" or \"stale\".", ex.Message);
    }
}
