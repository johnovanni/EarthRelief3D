using System.IO;

namespace EarthRelief3D.Tests;

public class SmokeTests
{
    [Fact]
    public void SamplesAreCopiedToOutput() =>
        Assert.True(File.Exists(Path.Combine(AppContext.BaseDirectory, "Samples", "README.md")));
}
