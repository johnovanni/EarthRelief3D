using System.IO;
using System.IO.Compression;
using System.Text;
using EarthRelief3D.Core.Feeds;
using EarthRelief3D.Core.Model;
using EarthRelief3D.DataTool;

namespace EarthRelief3D.Tests;

/// <summary>Plan 4 Task 4: the <c>airports</c> and <c>ports</c> verbs (P4-5; docs/data-sources.md §15, §16), run on the real excerpts in tests/Samples.</summary>
public class DataToolHubsTests
{
    private static readonly DateOnly Downloaded = new(2026, 11, 24);
    private static readonly DateOnly CsvDate = new(2026, 11, 5);

    private static SourceFile Airports() => new(Samples.Path("ourairports_excerpt.csv"), Downloaded, Downloaded);

    /// <summary>The WPI excerpt as the zip NGA serves: <c>UpdatedPub150.csv</c> inside, dated <paramref name="entryDate"/>.</summary>
    private static SourceFile PortsZip(TempDir dir, byte[]? csv = null, DateOnly? entryDate = null)
    {
        var path = dir.File("WPI.zip");
        using (var zip = ZipFile.Open(path, ZipArchiveMode.Create))
        {
            var e = zip.CreateEntry("UpdatedPub150.csv");
            var d = entryDate ?? CsvDate;
            e.LastWriteTime = new DateTimeOffset(d.Year, d.Month, d.Day, 19, 54, 44, TimeSpan.Zero);
            using var s = e.Open();
            s.Write(csv ?? File.ReadAllBytes(Samples.Path("wpi_excerpt.csv")));
        }
        return new SourceFile(path, Downloaded, Downloaded);
    }

    /// <summary>A WPI file of the real header with only the columns the extract reads filled in (the rest blank), one row each.</summary>
    private static byte[] WpiRows(params (string Name, string Locode, string Country, string Size)[] rows)
    {
        var header = File.ReadLines(Samples.Path("wpi_excerpt.csv")).First().TrimStart('﻿').Split(',');
        int Col(string n) => Array.IndexOf(header, n);
        var sb = new StringBuilder("﻿" + string.Join(",", header) + "\n");
        foreach (var (name, locode, country, size) in rows)
        {
            var cells = new string[header.Length];
            Array.Fill(cells, "");
            cells[Col("Main Port Name")] = name;
            cells[Col("UN/LOCODE")] = locode;
            cells[Col("Country Code")] = country;
            cells[Col("Harbor Size")] = size;
            cells[Col("Latitude")] = "10.5";
            cells[Col("Longitude")] = "20.25";
            sb.Append(string.Join(",", cells)).Append('\n');
        }
        return new UTF8Encoding(false).GetBytes(sb.ToString());
    }

    [Fact]
    public void AirportsKeepScheduledLargeAndMedium()
    {
        var x = HubExtractor.Airports(Airports(), SourceZips.Codes);

        // The excerpt's 200 rows hold 112 large or medium airports with scheduled service (48 large, 64 medium).
        Assert.Equal(112, x.Rows.Count);
        Assert.Equal(48, x.Rows.Count(r => r.Type == "large"));
        Assert.Equal(64, x.Rows.Count(r => r.Type == "medium"));
        Assert.All(x.Rows, r => Assert.Contains(r.Type, new[] { "large", "medium" }));
        // Keflavik: large, scheduled; the detail is its IATA code. Bildudalur is medium but has no scheduled service; small fields and heliports are out.
        var kef = Assert.Single(x.Rows, r => r.Name == "Keflavik International Airport");
        Assert.Equal(("ISL", "large", "KEF"), (kef.Iso3, kef.Type, kef.Extra["detail"]));
        Assert.Equal(63.985001, kef.Lat, 6);
        Assert.Equal(-22.6056, kef.Lon, 4);
        Assert.DoesNotContain(x.Rows, r => r.Name == "Bildudalur Airport");
        Assert.DoesNotContain(x.Rows, r => r.Name == "Yamoussoukro International Airport");   // large, but no scheduled service
        // Fiji's fields either side of 180 degrees stay where the file puts them; Sudan's are there under SDN.
        Assert.Equal(["detail"], x.ExtraColumns);
        Assert.Contains(x.Rows, r => r.Iso3 == "SDN" && r.Extra["detail"] == "KRT");
        Assert.Contains(x.Rows, r => r.Iso3 == "FJI" && r.Extra["detail"] == "NAN");
        Assert.Contains(x.Rows, r => r.Iso3 == "NOR" && r.Lat > 70);   // Norway has no ISO_A2 in Natural Earth: ISO_A2_EH
        Assert.Empty(x.Manifest.NotPlaced);
        // The counts are in the notes.
        Assert.Contains(x.Manifest.Notes, n => n.Contains("112 airports kept", StringComparison.Ordinal) && n.Contains("of 200 rows", StringComparison.Ordinal));
    }

    [Fact]
    public void AirportsRoundTripThroughTheLoader()
    {
        using var dir = new TempDir();
        DatasetWriter.WritePoints(HubExtractor.Airports(Airports(), SourceZips.Codes), dir.File("ourairports"));

        var load = BundledPoints.Load("Hubs", dir.File("ourairports"), null, new ListLog());

        Assert.True(load.Loaded, load.Problem);
        Assert.Equal(112, load.Points.Count);
        Assert.Equal(CitationDate.AsOf, load.Citation!.DateMeaning);
        Assert.Equal(new DateTime(2026, 11, 24, 0, 0, 0, DateTimeKind.Utc), load.Citation.AsOfUtc);
        Assert.Contains(load.Points, p => p.Name == "Keflavik International Airport" && p.Detail == "KEF" && p.Type == "large");
    }

    [Fact]
    public void PortsKeepLargeAndMedium()
    {
        using var dir = new TempDir();
        var x = HubExtractor.Ports(PortsZip(dir), SourceZips.Codes);

        // The excerpt's 100 rows: Large 24, Medium 34, Small 13, Very Small 17, blank 12.
        // Europa Point (Medium, Gibraltar) has no UN/LOCODE and no unit in the boundary set: listed in the manifest, not drawn.
        Assert.Equal(57, x.Rows.Count);
        Assert.Equal(24, x.Rows.Count(r => r.Type == "large"));
        Assert.Equal(33, x.Rows.Count(r => r.Type == "medium"));
        Assert.Equal(["Gibraltar"], x.Manifest.NotPlaced.Select(n => n.Code));
        Assert.DoesNotContain(x.Rows, r => r.Name == "Europa Point");
        Assert.Contains(x.Rows, r => r.Name == "Port Sudan" && r.Iso3 == "SDN" && r.Type == "medium" && r.Extra["detail"] == "SD PZU");
        Assert.Contains(x.Rows, r => r.Name == "Aden" && r.Iso3 == "YEM");
        Assert.Equal(["detail"], x.ExtraColumns);
        // The counts, with the sizes the file has, are written to the manifest's notes.
        var counts = Assert.Single(x.Manifest.Notes, n => n.StartsWith("Counts", StringComparison.Ordinal));
        Assert.Contains("57 ports kept", counts, StringComparison.Ordinal);
        Assert.Contains("large 24, medium 33", counts, StringComparison.Ordinal);
        Assert.Contains("of 100 rows", counts, StringComparison.Ordinal);
        Assert.Contains("Small 13", counts, StringComparison.Ordinal);
        Assert.Contains("Very Small 17", counts, StringComparison.Ordinal);
        Assert.Contains("(blank) 12", counts, StringComparison.Ordinal);
    }

    [Fact]
    public void PortalNoteIsWrittenAsGiven()
    {
        using var dir = new TempDir();
        const string note = "msi.nga.mil answered 503 on 2027-05-02";
        var x = HubExtractor.Ports(PortsZip(dir), SourceZips.Codes, portalNote: note);
        var licence = Assert.Single(x.Manifest.Notes, n => n.StartsWith("Licence:", StringComparison.Ordinal));
        Assert.Contains(note, licence, StringComparison.Ordinal);
        Assert.DoesNotContain("2026-11-24", licence, StringComparison.Ordinal);
        Assert.Single(System.Text.RegularExpressions.Regex.Matches(licence, @"\d{4}-\d{2}-\d{2}"));
    }

    [Fact]
    public void NoPortalNoteSaysNotRechecked()
    {
        using var dir = new TempDir();
        var x = HubExtractor.Ports(PortsZip(dir), SourceZips.Codes);
        var licence = Assert.Single(x.Manifest.Notes, n => n.StartsWith("Licence:", StringComparison.Ordinal));
        Assert.Contains("not re-checked for this extract", licence, StringComparison.Ordinal);
        Assert.DoesNotMatch(@"\d{4}-\d{2}-\d{2}", licence);
        Assert.DoesNotContain("maintenance", licence, StringComparison.Ordinal);
    }

    [Fact]
    public void PortsReadTheBareCsvToo()
    {
        var csv = new SourceFile(Samples.Path("wpi_excerpt.csv"), CsvDate, Downloaded);   // the file has the BOM

        var x = HubExtractor.Ports(csv, SourceZips.Codes);

        Assert.Equal(57, x.Rows.Count);
        Assert.Equal("2026-11-05", x.Manifest.Published);
    }

    [Fact]
    public void PortCountryFromUnLocode()
    {
        using var dir = new TempDir();
        var zip = PortsZip(dir, WpiRows(
            ("Port Sudan", "SD PZU", "Sudan", "Large"),
            ("Bonny", "NGBON", "Nigeria", "Medium"),                     // no space in the LOCODE
            ("Beshayer Oil Terminal", " ", "Sudan", "Medium"),            // no LOCODE: the country's name
            ("Wrong name but right code", "YE ADE", "Nowhere", "Large"),     // the LOCODE wins
            ("Nowhere Harbour", " ", "Atlantis", "Large"),                // neither: not placed
            ("Mystery", "ZZ ZZZ", "Lemuria", "Medium"),                   // an unknown code and an unknown name: not placed
            ("Small one", "SD SUA", "Sudan", "Small")));

        var x = HubExtractor.Ports(zip, SourceZips.Codes);

        Assert.Equal(("SDN", "SD PZU"), (x.Rows.Single(r => r.Name == "Port Sudan").Iso3, x.Rows.Single(r => r.Name == "Port Sudan").Extra["detail"]));
        Assert.Equal(("NGA", "NG BON"), (x.Rows.Single(r => r.Name == "Bonny").Iso3, x.Rows.Single(r => r.Name == "Bonny").Extra["detail"]));
        Assert.Equal("SDN", x.Rows.Single(r => r.Name == "Beshayer Oil Terminal").Iso3);
        Assert.Equal("YEM", x.Rows.Single(r => r.Name == "Wrong name but right code").Iso3);
        Assert.Equal(4, x.Rows.Count);
        // The two it cannot place are listed in the manifest, not dropped silently (C16).
        Assert.Equal(["Atlantis", "ZZ"], x.Manifest.NotPlaced.Select(n => n.Code));
        Assert.All(x.Manifest.NotPlaced, n => Assert.Contains("no unit in the boundary set", n.Reason, StringComparison.Ordinal));
        Assert.Equal(["Atlantis", "Lemuria"], x.Manifest.NotPlaced.Select(n => n.Name));
    }

    [Fact]
    public void ManifestsCiteTheSource()
    {
        using var dir = new TempDir();
        var airports = HubExtractor.Airports(Airports(), SourceZips.Codes).Manifest;
        Assert.Equal("ourairports", airports.Id);
        Assert.Equal("The Unlicense (public domain)", airports.Licence);
        Assert.Equal("AsOf", airports.DateMeaning);
        Assert.Equal("2026-11-24", airports.Published);   // the download date: the file has no date of its own
        Assert.Equal("2026-11-24", airports.Downloaded);
        Assert.Null(airports.DateText);
        Assert.StartsWith("https://raw.githubusercontent.com/davidmegginson/ourairports-data/", airports.SourceUrl, StringComparison.Ordinal);
        Assert.False(string.IsNullOrWhiteSpace(airports.Filter));
        Assert.Equal(64, airports.FileSha256.Length);
        Assert.Contains("OurAirports", airports.Attribution, StringComparison.Ordinal);

        var zip = PortsZip(dir);
        var ports = HubExtractor.Ports(zip, SourceZips.Codes).Manifest;
        Assert.Equal("wpi-ports", ports.Id);
        Assert.Equal("US National Geospatial-Intelligence Agency (NGA)", ports.Publisher);
        Assert.Equal("World Port Index (Pub 150)", ports.Dataset);
        Assert.Equal("Public domain (US government work, 17 U.S.C. 105)", ports.Licence);
        Assert.Equal("AsOf", ports.DateMeaning);
        Assert.Equal("2026-11-05", ports.Published);   // the CSV's date inside the zip, not the download day
        Assert.Equal("2026-11-24", ports.Downloaded);
        Assert.Equal("WPI.zip", ports.FileName);

        // Both load as bundled points with an "as of" citation (the Release-only rule is relaxed for AsOf).
        DatasetWriter.WritePoints(HubExtractor.Ports(zip, SourceZips.Codes), dir.File("wpi-ports"));
        var load = BundledPoints.Load("Hubs", dir.File("wpi-ports"), null, new ListLog());
        Assert.True(load.Loaded, load.Problem);
        Assert.Equal(CitationDate.AsOf, load.Citation!.DateMeaning);
        Assert.Equal(new DateTime(2026, 11, 5, 0, 0, 0, DateTimeKind.Utc), load.Citation.AsOfUtc);
        Assert.Equal(new DateTime(2026, 11, 24, 0, 0, 0, DateTimeKind.Utc), load.Citation.RetrievedUtc);
    }

    [Fact]
    public void ReleaseStillNeedsItsDateText()
    {
        using var dir = new TempDir();
        var m = HubExtractor.Airports(Airports(), SourceZips.Codes).Manifest with { DateMeaning = "Release" };
        var ex = Assert.Throws<InvalidDataException>(() => BundledPoints.Citation(m));
        Assert.Contains("dateText", ex.Message, StringComparison.Ordinal);
        Assert.Equal(CitationDate.AsOf, BundledPoints.Citation(m with { DateMeaning = "AsOf" }).DateMeaning);
        Assert.Throws<InvalidDataException>(() => BundledPoints.Citation(m with { DateMeaning = "Year" }));
    }

    [Fact]
    public void FromIso2UsesTheBoundarySet()
    {
        Assert.Equal("SDN", SourceZips.Codes.FromIso2("SD"));
        Assert.Equal("SDN", SourceZips.Codes.FromIso2(" sd "));
        Assert.Equal("NOR", SourceZips.Codes.FromIso2("NO"));   // Natural Earth's ISO_A2 is -99 for Norway
        Assert.Equal("FRA", SourceZips.Codes.FromIso2("FR"));
        Assert.Equal("AUS", SourceZips.Codes.FromIso2("AU"));   // not Ashmore and Cartier or the Indian Ocean Territories
        Assert.Null(SourceZips.Codes.FromIso2("ZZ"));
        Assert.Null(SourceZips.Codes.FromIso2(""));
    }
}
