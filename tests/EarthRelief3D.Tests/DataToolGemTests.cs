using System.IO;
using EarthRelief3D.Core.Feeds;
using EarthRelief3D.DataTool;
using static EarthRelief3D.Tests.SourceZips;

namespace EarthRelief3D.Tests;

/// <summary>
/// The <c>gem-power</c>, <c>gem-oilgas</c> and <c>gem-steel</c> commands (Task 7) on excerpts of at most 20 rows a sheet of the owner's real GEM
/// files (tests/Samples/gem_*_excerpt.json, CC BY 4.0): Kosovo's and Malta's power units, fields in Iraq, Kuwait and
/// Bahrain (and one in Canada), and ten steel plants.
/// </summary>
public class DataToolGemTests
{
    public const string PowerRelease = "September 2026 release";   // GEM's wording; "V2" is the file's only (R24)
    public const string OilGasRelease = "March 2026 release";
    public const string SteelRelease = "September 2026 (V1) release";

    private static PointExtract Power(double minMw = GemExtractor.PowerMinMw, int max = GemExtractor.MaxMarkers)
    {
        using var dir = new TempDir();
        return GemExtractor.Power(Gem(dir, "gem_power_excerpt.json", new DateOnly(2026, 9, 1)), PowerRelease, Codes, minMw, max, fileVersion: "V2");
    }

    private static PointExtract OilGas(int max = GemExtractor.MaxMarkers)
    {
        using var dir = new TempDir();
        return GemExtractor.OilGas(Gem(dir, "gem_oilgas_excerpt.json", new DateOnly(2026, 3, 1)), OilGasRelease, Codes, max);
    }

    private static PointExtract Steel(int max = GemExtractor.MaxMarkers)
    {
        using var dir = new TempDir();
        return GemExtractor.Steel(Gem(dir, "gem_steel_excerpt.json", new DateOnly(2026, 9, 1)), SteelRelease, Codes, GemExtractor.SteelMinMt, max);
    }

    private static PointRow Row(PointExtract e, string name) => Assert.Single(e.Rows, r => r.Name == name);

    // ---------------------------------------------------------------- power

    [Fact]
    public void PowerSumsUnitsPerPlantAndFilters500MW()
    {
        var e = Power();

        // Kosovo B: two operating units of 340 MW, neither 500 MW alone; Delimara: four oil/gas units, 547.8 MW.
        Assert.Equal(["Kosovo B power station", "Kosovo A power station", "Delimara power station"], e.Rows.Select(r => r.Name));
        var b = Row(e, "Kosovo B power station");
        Assert.Equal((680.0, "MW", "operating", "coal"), (b.Capacity!.Value, b.Unit, b.Status, b.Type));
        Assert.Equal("2", b.Extra["units"]);
        Assert.Equal(547.8, Row(e, "Delimara power station").Capacity!.Value, 6);
        // GEM's own code for Kosovo is XKX, which the boundary set lacks: placed by its name (Natural Earth's KOS).
        Assert.Equal("KOS", b.Iso3);
        Assert.Equal("MLT", Row(e, "Delimara power station").Iso3);
        Assert.Equal((42.69312, 21.056002), (b.Lat, b.Lon));
        // The small plants (Bajgora wind 103 MW, Ujmani hydro 35 MW, Kitka wind 32 MW) are under the filter.
        Assert.DoesNotContain(e.Rows, r => r.Capacity < 500);

        var m = e.Manifest;
        Assert.Equal("gem-power", m.Id);
        Assert.Equal("operating plants of 500 MW or more (units summed per plant)", m.Filter);
        Assert.Equal("Power plants: operating plants of 500 MW or more (units summed per plant)", Assert.Single(m.Series).Measures);
        Assert.DoesNotContain(m.Notes, n => n.StartsWith("P3-9 cap", StringComparison.Ordinal));   // 3 plants: no cap
    }

    [Fact]
    public void StatusFilterOperatingOnly()
    {
        var power = Power();
        // Kosovo A: units 3 to 5 operate (610 MW); units 1 and 2 are retired (190 MW more), not counted.
        Assert.Equal(610.0, Row(power, "Kosovo A power station").Capacity);
        // Kosovo C (500 MW, cancelled) and Ferizaj (500 MW, announced) are not on the map.
        Assert.DoesNotContain(power.Rows, r => r.Name is "Kosovo C power station" or "Ferizaj power plant");
        Assert.Equal(12, power.Manifest.FlagsSeen["Status operating"]);
        Assert.Equal(2, power.Manifest.FlagsSeen["Status retired"]);

        var oil = OilGas();
        // Homah and Jazza are discovered, Bina Bawi mothballed, Akkas Redevelopment in development, Khashman has no status.
        Assert.Equal(8, oil.Rows.Count);
        Assert.All(oil.Rows, r => Assert.Equal("operating", r.Status));
        Assert.DoesNotContain(oil.Rows, r => r.Name.StartsWith("Homah", StringComparison.Ordinal) || r.Name.StartsWith("Khashman", StringComparison.Ordinal));
        // Athabasca operates but has no coordinates: left out and counted.
        Assert.DoesNotContain(oil.Rows, r => r.Name.Contains("Athabasca", StringComparison.Ordinal));
        Assert.Equal(1, oil.Manifest.FlagsSeen["operating, no Latitude/Longitude: left out (1)"]);

        var steel = Steel();
        // POSCO Gwangyang: the operating BF/BOF line (22,999 thousand tonnes); its EAF under construction is not counted.
        var posco = Row(steel, "POSCO Gwangyang steel plant");
        Assert.Equal((22.999, "Mt/yr", "operating", "BF; BOF"), (posco.Capacity!.Value, posco.Unit, posco.Status, posco.Type));
        // Taranto still produces, with its retirement announced: counted, and its status says so.
        var taranto = Row(steel, "Acciaierie d'Italia Taranto steel plant");
        Assert.Equal((7.8, "operating pre-retirement"), (taranto.Capacity!.Value, taranto.Status));
        // Linz: 4,000 operating and 2,000 operating pre-retirement, summed; one row operates, so the plant does.
        Assert.Equal((6.0, "operating"), (Row(steel, "Voestalpine Stahl Linz steel plant").Capacity!.Value, Row(steel, "Voestalpine Stahl Linz steel plant").Status));
        // Under construction (Abba Steel), retired (KG Steel Dangjin), iron only (HyIron: DRI, no steel capacity), and 0.5 Mt (Alfa Acciai Catania).
        Assert.Equal(["POSCO Gwangyang steel plant", "Acciaierie d'Italia Taranto steel plant", "Voestalpine Stahl Linz steel plant",
            "Alfa Acciai Brescia steel plant", "Danieli ABS Pozzuolo del Friuli steel plant", "DaehanSteel Noksan steel plant"], steel.Rows.Select(r => r.Name));
        Assert.Equal(1.0, Row(steel, "DaehanSteel Noksan steel plant").Capacity);   // exactly 1 Mt/yr: kept
        Assert.Equal((34.920086, 127.74865), (posco.Lat, posco.Lon));   // from the "Coordinates" text cell, "34.9200860, 127.7486500"
    }

    [Fact]
    public void CapAt5000RaisesThresholdAndRecordsIt()
    {
        // Three plants pass 500 MW; with room for 2, the threshold rises in 100 MW steps until it fits: 600 MW leaves Kosovo A and B.
        var e = Power(max: 2);
        Assert.Equal(["Kosovo B power station", "Kosovo A power station"], e.Rows.Select(r => r.Name));
        Assert.Equal("operating plants of 600 MW or more (units summed per plant)", e.Manifest.Filter);
        Assert.Contains("filtered to operating plants of 600 MW or more", e.Manifest.Changes);
        Assert.Contains(e.Manifest.Notes, n => n == "P3-9 cap: 3 sites at 500 MW or more, over the 2 markers a layer draws; "
            + "the threshold was raised in steps of 100 MW to 600 MW, leaving 2.");
        Assert.Equal(GemExtractor.MaxMarkers, 5000);

        // Steel: 6 plants of 1 Mt/yr or more; with room for 4 the threshold rises in 0.5 Mt steps to 2 Mt/yr.
        var steel = Steel(max: 4);
        Assert.Equal(4, steel.Rows.Count);
        Assert.Equal("operating plants of 2 million tonnes of crude steel a year or more", steel.Manifest.Filter);

        // Oil and gas (ruling R23): 8 operating fields with coordinates; with room for 6, those GEM reports producing under
        // a threshold go, raised in 0.1 million boe/y steps: at 1.5, Siba (0.365) and Banan (1.428) go. Fields with no
        // usable production figure stay (Wafra's only figure is a 2028 forecast; Swara Tika's has no year; Marrat has none).
        var oil = OilGas(max: 6);
        Assert.Equal(6, oil.Rows.Count);
        Assert.DoesNotContain(oil.Rows, r => r.Name.StartsWith("Siba", StringComparison.Ordinal) || r.Name.StartsWith("Banan", StringComparison.Ordinal));
        Assert.Contains(oil.Rows, r => r.Name.StartsWith("Wafra", StringComparison.Ordinal) && r.Capacity is null);
        Assert.Equal("operating fields with coordinates, leaving out those GEM reports producing under 1.5 million barrels of oil equivalent a year",
            oil.Manifest.Filter);
        Assert.Contains(oil.Manifest.Notes, n => n.StartsWith("P3-9 cap: 8 operating fields", StringComparison.Ordinal) && n.Contains("ruling R23"));
        // Under the cap, no threshold at all.
        Assert.Equal("operating fields with coordinates", OilGas().Manifest.Filter);
    }

    [Fact]
    public void NonCommercialUnitsAreNeverBundled()
    {
        // Ruling R22: a unit with a TransitionZero id ("TZ: …", CC BY-NC 4.0 per the About sheet) is never bundled, nor is
        // its plant. The excerpt holds no such row (non-commercial data stays out of the repository too), so this test
        // gives Kitka's pre-construction unit 2 a made-up one: the whole plant goes, even with no size filter.
        using var dir = new TempDir();
        var src = Gem(dir, "gem_power_excerpt.json", new DateOnly(2026, 9, 1), sheets =>
        {
            var rows = sheets.Single(s => s.Name == GemExtractor.PowerSheet).Rows;
            int ids = Array.IndexOf(rows[0], "Other IDs (unit/phase)"), name = Array.IndexOf(rows[0], "Plant / Project name"), unit = Array.IndexOf(rows[0], "Unit / Phase name");
            rows.Single(r => r[name] as string == "Kitka wind farm" && r[unit] is 2.0)[ids] = "WKSL: 1,TZ: 999999";
        });
        var e = GemExtractor.Power(src, PowerRelease, Codes, minMw: 1, fileVersion: "V2");
        Assert.DoesNotContain(e.Rows, r => r.Name == "Kitka wind farm");
        Assert.Contains(e.Rows, r => r.Name == "Bajgora wind farm" && r.Capacity == 103);
        Assert.Contains(e.Rows, r => r.Name == "Ujmani hydroelectric plant" && r.Capacity == 35);
        Assert.Equal("Kitka wind farm", Assert.Single(e.Manifest.Dropped, d => d.Reason.Contains("CC BY-NC 4.0", StringComparison.Ordinal)).Name);
        Assert.Contains(e.Manifest.Notes, n => n.StartsWith("Ruling R22", StringComparison.Ordinal) && n.Contains("left out whole: 1 plants"));
        // Without the made-up id, Kitka's operating unit 1 (32 MW) is there; unit 2 is pre-construction.
        Assert.Contains(Power(minMw: 1).Rows, r => r.Name == "Kitka wind farm" && r.Capacity == 32);
    }

    [Fact]
    public void OilAndGasProductionAsGemGivesIt()
    {
        var e = OilGas();
        var rumaila = Assert.Single(e.Rows, r => r.Name == "Rumaila Oil Field (Iraq)");
        Assert.Equal(("IRQ", "oil", 518.3, "million boe/y"), (rumaila.Iso3, rumaila.Type, rumaila.Capacity!.Value, rumaila.Unit));
        Assert.Equal("oil 518 million bbl/y (2022)", rumaila.Extra["detail"]);
        // Khor Mor: gas 5,426.4 million m³ at 6,000 cubic feet per boe, plus 5.548 million bbl of condensate.
        var khorMor = Assert.Single(e.Rows, r => r.Name.StartsWith("Khor Mor", StringComparison.Ordinal));
        Assert.Equal(5426.4 * 35.3147 / 6000 + 5.548, khorMor.Capacity!.Value, 3);
        Assert.Equal("condensate 5.5 million bbl/y, gas 5,426 million m³/y (2025)", khorMor.Extra["detail"]);
        // Burgan: GEM gives "hydrocarbons" in million boe/y.
        Assert.Equal("hydrocarbons 620 million boe/y (2023)", Assert.Single(e.Rows, r => r.Name.StartsWith("Burgan", StringComparison.Ordinal)).Extra["detail"]);
        // Wafra's only figure is for 2028, a year not over at the March 2026 release: not used.
        var wafra = Assert.Single(e.Rows, r => r.Name.StartsWith("Wafra", StringComparison.Ordinal));
        Assert.Equal((null, "", ""), (wafra.Capacity, wafra.Unit, wafra.Extra["detail"]));
        Assert.Equal(1, e.Manifest.FlagsSeen["production row with Data Year 2026 or later (not over at the release): not used"]);
        Assert.Equal(1, e.Manifest.FlagsSeen["production row with no Data Year: not used"]);
    }

    [Fact]
    public void ManifestCreditsGemWithItsLicenceAndTheChanges()
    {
        var power = Power().Manifest;
        Assert.Equal("Global Energy Monitor, Global Integrated Power Tracker, September 2026 release.", power.Attribution);   // the About sheet's, verbatim (R24)
        Assert.Equal(("Global Energy Monitor", "CC BY 4.0", "https://creativecommons.org/licenses/by/4.0/"), (power.Publisher, power.Licence, power.LicenceUrl));
        Assert.Equal(("Release", "September 2026 release (file V2)", "September 2026 release, file V2"), (power.DateMeaning, power.DateText, power.Version));
        Assert.Equal("filtered to operating plants of 500 MW or more (units summed per plant); operating units summed per plant; "
            + "plants with a unit sourced from TransitionZero (CC BY-NC 4.0) left out", power.Changes);
        Assert.Contains(power.Notes, n => n.StartsWith("Licence: CC BY 4.0", StringComparison.Ordinal) && n.Contains("Changed by EarthRelief3D"));
        // The workbook says "September 2026 release": the version is from the file's name, and the manifest says so.
        Assert.Contains(power.Notes, n => n.StartsWith("Release: the workbook names \"September 2026\" without \"(V2)\"", StringComparison.Ordinal));
        Assert.Contains(power.Notes, n => n.StartsWith("About sheet: Copyright © Global Energy Monitor.", StringComparison.Ordinal));
        Assert.Equal("2026-09-01", power.Published);

        Assert.Equal("Global Energy Monitor, Global Oil and Gas Extraction Tracker, March 2026 release.", OilGas().Manifest.Attribution);
        var steel = Steel().Manifest;
        Assert.Equal("Global Energy Monitor, Global Iron and Steel Tracker, September 2026 (V1) release.", steel.Attribution);
        Assert.DoesNotContain(steel.Notes, n => n.StartsWith("Release:", StringComparison.Ordinal));   // the workbook names (V1)
        Assert.Contains("converted from thousand to million tonnes a year", steel.Changes);
    }

    [Fact]
    public void TheWrongFileOrReleaseIsRefused()
    {
        using var dir = new TempDir();
        var power = Gem(dir, "gem_power_excerpt.json", new DateOnly(2026, 9, 1));
        Assert.Throws<InvalidDataException>(() => GemExtractor.Power(power, "March 2026 Release", Codes));   // the About sheet names September
        var noSheet = Gem(dir, "gem_power_excerpt.json", new DateOnly(2026, 9, 1), sheets => sheets.RemoveAll(s => s.Name == GemExtractor.PowerSheet));
        Assert.Throws<InvalidDataException>(() => GemExtractor.Power(noSheet, PowerRelease, Codes));
    }

    [Fact]
    public void WrittenExtractLoadsAsTheAppLoadsIt()
    {
        using var dir = new TempDir();
        var e = Power();
        DatasetWriter.WritePoints(e, dir.File("gem-power"));
        var load = BundledPoints.Load("GEM", dir.File("gem-power"), null, new ListLog());
        Assert.True(load.Loaded, load.Problem);
        Assert.Equal(e.Rows.Select(r => (r.Name, r.Iso3, r.Lat, r.Lon, r.Capacity)), load.Points.Select(p => (p.Name, p.Iso3, p.Lat, p.Lon, p.Capacity)));
        Assert.Equal("September 2026 release (file V2)", load.Citation!.DateText);
        Assert.Equal(Core.Model.CitationDate.Release, load.Citation.DateMeaning);

        // A code the boundary set lacks is not drawn and is listed as not placed, with how many sites.
        var noMalta = BundledPoints.Load("GEM", dir.File("gem-power"), iso => iso != "MLT", new ListLog());
        Assert.DoesNotContain(noMalta.Points, p => p.Iso3 == "MLT");
        Assert.Contains(noMalta.NotPlaced, n => n.Code == "MLT" && n.Reason.Contains("1 site not drawn", StringComparison.Ordinal));
    }

    // ---------------------------------------------------------------- renewables (ruling R25)

    /// <summary>Changes one unit's cell in the excerpt's "Power facilities" sheet (a test's own variant of real rows).</summary>
    private static void SetUnit(List<(string Name, List<object?[]> Rows)> sheets, string plant, object? unit, string column, object value)
    {
        var rows = sheets.Single(s => s.Name == GemExtractor.PowerSheet).Rows;
        int name = Array.IndexOf(rows[0], "Plant / Project name"), unitCol = Array.IndexOf(rows[0], "Unit / Phase name"), col = Array.IndexOf(rows[0], column);
        Assert.True(col >= 0, column);
        rows.Single(r => r.Length > name && r[name] as string == plant && (unit is null || Equals(r[unitCol], unit)))[col] = value;
    }

    private static PointExtract Renewables(Action<List<(string Name, List<object?[]> Rows)>>? edit = null, int max = GemExtractor.MaxMarkers)
    {
        using var dir = new TempDir();
        return GemExtractor.Renewables(Gem(dir, "gem_power_excerpt.json", new DateOnly(2026, 9, 1), edit), PowerRelease, Codes, max, fileVersion: "V2");
    }

    [Fact]
    public void RenewablesKeepOnlyRenewableFuelsFrom100MW()
    {
        Assert.Equal(["wind", "utility-scale solar", "hydropower", "geothermal", "bioenergy"], GemExtractor.RenewableTypes);
        var e = Renewables();
        // Bajgora wind farm (103 MW) only: Ujmani hydro is 35 MW, Kitka wind 32 MW operating, Zhur hydro (305 MW) cancelled,
        // and the coal and oil/gas plants (Kosovo A and B, Delimara) are not renewable.
        var bajgora = Row(e, "Bajgora wind farm");
        Assert.Single(e.Rows);
        Assert.Equal((103.0, "wind", "operating", "KOS"), (bajgora.Capacity!.Value, bajgora.Type, bajgora.Status, bajgora.Iso3));
        var m = e.Manifest;
        Assert.Equal("gem-renewables", m.Id);
        Assert.Equal("operating renewable plants of 100 MW or more (renewable units summed per plant)", m.Filter);
        Assert.Equal(new Dictionary<string, int> { ["100 MW"] = 1 }, m.Thresholds);
        // The same credit as gem-power (R24).
        Assert.Equal("Global Energy Monitor, Global Integrated Power Tracker, September 2026 release.", m.Attribution);
        Assert.Equal(("September 2026 release (file V2)", "CC BY 4.0"), (m.DateText, m.Licence));
        Assert.StartsWith("filtered to operating renewable plants of 100 MW or more", m.Changes);
        Assert.Contains("plants with a unit sourced from TransitionZero (CC BY-NC 4.0) left out", m.Changes);
    }

    [Fact]
    public void RenewablesThresholdStepsThrough150200And300MW()
    {
        void Bigger(List<(string Name, List<object?[]> Rows)> s)
        {
            SetUnit(s, "Ujmani hydroelectric plant", null, "Capacity (MW)", 160.0);
            SetUnit(s, "Kitka wind farm", 1.0, "Capacity (MW)", 210.0);
        }
        // Three plants pass 100 MW (Bajgora 103, Ujmani 160, Kitka 210). With room for one, 150 leaves two, 200 leaves one.
        var e = Renewables(Bigger, max: 1);
        Assert.Equal("Kitka wind farm", Assert.Single(e.Rows).Name);
        Assert.Equal(new Dictionary<string, int> { ["100 MW"] = 3, ["150 MW"] = 2, ["200 MW"] = 1 }, e.Manifest.Thresholds);
        Assert.Equal("operating renewable plants of 200 MW or more (renewable units summed per plant)", e.Manifest.Filter);
        Assert.Contains(e.Manifest.Notes, n => n.Contains("100 MW: 3 plants; 150 MW: 2 plants; 200 MW: 1 plants. Kept at 200 MW.", StringComparison.Ordinal));
        // With room for none, the steps run on to 300 MW.
        Assert.Equal(["100 MW", "150 MW", "200 MW", "300 MW"], Renewables(Bigger, max: 0).Manifest.Thresholds!.Keys);
    }

    [Fact]
    public void RenewablesSumOnlyTheRenewableOperatingUnitsPerPlant()
    {
        var e = Renewables(s =>
        {
            // Kitka's unit 2 goes into operation at 80 MW: 32 + 80 = 112 MW, summed per plant.
            SetUnit(s, "Kitka wind farm", 2.0, "Status", "operating");
            SetUnit(s, "Kitka wind farm", 2.0, "Capacity (MW)", 80.0);
            // Kosovo A's unit 3 is made bioenergy: 200 MW counted here; its coal units 4 and 5 (410 MW) are not.
            SetUnit(s, "Kosovo A power station", "Unit 3", "Type", "bioenergy");
        });
        var kitka = Row(e, "Kitka wind farm");
        Assert.Equal((112.0, "2"), (kitka.Capacity!.Value, kitka.Extra["units"]));
        var kosovoA = Row(e, "Kosovo A power station");
        Assert.Equal((200.0, "bioenergy", ""), (kosovoA.Capacity!.Value, kosovoA.Type, kosovoA.Extra["types"]));
        Assert.Contains(e.Manifest.Notes, n => n.StartsWith("1 kept plants also have operating units of other fuels", StringComparison.Ordinal));
    }

    [Fact]
    public void RenewablesLeaveOutPlantsWithATransitionZeroUnit()
    {
        // R22 still applies: Bajgora with a made-up TransitionZero id is left out whole and listed as dropped.
        var e = Renewables(s => SetUnit(s, "Bajgora wind farm", null, "Other IDs (unit/phase)", "TZ: 999999"));
        Assert.Empty(e.Rows);
        Assert.Equal("Bajgora wind farm", Assert.Single(e.Manifest.Dropped).Name);
        Assert.Contains("CC BY-NC 4.0", e.Manifest.Dropped[0].Reason);
    }

    [Fact]
    public void APlantsTypeIsItsLargestFuelNotTheFirstListed()
    {
        // Delimara's first unit (2A, 75 MW) is made hydropower; its other units are oil/gas (472.8 MW): the plant is oil/gas.
        using var dir = new TempDir();
        var src = Gem(dir, "gem_power_excerpt.json", new DateOnly(2026, 9, 1), s => SetUnit(s, "Delimara power station", "Unit 2A", "Type", "hydropower"));
        var delimara = Row(GemExtractor.Power(src, PowerRelease, Codes, fileVersion: "V2"), "Delimara power station");
        Assert.Equal(("oil/gas", "oil/gas 472.8 MW; hydropower 75 MW"), (delimara.Type, delimara.Extra["types"]));
    }
}