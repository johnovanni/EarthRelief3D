using System.Text.Json.Serialization;
using AstroCore.Earth;
using EarthRelief3D.Core.Logic;
using GlobeCore.Geo;

namespace EarthRelief3D.Core.Model;

public enum CrisisKind { ArmedConflict, FoodCrisis, Outbreak }

/// <summary>The source's own measure, shown as is (C7): the 1-5 <see cref="Crisis.Band"/> only sizes and colours markers.</summary>
public sealed record NativeSeverity(double Value, string ScaleName, string Text);

/// <summary>
/// Value equality: list-valued members compare by content (like EarthWatch3D's CycloneAdvisory), so the store can tell
/// a real change from a re-fetch of identical data.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(ConflictCrisis), "conflict")]
[JsonDerivedType(typeof(FoodCrisis), "food")]
[JsonDerivedType(typeof(OutbreakCrisis), "outbreak")]
public abstract record Crisis(string Id, CrisisKind Kind, string CountryIso3, string Title, NativeSeverity Severity,
    int Band, DateTime? Began, Trend? Trend, IReadOnlyList<SourceCitation> Citations)
{
    public virtual bool Equals(Crisis? o) => o is not null && EqualityContract == o.EqualityContract
        && Id == o.Id && Kind == o.Kind && CountryIso3 == o.CountryIso3 && Title == o.Title && Severity == o.Severity
        && Band == o.Band && Began == o.Began && Trend == o.Trend && ListEquality.Equal(Citations, o.Citations);

    public override int GetHashCode() => HashCode.Combine(Id, Kind, CountryIso3, Title, Band, Began, ListEquality.Hash(Citations));
}

public sealed record ConflictEvent(long Id, DateTime StartUtc, GeoPosition Position, int BestDeaths, int ViolenceType);

public sealed record ConflictCrisis(string Id, string CountryIso3, string Title, NativeSeverity Severity, int Band,
    DateTime? Began, Trend? Trend, IReadOnlyList<SourceCitation> Citations,
    int UcdpConflictId, string SideA, string SideB, IReadOnlyList<ConflictEvent> Events,
    IReadOnlyDictionary<DateOnly, int> DeathsByMonth)
    : Crisis(Id, CrisisKind.ArmedConflict, CountryIso3, Title, Severity, Band, Began, Trend, Citations)
{
    public bool Equals(ConflictCrisis? o) => base.Equals(o) && UcdpConflictId == o!.UcdpConflictId && SideA == o.SideA
        && SideB == o.SideB && ListEquality.Equal(Events, o.Events) && MapEqual(DeathsByMonth, o.DeathsByMonth);

    public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), UcdpConflictId, SideA, SideB,
        ListEquality.Hash(Events), DeathsByMonth?.Count);

    private static bool MapEqual(IReadOnlyDictionary<DateOnly, int>? a, IReadOnlyDictionary<DateOnly, int>? b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a is null || b is null || a.Count != b.Count) return false;
        foreach (var (k, v) in a) if (!b.TryGetValue(k, out var w) || v != w) return false;
        return true;
    }
}

/// <summary>
/// One country's IPC national analysis. Most are current-period figures: a crisis when Phase 3 or higher has people in it
/// and the period ended within <see cref="StaleAfterMonths"/> months (P2-4). An analysis with no current-period figures
/// but a projection is kept as a <see cref="ProjectionOnly"/> record: never a crisis marker, but not "no crisis" either.
/// </summary>
/// <param name="PeriodFrom">The current period's start; for a <see cref="ProjectionOnly"/> record, the projection's.</param>
/// <param name="Phase3PlusPeople">People in Phase 3 or higher in that period (for a projection-only record, projected).</param>
/// <param name="Phase3PlusShare">The same as a fraction (0.41) of the people analysed.</param>
/// <param name="IsStale">The period ended more than <see cref="StaleAfterMonths"/> months before the feed ran; see also <see cref="IsStaleAt"/>.</param>
public sealed record FoodCrisis(string Id, string CountryIso3, string Title, NativeSeverity Severity, int Band,
    DateTime? Began, Trend? Trend, IReadOnlyList<SourceCitation> Citations,
    int HighestPhase, long Phase3PlusPeople, double Phase3PlusShare, DateOnly PeriodFrom, DateOnly PeriodTo, bool IsStale)
    : Crisis(Id, CrisisKind.FoodCrisis, CountryIso3, Title, Severity, Band, Began, Trend, Citations)
{
    /// <summary>An analysis whose period ended more than this many months ago is not a current crisis (P2-4).</summary>
    public const int StaleAfterMonths = 12;

    /// <summary>People in Phase 3 (Crisis) in the current period (ruling R16); 0 when the file leaves it blank.</summary>
    public long Phase3People { get; init; }

    /// <summary>People in Phase 4 (Emergency) in the current period.</summary>
    public long Phase4People { get; init; }

    /// <summary>People in Phase 5 (Catastrophe/Famine) in the current period.</summary>
    public long Phase5People { get; init; }

    /// <summary>
    /// The analysis has no current-period figures, only a projection (<see cref="ProjectionName"/>); the period and the
    /// Phase 3+ figures are the projection's. Not a crisis marker, and not "no crisis" either.
    /// </summary>
    public bool ProjectionOnly { get; init; }

    /// <summary>"first projection" or "second projection" for a <see cref="ProjectionOnly"/> record, else null.</summary>
    public string? ProjectionName { get; init; }

    /// <summary>Stale (P2-4) when judged at <paramref name="nowUtc"/>: a cached analysis ages while the app runs or waits.</summary>
    public bool IsStaleAt(DateTime nowUtc) => IsStale || PeriodTo < DateOnly.FromDateTime(nowUtc).AddMonths(-StaleAfterMonths);

    public bool Equals(FoodCrisis? o) => base.Equals(o) && HighestPhase == o!.HighestPhase
        && Phase3PlusPeople == o.Phase3PlusPeople && Phase3PlusShare.Equals(o.Phase3PlusShare)
        && PeriodFrom == o.PeriodFrom && PeriodTo == o.PeriodTo && IsStale == o.IsStale
        && Phase3People == o.Phase3People && Phase4People == o.Phase4People && Phase5People == o.Phase5People
        && ProjectionOnly == o.ProjectionOnly && ProjectionName == o.ProjectionName;

    public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), HighestPhase, Phase3PlusPeople, PeriodFrom, PeriodTo, IsStale,
        HashCode.Combine(Phase3People, Phase4People, Phase5People, ProjectionOnly, ProjectionName));
}

public sealed record OutbreakCrisis(string Id, string CountryIso3, string Title, NativeSeverity Severity, int Band,
    DateTime? Began, Trend? Trend, IReadOnlyList<SourceCitation> Citations,
    string Disease, string DonId, DateTime PublishedUtc, Uri ReportUrl)
    : Crisis(Id, CrisisKind.Outbreak, CountryIso3, Title, Severity, Band, Began, Trend, Citations)
{
    /// <summary>A WHO Outbreak News report is a crisis for this long after it is published (C11).</summary>
    public static readonly TimeSpan Window = TimeSpan.FromDays(90);

    /// <summary>Still inside <see cref="Window"/> at <paramref name="nowUtc"/> (a cached report ages while the app runs or waits).</summary>
    public bool IsCurrentAt(DateTime nowUtc) => PublishedUtc >= nowUtc - Window;

    public bool Equals(OutbreakCrisis? o) => base.Equals(o) && Disease == o!.Disease && DonId == o.DonId
        && PublishedUtc == o.PublishedUtc && ReportUrl == o.ReportUrl;

    public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), Disease, DonId, PublishedUtc, ReportUrl);
}
