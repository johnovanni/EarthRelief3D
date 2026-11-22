namespace EarthRelief3D.Core.Logic;

/// <summary>Maps each source's native measure to the 1-5 band that sizes and colours markers. See docs/severity-bands.md.</summary>
public static class SeverityBands
{
    /// <summary>0 below 25 deaths (no crisis, C11); 1 for 25-999; 2 for 1,000-9,999; 3 for 10,000-49,999; 4 for 50,000 and up.</summary>
    public static int ForConflict(int deathsLast12Months) => deathsLast12Months switch
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
