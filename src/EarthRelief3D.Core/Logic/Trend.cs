namespace EarthRelief3D.Core.Logic;

/// <summary>Counts over the last 3 complete months against the 3 before.</summary>
public sealed record Trend(long Recent, long Previous, string Window)
{
    /// <summary>
    /// "Last 3 months" are the 3 calendar months ending at <paramref name="latestCompleteMonth"/> inclusive; "the 3 before"
    /// are the 3 months before those. Months are keyed by any day of the month (normalised to the 1st). Null if any of
    /// the 6 months is missing from <paramref name="byMonth"/> (a month with no events must be present as 0).
    /// </summary>
    public static Trend? FromMonths(IReadOnlyDictionary<DateOnly, int> byMonth, DateOnly latestCompleteMonth)
    {
        var last = new DateOnly(latestCompleteMonth.Year, latestCompleteMonth.Month, 1);
        var months = new Dictionary<DateOnly, int>();
        foreach (var (k, v) in byMonth) months[new DateOnly(k.Year, k.Month, 1)] = v;
        long recent = 0, previous = 0;
        for (var i = 0; i < 6; i++)
        {
            if (!months.TryGetValue(last.AddMonths(-i), out var n)) return null;
            if (i < 3) recent += n; else previous += n;
        }
        return new Trend(recent, previous, "last 3 months vs the 3 before");
    }
}
