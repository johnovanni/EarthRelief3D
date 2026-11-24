using System.IO;
using EarthRelief3D.Core.Feeds;
using EarthRelief3D.Core.Model;
using GlobeCore.Feeds;

namespace EarthRelief3D.Tests;

/// <summary>Loading a bundled extract (data\datasets\&lt;id&gt;: data.csv and manifest.json, ruling P3-10) into series.</summary>
public class BundledDatasetTests
{
    private const string Manifest = """
        {
          "id": "fao-food-security",
          "publisher": "Food and Agriculture Organization of the United Nations (FAO)",
          "dataset": "FAOSTAT: Suite of Food Security Indicators",
          "version": "FS, updated 2026-11-23",
          "sourceUrl": "https://bulks-faostat.fao.org/production/Food_Security_Data_E_All_Data_(Normalized).zip",
          "pageUrl": "https://www.fao.org/faostat/en/#data/FS",
          "licence": "CC BY 4.0",
          "attribution": "FAO. 2026. FAOSTAT: Suite of Food Security Indicators. Accessed on 23 November 2026.",
          "dateMeaning": "EstimateYear",
          "asOf": "each country's latest 3-year average, 2020-2022 to 2023-2025",
          "published": "2026-11-23",
          "downloaded": "2026-11-23",
          "fileName": "Food_Security_Data_E_All_Data_(Normalized).zip",
          "fileSha256": "dc5b604debef710317979d563028b13daf2b8a0e1d5b6a9d2761c1001e114a9b",
          "command": "DataTool fao <downloads>/Food_Security_Data_E_All_Data_(Normalized).zip --published 2026-11-23",
          "series": [
            { "id": "fao.undernourishment", "measures": "Prevalence of undernourishment", "unit": "%", "sourceItem": "x", "countries": 3 },
            { "id": "fao.energyAdequacy", "measures": "Average dietary energy supply adequacy", "unit": "%", "sourceItem": "y", "countries": 1 }
          ],
          "notPlaced": [ { "code": "M49 772", "name": "Tokelau", "reason": "no country in the boundary set has this M49 code" } ],
          "dropped": [],
          "qualifiersSeen": { "<2.5": 1 },
          "flagsSeen": { "E": 4 },
          "notes": [ "a note" ]
        }
        """;

    private const string Data = """
        series,iso3,year,value,qualifier,period,m49,area
        fao.undernourishment,HTI,2025,51.4,,2023-2025,332,Haiti
        fao.undernourishment,SDN,2022,10.9,,2020-2022,729,Sudan
        fao.undernourishment,ISL,2025,2.5,<2.5,2023-2025,352,Iceland
        fao.energyAdequacy,HTI,2025,84,,2023-2025,332,Haiti
        fao.undernourishment,ZZZ,2025,9,,2023-2025,999,Nowhere
        """;

    private static string Folder(TempDir dir, string manifest = Manifest, string data = Data)
    {
        var folder = dir.File("fao-food-security");
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, BundledDataset.ManifestFile), manifest);
        File.WriteAllText(Path.Combine(folder, BundledDataset.DataFile), data);
        return folder;
    }

    private static bool Placed(string iso3) => CountryResolver.LoadDefault().ByIso3(iso3) is not null;

    [Fact]
    public void CitationComesFromManifest()
    {
        using var dir = new TempDir();
        var log = new ListLog();
        var load = BundledDataset.Load("FAO", Folder(dir), Placed, log);

        Assert.True(load.Loaded);
        Assert.Equal(["fao.undernourishment", "fao.energyAdequacy"], load.Series.Select(s => s.Id));
        var pou = load.Series[0];
        Assert.Equal(("Prevalence of undernourishment", "%"), (pou.Measures, pou.Unit));
        var hti = pou.For("HTI")!;
        Assert.Equal((51.4, 2025, (string?)null), (hti.Value, hti.Year, hti.Qualifier));
        Assert.Equal("<2.5", pou.For("ISL")!.Qualifier);
        Assert.Equal(2022, pou.For("SDN")!.Year);

        var c = hti.Citation;
        Assert.Equal("Food and Agriculture Organization of the United Nations (FAO)", c.Publisher);
        Assert.Equal("FAOSTAT: Suite of Food Security Indicators", c.Dataset);
        Assert.Equal("FS, updated 2026-11-23", c.Version);
        Assert.Equal(new Uri("https://www.fao.org/faostat/en/#data/FS"), c.Url);
        Assert.Equal("CC BY 4.0", c.Licence);
        Assert.StartsWith("FAO. 2026.", c.Attribution);
        // The figure's year, never the download date, is the as-of (an estimate year: the day is never shown).
        Assert.Equal(CitationDate.EstimateYear, c.DateMeaning);
        Assert.Equal(new DateTime(2025, 12, 31, 0, 0, 0, DateTimeKind.Utc), c.AsOfUtc);
        Assert.Equal(new DateTime(2022, 12, 31, 0, 0, 0, DateTimeKind.Utc), pou.For("SDN")!.Citation.AsOfUtc);
        Assert.Equal(new DateTime(2026, 11, 23, 0, 0, 0, DateTimeKind.Utc), c.RetrievedUtc);

        // A code with no unit in the boundary set is logged and listed with the manifest's, never dropped silently.
        Assert.Null(pou.For("ZZZ"));
        Assert.Equal(["M49 772", "ZZZ"], load.NotPlaced.Select(n => n.Code));
        Assert.Contains(log.Lines, l => l.StartsWith("WARN FAO", StringComparison.Ordinal) && l.Contains("ZZZ"));

        // Its status dot: the data's date and that it ships with the app.
        var status = BundledDataset.Status(load, cite => "estimate year " + cite.AsOfUtc.Year);
        Assert.Equal(("FAO", FeedState.Ok), (status.Name, status.State));
        Assert.Equal("estimate year 2025 · bundled; updated with the app", status.Note);
    }

    [Fact]
    public void ABaselineIsDatedByTheManifestsWords()
    {
        // I3: Aqueduct's figures are a modelled 1979-2019 baseline published in 2023: the manifest's dateText says so, and
        // the citation carries it; a Baseline manifest without it is not loaded.
        using var dir = new TempDir();
        var manifest = Manifest.Replace("\"dateMeaning\": \"EstimateYear\",", "\"dateMeaning\": \"Baseline\", \"dateText\": \"baseline 1979–2019 (published 2023)\",");
        var load = BundledDataset.Load("FAO", Folder(dir, manifest), Placed, new ListLog());
        Assert.True(load.Loaded, load.Problem);
        var c = load.Series[0].For("HTI")!.Citation;
        Assert.Equal(CitationDate.Baseline, c.DateMeaning);
        Assert.Equal("baseline 1979–2019 (published 2023)", c.DateText);
        Assert.Equal("baseline 1979–2019 (published 2023)", App.Presentation.CrisisText.DataDate(c, DateTime.UtcNow));
        Assert.Equal("baseline 1979–2019 (published 2023)", App.Presentation.CrisisText.SeriesDate(c, DateTime.UtcNow));

        using var dir2 = new TempDir();
        var bare = Manifest.Replace("\"dateMeaning\": \"EstimateYear\",", "\"dateMeaning\": \"Baseline\",");
        var log = new ListLog();
        Assert.False(BundledDataset.Load("FAO", Folder(dir2, bare), Placed, log).Loaded);
        Assert.Contains(log.Lines, l => l.Contains("dateText"));

        // C4: a full year is "year 2025", never an estimate.
        var year = c with { DateMeaning = CitationDate.Year, DateText = null, AsOfUtc = new DateTime(2025, 12, 31, 0, 0, 0, DateTimeKind.Utc) };
        Assert.Equal("year 2025", App.Presentation.CrisisText.DataDate(year, DateTime.UtcNow));
    }

    [Fact]
    public void MissingFolderIsLoggedNotFatal()
    {
        using var dir = new TempDir();
        var log = new ListLog();
        var load = BundledDataset.Load("US EIA", dir.File("eia-crude-oil"), Placed, log);

        Assert.False(load.Loaded);
        Assert.Empty(load.Series);
        Assert.Contains(log.Lines, l => l.StartsWith("WARN US EIA", StringComparison.Ordinal));
        var status = BundledDataset.Status(load, _ => throw new InvalidOperationException("no citation to date"));
        Assert.Equal(FeedState.Attention, status.State);
        Assert.EndsWith(BundledDataset.StatusNote, status.Note);
    }

    [Fact]
    public void ABrokenFileIsLoggedNotFatal()
    {
        using var dir = new TempDir();
        var log = new ListLog();
        var load = BundledDataset.Load("FAO", Folder(dir, data: "series,iso3,year\nfao.undernourishment,HTI,2025\n"), Placed, log);
        Assert.False(load.Loaded);
        Assert.Empty(load.Series);
        Assert.Contains(log.Lines, l => l.StartsWith("WARN FAO", StringComparison.Ordinal));
    }

    // ---------------------------------------------------------------- the committed extracts (hand checks, pinned)

    private static BundledDatasetLoad Committed(string name, string folder)
    {
        var load = BundledDataset.Load(name, Path.Combine(BundledDataset.DatasetsDir, folder), Placed, new ListLog());
        Assert.True(load.Loaded, load.Problem);
        return load;
    }

    [Fact]
    public void CommittedFaoExtractHasTheHandCheckedCountries()
    {
        var load = Committed("FAO", "fao-food-security");
        var pou = Assert.Single(load.Series, s => s.Id == "fao.undernourishment");
        Assert.Single(load.Series, s => s.Id == "fao.energyAdequacy");
        Assert.True(pou.Values.Count > 150);
        // Sudan's newest published range is 2020-2022 (later ranges are missing in FAO's file); Haiti has 2023-2025.
        Assert.Equal(2022, pou.For("SDN")!.Year);
        Assert.Equal(2025, pou.For("HTI")!.Year);
        Assert.Null(pou.For("YEM"));   // FAO publishes no undernourishment figure for Yemen (every range flagged O, missing)
        Assert.Equal(1, pou.Values.Count(v => v.Iso3 == "CHN"));
        Assert.All(pou.Values.Where(v => v.Qualifier is not null), v => Assert.StartsWith("<", v.Qualifier));
        Assert.All(load.Manifest!.Notes.Where(n => n.StartsWith("Check", StringComparison.Ordinal)), n => Assert.DoesNotContain("FAILED", n));
    }

    [Fact]
    public void CommittedEiaExtractHasTheTopFiveProducers()
    {
        var oil = Assert.Single(Committed("US EIA", "eia-crude-oil").Series);
        Assert.Equal("eia.crudeOil", oil.Id);
        Assert.Equal(["USA", "RUS", "SAU", "CAN", "IRQ"], oil.Values.OrderByDescending(v => v.Value).Take(5).Select(v => v.Iso3));
        Assert.Equal(2025, oil.For("USA")!.Year);
        Assert.Equal(CitationDate.Year, oil.For("USA")!.Citation.DateMeaning);   // C4
    }

    [Fact]
    public void CommittedAqueductExtractUsesWrisLabels()
    {
        var bws = Assert.Single(Committed("WRI Aqueduct", "aqueduct-water-stress").Series);
        Assert.Equal("wri.bws", bws.Id);
        Assert.Equal("category", bws.Unit);
        var scale = App.Presentation.ShadingScales.For(App.Presentation.ShadingLayer.WaterStress);
        // Every shaded value's label (WRI's own, in the qualifier) is the scale's label for its class.
        foreach (var v in bws.Values.Where(v => scale.ClassOf(v.Value) is not null))
            Assert.Equal(scale.ClassLabels[scale.ClassOf(v.Value)!.Value], v.Qualifier);
        Assert.Equal("Extremely High (>80%)", bws.For("YEM")!.Qualifier);
        Assert.Equal("Low - Medium (10-20%)", bws.For("SDN")!.Qualifier);
        // I3: dated as the modelled baseline it is, everywhere the date is shown.
        var c = bws.For("SDN")!.Citation;
        Assert.Equal(CitationDate.Baseline, c.DateMeaning);
        Assert.Equal("baseline 1979–2019 (published 2023)", App.Presentation.CrisisText.DataDate(c, DateTime.UtcNow));
    }
}
