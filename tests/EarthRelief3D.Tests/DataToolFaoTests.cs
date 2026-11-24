using System.IO;
using EarthRelief3D.DataTool;
using static EarthRelief3D.Tests.SourceZips;

namespace EarthRelief3D.Tests;

/// <summary>The <c>fao</c> command (Task 6, ruling P3-11; R3 for "&lt;2.5"), on tiny zips in FAO's real shape.</summary>
public class DataToolFaoTests
{
    private static Extract Run(params string[] rows)
    {
        using var dir = new TempDir();
        return FaoExtractor.Run(Fao(dir, rows), Codes);
    }

    private static IReadOnlyList<ExtractRow> Of(Extract e, string series, string iso3) =>
        [.. e.Rows.Where(r => r.Series == series && r.Iso3 == iso3)];

    [Fact]
    public void ChinaIsCountedOnce()
    {
        var e = Run(
            PoU(351, "159", "China", "2023-2025", "<2.5"),
            PoU(41, "156", "China, mainland", "2023-2025", "<2.5"),
            Adequacy(351, "159", "China", "2023-2025", "141"),
            Adequacy(41, "156", "China, mainland", "2023-2025", "142"),
            PoU(96, "344", "China, Hong Kong SAR", "2023-2025", "<2.5"),
            PoU(128, "446", "China, Macao SAR", "2023-2025", "<2.5"),
            PoU(214, "158", "China, Taiwan Province of", "2023-2025", "3.1"));

        // FAO's "China, mainland" (41) is CHN; the "China" aggregate (351) is dropped, so CHN appears once per item.
        var chn = Assert.Single(Of(e, FaoExtractor.Undernourishment, "CHN"));
        Assert.Equal("156", chn.Extra["m49"]);
        Assert.Equal(142, Assert.Single(Of(e, FaoExtractor.EnergyAdequacy, "CHN")).Value);
        // Hong Kong, Macao and Taiwan keep their own codes (Taiwan through ISO_N3_EH: its UN_A3 is -099 in Natural Earth).
        Assert.Single(Of(e, FaoExtractor.Undernourishment, "HKG"));
        Assert.Single(Of(e, FaoExtractor.Undernourishment, "MAC"));
        Assert.Equal(3.1, Assert.Single(Of(e, FaoExtractor.Undernourishment, "TWN")).Value);
        Assert.Contains(e.Manifest.Dropped, d => d.Code == "FAO 351" && d.Name == "China");
        Assert.Empty(e.Manifest.NotPlaced);
    }

    [Fact]
    public void AggregatesAreDropped()
    {
        var e = Run(
            PoU(5000, "001", "World", "2023-2025", "8.2"),
            PoU(5100, "002", "Africa", "2023-2025", "19.9"),
            PoU(420, "202", "Sub-Saharan Africa", "2023-2025", "23.0"),
            PoU(9010, "903", "High-income economies", "2023-2025", "<2.5"),
            PoU(5801, "199", "Least Developed Countries (LDCs)", "2023-2025", "21.1"),
            PoU(2, "004", "Afghanistan", "2023-2025", "27.8"));

        Assert.Equal(["AFG"], e.Rows.Select(r => r.Iso3).Distinct());
        Assert.Equal(["World", "Africa", "Sub-Saharan Africa", "High-income economies", "Least Developed Countries (LDCs)"],
            e.Manifest.Dropped.Select(d => d.Name));
        Assert.All(e.Manifest.Dropped, d => Assert.Contains("aggregate", d.Reason));
        Assert.Empty(e.Manifest.NotPlaced);   // an aggregate is not a country that failed to map
    }

    [Fact]
    public void UnmappedCodesAreListedInManifest()
    {
        var e = Run(
            PoU(999, "999", "Atlantis", "2023-2025", "12.0"),
            PoU(218, "772", "Tokelau", "2023-2025", "4.0"),
            PoU(93, "332", "Haiti", "2023-2025", "51.4"));

        Assert.Equal(["HTI"], e.Rows.Select(r => r.Iso3).Distinct());
        Assert.Contains(e.Manifest.NotPlaced, n => n.Code == "M49 999" && n.Name == "Atlantis");
        Assert.Contains(e.Manifest.NotPlaced, n => n.Code == "M49 772" && n.Name == "Tokelau");
        Assert.All(e.Manifest.NotPlaced, n => Assert.False(string.IsNullOrWhiteSpace(n.Reason)));
    }

    [Fact]
    public void LatestYearPerCountry()
    {
        var e = Run(
            // Sudan: the newest ranges are missing (flag O, empty value), so 2020-2022 is its latest figure.
            PoU(276, "729", "Sudan", "2019-2021", "11.1"),
            PoU(276, "729", "Sudan", "2020-2022", "10.9"),
            PoU(276, "729", "Sudan", "2021-2023", "", "O"),
            PoU(276, "729", "Sudan", "2023-2025", "", "O"),
            // Haiti: rows out of order; the newest range wins.
            PoU(93, "332", "Haiti", "2023-2025", "51.4"),
            PoU(93, "332", "Haiti", "2022-2024", "50.9"),
            Adequacy(93, "332", "Haiti", "2023-2025", "84"),
            // Yemen: every range missing: no figure at all.
            PoU(249, "887", "Yemen", "2023-2025", "", "O"),
            // R3: "<2.5" is stored as 2.5 with its qualifier.
            PoU(99, "352", "Iceland", "2023-2025", "<2.5"),
            // Latin-1: "Côte d'Ivoire" in FAO's encoding.
            PoU(107, "384", "Côte d'Ivoire", "2023-2025", "7.4"));

        var sdn = Assert.Single(Of(e, FaoExtractor.Undernourishment, "SDN"));
        Assert.Equal((2022, 10.9, "2020-2022"), (sdn.Year, sdn.Value, sdn.Extra["period"]));
        var hti = Assert.Single(Of(e, FaoExtractor.Undernourishment, "HTI"));
        Assert.Equal((2025, 51.4), (hti.Year, hti.Value));
        Assert.Equal(84, Assert.Single(Of(e, FaoExtractor.EnergyAdequacy, "HTI")).Value);
        Assert.Empty(Of(e, FaoExtractor.Undernourishment, "YEM"));
        var isl = Assert.Single(Of(e, FaoExtractor.Undernourishment, "ISL"));
        Assert.Equal((2.5, "<2.5"), (isl.Value, isl.Qualifier));
        Assert.Equal(1, e.Manifest.QualifiersSeen["<2.5"]);
        Assert.Equal("Côte d'Ivoire", Assert.Single(Of(e, FaoExtractor.Undernourishment, "CIV")).Extra["area"]);
        Assert.Equal(3, e.Manifest.FlagsSeen["O"]);

        // The manifest: the source, the licence from Task 1's records, the file's hash and the command (P3-10).
        var m = e.Manifest;
        Assert.Equal(FaoExtractor.FolderName, m.Id);
        Assert.Equal("CC BY 4.0", m.Licence);
        Assert.StartsWith("FAO. 2026. FAOSTAT: Suite of Food Security Indicators. Accessed on 23 November 2026.", m.Attribution);
        Assert.Equal("ThreeYearAverage", m.DateMeaning);   // fix round 1: shown as the range, never one year
        Assert.Equal(64, m.FileSha256.Length);
        Assert.StartsWith("DataTool fao ", m.Command);
        Assert.Equal([FaoExtractor.Undernourishment, FaoExtractor.EnergyAdequacy], m.Series.Select(s => s.Id));
    }

    [Fact]
    public void AChangedItemNameIsAFormatChange()
    {
        using var dir = new TempDir();
        var src = Fao(dir, FaoRow(93, "332", "Haiti", "210041", "Prevalence of undernourishment (percent)", "2023-2025", "51.4"));
        Assert.Throws<InvalidDataException>(() => FaoExtractor.Run(src, Codes));
    }
}
