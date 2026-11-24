using System.IO;
using System.Text.Json;
using EarthRelief3D.DataTool;
using ExcelDataReader;

namespace EarthRelief3D.Tests;

/// <summary>
/// The <c>workbook</c> verb's writer (<see cref="XlsxWriter"/>): what it writes, the DataTool's own workbook reader reads back,
/// and the committed synthetic ACAPS workbook (tests/Samples, ruling P4-2) is the one its JSON describes.
/// </summary>
public class DataToolWorkbookTests
{
    private const string Spec = """
        { "sheets": [
            { "name": "csv", "rows": [ ["Crisis ID", "ACCESS", "Country Name"], ["SDN001", 5, "Sudan & <friends>"], ["XXK001", 3, null] ] },
            { "name": "About", "rows": [ ["a note"] ] } ] }
        """;

    [Fact]
    public void WrittenWorkbookRoundTripsThroughTheDataToolReader()
    {
        using var dir = new TempDir();
        var path = dir.File("book.xlsx");
        File.WriteAllBytes(path, XlsxWriter.Write(XlsxWriter.ReadSpec(Spec)));

        using var doc = JsonDocument.Parse(WorkbookDescriber.Excerpt(path, ["csv||10", "About||10"]));
        var sheets = doc.RootElement.GetProperty("sheets").EnumerateArray().ToList();
        Assert.Equal(["csv", "About"], sheets.Select(s => s.GetProperty("name").GetString()));
        Assert.Equal(["Crisis ID", "ACCESS", "Country Name"], sheets[0].GetProperty("header").EnumerateArray().Select(e => e.GetString()));
        var rows = sheets[0].GetProperty("rows").EnumerateArray().ToList();
        Assert.Equal(2, rows.Count);
        Assert.Equal("SDN001", rows[0][0].GetString());
        Assert.Equal(5.0, rows[0][1].GetDouble());   // a number stays a number
        Assert.Equal("Sudan & <friends>", rows[0][2].GetString());
        Assert.Equal(JsonValueKind.Null, rows[1][2].ValueKind);   // null stays a blank cell
    }

    [Theory]
    [InlineData("""{ "rows": [] }""")]
    [InlineData("""{ "sheets": [ { "name": "x" } ] }""")]
    [InlineData("""{ "sheets": [ { "name": "x", "rows": [ [true] ] } ] }""")]
    public void BadSpecIsRejected(string json) => Assert.Throws<ArgumentException>(() => XlsxWriter.ReadSpec(json));

    [Fact]
    public void CommittedSyntheticAcapsWorkbookMatchesItsJson()
    {
        // Rebuilt with: dotnet run --project tools/EarthRelief3D.DataTool -- workbook tests/Samples/acaps_access_synthetic.json --out tests/Samples/acaps_access_synthetic.xlsx
        var expected = XlsxWriter.ReadSpec(Samples.Text("acaps_access_synthetic.json"));
        Assert.Contains(expected, s => s.Name == "csv");
        Assert.Contains(expected, s => s.Name == "indicators");
        using var stream = File.OpenRead(Samples.Path("acaps_access_synthetic.xlsx"));
        using var reader = ExcelReaderFactory.CreateOpenXmlReader(stream);
        foreach (var (name, rows) in expected)
        {
            Assert.Equal(name, reader.Name);
            foreach (var row in rows)
            {
                Assert.True(reader.Read());
                for (int c = 0; c < row.Length; c++)
                    Assert.Equal(row[c], reader.GetValue(c));
            }
            Assert.False(reader.Read());
            reader.NextResult();
        }
    }
}
