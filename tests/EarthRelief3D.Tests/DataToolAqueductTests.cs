using System.IO;
using EarthRelief3D.App.Layers;
using EarthRelief3D.App.Presentation;
using EarthRelief3D.DataTool;
using static EarthRelief3D.Tests.SourceZips;

namespace EarthRelief3D.Tests;

/// <summary>The <c>aqueduct</c> command (Task 6) on a tiny workbook in WRI's real shape.</summary>
public class DataToolAqueductTests
{
    private static Extract Run(params object[][] rows)
    {
        using var dir = new TempDir();
        return AqueductExtractor.Run(Aqueduct(dir, rows), Codes);
    }

    [Fact]
    public void TotalWeightOnly()
    {
        var e = Run(
            AqRow("SDN", "Sudan", "bws", "Dom", 0.5, 0, "Low (<10%)"),
            AqRow("SDN", "Sudan", "bws", "Tot", 1.4579581779999999, 1, "Low - Medium (10-20%)"),
            AqRow("SDN", "Sudan", "bws", "Irr", 2.5, 2, "Medium - High (20-40%)"),
            AqRow("SDN", "Sudan", "drr", "Tot", 3.5, 3, "High (0.6-0.8)"),
            AqRow("YEM", "Yemen", "bws", "Tot", 4.6936758330000004, 4, "Extremely High (>80%)"),
            AqRow("GRL", "Greenland", "bws", "Tot", -9999, -9999, "NoData"));

        Assert.Equal(["SDN", "YEM", "GRL"], e.Rows.Select(r => r.Iso3));
        var sdn = e.Rows[0];
        Assert.Equal(AqueductExtractor.WaterStress, sdn.Series);
        Assert.Equal((1.0, "Low - Medium (10-20%)"), (sdn.Value, sdn.Qualifier));
        // The columns the brief names, as WRI wrote them.
        Assert.Equal("SDN", sdn.Extra["gid_0"]);
        Assert.Equal("1.457958178", sdn.Extra["score"]);
        Assert.Equal("1", sdn.Extra["cat"]);
        Assert.Equal("Low - Medium (10-20%)", sdn.Extra["label"]);
        // WRI's "no data" stays in the extract and is no figure on the map (below the first break).
        var grl = e.Rows[2];
        Assert.False(CountryValueLayer.HasFigure(new(grl.Iso3, grl.Value, grl.Year, TestData.Cite("WRI"), grl.Qualifier), ShadingScales.For(ShadingLayer.WaterStress)));

        var m = e.Manifest;
        Assert.Equal(3, m.QualifiersSeen.Count);   // the (cat, label) pairs seen
        Assert.Equal(1, m.QualifiersSeen["4: Extremely High (>80%)"]);
        Assert.Equal("CC BY 4.0", m.Licence);
        Assert.Contains("Kuzma", m.Attribution);
        // I3: a modelled baseline, dated in words; only the date fields say so.
        Assert.Equal("Baseline", m.DateMeaning);
        Assert.Equal("baseline 1979–2019 (published 2023)", m.DateText);
    }

    [Fact]
    public void AridCategoryKeptWithLabel()
    {
        var e = Run(
            AqRow("EGY", "Egypt", "bws", "Tot", 4.85, 4, "Extremely High (>80%)"),
            AqRow("ESH", "Western Sahara", "bws", "Tot", -1, -1, "Arid and Low Water Use"));

        var arid = Assert.Single(e.Rows, r => r.Iso3 == "ESH");
        Assert.Equal((-1.0, "Arid and Low Water Use"), (arid.Value, arid.Qualifier));
        // The map gives it the grey arid class, named with WRI's label.
        var scale = ShadingScales.For(ShadingLayer.WaterStress);
        Assert.Equal(0, scale.ClassOf(arid.Value));
        Assert.Equal(arid.Qualifier, scale.ClassLabels[0]);
        Assert.Equal(ShadingScales.AridColor, scale.ColorOf(arid.Value));
    }

    [Fact]
    public void AMissingSheetIsAFormatChange()
    {
        using var dir = new TempDir();
        var path = Zip(dir, "aqueduct.zip", ("x/book.xlsx", Xlsx([("Read Me", [new object[] { "x" }])])));
        Assert.Throws<InvalidDataException>(() => AqueductExtractor.Run(new SourceFile(path, Published, Downloaded), Codes));
    }
}
