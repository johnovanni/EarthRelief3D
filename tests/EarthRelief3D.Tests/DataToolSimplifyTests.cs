using System.Text.Json;
using EarthRelief3D.DataTool;

namespace EarthRelief3D.Tests;

/// <summary>The <c>simplify</c> command, used to trim test samples (Douglas-Peucker, 3 decimals).</summary>
public class DataToolSimplifyTests
{
    private const string Sample = """
        {"type":"FeatureCollection","features":[
          {"type":"Feature","properties":{"title":"Bama IDPs","overall_phase":4},"geometry":{"type":"Point","coordinates":[13.68512,11.52234]}},
          {"type":"Feature","properties":{"title":"Area"},"geometry":{"type":"Polygon","coordinates":[[[0,0],[1,0.0001],[2,0],[2,2],[1.0001,2.0004],[0,2],[0,0]]]}},
          {"type":"Feature","properties":{"title":"Tiny"},"geometry":{"type":"Polygon","coordinates":[[[0,0],[0.0001,0],[0.0001,0.0001],[0,0.0001],[0,0]]]}}
        ]}
        """;

    [Fact]
    public void DropsNearPointsKeepsRingsPointsAndProperties()
    {
        using var doc = JsonDocument.Parse(GeoJsonSimplifier.Simplify(Sample, 0.004));
        var f = doc.RootElement.GetProperty("features");

        // A Point and every property are untouched (not even rounded).
        Assert.Equal("Bama IDPs", f[0].GetProperty("properties").GetProperty("title").GetString());
        Assert.Equal(4, f[0].GetProperty("properties").GetProperty("overall_phase").GetInt32());
        Assert.Equal("[13.68512,11.52234]", f[0].GetProperty("geometry").GetProperty("coordinates").GetRawText());

        // The two near-collinear points go; the ring stays closed.
        var ring = f[1].GetProperty("geometry").GetProperty("coordinates")[0];
        Assert.Equal("[[0,0],[2,0],[2,2],[0,2],[0,0]]", ring.GetRawText());

        // A ring smaller than the tolerance is kept whole (rounded), never collapsed.
        Assert.Equal(5, f[2].GetProperty("geometry").GetProperty("coordinates")[0].GetArrayLength());
    }

    [Fact]
    public void ToleranceMustBeANumberOfDegrees()
    {
        Assert.Equal(0.004, GeoJsonSimplifier.ParseTolerance("0.004"));
        Assert.Throws<ArgumentException>(() => GeoJsonSimplifier.ParseTolerance("-1"));
        Assert.Throws<ArgumentException>(() => GeoJsonSimplifier.ParseTolerance("abc"));
    }
}
