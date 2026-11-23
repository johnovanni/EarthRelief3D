namespace EarthRelief3D.Core.Model;

/// <summary>
/// The result of <see cref="IpcAreaRule.Classify"/> for one IPC analysis (ruling R17).
/// </summary>
/// <param name="LinePhase">The phase the "worst 20%" line falls in (1 to 5): the highest phase p such that the people in
/// Phase p or higher are at least 20% of <paramref name="Denominator"/>. Null when the share is unknown.</param>
/// <param name="ShadedPhase"><paramref name="LinePhase"/> when it is 3, 4 or 5 (the country is shaded in that phase's
/// colour); null otherwise (Phases 1 and 2 are never shaded: the layer shows food crises).</param>
/// <param name="OutlinePhase">The worst phase from 3 to 5 that has people in it, when it is worse than
/// <paramref name="ShadedPhase"/> (or nothing is shaded); null otherwise.</param>
/// <param name="Denominator">The people analysed, or the sum of the Phase 1 to 5 counts when that is blank; 0 when neither
/// is known ("share unknown").</param>
/// <param name="Shares">Phases 1 to 5 as fractions of <paramref name="Denominator"/> (all 0 when the share is unknown).</param>
public sealed record IpcAreaClass(int? LinePhase, int? ShadedPhase, int? OutlinePhase, long Denominator, IReadOnlyList<double> Shares)
{
    /// <summary>There is a denominator: the shares and the 20% rule can be worked out.</summary>
    public bool ShareKnown => Denominator > 0;

    /// <summary>Phase <paramref name="phase"/>'s share (1 to 5) of the people analysed.</summary>
    public double Share(int phase) => Shares[IpcAreaRule.Index(phase)];

    /// <summary>The share of the people analysed in Phase <paramref name="phase"/> or higher.</summary>
    public double ShareAtOrAbove(int phase) => Shares.Skip(IpcAreaRule.Index(phase)).Sum();

    public bool Equals(IpcAreaClass? o) => o is not null && LinePhase == o.LinePhase && ShadedPhase == o.ShadedPhase
        && OutlinePhase == o.OutlinePhase && Denominator == o.Denominator && Shares.SequenceEqual(o.Shares);

    public override int GetHashCode() => HashCode.Combine(LinePhase, ShadedPhase, OutlinePhase, Denominator);
}

/// <summary>
/// How the food layer classifies a country (ruling R17, the owner's decision of 2026-11-23): IPC's "20% rule" for area
/// classification, applied here to the whole country (our adaptation: the national file has no areas). A country is shaded
/// in the colour of the worst phase that, with the phases above it, covers at least 20% of the people analysed; only Phases
/// 3 to 5 are shaded. An outline in a worse phase's colour shows that some people are in that phase. Exact counts are used,
/// never the file's rounded percentages. One function, used by the map, the tooltip, the legend's wording and the panel.
/// </summary>
public static class IpcAreaRule
{
    /// <summary>IPC's area threshold: 20% of the people analysed (compared in whole numbers: 5 x people >= analysed).</summary>
    public const double Threshold = 0.20;

    /// <summary>The people in <paramref name="phase"/> (1 to 5) in the current period; 0 when the file leaves it blank.</summary>
    public static long People(FoodCrisis f, int phase) => phase switch
    {
        1 => f.Phase1People,
        2 => f.Phase2People,
        3 => f.Phase3People,
        4 => f.Phase4People,
        5 => f.Phase5People,
        _ => throw new ArgumentOutOfRangeException(nameof(phase), phase, "IPC phases are 1 to 5."),
    };

    internal static int Index(int phase) =>
        phase is >= 1 and <= 5 ? phase - 1 : throw new ArgumentOutOfRangeException(nameof(phase), phase, "IPC phases are 1 to 5.");

    /// <summary>
    /// The 20% rule, the outline and the shares for <paramref name="f"/>'s current-period counts. Stale records are classified
    /// too (the panel's bar for a past analysis); <see cref="MapStyle"/> is what the map draws.
    /// </summary>
    public static IpcAreaClass Classify(FoodCrisis f)
    {
        ArgumentNullException.ThrowIfNull(f);
        var people = Enumerable.Range(1, 5).Select(p => People(f, p)).ToArray();
        long sum = people.Sum();
        long denominator = f.PopulationAnalysed > 0 ? f.PopulationAnalysed : sum > 0 ? sum : 0;

        // The people in Phase p or higher. For Phase 3, the file's own "Phase 3+ number" when the three counts are blank.
        long AtOrAbove(int p)
        {
            long n = people.Skip(p - 1).Sum();
            return p == 3 && n == 0 ? Math.Max(0, f.Phase3PlusPeople) : n;
        }

        int? line = null;
        if (denominator > 0)
            for (int p = 5; p >= 1; p--)
                if (AtOrAbove(p) * 5 >= denominator) { line = p; break; }   // >= 20%, exact
        int? shaded = line is >= 3 ? line : null;

        int? worst = null;
        for (int p = 5; p >= 3; p--)
            if (people[p - 1] > 0) { worst = p; break; }
        // Phase 3+ people but no per-phase counts: the reader's highest phase (3 then).
        if (worst is null && f.Phase3PlusPeople > 0) worst = Math.Clamp(f.HighestPhase, 3, 5);
        int? outline = worst is { } w && w > (shaded ?? 0) ? worst : null;

        double[] shares = denominator > 0 ? [.. people.Select(n => (double)n / denominator)] : new double[5];
        return new IpcAreaClass(line, shaded, outline, denominator, shares);
    }

    /// <summary>
    /// What the map draws for <paramref name="f"/>: the fill phase and the outline phase. Stale and projection-only records
    /// are neither shaded nor outlined (P2-4).
    /// </summary>
    public static (int? Shaded, int? Outline) MapStyle(FoodCrisis f)
    {
        ArgumentNullException.ThrowIfNull(f);
        if (f.IsStale || f.ProjectionOnly) return (null, null);
        var c = Classify(f);
        return (c.ShadedPhase, c.OutlinePhase);
    }
}
