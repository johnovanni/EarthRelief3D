namespace EarthRelief3D.Core.Logic;

/// <summary>Maps each source's native measure to the 1-5 band that sizes and colours markers. See docs/severity-bands.md.</summary>
public static class SeverityBands
{
    /// <summary>
    /// Band from a number of best-estimate deaths: 0 below 25; 1 for 25-999; 2 for 1,000-9,999; 3 for 10,000-49,999; 4 for
    /// 50,000 and up. The UCDP feed passes the deaths of one conflict in one country over the data window (the newest
    /// quarterly file plus later monthly files, about 9 to 12 months), and gives a conflict that qualifies as a crisis
    /// (25 or more deaths in a calendar year over all its countries, C11 and ruling R9) at least band 1 in every country
    /// it has events in, so a country's share below 25 still gets a marker.
    /// </summary>
    public static int ForConflict(int deathsInWindow) => deathsInWindow switch
    {
        < 25 => 0,
        < 1_000 => 1,
        < 10_000 => 2,
        < 50_000 => 3,
        _ => 4,
    };

    /// <summary>The IPC phase itself (3 to 5 for a crisis).</summary>
    public static int ForFood(int highestPhase) => highestPhase;

    /// <summary>A WHO Outbreak News report carries no graded scale, so every outbreak is band 3.</summary>
    public static int ForOutbreak() => 3;
}
