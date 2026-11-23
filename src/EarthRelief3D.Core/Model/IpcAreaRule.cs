namespace EarthRelief3D.Core.Model;

/// <summary>Where the 20% rule's denominator came from.</summary>
public enum IpcDenominator
{
    /// <summary>Neither the people analysed nor any phase count: the share is unknown.</summary>
    Unknown,
    /// <summary>"Population analyzed current" (at least the sum of the five phases).</summary>
    Analysed,
    /// <summary>The sum of the five phases, because the people analysed is blank.</summary>
    PhaseSum,
    /// <summary>The sum of the five phases, because it is larger than the people analysed (Sudan's sample row: by 30).</summary>
    PhaseSumLarger,
}

/// <summary>
/// The result of <see cref="IpcAreaRule.Classify"/> for one IPC analysis (ruling R17). One denominator, D, serves the rule,
/// the panel's figures and the phase bar, so the bar's marker always falls in <see cref="LinePhase"/>.
/// </summary>
public sealed record IpcAreaClass
{
    /// <summary>
    /// The phase the "worst 20%" line falls in (1 to 5): the highest phase p such that the people in Phase p or higher are at
    /// least 20% of <see cref="Denominator"/>. Null when the share is unknown, or when even all the people in a reported
    /// phase are under 20% of D (the line then falls among the people in no reported phase).
    /// </summary>
    public int? LinePhase { get; init; }

    /// <summary><see cref="LinePhase"/> when it is 3, 4 or 5; null otherwise (Phases 1 and 2 are never shaded).</summary>
    public int? ShadedPhase { get; init; }

    /// <summary>The worst phase from 3 to 5 that has people in it, when it is worse than <see cref="ShadedPhase"/> (or nothing is shaded).</summary>
    public int? OutlinePhase { get; init; }

    /// <summary>
    /// The faint tint's phase: <see cref="OutlinePhase"/> when nothing is shaded ("outline only": some people in Phase 3 or
    /// worse, but no phase covers 20%); null for a shaded country.
    /// </summary>
    public int? FaintPhase => ShadedPhase is null ? OutlinePhase : null;

    /// <summary>
    /// D: the people analysed when that is at least the sum of the five phases; the sum when it is larger or the people
    /// analysed is blank; 0 when neither is known ("share unknown").
    /// </summary>
    public long Denominator { get; init; }

    public IpcDenominator Source { get; init; }

    /// <summary>The sum of the five phase counts (blank cells count 0).</summary>
    public long PhaseSum { get; init; }

    /// <summary>D minus <see cref="PhaseSum"/>: people analysed but in no reported phase (0 when D is the sum).</summary>
    public long NotInAPhase { get; init; }

    /// <summary>
    /// The people in Phase 3 or higher as the rule counts them: the three counts, or the file's "Phase 3+ number" when the
    /// three are blank. The panel shows this number with <see cref="Phase3PlusShare"/>, so the two always agree.
    /// </summary>
    public long Phase3PlusPeople { get; init; }

    /// <summary><see cref="Phase3PlusPeople"/> as a fraction of D (0 when the share is unknown).</summary>
    public double Phase3PlusShare { get; init; }

    /// <summary>Phases 1 to 5 as fractions of D (all 0 when the share is unknown).</summary>
    public IReadOnlyList<double> Shares { get; init; } = new double[5];

    /// <summary>
    /// The five phases can be stacked into a bar whose marker matches the rule: some phase has people, and Phases 3 to 5
    /// are not blank while a "Phase 3+ number" is given (the rule would then use a figure the bar cannot show).
    /// </summary>
    public bool CanStack { get; init; }

    /// <summary>There is a denominator: the shares and the 20% rule can be worked out.</summary>
    public bool ShareKnown => Denominator > 0;

    /// <summary><see cref="NotInAPhase"/> as a fraction of D.</summary>
    public double NotInAPhaseShare => ShareKnown ? (double)NotInAPhase / Denominator : 0;

    /// <summary>Phase <paramref name="phase"/>'s share (1 to 5) of D.</summary>
    public double Share(int phase) => Shares[IpcAreaRule.Index(phase)];

    /// <summary>The share of D in Phase <paramref name="phase"/> or higher, from the per-phase counts.</summary>
    public double ShareAtOrAbove(int phase) => Shares.Skip(IpcAreaRule.Index(phase)).Sum();

    public bool Equals(IpcAreaClass? o) => o is not null && LinePhase == o.LinePhase && ShadedPhase == o.ShadedPhase
        && OutlinePhase == o.OutlinePhase && Denominator == o.Denominator && Source == o.Source && PhaseSum == o.PhaseSum
        && Phase3PlusPeople == o.Phase3PlusPeople && CanStack == o.CanStack && Shares.SequenceEqual(o.Shares);

    public override int GetHashCode() => HashCode.Combine(LinePhase, ShadedPhase, OutlinePhase, Denominator, Source, PhaseSum);
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
        // One denominator D for the rule, the figures and the bar.
        var (denominator, source) =
            f.PopulationAnalysed > 0 && f.PopulationAnalysed >= sum ? (f.PopulationAnalysed, IpcDenominator.Analysed)
            : sum > 0 ? (sum, f.PopulationAnalysed > 0 ? IpcDenominator.PhaseSumLarger : IpcDenominator.PhaseSum)
            : (0L, IpcDenominator.Unknown);

        // The people in Phase p or higher. When the Phase 3 to 5 counts are blank, the file's own "Phase 3+ number" stands
        // for them (in Phase 3 or higher, and so also in Phase 1 or 2 or higher).
        bool threePlusFallback = people[2] + people[3] + people[4] == 0 && f.Phase3PlusPeople > 0;
        long AtOrAbove(int p)
        {
            long n = people.Skip(p - 1).Sum();
            return threePlusFallback && p <= 3 ? n + f.Phase3PlusPeople : n;
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
        long threePlus = AtOrAbove(3);
        return new IpcAreaClass
        {
            LinePhase = line,
            ShadedPhase = shaded,
            OutlinePhase = outline,
            Denominator = denominator,
            Source = source,
            PhaseSum = sum,
            NotInAPhase = Math.Max(0, denominator - sum),
            Phase3PlusPeople = threePlus,
            Phase3PlusShare = denominator > 0 ? (double)threePlus / denominator : 0,
            Shares = shares,
            CanStack = sum > 0 && !threePlusFallback,
        };
    }

    /// <summary>
    /// What the map draws for <paramref name="f"/>: the fill phase, the outline phase, and the faint tint (the outline's
    /// phase, for a country that has an outline but no shaded phase). Stale and projection-only records are neither shaded
    /// nor outlined (P2-4).
    /// </summary>
    public static IpcMapStyle MapStyle(FoodCrisis f)
    {
        ArgumentNullException.ThrowIfNull(f);
        if (f.IsStale || f.ProjectionOnly) return IpcMapStyle.None;
        var c = Classify(f);
        return new IpcMapStyle(c.ShadedPhase, c.OutlinePhase, c.FaintPhase);
    }
}

/// <summary>
/// The map's drawing of one country (R17). <paramref name="Faint"/> (owner's decision, round 2): a country with people in
/// Phase 3 or worse but no phase covering 20% gets a faint tint in its outline's phase, with the outline; a shaded country
/// never gets one.
/// </summary>
public readonly record struct IpcMapStyle(int? Shaded, int? Outline, int? Faint)
{
    public static IpcMapStyle None => default;

    public bool IsEmpty => Shaded is null && Outline is null && Faint is null;
}
