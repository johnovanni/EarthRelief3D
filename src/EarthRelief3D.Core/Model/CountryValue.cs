using GlobeCore.Geo;

namespace EarthRelief3D.Core.Model;

/// <summary>
/// One country's figure in a series: the value shown on the map (already in the series' wording, so a "without" share is
/// stored as the share without), the year it is for, and where it came from.
/// </summary>
/// <param name="Qualifier">
/// The source's own text for the value when the number alone would say more than the source does, printed instead of the
/// number: FAO's "&lt;2.5" (stored as 2.5, shown "under 2.5%", ruling R3), or WRI's category label. Null for a plain figure.
/// </param>
public sealed record CountryValue(string Iso3, double Value, int Year, SourceCitation Citation, string? Qualifier = null);

/// <summary>
/// A country-value series, such as the share of people without at least basic drinking water. Compares by value (the
/// values by content), so the store can skip writes that change nothing.
/// </summary>
/// <param name="Id">The store key, such as "gho.water" or "fao.undernourishment".</param>
/// <param name="Measures">Exactly what is measured, as the legend says it, without the year (§7): "Share of people without at least basic sanitation".</param>
/// <param name="Unit">"%", "thousand barrels per day", "category", ...</param>
public sealed record IndicatorSeries(string Id, string Measures, string Unit, IReadOnlyList<CountryValue> Values)
{
    /// <summary>The country's newest figure (by year; the later one in the list for a tie), or null when the series has none.</summary>
    public CountryValue? For(string iso3)
    {
        CountryValue? best = null;
        foreach (var v in Values)
            if (string.Equals(v.Iso3, iso3, StringComparison.OrdinalIgnoreCase) && (best is null || v.Year >= best.Year)) best = v;
        return best;
    }

    public bool Equals(IndicatorSeries? o) => o is not null && Id == o.Id && Measures == o.Measures && Unit == o.Unit
        && ListEquality.Equal(Values, o.Values);

    public override int GetHashCode() => HashCode.Combine(Id, Measures, Unit, Values.Count);
}
