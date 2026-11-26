using System.IO;
using EarthRelief3D.App.Services;
using EarthRelief3D.Core.Feeds;
using EarthRelief3D.Core.Sources;

namespace EarthRelief3D.Tests;

/// <summary>Plan 5 Task 4: the source list leaves nothing out, so the Data card names every status dot and every bundled folder.</summary>
public sealed class SourceCoverageTests
{
    private static SourceRegistry Real() => SourceRegistry.Load(Path.Combine(Repo.Root(), "data"));

    [Fact]
    public void EveryStatusDotHasASource()
    {
        var registry = Real();
        foreach (var dot in AppServices.StatusDotNames)
            Assert.True(registry.Sources.Any(s => s.StatusDots.Contains(dot, StringComparer.Ordinal)), $"no source in data/sources.json feeds the status dot \"{dot}\"");
    }

    [Fact]
    public void EveryBundledDatasetHasASource()
    {
        var registry = Real();
        var folders = Directory.GetDirectories(BundledDataset.DatasetsDir).Select(Path.GetFileName).ToList();
        Assert.NotEmpty(folders);
        foreach (var folder in folders)
            Assert.True(registry.Sources.Any(s => s.DatasetFolders.Contains(folder!, StringComparer.Ordinal)), $"no source in data/sources.json lists the folder data/datasets/{folder}");
    }
}
