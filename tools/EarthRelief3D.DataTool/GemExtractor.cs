using System.Globalization;
using EarthRelief3D.Core.Feeds;

namespace EarthRelief3D.DataTool;

/// <summary>
/// The three Global Energy Monitor trackers (Task 7, docs/data-sources.md §11) to point extracts in
/// <c>data\datasets\gem-power</c>, <c>gem-oilgas</c> and <c>gem-steel</c> (P3-9: each layer at most
/// <see cref="MaxMarkers"/> markers, with its filter in the manifest and the legend). GEM's data is CC BY 4.0
/// (https://globalenergymonitor.org/creative-commons-license, read 2026-11-24): each manifest gives GEM's recommended
/// citation, the licence and what the extract changed.
/// </summary>
public static class GemExtractor
{
    /// <summary>The marker cap per layer (P3-9; the app's <c>PointMarkerLayer.MaxMarkers</c>).</summary>
    public const int MaxMarkers = 5000;
    public const string Publisher = "Global Energy Monitor";
    public const string Licence = "CC BY 4.0";
    public const string LicenceUrl = "https://creativecommons.org/licenses/by/4.0/";
    /// <summary>GEM's own licence page, which gives the full CC BY 4.0 text (read 2026-11-24).</summary>
    public const string GemLicencePage = "https://globalenergymonitor.org/creative-commons-license";

    public const string PowerFolder = "gem-power";
    public const string OilGasFolder = "gem-oilgas";
    public const string SteelFolder = "gem-steel";

    public const string PowerDataset = "Global Integrated Power Tracker";
    public const string OilGasDataset = "Global Oil and Gas Extraction Tracker";
    public const string SteelDataset = "Global Iron and Steel Tracker";

    public const string PowerPage = "https://globalenergymonitor.org/projects/global-integrated-power-tracker/";
    public const string OilGasPage = "https://globalenergymonitor.org/projects/global-oil-gas-extraction-tracker/";
    public const string SteelPage = "https://globalenergymonitor.org/projects/global-iron-and-steel-tracker/";
    public const string PowerDownload = "https://globalenergymonitor.org/projects/global-integrated-power-tracker/#download";
    public const string OilGasDownload = "https://globalenergymonitor.org/projects/global-oil-gas-extraction-tracker/#download";
    public const string SteelDownload = "https://globalenergymonitor.org/projects/global-iron-and-steel-tracker/#download";

    /// <summary>Power: operating plants of this many MW or more (P3-9), units summed per plant.</summary>
    public const double PowerMinMw = 500;
    /// <summary>When the cap is passed, the power threshold rises in these steps.</summary>
    public const double PowerStepMw = 100;
    /// <summary>Steel: operating plants of 1 Mt/yr or more (P3-9).</summary>
    public const double SteelMinMt = 1;
    public const double SteelStepMt = 0.5;

    // GEM's columns, as in the September 2026 (V2) power file, the March 2026 oil and gas file and the September 2026 (V1)
    // plant-level steel file (checked with "DataTool describe" on 2026-11-24; docs/data-sources.md §11).
    public const string PowerSheet = "Power facilities";
    public const string PowerCountrySheet = "Regions, area, and countries";
    private static readonly string[] PowerColumns =
    [
        "Type", "Country/area", "Plant / Project name", "Unit / Phase name", "Capacity (MW)", "Status", "Latitude", "Longitude",
        "Location accuracy", "Other IDs (unit/phase)", "GEM location ID", "GEM unit/phase ID",
    ];
    public const string OilGasSheet = "Field-level main data";
    private static readonly string[] OilGasColumns =
        ["Unit ID", "Unit Name", "Fuel type", "Country/Area", "Status", "Latitude", "Longitude", "Location accuracy", "Onshore/Offshore", "Production Type"];
    public const string SteelPlantSheet = "Plant data";
    public const string SteelCapacitySheet = "Plant capacities and status";
    private static readonly string[] SteelPlantColumns = ["GEM plant ID", "Plant name (English)", "Country/area", "Coordinates", "Coordinate accuracy"];
    private static readonly string[] SteelCapacityColumns = ["GEM plant ID", "Status", "Nominal crude steel capacity (ttpa)", "Main production equipment"];

    /// <summary>GEM's statuses counted as operating: "operating", and for steel "operating pre-retirement" (still producing).</summary>
    public static bool IsOperating(string status) => status is "operating" or "operating pre-retirement";

    /// <summary>
    /// GEM's recommended citation, publisher first, with the release as GEM words it ("Global Energy Monitor, Global
    /// Integrated Power Tracker, September 2026 release."; ruling R24). A version GEM gives only in the file's name (power's
    /// "V2") is not part of it: it goes in the manifest's version and date text ("September 2026 release (file V2)").
    /// </summary>
    public static string Citation(string dataset, string release) => $"{Publisher}, {dataset}, {release}.";

    // ------------------------------------------------------------------------------------------------ power

    private sealed class Plant
    {
        public required string Id { get; init; }
        public string Name = "", Country = "", Accuracy = "";
        public double Lat, Lon;
        public bool HasPosition, NonCommercial;
        public readonly Dictionary<string, double> ByType = new(StringComparer.Ordinal);
        public readonly Dictionary<string, double> ByCountry = new(StringComparer.Ordinal);
        public int Units;
        public bool Moved;
        /// <summary>Operating capacity of the types not counted (renewables: a plant's fossil units), per type.</summary>
        public Dictionary<string, double> Others = new(StringComparer.Ordinal);
        public double Capacity => ByType.Values.Sum();
    }

    /// <summary>The power workbook read into plants: each plant's operating units of the types asked for, summed.</summary>
    private sealed record PlantRead(Dictionary<string, Plant> Plants, Dictionary<string, int> Statuses, Dictionary<string, int> Flags, int Rows,
        Dictionary<string, string> IsoByName, List<string> About);

    /// <summary>
    /// Reads the "Power facilities" sheet: units whose Type passes <paramref name="includeType"/> and whose Status is
    /// "operating", summed per GEM location ID. A plant with any unit (of any type or status) carrying a TransitionZero id
    /// is marked non-commercial (R22). Operating units of other types are kept apart, per type, for the notes.
    /// </summary>
    private static PlantRead ReadPlants(SourceFile src, string release, string? fileVersion, string source, Func<string, bool> includeType)
    {
        var about = CheckAbout(src, release, fileVersion, source);
        var isoByName = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var r in GemWorkbook.Rows(src.Path, PowerCountrySheet, ["GEM Standard Country Name/Area", "ISO-alpha3 Code"], source))
            if (r.Text("ISO-alpha3 Code") is { Length: 3 } iso) isoByName.TryAdd(r.Text("GEM Standard Country Name/Area"), iso);

        var statuses = new Dictionary<string, int>(StringComparer.Ordinal);
        var flags = new Dictionary<string, int>(StringComparer.Ordinal);
        var plants = new Dictionary<string, Plant>(StringComparer.Ordinal);
        var tainted = new HashSet<string>(StringComparer.Ordinal);
        var others = new Dictionary<string, Dictionary<string, double>>(StringComparer.Ordinal);
        int rows = 0;
        foreach (var r in GemWorkbook.Rows(src.Path, PowerSheet, PowerColumns, source))
        {
            rows++;
            var id = r.Text("GEM location ID");
            if (id.Length == 0) throw new InvalidDataException($"{source}: row {r.RowNumber} has no GEM location ID");
            if (r.Text("Other IDs (unit/phase)").Split(',').Any(x => x.Trim().StartsWith("TZ:", StringComparison.Ordinal))) tainted.Add(id);
            var type = r.Text("Type");
            var status = r.Text("Status");
            if (!includeType(type))
            {
                if (status == "operating" && r.Number("Capacity (MW)") is { } other)
                {
                    if (!others.TryGetValue(id, out var o)) others[id] = o = new(StringComparer.Ordinal);
                    o[type] = o.GetValueOrDefault(type) + other;
                }
                continue;
            }
            Tally.Add(statuses, status);
            if (status != "operating") continue;
            var mw = r.Number("Capacity (MW)");
            if (mw is null) { Tally.Add(flags, $"operating unit with Capacity (MW) \"{r.Text("Capacity (MW)")}\": not counted"); continue; }
            if (!plants.TryGetValue(id, out var p))
                plants[id] = p = new Plant { Id = id, Name = r.Text("Plant / Project name"), Accuracy = r.Text("Location accuracy") };
            p.Units++;
            p.ByType[type] = p.ByType.GetValueOrDefault(type) + mw.Value;
            var country = r.Text("Country/area");
            p.ByCountry[country] = p.ByCountry.GetValueOrDefault(country) + mw.Value;
            if (r.Number("Latitude") is { } lat && r.Number("Longitude") is { } lon)
            {
                if (!p.HasPosition) (p.Lat, p.Lon, p.HasPosition) = (lat, lon, true);
                else if (Math.Abs(lat - p.Lat) > 0.05 || Math.Abs(lon - p.Lon) > 0.05) p.Moved = true;
            }
        }
        if (plants.Count == 0) throw new InvalidDataException($"{source}: no operating units found (format changed)");
        foreach (var p in plants.Values)
        {
            p.NonCommercial = tainted.Contains(p.Id);
            p.Country = p.ByCountry.MaxBy(kv => kv.Value).Key;
            if (others.TryGetValue(p.Id, out var o)) p.Others = o;
        }
        return new PlantRead(plants, statuses, flags, rows, isoByName, about);
    }

    /// <summary>The kept plants as rows (largest first), the codes with no country, the types tally and the R22 and position notes.</summary>
    private static (List<PointRow> Rows, List<ManifestCode> NotPlaced, Dictionary<string, int> Types, List<ManifestCode> Dropped) PlantRows(
        PlantRead read, CountryCodes codes, double threshold)
    {
        var dropped = new List<ManifestCode>();
        var eligible = read.Plants.Values.Where(p => !p.NonCommercial).ToList();
        foreach (var p in read.Plants.Values.Where(p => p.NonCommercial && p.Capacity >= threshold).OrderByDescending(p => p.Capacity))
            dropped.Add(new ManifestCode(p.Id, p.Name, string.Create(CultureInfo.InvariantCulture,
                $"{p.Capacity:N0} MW ({string.Join(", ", p.ByType.Keys)}): a unit sourced from TransitionZero's Solar Asset Mapper (CC BY-NC 4.0): left out (ruling R22)")));
        foreach (var p in eligible.Where(p => p.Capacity >= threshold && !p.HasPosition))
            dropped.Add(new ManifestCode(p.Id, p.Name, "no Latitude/Longitude: left out"));

        var kept = eligible.Where(p => p.Capacity >= threshold && p.HasPosition).OrderByDescending(p => p.Capacity).ThenBy(p => p.Id, StringComparer.Ordinal).ToList();
        var types = new Dictionary<string, int>(StringComparer.Ordinal);
        var unresolved = new Dictionary<string, (int Sites, double Total)>(StringComparer.Ordinal);
        var outRows = new List<PointRow>();
        foreach (var p in kept)
        {
            var iso = (read.IsoByName.TryGetValue(p.Country, out var i3) ? codes.FromIso3(i3) : null) ?? codes.FromName(p.Country);
            if (iso is null)
            {
                var u = unresolved.GetValueOrDefault(p.Country);
                unresolved[p.Country] = (u.Sites + 1, u.Total + p.Capacity);
                continue;
            }
            var type = p.ByType.MaxBy(kv => kv.Value).Key;
            Tally.Add(types, type);
            outRows.Add(new PointRow(p.Name, iso, p.Lat, p.Lon, Math.Round(p.Capacity, 3), "MW", "operating", type, new Dictionary<string, string>
            {
                ["gem_location_id"] = p.Id,
                ["gem_country"] = p.Country,
                ["units"] = p.Units.ToString(CultureInfo.InvariantCulture),
                ["types"] = p.ByType.Count == 1 ? "" : string.Join("; ", p.ByType.OrderByDescending(kv => kv.Value)
                    .Select(kv => string.Create(CultureInfo.InvariantCulture, $"{kv.Key} {kv.Value:0.###} MW"))),
                ["location_accuracy"] = p.Accuracy,
            }));
        }
        return (outRows, NotPlaced(unresolved, "plants", "MW"), types, dropped);
    }

    private static string R22Note(PlantRead read, double threshold)
    {
        var nc = read.Plants.Values.Where(p => p.NonCommercial).ToList();
        return string.Create(CultureInfo.InvariantCulture,
            $"Ruling R22: the About sheet says records with a \"TZ ID\" in Other IDs (unit/phase) come partly or wholly from TransitionZero's Solar Asset Mapper (August 2025), "
            + $"CC BY-NC 4.0. Non-commercial data is never bundled (C6), so a plant with any such unit is left out whole: {nc.Count:N0} plants with operating units, "
            + $"{nc.Count(p => p.Capacity >= threshold):N0} of them {threshold:N0} MW or more (listed under dropped).");
    }

    private static string PositionNote(PlantRead read, double threshold)
    {
        var kept = read.Plants.Values.Where(p => !p.NonCommercial && p.Capacity >= threshold && p.HasPosition).ToList();
        return "A plant's position is its first operating unit's Latitude and Longitude; "
            + $"{kept.Count(p => p.Moved)} kept plants have units more than 0.05 degrees apart. A plant's type is its largest Type by capacity "
            + $"({kept.Count(p => p.ByType.Count > 1)} kept plants have more than one; see the types column), and its country the Country/area with the most capacity "
            + $"({kept.Count(p => p.ByCountry.Count > 1)} span two).";
    }

    private const string CountryNote = "Country/area names map to ISO3 through the workbook's own \"Regions, area, and countries\" sheet (ISO-alpha3 Code), then the app's country names.";

    private static string LargestNote(List<PointRow> rows) => "Check: largest plants: " + string.Join("; ", rows.Take(5).Select(r => string.Create(CultureInfo.InvariantCulture,
        $"{r.Name} ({r.Iso3}, {r.Type}) {r.Capacity:N0} MW"))) + ".";

    /// <summary>
    /// <c>gem-power &lt;xlsx&gt;</c>: operating units summed per plant (GEM location ID); plants of <paramref name="minMw"/> or more.
    /// A plant with any unit whose "Other IDs (unit/phase)" has a TransitionZero id ("TZ: …", Solar Asset Mapper,
    /// CC BY-NC 4.0, per the workbook's About sheet) is left out whole (ruling R22: non-commercial data is never bundled, C6).
    /// </summary>
    public static PointExtract Power(SourceFile src, string release, CountryCodes codes, double minMw = PowerMinMw, int maxMarkers = MaxMarkers,
        string? fileVersion = null)
    {
        ArgumentNullException.ThrowIfNull(src);
        ArgumentNullException.ThrowIfNull(codes);
        var read = ReadPlants(src, release, fileVersion, "GEM power", _ => true);
        var eligible = read.Plants.Values.Where(p => !p.NonCommercial).ToList();
        double threshold = Raise(minMw, PowerStepMw, t => eligible.Count(p => p.Capacity >= t && p.HasPosition), maxMarkers, out var raisedNote, "MW");
        var (outRows, notPlaced, types, dropped) = PlantRows(read, codes, threshold);

        var filter = string.Create(CultureInfo.InvariantCulture, $"operating plants of {threshold:N0} MW or more (units summed per plant)");
        var notes = new List<string>(read.About)
        {
            string.Create(CultureInfo.InvariantCulture,
                $"Rows: {read.Rows:N0} units in \"{PowerSheet}\"; {read.Plants.Count:N0} plants (GEM location ID) with an operating unit; operating units' Capacity (MW) summed per plant."),
        };
        if (raisedNote is not null) notes.Add(raisedNote);
        notes.Add(R22Note(read, threshold));
        notes.Add(PositionNote(read, threshold));
        notes.Add(CountryNote);
        notes.Add(LargestNote(outRows));
        return new PointExtract(Manifest(src, release, fileVersion, PowerFolder, PowerDataset, PowerPage, PowerDownload, "gem-power", filter,
            $"filtered to {filter}; operating units summed per plant; plants with a unit sourced from TransitionZero (CC BY-NC 4.0) left out",
            new ManifestSeries("gem.power", "Power plants: " + filter, "MW",
                $"{PowerSheet}: Capacity (MW) of units with Status \"operating\", summed by GEM location ID", outRows.Select(r => r.Iso3).Distinct().Count()),
            notPlaced, dropped, read.Statuses, read.Flags, types, notes), outRows,
            ["gem_location_id", "gem_country", "units", "types", "location_accuracy"]);
    }

    // ------------------------------------------------------------------------------------------------ renewables (R25)

    public const string RenewablesFolder = "gem-renewables";

    /// <summary>
    /// GEM's renewable Types (R25), as the workbook names them. Solar thermal is not a Type of its own: it is a Technology
    /// ("Solar Thermal") within "utility-scale solar", so it is included with it.
    /// </summary>
    public static IReadOnlyList<string> RenewableTypes { get; } = ["wind", "utility-scale solar", "hydropower", "geothermal", "bioenergy"];

    /// <summary>R25's thresholds, tried in turn until a layer fits under the cap; past the last, 100 MW steps.</summary>
    public static IReadOnlyList<double> RenewableSteps { get; } = [100, 150, 200, 300];

    /// <summary>
    /// <c>gem-renewables &lt;xlsx&gt;</c> (ruling R25): the power workbook's renewable units (<see cref="RenewableTypes"/>)
    /// with Status "operating", summed per plant (GEM location ID); a plant's units of other fuels are not counted here. R22
    /// applies (no plant with a TransitionZero unit). The threshold starts at 100 MW and rises through 150, 200 and 300 MW
    /// (then 100 MW steps) until at most <paramref name="maxMarkers"/> plants remain; each step's count is recorded.
    /// </summary>
    public static PointExtract Renewables(SourceFile src, string release, CountryCodes codes, int maxMarkers = MaxMarkers, string? fileVersion = null)
    {
        ArgumentNullException.ThrowIfNull(src);
        ArgumentNullException.ThrowIfNull(codes);
        var read = ReadPlants(src, release, fileVersion, "GEM renewables", t => RenewableTypes.Contains(t));
        var eligible = read.Plants.Values.Where(p => !p.NonCommercial && p.HasPosition).ToList();
        int Count(double t) => eligible.Count(p => p.Capacity >= t);
        var tried = new List<(double Mw, int Plants)>();
        double threshold = RenewableSteps[0];
        for (int k = 0; ; k++)
        {
            threshold = k < RenewableSteps.Count ? RenewableSteps[k] : RenewableSteps[^1] + 100 * (k - RenewableSteps.Count + 1);
            tried.Add((threshold, Count(threshold)));
            if (tried[^1].Plants <= maxMarkers) break;
        }
        var (outRows, notPlaced, types, dropped) = PlantRows(read, codes, threshold);
        var steps = string.Join("; ", tried.Select(s => string.Create(CultureInfo.InvariantCulture, $"{s.Mw:N0} MW: {s.Plants:N0} plants")));

        var filter = string.Create(CultureInfo.InvariantCulture, $"operating renewable plants of {threshold:N0} MW or more (renewable units summed per plant)");
        var keptPlants = read.Plants.Values.Where(p => !p.NonCommercial && p.HasPosition && p.Capacity >= threshold).ToList();
        var notes = new List<string>(read.About)
        {
            string.Create(CultureInfo.InvariantCulture,
                $"Rows: {read.Rows:N0} units in \"{PowerSheet}\"; Types kept: {string.Join(", ", RenewableTypes)} (solar thermal is a Technology of utility-scale solar, "
                + $"so it is included). {read.Plants.Count:N0} plants (GEM location ID) with an operating renewable unit; their renewable units' Capacity (MW) summed per plant."),
            string.Create(CultureInfo.InvariantCulture,
                $"Threshold (ruling R25): plants with coordinates and no TransitionZero unit at each step tried, up to the {maxMarkers:N0} markers a layer draws: {steps}. "
                + $"Kept at {threshold:N0} MW."),
            string.Create(CultureInfo.InvariantCulture,
                $"{keptPlants.Count(p => p.Others.Count > 0)} kept plants also have operating units of other fuels (not counted here; the Power plants layer counts all fuels)."),
            "The Power plants layer shows all fuels at 500 MW or more, so a large renewable plant is in both layers when both are on.",
        };
        notes.Add(R22Note(read, threshold));
        notes.Add(PositionNote(read, threshold));
        notes.Add(CountryNote);
        notes.Add("Check: kept plants by largest Type: " + string.Join("; ", types.OrderByDescending(kv => kv.Value).Select(kv => $"{kv.Key} {kv.Value:N0}")) + ".");
        notes.Add(LargestNote(outRows));
        return new PointExtract(Manifest(src, release, fileVersion, RenewablesFolder, PowerDataset, PowerPage, PowerDownload, "gem-renewables", filter,
            $"filtered to {filter}; renewable units ({string.Join(", ", RenewableTypes)}) only; operating units summed per plant; "
            + "plants with a unit sourced from TransitionZero (CC BY-NC 4.0) left out",
            new ManifestSeries("gem.renewables", "Renewable power plants: " + filter, "MW",
                $"{PowerSheet}: Capacity (MW) of units of Type {string.Join(", ", RenewableTypes)} with Status \"operating\", summed by GEM location ID",
                outRows.Select(r => r.Iso3).Distinct().Count()),
            notPlaced, dropped, read.Statuses, read.Flags, types, notes) with { Thresholds = tried.ToDictionary(s => s.Mw.ToString("0", CultureInfo.InvariantCulture) + " MW", s => s.Plants) },
            outRows, ["gem_location_id", "gem_country", "units", "types", "location_accuracy"]);
    }

    // ------------------------------------------------------------------------------------------------ oil and gas

    public const string OilGasProductionSheet = "Field-level production data";
    private static readonly string[] OilGasProductionColumns = ["Unit ID", "Fuel description", "Quantity (converted)", "Units (converted)", "Data Year"];
    /// <summary>
    /// Gas to oil equivalent at the industry's 6,000 cubic feet per barrel (6:1): 1 million m³ = 35.3147 million ft³ =
    /// 0.0058858 million barrels of oil equivalent (boe). Used only to rank the fields for the P3-9 cap (ruling R23).
    /// </summary>
    public const double BoePerCubicMetre = 35.3147 / 6000;
    /// <summary>When the 5,000 cap is passed, oil and gas fields reported producing less than this (million boe a year) are left out; raised in steps of it.</summary>
    public const double OilGasStepBoe = 0.1;

    /// <summary>A field's newest production per fuel, in GEM's converted units, and its total in million boe a year.</summary>
    public sealed record FieldProduction(IReadOnlyList<(string Fuel, double Quantity, string Unit, int Year)> Latest, double MillionBoe);

    /// <summary>
    /// GEM's "Field-level production data": for each field and fuel description, the newest Data Year's Quantity (converted)
    /// (rows of the same field, fuel, unit and year are added), and the field's total in million boe a year (oil, condensate
    /// and NGL in million bbl/y as they are; gas in million m³/y at <see cref="BoePerCubicMetre"/>; GEM's own million boe/y as given).
    /// A row with no Data Year, or one in <paramref name="releaseYear"/> or later (a year not over when GEM released the
    /// file: a forecast, such as Wafra's 2028), is not used, and counted in <paramref name="flags"/>.
    /// </summary>
    public static Dictionary<string, FieldProduction> Production(string path, int releaseYear, Dictionary<string, int> flags, string source)
    {
        var byField = new Dictionary<string, Dictionary<string, (double Quantity, string Unit, int Year)>>(StringComparer.Ordinal);
        foreach (var r in GemWorkbook.Rows(path, OilGasProductionSheet, OilGasProductionColumns, source))
        {
            var unit = r.Text("Units (converted)");
            if (r.Number("Quantity (converted)") is not { } q || ToBoe(unit, 1) is null)
            {
                Tally.Add(flags, $"production row with Quantity (converted) \"{r.Text("Quantity (converted)")}\" in \"{unit}\": not used");
                continue;
            }
            if (r.Number("Data Year") is not { } y)
            {
                Tally.Add(flags, "production row with no Data Year: not used");
                continue;
            }
            if (y >= releaseYear)
            {
                Tally.Add(flags, string.Create(CultureInfo.InvariantCulture, $"production row with Data Year {releaseYear} or later (not over at the release): not used"));
                continue;
            }
            var id = r.Text("Unit ID");
            var fuel = r.Text("Fuel description");
            if (!byField.TryGetValue(id, out var fuels)) byField[id] = fuels = new(StringComparer.Ordinal);
            int year = (int)y;
            if (!fuels.TryGetValue(fuel, out var had) || year > had.Year) fuels[fuel] = (q, unit, year);
            else if (year == had.Year && unit == had.Unit) fuels[fuel] = (had.Quantity + q, unit, year);
        }
        return byField.ToDictionary(kv => kv.Key, kv => new FieldProduction(
            [.. kv.Value.OrderBy(f => f.Key, StringComparer.Ordinal).Select(f => (f.Key, f.Value.Quantity, f.Value.Unit, f.Value.Year))],
            kv.Value.Values.Sum(v => ToBoe(v.Unit, v.Quantity)!.Value)), StringComparer.Ordinal);
    }

    private static double? ToBoe(string unit, double quantity) => unit switch
    {
        "million bbl/y" or "million boe/y" => quantity,
        "million m³/y" => quantity * BoePerCubicMetre,
        _ => null,
    };

    /// <summary>"oil 41.4 million bbl/y, gas 1,200 million m³/y (2023)": GEM's newest figures, its units, the year(s).</summary>
    public static string ProductionText(FieldProduction p)
    {
        ArgumentNullException.ThrowIfNull(p);
        var years = p.Latest.Select(f => f.Year).Distinct().Order().ToList();
        var parts = p.Latest.Select(f => $"{f.Fuel} {Round(f.Quantity)} {f.Unit}"
            + (years.Count > 1 ? string.Create(CultureInfo.InvariantCulture, $" ({f.Year})") : ""));
        return string.Join(", ", parts) + (years.Count == 1 ? string.Create(CultureInfo.InvariantCulture, $" ({years[0]})") : "");
    }

    private static string Round(double v) => v >= 100 ? v.ToString("N0", CultureInfo.InvariantCulture)
        : v >= 1 ? v.ToString("0.#", CultureInfo.InvariantCulture) : v.ToString("0.###", CultureInfo.InvariantCulture);

    /// <summary>
    /// <c>gem-oilgas &lt;xlsx&gt;</c>: fields ("Field-level main data") whose Status is "operating", with coordinates. If that
    /// passes the 5,000 cap (P3-9), fields whose newest reported production is under a threshold, in million boe a year,
    /// are left out, the threshold raised from <see cref="OilGasStepBoe"/> in steps of it; fields with no production figure
    /// stay (ruling R23).
    /// </summary>
    public static PointExtract OilGas(SourceFile src, string release, CountryCodes codes, int maxMarkers = MaxMarkers, string? fileVersion = null)
    {
        ArgumentNullException.ThrowIfNull(src);
        ArgumentNullException.ThrowIfNull(codes);
        const string Source = "GEM oil and gas";
        var about = CheckAbout(src, release, fileVersion, Source);
        var statuses = new Dictionary<string, int>(StringComparer.Ordinal);
        var flags = new Dictionary<string, int>(StringComparer.Ordinal);
        var production = Production(src.Path, ReleaseYear(release), flags, Source);
        var fields = new List<(SheetRow Row, double Lat, double Lon, FieldProduction? Output)>();
        int rows = 0, operating = 0, noPosition = 0;
        foreach (var r in GemWorkbook.Rows(src.Path, OilGasSheet, OilGasColumns, Source))
        {
            rows++;
            var status = r.Text("Status");
            Tally.Add(statuses, status);
            if (status != "operating") continue;
            operating++;
            if (r.Number("Latitude") is not { } lat || r.Number("Longitude") is not { } lon || lat is < -90 or > 90 || lon is < -180 or > 180) { noPosition++; continue; }
            fields.Add((r, lat, lon, production.GetValueOrDefault(r.Text("Unit ID"))));
        }
        if (operating == 0) throw new InvalidDataException($"{Source}: no operating fields found (format changed)");

        // P3-9: all operating fields with coordinates if they fit; otherwise the fields GEM reports producing less than a
        // threshold are left out (R23). A field with no field-level figure is kept: GEM reports some of the largest only at
        // project level (Ghawar's fields, such as Ain Dar), so leaving out the unmeasured would hide giants.
        double threshold = 0;
        string? raisedNote = null;
        bool Keeps(FieldProduction? output, double t) => t == 0 || output is null || output.MillionBoe >= t - 1e-9;
        if (fields.Count > maxMarkers)
        {
            int Kept(double t) => fields.Count(f => Keeps(f.Output, t));
            threshold = Raise(OilGasStepBoe, OilGasStepBoe, Kept, maxMarkers, out _, "million boe/y");
            raisedNote = string.Create(CultureInfo.InvariantCulture,
                $"P3-9 cap: {fields.Count:N0} operating fields with coordinates, over the {maxMarkers:N0} markers a layer draws, so the fields GEM reports producing "
                + $"less than a threshold are left out (ruling R23), the threshold starting at {OilGasStepBoe:0.##} million boe/y and raised in steps of {OilGasStepBoe:0.##}: "
                + $"at {threshold:0.##} million boe/y, {fields.Count(f => !Keeps(f.Output, threshold)):N0} fields are left out and {Kept(threshold):N0} remain, "
                + $"{fields.Count(f => f.Output is null):N0} of them with no field-level production figure (kept: some of the largest are reported only at project level).");
        }
        var kept = fields.Where(f => Keeps(f.Output, threshold))
            .OrderByDescending(f => f.Output?.MillionBoe ?? -1).ThenBy(f => f.Row.Text("Unit ID"), StringComparer.Ordinal).ToList();   // largest first, unmeasured last

        var fuels = new Dictionary<string, int>(StringComparer.Ordinal);
        var unresolved = new Dictionary<string, (int Sites, double Total)>(StringComparer.Ordinal);
        var outRows = new List<PointRow>();
        foreach (var (r, lat, lon, output) in kept)
        {
            var country = r.Text("Country/Area");
            if (codes.FromName(country) is not { } iso)
            {
                var u = unresolved.GetValueOrDefault(country);
                unresolved[country] = (u.Sites + 1, 0);
                continue;
            }
            var fuel = r.Text("Fuel type");
            Tally.Add(fuels, fuel);
            outRows.Add(new PointRow(r.Text("Unit Name"), iso, lat, lon, output is null ? null : Math.Round(output.MillionBoe, 3),
                output is null ? "" : "million boe/y", "operating", fuel, new Dictionary<string, string>
                {
                    ["gem_unit_id"] = r.Text("Unit ID"),
                    ["gem_country"] = country,
                    ["detail"] = output is null ? "" : ProductionText(output),
                    ["production_type"] = r.Text("Production Type"),
                    ["onshore_offshore"] = r.Text("Onshore/Offshore"),
                    ["location_accuracy"] = r.Text("Location accuracy"),
                }));
        }
        Tally.Add(flags, $"operating, no Latitude/Longitude: left out ({noPosition})");
        var filter = threshold == 0 ? "operating fields with coordinates"
            : string.Create(CultureInfo.InvariantCulture,
                $"operating fields with coordinates, leaving out those GEM reports producing under {threshold:0.##} million barrels of oil equivalent a year");
        var notes = new List<string>(about)
        {
            string.Create(CultureInfo.InvariantCulture,
                $"Rows: {rows:N0} fields in \"{OilGasSheet}\"; {operating:N0} with Status \"operating\", of which {noPosition:N0} have no Latitude/Longitude (left out)."),
            "Production: \"Field-level production data\", each fuel's newest Data Year, in GEM's converted units (the detail column, as GEM gives them). "
                + "The capacity column is their total in million barrels of oil equivalent a year (boe): oil, condensate and NGL in million bbl/y as they are, gas at "
                + "6,000 cubic feet per boe (1 million m³ = 0.0058858 million boe), GEM's own million boe/y as given. The years differ between fields.",
        };
        if (raisedNote is not null) notes.Add(raisedNote);
        notes.Add("Check: largest fields: " + string.Join("; ", outRows.Take(5).Select(r => $"{r.Name} ({r.Iso3}) {r.Extra["detail"]}")) + ".");
        var changes = threshold == 0 ? $"filtered to {filter}"
            : $"filtered to {filter} (each fuel's newest reported year, summed; gas converted at 6,000 cubic feet per barrel of oil equivalent)";
        return new PointExtract(Manifest(src, release, fileVersion, OilGasFolder, OilGasDataset, OilGasPage, OilGasDownload, "gem-oilgas", filter, changes,
            new ManifestSeries("gem.oilgas", "Oil and gas fields: " + filter, threshold == 0 ? "" : "million boe/y",
                $"{OilGasSheet}: fields with Status \"operating\" and a Latitude and Longitude; {OilGasProductionSheet}: newest Quantity (converted) per fuel",
                outRows.Select(r => r.Iso3).Distinct().Count()),
            NotPlaced(unresolved, "fields", null), [], statuses, flags, fuels, notes), outRows,
            ["gem_unit_id", "gem_country", "detail", "production_type", "onshore_offshore", "location_accuracy"]);
    }

    // ------------------------------------------------------------------------------------------------ steel

    /// <summary>
    /// <c>gem-steel &lt;plant-level xlsx&gt;</c>: the plant-level file's "Plant data" (name, country, coordinates) joined on GEM
    /// plant ID to "Plant capacities and status": a plant's Nominal crude steel capacity (ttpa) summed over its rows whose
    /// Status is operating (or operating pre-retirement), in million tonnes a year; plants of <paramref name="minMt"/> or more.
    /// </summary>
    public static PointExtract Steel(SourceFile src, string release, CountryCodes codes, double minMt = SteelMinMt, int maxMarkers = MaxMarkers,
        string? fileVersion = null)
    {
        ArgumentNullException.ThrowIfNull(src);
        ArgumentNullException.ThrowIfNull(codes);
        const string Source = "GEM steel";
        var about = CheckAbout(src, release, fileVersion, Source);
        var info = new Dictionary<string, SheetRow>(StringComparer.Ordinal);
        foreach (var r in GemWorkbook.Rows(src.Path, SteelPlantSheet, SteelPlantColumns, Source))
            if (!info.TryAdd(r.Text("GEM plant ID"), r)) throw new InvalidDataException($"{Source}: plant {r.Text("GEM plant ID")} is listed twice in \"{SteelPlantSheet}\"");

        var statuses = new Dictionary<string, int>(StringComparer.Ordinal);
        var flags = new Dictionary<string, int>(StringComparer.Ordinal);
        var capacity = new Dictionary<string, (double Ttpa, bool Operating, SortedSet<string> Equipment, int Rows)>(StringComparer.Ordinal);
        int rows = 0;
        foreach (var r in GemWorkbook.Rows(src.Path, SteelCapacitySheet, SteelCapacityColumns, Source))
        {
            rows++;
            var status = r.Text("Status");
            Tally.Add(statuses, status);
            if (!IsOperating(status)) continue;
            var id = r.Text("GEM plant ID");
            var ttpa = r.Number("Nominal crude steel capacity (ttpa)");
            if (ttpa is null) { Tally.Add(flags, $"operating row with Nominal crude steel capacity (ttpa) \"{r.Text("Nominal crude steel capacity (ttpa)")}\": not counted"); continue; }
            if (!capacity.TryGetValue(id, out var c)) c = (0, false, new SortedSet<string>(StringComparer.Ordinal), 0);
            if (r.Text("Main production equipment") is { Length: > 0 } eq) c.Equipment.Add(eq);
            capacity[id] = (c.Ttpa + ttpa.Value, c.Operating || status == "operating", c.Equipment, c.Rows + 1);
        }
        if (capacity.Count == 0) throw new InvalidDataException($"{Source}: no operating plants with a crude steel capacity found (format changed)");

        var dropped = new List<ManifestCode>();
        var positioned = new Dictionary<string, (double Lat, double Lon)>(StringComparer.Ordinal);
        foreach (var id in capacity.Keys)
        {
            if (!info.TryGetValue(id, out var p)) throw new InvalidDataException($"{Source}: plant {id} has capacities but no row in \"{SteelPlantSheet}\"");
            if (Coordinates(p.Text("Coordinates")) is { } at) positioned[id] = at;
        }
        double threshold = Raise(minMt, SteelStepMt, t => capacity.Count(kv => kv.Value.Ttpa / 1000 >= t && positioned.ContainsKey(kv.Key)), maxMarkers, out var raisedNote, "Mt/yr");
        var kept = capacity.Where(kv => kv.Value.Ttpa / 1000 >= threshold).OrderByDescending(kv => kv.Value.Ttpa).ThenBy(kv => kv.Key, StringComparer.Ordinal).ToList();
        foreach (var (id, _) in kept.Where(kv => !positioned.ContainsKey(kv.Key)))
            dropped.Add(new ManifestCode(id, info[id].Text("Plant name (English)"), $"Coordinates \"{info[id].Text("Coordinates")}\" is not a position: left out"));

        var unresolved = new Dictionary<string, (int Sites, double Total)>(StringComparer.Ordinal);
        var types = new Dictionary<string, int>(StringComparer.Ordinal);
        var outRows = new List<PointRow>();
        foreach (var (id, c) in kept.Where(kv => positioned.ContainsKey(kv.Key)))
        {
            var p = info[id];
            var country = p.Text("Country/area");
            var mt = Math.Round(c.Ttpa / 1000, 3);
            if (codes.FromName(country) is not { } iso)
            {
                var u = unresolved.GetValueOrDefault(country);
                unresolved[country] = (u.Sites + 1, u.Total + mt);
                continue;
            }
            var type = string.Join("; ", c.Equipment);
            Tally.Add(types, type);
            var (lat, lon) = positioned[id];
            outRows.Add(new PointRow(p.Text("Plant name (English)"), iso, lat, lon, mt, "Mt/yr", c.Operating ? "operating" : "operating pre-retirement", type,
                new Dictionary<string, string>
                {
                    ["gem_plant_id"] = id,
                    ["gem_country"] = country,
                    ["crude_steel_ttpa"] = DatasetWriter.Number(c.Ttpa),
                    ["coordinate_accuracy"] = p.Text("Coordinate accuracy"),
                }));
        }
        var filter = string.Create(CultureInfo.InvariantCulture, $"operating plants of {threshold:0.#} million tonnes of crude steel a year or more");
        var notes = new List<string>(about)
        {
            string.Create(CultureInfo.InvariantCulture,
                $"Rows: {info.Count:N0} plants in \"{SteelPlantSheet}\"; {rows:N0} rows in \"{SteelCapacitySheet}\" (a plant can have several, one per status and equipment); "
                + $"{capacity.Count:N0} plants with an operating row that gives a crude steel capacity."),
            "A plant's capacity is the sum of Nominal crude steel capacity (ttpa) over its rows with Status \"operating\" or \"operating pre-retirement\" "
                + "(still producing; the status column says which), divided by 1,000 to give million tonnes a year (Mt/yr). Iron-only capacity is not counted.",
            "Coordinates is one text cell, \"latitude, longitude\".",
        };
        if (raisedNote is not null) notes.Add(raisedNote);
        notes.Add(string.Create(CultureInfo.InvariantCulture, $"Check: {kept.Count(kv => kv.Value.Rows > 1)} kept plants have more than one operating row."));
        notes.Add("Check: largest plants: " + string.Join("; ", outRows.Take(5).Select(r => string.Create(CultureInfo.InvariantCulture,
            $"{r.Name} ({r.Iso3}) {r.Capacity:0.##} Mt/yr"))) + ".");
        return new PointExtract(Manifest(src, release, fileVersion, SteelFolder, SteelDataset, SteelPage, SteelDownload, "gem-steel", filter,
            $"filtered to {filter}; a plant's operating crude steel capacities summed and converted from thousand to million tonnes a year",
            new ManifestSeries("gem.steel", "Steel plants: " + filter, "Mt/yr",
                $"{SteelCapacitySheet}: Nominal crude steel capacity (ttpa) of rows with Status \"operating\" or \"operating pre-retirement\", summed by GEM plant ID",
                outRows.Select(r => r.Iso3).Distinct().Count()),
            NotPlaced(unresolved, "plants", "Mt/yr"), dropped, statuses, flags, types, notes), outRows,
            ["gem_plant_id", "gem_country", "crude_steel_ttpa", "coordinate_accuracy"]);
    }

    /// <summary>"36.7474130, 36.2173300" to a position, or null.</summary>
    public static (double Lat, double Lon)? Coordinates(string text)
    {
        var parts = text.Split(',');
        if (parts.Length != 2) return null;
        if (!double.TryParse(parts[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var lat)
            || !double.TryParse(parts[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var lon)) return null;
        return lat is >= -90 and <= 90 && lon is >= -180 and <= 180 ? (lat, lon) : null;
    }

    // ------------------------------------------------------------------------------------------------ shared

    /// <summary>
    /// P3-9: the lowest threshold from <paramref name="start"/> up, in <paramref name="step"/>s, that leaves at most
    /// <paramref name="max"/> sites; the note says by how much it was raised, or null when it was not.
    /// </summary>
    public static double Raise(double start, double step, Func<double, int> count, int max, out string? note, string unit)
    {
        double t = start;
        int first = count(t);
        for (int k = 1; count(t) > max; k++) t = Math.Round(start + k * step, 6);   // no drift from adding 0.1s
        note = t == start ? null : string.Create(CultureInfo.InvariantCulture,
            $"P3-9 cap: {first:N0} sites at {start:0.##} {unit} or more, over the {max:N0} markers a layer draws; the threshold was raised in steps of "
            + $"{step:0.##} {unit} to {t:0.##} {unit}, leaving {count(t):N0}.");
        return t;
    }

    /// <summary>The release as the screen dates it: GEM's wording, then a file-only version ("September 2026 release (file V2)").</summary>
    public static string DateText(string release, string? fileVersion) => fileVersion is null ? release : $"{release} (file {fileVersion})";

    /// <summary>The release's year: "March 2026 release" to 2026.</summary>
    public static int ReleaseYear(string release) =>
        release.Split(' ', StringSplitOptions.RemoveEmptyEntries) is [_, var y, ..] && int.TryParse(y, NumberStyles.None, CultureInfo.InvariantCulture, out var year)
            ? year : throw new ArgumentException($"the release \"{release}\" does not start with a month and a year");

    /// <summary>The About sheet: the release must be named there (its month and year), and its copyright and licence lines are kept.</summary>
    private static List<string> CheckAbout(SourceFile src, string release, string? fileVersion, string source)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(release);
        var cells = GemWorkbook.Cells(src.Path, "About", source);
        var words = release.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var monthYear = string.Join(' ', words.Take(2));   // "September 2026 (V2) release" -> "September 2026"
        if (words.Length < 2 || !cells.Any(c => c.Contains(monthYear, StringComparison.Ordinal)))
            throw new InvalidDataException($"{source}: the About sheet does not name the release \"{monthYear}\" (is this the right file?)");
        var notes = new List<string>();
        var version = fileVersion is null ? words.FirstOrDefault(w => w.StartsWith("(V", StringComparison.Ordinal)) : $"({fileVersion})";
        if (version is not null && !cells.Any(c => c.Contains($"{monthYear} {version}", StringComparison.Ordinal)))
            notes.Add($"Release: the workbook names \"{monthYear}\" without \"{version}\"; the version is the file name's ({src.FileName}).");
        foreach (var c in cells.Where(c => c.StartsWith("Copyright", StringComparison.Ordinal) || c.StartsWith("Recommended Citation", StringComparison.Ordinal)
                                          || c.StartsWith("Please note", StringComparison.Ordinal)).Distinct())
            notes.Add("About sheet: " + c.ReplaceLineEndings(" "));
        return notes;
    }

    private static List<ManifestCode> NotPlaced(Dictionary<string, (int Sites, double Total)> unresolved, string what, string? unit) =>
        [.. unresolved.OrderBy(kv => kv.Key, StringComparer.Ordinal).Select(kv => new ManifestCode(kv.Key, kv.Key, string.Create(CultureInfo.InvariantCulture,
            $"no unit in the boundary set for this Country/area: {kv.Value.Sites} {what}{(unit is null ? "" : $" ({kv.Value.Total:N0} {unit})")} not drawn")))];

    private static DatasetManifest Manifest(SourceFile src, string release, string? fileVersion, string folder, string dataset, string page, string download, string verb,
        string filter, string changes, ManifestSeries series, IReadOnlyList<ManifestCode> notPlaced, IReadOnlyList<ManifestCode> dropped,
        Dictionary<string, int> statuses, Dictionary<string, int> flags, Dictionary<string, int> types, List<string> notes)
    {
        var citation = Citation(dataset, release);
        var allFlags = new Dictionary<string, int>(flags, StringComparer.Ordinal);
        foreach (var (s, n) in statuses) allFlags["Status " + (s.Length == 0 ? "(blank)" : s)] = n;
        notes.Insert(0, $"Licence: {Licence} ({LicenceUrl}); GEM's licence page {GemLicencePage} (read 2026-11-24) gives the full CC BY 4.0 text. "
            + $"Changed by EarthRelief3D: {changes}.");
        return new DatasetManifest
        {
            Id = folder,
            Publisher = Publisher,
            Dataset = dataset,
            Version = fileVersion is null ? release : $"{release}, file {fileVersion}",
            SourceUrl = download,
            PageUrl = page,
            Licence = Licence,
            LicenceUrl = LicenceUrl,
            Attribution = citation,
            DateMeaning = "Release",
            DateText = DateText(release, fileVersion),
            AsOf = $"{dataset}, {DateText(release, fileVersion)}: the status GEM recorded for that release",
            Published = src.Published.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            Downloaded = src.Downloaded.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            FileName = src.FileName,
            FileSha256 = src.Sha256(),
            Command = $"DataTool {verb} \"<downloads>/{src.FileName}\" --release \"{release}\"{(fileVersion is null ? "" : $" --file-version {fileVersion}")} --published {src.Published:yyyy-MM-dd} --downloaded {src.Downloaded:yyyy-MM-dd}",
            Series = [series],
            NotPlaced = notPlaced,
            Dropped = dropped,
            QualifiersSeen = Tally.Sorted(types),
            FlagsSeen = Tally.Sorted(allFlags),
            Notes = notes,
            Filter = filter,
            Changes = changes,
        };
    }
}
