using System.IO;
using EarthRelief3D.Core.Help;

namespace EarthRelief3D.Tests;

/// <summary>data/help/logistics-cluster.json (Plan 4 Task 3): links only, https, each with a title and a checked date.</summary>
public class LogisticsClusterLinksTests
{
    [Fact]
    public void LogisticsClusterFileIsValid()
    {
        var links = LogisticsClusterLinks.Load(Path.Combine(Repo.Root(), "data", "help"));
        Assert.Equal(19, links.All.Count);
        Assert.All(links.All, l => Assert.Equal(Uri.UriSchemeHttps, l.Url.Scheme));
        Assert.Null(links.For("ISL"));
        Assert.Equal("https://logcluster.org/en/where-we-work/sdn/sudan", links.For("sdn")!.Url.ToString());
    }

    [Theory]
    [InlineData("""[{"iso3":"SDN","url":"http://x.org/","title":"t","checkedOn":"2026-11-24"}]""", "https")]
    [InlineData("""[{"iso3":"sd","url":"https://x.org/","title":"t","checkedOn":"2026-11-24"}]""", "iso3")]
    [InlineData("""[{"iso3":"SDN","url":"https://x.org/","title":"","checkedOn":"2026-11-24"}]""", "title")]
    [InlineData("""[{"iso3":"SDN","url":"https://x.org/","title":"t"}]""", "checkedOn")]
    public void ABadLogisticsRowFailsTheLoad(string json, string problem)
    {
        var dir = Path.Combine(Path.GetTempPath(), "er3d-help-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            File.WriteAllText(Path.Combine(dir, "logistics-cluster.json"), json);
            var ex = Assert.Throws<InvalidDataException>(() => LogisticsClusterLinks.Load(dir));
            Assert.Contains(problem, ex.Message);
        }
        finally { Directory.Delete(dir, recursive: true); }
    }
}
