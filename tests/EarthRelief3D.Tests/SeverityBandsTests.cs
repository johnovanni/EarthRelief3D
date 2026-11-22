using System.IO;
using EarthRelief3D.Core.Logic;

namespace EarthRelief3D.Tests;

public class SeverityBandsTests
{
    [Theory]
    [InlineData(24, 0)]
    [InlineData(25, 1)]
    [InlineData(999, 1)]
    [InlineData(1_000, 2)]
    [InlineData(9_999, 2)]
    [InlineData(10_000, 3)]
    [InlineData(49_999, 3)]
    [InlineData(50_000, 4)]
    public void BandsMatchDoc(int deaths, int band) => Assert.Equal(band, SeverityBands.ForConflict(deaths));

    [Fact]
    public void FoodIsThePhaseAndOutbreaksAreThree()
    {
        Assert.Equal(4, SeverityBands.ForFood(4));
        Assert.Equal(3, SeverityBands.ForOutbreak());
    }

    [Fact]
    public void DocMentionsEveryThreshold()
    {
        var dir = AppContext.BaseDirectory;
        while (dir is not null && !File.Exists(Path.Combine(dir, "docs", "severity-bands.md"))) dir = Path.GetDirectoryName(dir);
        Assert.NotNull(dir);
        var text = File.ReadAllText(Path.Combine(dir!, "docs", "severity-bands.md"));
        foreach (var s in new[] { "25", "1,000", "10,000", "50,000", "native" }) Assert.Contains(s, text);
    }
}
