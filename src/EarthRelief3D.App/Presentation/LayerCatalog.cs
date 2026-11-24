namespace EarthRelief3D.App.Presentation;

/// <summary>
/// The names of every shading and marker layer (Plan 3): the strip's label and tooltip, the snapshot scenes' name, the
/// series each shading draws from, and what the legend says before the data is loaded (ruling R2).
/// </summary>
public static class LayerCatalog
{
    /// <summary>The legend's date text for a layer whose data is not loaded yet (ruling R2): it draws nothing.</summary>
    public const string NotLoaded = "Data not loaded yet";

    /// <summary>The shadings of the Needs group, in strip order.</summary>
    public static IReadOnlyList<ShadingLayer> NeedsShadings { get; } =
        [ShadingLayer.Water, ShadingLayer.Sanitation, ShadingLayer.Health, ShadingLayer.Hunger, ShadingLayer.ShelterNeed];

    /// <summary>The shadings of the Resources group, in strip order.</summary>
    public static IReadOnlyList<ShadingLayer> ResourceShadings { get; } = [ShadingLayer.OilProduction, ShadingLayer.WaterStress];

    /// <summary>The marker layers of the Resources group, in strip order.</summary>
    public static IReadOnlyList<MarkerLayer> ResourceMarkers { get; } = [MarkerLayer.PowerPlants, MarkerLayer.Renewables, MarkerLayer.OilGasFields, MarkerLayer.SteelPlants];

    /// <summary>The marker layers Plan 3 adds to the Crises group, after Plan 2's four layers.</summary>
    public static IReadOnlyList<MarkerLayer> CrisisMarkersAdded { get; } = [MarkerLayer.Atrocities, MarkerLayer.Camps];

    /// <summary>The Plan 2 marker layers, on by default.</summary>
    public static IReadOnlyList<MarkerLayer> DefaultMarkers { get; } = [MarkerLayer.Conflict, MarkerLayer.Outbreaks, MarkerLayer.Displacement];

    // ---------------------------------------------------------------- shadings

    /// <summary>The name in snapshot scenes and <c>--layers</c> (and the layer's id on the globe).</summary>
    public static string Id(ShadingLayer layer) => layer switch
    {
        ShadingLayer.Food => "food",
        ShadingLayer.Water => "water",
        ShadingLayer.Sanitation => "sanitation",
        ShadingLayer.Health => "health",
        ShadingLayer.Hunger => "hunger",
        ShadingLayer.ShelterNeed => "shelter",
        ShadingLayer.OilProduction => "oil",
        ShadingLayer.WaterStress => "waterstress",
        _ => throw new ArgumentOutOfRangeException(nameof(layer)),
    };

    public static string Label(ShadingLayer layer) => layer switch
    {
        ShadingLayer.Food => "Food crisis",
        ShadingLayer.Water => "Water",
        ShadingLayer.Sanitation => "Sanitation",
        ShadingLayer.Health => "Health care",
        ShadingLayer.Hunger => "Hunger",
        ShadingLayer.ShelterNeed => "Shelter need",
        ShadingLayer.OilProduction => "Oil production",
        ShadingLayer.WaterStress => "Water stress",
        _ => throw new ArgumentOutOfRangeException(nameof(layer)),
    };

    /// <summary>
    /// The store id of the series the shading draws (<see cref="EarthRelief3D.Core.Store.IndicatorStore"/>). The series
    /// holds the value shown: for water and sanitation the share <em>without</em> (100 minus WHO's figure), for health
    /// care 100 minus the UHC index (Task 3 converts).
    /// </summary>
    public static string SeriesId(ShadingLayer layer) => layer switch
    {
        ShadingLayer.Water => "gho.water",
        ShadingLayer.Sanitation => "gho.sanitation",
        ShadingLayer.Health => "gho.uhc",
        ShadingLayer.Hunger => "fao.undernourishment",
        ShadingLayer.ShelterNeed => "hno.shelter",
        ShadingLayer.OilProduction => "eia.crudeOil",
        ShadingLayer.WaterStress => "wri.bws",
        _ => throw new ArgumentOutOfRangeException(nameof(layer), layer, "Only the country-value shadings draw a series."),
    };

    /// <summary>The source's short name, as in the tooltip ("… · WHO GHO") and the status dot.</summary>
    public static string SourceName(ShadingLayer layer) => layer switch
    {
        ShadingLayer.Food => "IPC",
        ShadingLayer.Water or ShadingLayer.Sanitation or ShadingLayer.Health => "WHO GHO",
        ShadingLayer.Hunger => "FAO",
        ShadingLayer.ShelterNeed => "UN HNO",
        ShadingLayer.OilProduction => "US EIA",
        ShadingLayer.WaterStress => "WRI Aqueduct",
        _ => throw new ArgumentOutOfRangeException(nameof(layer)),
    };

    /// <summary>
    /// What the shading measures, as the legend says it until its series is loaded (afterwards the series'
    /// <see cref="EarthRelief3D.Core.Model.IndicatorSeries.Measures"/> is used, with the year).
    /// </summary>
    public static string DefaultMeasures(ShadingLayer layer) => layer switch
    {
        ShadingLayer.Water => "Share of people without at least basic drinking water",
        ShadingLayer.Sanitation => "Share of people without at least basic sanitation",
        ShadingLayer.Health => "Essential health services not covered: 100 minus WHO's UHC service coverage index",
        ShadingLayer.Hunger => "Prevalence of undernourishment (share of people, 3-year average)",
        ShadingLayer.ShelterNeed => "People in need of shelter, as a share of the population (UN humanitarian needs overviews)",
        ShadingLayer.OilProduction => "Crude oil production, including lease condensate, in thousand barrels per day",
        ShadingLayer.WaterStress => "Baseline water stress (WRI Aqueduct category)",
        _ => throw new ArgumentOutOfRangeException(nameof(layer)),
    };

    /// <summary>The strip's tooltip: what is measured, by whom, and the one-shading rule (P3-6).</summary>
    public static string Tooltip(ShadingLayer layer) => layer == ShadingLayer.Food
        ? throw new ArgumentOutOfRangeException(nameof(layer), layer, "The food tooltip is LegendViewModel.FoodTooltip (R17).")
        : $"{Label(layer)} ({SourceName(layer)}): {LowerFirst(DefaultMeasures(layer))}. A country with no figure is not shaded. "
          + OneShadingNote;

    /// <summary>Said by every shading toggle.</summary>
    public const string OneShadingNote = "One shading shows at a time: turning this on turns the other off.";

    // ---------------------------------------------------------------- markers

    /// <summary>The name in snapshot scenes and <c>--layers</c> (and the layer's id on the globe).</summary>
    public static string Id(MarkerLayer layer) => layer switch
    {
        MarkerLayer.Conflict => "conflict",
        MarkerLayer.Outbreaks => "outbreaks",
        MarkerLayer.Displacement => "displacement",
        MarkerLayer.Atrocities => "atrocities",
        MarkerLayer.Camps => "camps",
        MarkerLayer.PowerPlants => "power",
        MarkerLayer.OilGasFields => "oilgas",
        MarkerLayer.SteelPlants => "steel",
        MarkerLayer.Renewables => "renewables",
        _ => throw new ArgumentOutOfRangeException(nameof(layer)),
    };

    public static string Label(MarkerLayer layer) => layer switch
    {
        MarkerLayer.Conflict => "Armed conflict",
        MarkerLayer.Outbreaks => "Disease outbreaks",
        MarkerLayer.Displacement => "People forced from home",
        MarkerLayer.Atrocities => "Genocide and mass atrocities",
        MarkerLayer.Camps => "Prison camps",
        MarkerLayer.PowerPlants => "Power plants",
        MarkerLayer.OilGasFields => "Oil and gas fields",
        MarkerLayer.SteelPlants => "Steel plants",
        MarkerLayer.Renewables => "Renewable power plants",
        _ => throw new ArgumentOutOfRangeException(nameof(layer)),
    };

    /// <summary>The source's name, as the legend says it until the layer's data is loaded.</summary>
    public static string SourceName(MarkerLayer layer) => layer switch
    {
        MarkerLayer.Conflict => "Uppsala Conflict Data Program (UCDP)",
        MarkerLayer.Outbreaks => "World Health Organization, Disease Outbreak News",
        MarkerLayer.Displacement => "UNHCR, Refugee Population Statistics Database",
        MarkerLayer.Atrocities => "Courts, UN bodies and governments (curated, cited)",
        MarkerLayer.Camps => "UN Commission of Inquiry and HRNK reports (curated, cited)",
        MarkerLayer.PowerPlants => "Global Energy Monitor, Global Integrated Power Tracker",
        MarkerLayer.OilGasFields => "Global Energy Monitor, Global Oil and Gas Extraction Tracker",
        MarkerLayer.SteelPlants => "Global Energy Monitor, Global Iron and Steel Tracker",
        MarkerLayer.Renewables => "Global Energy Monitor, Global Integrated Power Tracker",
        _ => throw new ArgumentOutOfRangeException(nameof(layer)),
    };

    /// <summary>What a Plan 3 marker layer shows, for its legend line. (The Plan 2 layers' wording is LegendViewModel.Measures.)</summary>
    public static string Measures(MarkerLayer layer) => $"{Label(layer)}: {What(layer)}";

    private static string What(MarkerLayer layer) => layer switch
    {
        MarkerLayer.Atrocities => "findings by courts, UN bodies and governments, at the country",
        MarkerLayer.Camps => "documented sites, each with its source",
        MarkerLayer.PowerPlants => "operating plants of 500 MW or more (units summed per plant), coloured by its largest fuel",
        MarkerLayer.OilGasFields => "operating fields, leaving out the smallest reported producers (the legend gives the threshold)",
        MarkerLayer.SteelPlants => "operating plants of 1 million tonnes of crude steel a year or more",
        MarkerLayer.Renewables => "operating wind, solar, hydro, geothermal and bioenergy plants, coloured by its largest fuel (the legend gives the size threshold; large ones are also in Power plants)",
        _ => throw new ArgumentOutOfRangeException(nameof(layer), layer, "The Plan 2 layers' wording is in LegendViewModel.Measures."),
    };

    /// <summary>The strip's tooltip for a marker layer.</summary>
    public static string Tooltip(MarkerLayer layer) => layer switch
    {
        MarkerLayer.Conflict => "Armed conflict (UCDP): a dot at each event of a conflict with 25 or more deaths in a year. Bigger dots: more deaths in the conflict; lighter dots: newer events.",
        MarkerLayer.Outbreaks => "Disease outbreaks (WHO Disease Outbreak News): a cross at each country with a report in the last 90 days, labelled with the disease.",
        MarkerLayer.Displacement => "People forced from home (UNHCR): a ring at each country of origin; its area grows with the number of refugees, asylum-seekers and internally displaced people.",
        _ => $"{Label(layer)}: {What(layer)}. Source: {SourceName(layer)}. Marker layers can be shown together.",
    };

    // ---------------------------------------------------------------- names

    /// <summary>The shading or marker layer named <paramref name="name"/> in a scene or <c>--layers</c> (case does not matter).</summary>
    public static bool TryParse(string name, out ShadingLayer? shading, out MarkerLayer? marker)
    {
        shading = Enum.GetValues<ShadingLayer>().Where(l => l != ShadingLayer.None)
            .Select(l => (ShadingLayer?)l).FirstOrDefault(l => string.Equals(Id(l!.Value), name, StringComparison.OrdinalIgnoreCase));
        marker = Enum.GetValues<MarkerLayer>()
            .Select(l => (MarkerLayer?)l).FirstOrDefault(l => string.Equals(Id(l!.Value), name, StringComparison.OrdinalIgnoreCase));
        return shading is not null || marker is not null;
    }

    /// <summary>Every layer name, shadings first (food first), then markers.</summary>
    public static IReadOnlyList<string> AllNames { get; } =
    [
        .. Enum.GetValues<ShadingLayer>().Where(l => l != ShadingLayer.None).Select(Id),
        .. Enum.GetValues<MarkerLayer>().Select(Id),
    ];

    /// <summary>"Share of …" → "share of …", leaving an acronym ("UHC …") as it is.</summary>
    public static string LowerFirst(string text) =>
        text.Length > 1 && char.IsUpper(text[0]) && char.IsLower(text[1]) ? char.ToLowerInvariant(text[0]) + text[1..] : text;
}
