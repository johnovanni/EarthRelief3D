using System.IO;
using EarthRelief3D.DataTool;
using static EarthRelief3D.Tests.SourceZips;

namespace EarthRelief3D.Tests;

/// <summary>The <c>eia</c> command (Task 6) on a tiny INTL.zip in EIA's real shape.</summary>
public class DataToolEiaTests
{
    private const string Updated = "2026-10-02T17:12:47-04:00";

    private static Extract Run(params string[] lines)
    {
        using var dir = new TempDir();
        return EiaExtractor.Run(Eia(dir, lines), Codes);
    }

    [Fact]
    public void KeepsLatestFullYear()
    {
        var e = Run(
            // Updated after 2025 ended: 2025 is a full year.
            EiaSeries("USA", "United States", "USA", Updated, ("2025", 13661.93), ("2024", 13266.82)),
            // "--" is EIA's "no value": the newest year with a number is kept.
            EiaSeries("SAU", "Saudi Arabia", "SAU", Updated, ("2025", "--"), ("2024", 9208.67), ("2023", 9716.48)),
            // Updated in June 2025: its 2025 figure cannot cover the whole year, so 2024 is kept.
            EiaSeries("NOR", "Norway", "NOR", "2025-06-30T10:00:00-04:00", ("2025", 2000.0), ("2024", 1858.46)),
            // Regions, and EIA's region code that repeats a country (WP27 is the United States again): dropped, never a second USA.
            EiaSeries("WORL", "World", "ALB+USA+SAU", Updated, ("2025", 84000.0)),
            EiaSeries("WP27", "United States", "USA", Updated, ("2025", 13661.93)),
            // A former country with no figure for decades: dropped, not "not placed".
            EiaSeries("SUN", "Former U.S.S.R.", "SUN", "2025-02-19T20:28:17-05:00", ("2023", "--"), ("1991", 9991.99)),
            // A code with no unit in the boundary set but a figure: listed as not placed.
            EiaSeries("HITZ", "Hawaiian Trade Zone", "HITZ", Updated, ("2025", 1.5)),
            // Kosovo: EIA's XKS is not Natural Earth's code, so the name places it.
            EiaSeries("XKS", "Kosovo", "XKS", Updated, ("2025", 0.0)));

        var rows = e.Rows.ToDictionary(r => r.Iso3);
        Assert.Equal((2025, 13661.93), (rows["USA"].Year, rows["USA"].Value));
        Assert.Equal((2024, 9208.67), (rows["SAU"].Year, rows["SAU"].Value));
        Assert.Equal((2024, 1858.46), (rows["NOR"].Year, rows["NOR"].Value));
        Assert.Equal("XKS", rows["KOS"].Extra["eia_code"]);
        Assert.Equal(["KOS", "NOR", "SAU", "USA"], rows.Keys.Order(StringComparer.Ordinal));
        Assert.All(e.Rows, r => Assert.Equal(EiaExtractor.CrudeOil, r.Series));

        var m = e.Manifest;
        Assert.Contains(m.Dropped, d => d.Code == "WORL" && d.Reason.Contains("aggregate"));
        Assert.Contains(m.Dropped, d => d.Code == "WP27");
        Assert.Contains(m.Dropped, d => d.Code == "SUN" && d.Reason.Contains("1991"));
        Assert.Equal("HITZ", Assert.Single(m.NotPlaced).Code);
        // The flags EIA's file carries: the series frequency and the "no value" marker.
        Assert.Equal(8, m.FlagsSeen["f=A"]);
        Assert.Equal(2, m.FlagsSeen["value \"--\""]);
        Assert.Contains(m.Notes, n => n.Contains("NOR 2025") && n.Contains("not a full year"));
        Assert.Equal("Public domain (U.S. government work)", m.Licence);
        Assert.Equal("Source: U.S. Energy Information Administration (Nov 2026)", m.Attribution);
        Assert.Equal("Year", m.DateMeaning);   // C4: full years, not estimates
        Assert.Null(m.DateText);
    }

    [Fact]
    public void ZeroProducersWithNoUnitAreRecordedNotListed()
    {
        // Ruling R16: a code with no map unit and 0 production is recorded in the manifest, not listed "Not placed on the
        // map"; one with a figure above 0 is still listed.
        var e = Run(
            EiaSeries("USA", "United States", "USA", Updated, ("2025", 13661.93)),
            EiaSeries("GUF", "French Guiana", "GUF", Updated, ("2025", 0.0), ("2024", 0.0)),
            EiaSeries("HITZ", "Hawaiian Trade Zone", "HITZ", Updated, ("2025", 1.5)));

        Assert.Equal(["USA"], e.Rows.Select(r => r.Iso3));
        Assert.Equal("HITZ", Assert.Single(e.Manifest.NotPlaced).Code);
        var guf = Assert.Single(e.Manifest.Dropped, d => d.Code == "GUF");
        Assert.Equal("French Guiana", guf.Name);
        Assert.Contains("0 in 2025", guf.Reason);
        Assert.Contains("R16", guf.Reason);
    }

    [Fact]
    public void AChangedUnitIsAFormatChange()
    {
        using var dir = new TempDir();
        var src = Eia(dir, EiaSeries("USA", "United States", "USA", Updated, ("2025", 1.0)).Replace("thousand barrels per day", "barrels", StringComparison.Ordinal));
        Assert.Throws<InvalidDataException>(() => EiaExtractor.Run(src, Codes));
    }
}
