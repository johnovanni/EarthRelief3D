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

public sealed record FoodCrisis(string Id, string CountryIso3, string Title, NativeSeverity Severity, int Band,
    DateTime? Began, Trend? Trend, IReadOnlyList<SourceCitation> Citations,
    int HighestPhase, long Phase3PlusPeople, double Phase3PlusShare, DateOnly PeriodFrom, DateOnly PeriodTo, bool IsStale)
    : Crisis(Id, CrisisKind.FoodCrisis, CountryIso3, Title, Severity, Band, Began, Trend, Citations)
{
    public bool Equals(FoodCrisis? o) => base.Equals(o) && HighestPhase == o!.HighestPhase
        && Phase3PlusPeople == o.Phase3PlusPeople && Phase3PlusShare.Equals(o.Phase3PlusShare)
        && PeriodFrom == o.PeriodFrom && PeriodTo == o.PeriodTo && IsStale == o.IsStale;

    public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), HighestPhase, Phase3PlusPeople, PeriodFrom, PeriodTo, IsStale);
}

public sealed record OutbreakCrisis(string Id, string CountryIso3, string Title, NativeSeverity Severity, int Band,
    DateTime? Began, Trend? Trend, IReadOnlyList<SourceCitation> Citations,
    string Disease, string DonId, DateTime PublishedUtc, Uri ReportUrl)
    : Crisis(Id, CrisisKind.Outbreak, CountryIso3, Title, Severity, Band, Began, Trend, Citations)
{
    public bool Equals(OutbreakCrisis? o) => base.Equals(o) && Disease == o!.Disease && DonId == o.DonId
        && PublishedUtc == o.PublishedUtc && ReportUrl == o.ReportUrl;

    public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), Disease, DonId, PublishedUtc, ReportUrl);
}
