using System.Globalization;
using System.Windows.Media;
using EarthRelief3D.App.Presentation;
using EarthRelief3D.Core.Feeds;
using EarthRelief3D.Core.Model;
using GlobeCore.Feeds;

namespace EarthRelief3D.App.Layers;

/// <summary>The power plants' fuel groups (Task 7): the fill colour of each disc, with a legend key each.</summary>
public enum FuelGroup { Fossil, Nuclear, Hydro, WindOrSolar, Other }

/// <summary>
/// The Resources group's three GEM marker layers (Task 7, P3-9): the bundled extracts in <c>data\datasets\gem-*</c> as
/// points, each with its tooltip (name, type, capacity, status, and the tracker's release), its size by capacity class and,
/// for power plants, its largest fuel's group colour (the disc's dark edge, R8, keeps it apart from conflict dots). The legend line
/// gives the filter, the keys, and GEM's CC BY 4.0 credit: the citation, the licence's link and what was changed.
/// </summary>
public static class ResourceMarkers
{
    /// <summary>The status dot shared by the GEM extracts.</summary>
    public const string DotName = "GEM";

    /// <summary>The layers and their extracts' folders under <c>data\datasets</c>.</summary>
    public static IReadOnlyList<(MarkerLayer Layer, string Folder)> Datasets { get; } =
        [(MarkerLayer.PowerPlants, "gem-power"), (MarkerLayer.Renewables, "gem-renewables"), (MarkerLayer.OilGasFields, "gem-oilgas"), (MarkerLayer.SteelPlants, "gem-steel")];

    // ---------------------------------------------------------------- power: fuel groups

    /// <summary>GEM's power Type to its group: coal and oil/gas are fossil; wind and utility-scale solar one group; bioenergy, geothermal (and anything new) other.</summary>
    public static FuelGroup FuelGroupOf(string gemType) => gemType switch
    {
        "coal" or "oil/gas" => FuelGroup.Fossil,
        "nuclear" => FuelGroup.Nuclear,
        "hydropower" => FuelGroup.Hydro,
        "wind" or "utility-scale solar" => FuelGroup.WindOrSolar,
        _ => FuelGroup.Other,
    };

    /// <summary>
    /// Okabe–Ito colours, colour-blind safe and apart by lightness too: fossil the layer's orange (#E69F00), nuclear reddish
    /// purple (#CC79A7), hydro blue (#0072B2), wind or solar yellow (#F0E442), other light grey (#BBBBBB). Each disc has the
    /// dark edge (R8), so the purple never reads as a conflict dot, nor the yellow as a camp's filled square.
    /// </summary>
    public static Color FuelGroupColor(FuelGroup group) => group switch
    {
        FuelGroup.Fossil => LayerStyle.PowerPlantColor,
        FuelGroup.Nuclear => Color.FromRgb(0xCC, 0x79, 0xA7),
        FuelGroup.Hydro => Color.FromRgb(0x00, 0x72, 0xB2),
        FuelGroup.WindOrSolar => Color.FromRgb(0xF0, 0xE4, 0x42),
        FuelGroup.Other => Color.FromRgb(0xBB, 0xBB, 0xBB),
        _ => throw new ArgumentOutOfRangeException(nameof(group)),
    };

    public static string FuelGroupLabel(FuelGroup group) => group switch
    {
        FuelGroup.Fossil => "fossil (coal, oil, gas)",
        FuelGroup.Nuclear => "nuclear",
        FuelGroup.Hydro => "hydro",
        FuelGroup.WindOrSolar => "wind or solar",
        FuelGroup.Other => "other (bioenergy, geothermal)",
        _ => throw new ArgumentOutOfRangeException(nameof(group)),
    };

    // ---------------------------------------------------------------- sizes

    private static readonly (string Label, double Min, double SizePx)[] PowerClasses =
    [
        ("under 1,000 MW", 0, 3), ("1,000–2,499 MW", 1000, 4), ("2,500–4,999 MW", 2500, 5), ("5,000 MW or more", 5000, 6.5),
    ];

    private static readonly (string Label, double Min, double SizePx)[] SteelClasses =
    [
        ("1–2.9 million tonnes a year", 0, 3.5), ("3–9.9 million tonnes a year", 3, 4.5), ("10 million tonnes a year or more", 10, 6),
    ];

    /// <summary>Renewable plants (R25): smaller classes, as the layer starts at 100 to 300 MW.</summary>
    private static readonly (string Label, double Min, double SizePx)[] RenewableClasses =
    [
        ("under 500 MW", 0, 3), ("500–999 MW", 500, 4), ("1,000–4,999 MW", 1000, 5), ("5,000 MW or more", 5000, 6.5),
    ];

    /// <summary>Oil and gas fields: one size (their production years and units differ, and many have no figure).</summary>
    public const double FieldSizePx = 3.5;

    /// <summary>
    /// The layer's size classes, smallest first: each one's legend label, its lower bound (in the extract's unit: MW, Mt/yr)
    /// and its marker radius in pixels. Empty for oil and gas fields. The first class's label holds whatever the filter's
    /// threshold is (P3-9 may raise it).
    /// </summary>
    public static IReadOnlyList<(string Label, double Min, double SizePx)> SizeClasses(MarkerLayer layer) => layer switch
    {
        MarkerLayer.PowerPlants => PowerClasses,
        MarkerLayer.SteelPlants => SteelClasses,
        MarkerLayer.Renewables => RenewableClasses,
        MarkerLayer.OilGasFields => [],
        _ => throw new ArgumentOutOfRangeException(nameof(layer), layer, "Only the GEM layers are sized here."),
    };

    /// <summary>The marker's radius in pixels for its capacity (the class it falls in).</summary>
    public static double SizeOf(MarkerLayer layer, double? capacity)
    {
        var classes = SizeClasses(layer);
        if (classes.Count == 0 || capacity is not { } c) return FieldSizePx;
        return classes.Last(k => c >= k.Min).SizePx;
    }

    // ---------------------------------------------------------------- tooltips

    private static readonly Dictionary<string, string> Equipment = new(StringComparer.Ordinal)
    {
        ["BF"] = "blast furnace",
        ["BOF"] = "basic oxygen furnace",
        ["EAF"] = "electric arc furnace",
        ["IF"] = "induction furnace",
        ["DRI"] = "direct reduced iron",
        ["OHF"] = "open hearth furnace",
        ["Steel other/unspecified"] = "other steelmaking",
        ["Iron other/unspecified"] = "other ironmaking",
    };

    /// <summary>GEM's "Main production equipment" ("BF; BOF") in words: "blast furnace, basic oxygen furnace"; an unknown code stays as GEM wrote it.</summary>
    public static string EquipmentText(string type) => string.Join(", ", type.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Select(e => Equipment.TryGetValue(e, out var words) ? words : e));

    /// <summary>The power legend's colour rule (R24): a plant of several fuels takes its largest fuel's colour.</summary>
    public const string ColouredByLargestFuel = "coloured by its largest fuel";

    /// <summary>The renewables legend measure's last words (R26).</summary>
    public const string OtherFuelsNotCounted = "a plant's units of other fuels are not counted";

    /// <summary>
    /// A renewable plant's other fuels in the tooltip (R26): "renewable units only; the plant also has 96 MW of oil/gas"
    /// (several: "410 MW of coal, 50 MW of oil/gas"), from the extract's <c>other_fuels</c> column ("oil/gas 96 MW").
    /// </summary>
    public static string OtherFuelsText(string otherFuels) => "renewable units only; the plant also has " + string.Join(", ",
        otherFuels.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(part =>
        {
            var words = FuelsText(part);   // "oil/gas 96 MW"
            int at = words.LastIndexOf(' ', words.Length - 4);
            return at > 0 && words.EndsWith(" MW", StringComparison.Ordinal) ? $"{words[(at + 1)..]} of {words[..at]}" : words;
        }));

    /// <summary>The renewables legend's note (R25): the two layers overlap on purpose.</summary>
    public const string OverlapNote = "The Power plants layer shows every fuel at 500 MW or more, so a large renewable plant appears in both layers when both are on.";

    /// <summary>
    /// A marker's own colour: for power and renewable plants the fuel group of the plant's type, which the extract makes its
    /// largest fuel by capacity (never the first listed, R24); null for the other layers (the layer's colour).
    /// </summary>
    public static Color? ColorOf(MarkerLayer layer, MarkerPoint p)
    {
        ArgumentNullException.ThrowIfNull(p);
        return layer is MarkerLayer.PowerPlants or MarkerLayer.Renewables ? FuelGroupColor(FuelGroupOf(p.Type)) : null;
    }

    /// <summary>
    /// A plant's capacity per fuel, as the extract's <c>types</c> column has it ("coal 5500 MW; oil/gas 280 MW"), in the
    /// tooltip's words: "coal 5,500 MW, oil/gas 280 MW" (R24). A part it cannot read is shown as written.
    /// </summary>
    public static string FuelsText(string types) => string.Join(", ", types.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Select(part =>
        {
            if (!part.EndsWith(" MW", StringComparison.Ordinal)) return part;
            var body = part[..^3];
            int space = body.LastIndexOf(' ');
            return space > 0 && double.TryParse(body[(space + 1)..], NumberStyles.Float, CultureInfo.InvariantCulture, out var mw)
                ? $"{body[..space]} {mw.ToString("#,##0.#", CultureInfo.InvariantCulture)} MW" : part;
        }));

    /// <summary>"GEM Global Integrated Power Tracker, September 2026 release (file V2)": the source and its date on every marker.</summary>
    public static string SourceText(SourceCitation c) => $"GEM {c.Dataset}, {CrisisText.DataDate(c, DateTime.MaxValue)}";

    /// <summary>
    /// "Taichung power station · coal 5,500 MW, oil/gas 280 MW · 5,780 MW · operating · GEM Global Integrated Power Tracker,
    /// September 2026 release (file V2)" (a plant of one fuel names it alone, "· coal ·");
    /// fields say their production as GEM gives it, plants their equipment in words. Long lines are wrapped (<see cref="TooltipText.Wrap"/>).
    /// </summary>
    public static string Tooltip(MarkerLayer layer, MarkerPoint p, SourceCitation c)
    {
        ArgumentNullException.ThrowIfNull(p);
        ArgumentNullException.ThrowIfNull(c);
        var inv = CultureInfo.InvariantCulture;
        string[] parts = layer switch
        {
            MarkerLayer.PowerPlants or MarkerLayer.Renewables => [p.Name, p.Types.Length > 0 ? FuelsText(p.Types) : p.Type,
                p.Capacity is { } mw ? mw.ToString("N0", inv) + " MW" : "capacity not given",
                layer == MarkerLayer.Renewables && p.OtherFuels.Length > 0 ? OtherFuelsText(p.OtherFuels) : "", p.Status],
            MarkerLayer.OilGasFields => [p.Name, p.Type, p.Detail.Length > 0 ? "production: " + p.Detail : "no production figure in GEM's field data", p.Status],
            MarkerLayer.SteelPlants => [p.Name, EquipmentText(p.Type),
                p.Capacity is { } mt ? mt.ToString("0.#", inv) + " million tonnes of crude steel a year" : "capacity not given", p.Status],
            _ => throw new ArgumentOutOfRangeException(nameof(layer), layer, "Only the GEM layers have these tooltips."),
        };
        return TooltipText.Wrap(string.Join(" · ", parts.Where(s => s.Length > 0)) + " · " + SourceText(c));
    }

    // ---------------------------------------------------------------- the layer's points and legend

    /// <summary>The layer's points from its loaded extract, or null when it did not load (the legend then says "Data not loaded yet", R2).</summary>
    public static PointSet? For(MarkerLayer layer, BundledPointsLoad load)
    {
        ArgumentNullException.ThrowIfNull(load);
        if (!load.Loaded) return null;
        var c = load.Citation!;
        var items = load.Points.Select(p => new PointItem(new AstroCore.Earth.GeoPosition(p.Lat, p.Lon), SizeOf(layer, p.Capacity), Tooltip(layer, p, c),
            p.Iso3, ColorOf(layer, p))).ToList();
        var filter = load.Manifest!.Filter!;
        bool byFuel = layer is MarkerLayer.PowerPlants or MarkerLayer.Renewables;
        var measures = $"{LayerCatalog.Label(layer)}: {filter}{(byFuel ? ", " + ColouredByLargestFuel : "")}"
            + (layer == MarkerLayer.Renewables ? "; " + OtherFuelsNotCounted : "");
        var note = (layer == MarkerLayer.Renewables ? OverlapNote + " " : "") + Credit(load.Manifest);
        return new PointSet(items, c, note) { Measures = measures };
    }

    /// <summary>
    /// The legend's note (CC BY 4.0): GEM's citation, the licence and its link, and what EarthRelief3D changed:
    /// "Global Energy Monitor, Global Integrated Power Tracker, September 2026 release. Licence: CC BY 4.0,
    /// https://creativecommons.org/licenses/by/4.0/. Changed: filtered to …".
    /// </summary>
    public static string Credit(DatasetManifest m)
    {
        ArgumentNullException.ThrowIfNull(m);
        return $"{m.Attribution} Licence: {m.Licence}{(m.LicenceUrl is { } url ? ", " + url : "")}." + (m.Changes is { } ch ? $" Changed: {ch}." : "");
    }

    /// <summary>
    /// The one "GEM" status dot: green with each tracker's release when all three loaded, otherwise amber naming the one
    /// that did not and why.
    /// </summary>
    public static FeedStatus Status(IReadOnlyList<(MarkerLayer Layer, BundledPointsLoad Load)> loads)
    {
        ArgumentNullException.ThrowIfNull(loads);
        var parts = loads.Select(l => $"{Short(l.Layer)} " + (l.Load.Loaded ? CrisisText.DataDate(l.Load.Citation!, DateTime.MaxValue) : $"not loaded ({l.Load.Problem})"));
        var note = string.Join("; ", parts) + " · " + BundledDataset.StatusNote;
        bool ok = loads.Count > 0 && loads.All(l => l.Load.Loaded);
        return new FeedStatus(DotName, ok ? FeedState.Ok : FeedState.Attention, null, ok ? null : "not loaded", 0, default, note);
    }

    private static string Short(MarkerLayer layer) => layer switch
    {
        MarkerLayer.PowerPlants => "power",
        MarkerLayer.Renewables => "renewables",
        MarkerLayer.OilGasFields => "oil and gas",
        MarkerLayer.SteelPlants => "steel",
        _ => LayerCatalog.Label(layer),
    };
}
