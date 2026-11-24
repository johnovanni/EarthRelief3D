using EarthRelief3D.Core.Model;
using GlobeCore.Geo;
using GlobeCore.Stores;

namespace EarthRelief3D.Core.Store;

/// <summary>
/// The country-value series by id, and the UN humanitarian needs rows (Plan 3: the panel lists them). Compares by value, so
/// the store can skip writes that change nothing.
/// </summary>
public sealed record IndicatorSnapshot(IReadOnlyDictionary<string, IndicatorSeries> Series, IReadOnlyList<HumanitarianNeed> Needs)
{
    public static readonly IndicatorSnapshot Empty = new(new Dictionary<string, IndicatorSeries>(StringComparer.Ordinal));

    public IndicatorSnapshot(IReadOnlyDictionary<string, IndicatorSeries> series) : this(series, []) { }

    /// <summary>The country's HNO rows (empty when it has no plan, or while the file has not been loaded).</summary>
    public IReadOnlyList<HumanitarianNeed> NeedsOf(string iso3) =>
        [.. Needs.Where(n => string.Equals(n.Iso3, iso3, StringComparison.OrdinalIgnoreCase))];

    /// <summary>ACAPS's access assessments (Plan 4); empty until the feed has run.</summary>
    public IReadOnlyList<AccessAssessment> Access { get; init; } = [];

    /// <summary>The year's FTS appeals, with regional plans flagged (Plan 4); empty until the feed has run.</summary>
    public IReadOnlyList<AppealFunding> Appeals { get; init; } = [];

    /// <summary>The country's ACAPS assessment, or null when the file has none for it (or has not been loaded).</summary>
    public AccessAssessment? AccessOf(string iso3) =>
        Access.FirstOrDefault(a => string.Equals(a.Iso3, iso3, StringComparison.OrdinalIgnoreCase));

    /// <summary>The country's appeals for the year (empty when it has none, or while the file has not been loaded).</summary>
    public IReadOnlyList<AppealFunding> AppealsOf(string iso3) =>
        [.. Appeals.Where(a => string.Equals(a.Iso3, iso3, StringComparison.OrdinalIgnoreCase))];

    /// <summary>The series, or null while it has not been loaded (ruling R2: its layer says "Data not loaded yet").</summary>
    public IndicatorSeries? Get(string seriesId) => Series.TryGetValue(seriesId, out var s) ? s : null;

    public bool Equals(IndicatorSnapshot? o)
    {
        if (o is null) return false;
        if (ReferenceEquals(this, o)) return true;
        // The needs first (C6): the same series dictionary must not hide a change in the needs rows.
        if (!ListEquality.Equal(Needs, o.Needs)) return false;
        if (!ListEquality.Equal(Access, o.Access) || !ListEquality.Equal(Appeals, o.Appeals)) return false;
        if (ReferenceEquals(Series, o.Series)) return true;
        if (Series.Count != o.Series.Count) return false;
        foreach (var (id, s) in Series)
            if (!o.Series.TryGetValue(id, out var t) || !s.Equals(t)) return false;
        return true;
    }

    public override int GetHashCode() => Series.Count;
}

/// <summary>
/// Thread-safe holder of the current <see cref="IndicatorSnapshot"/> (as <see cref="CrisisStore"/> for crises): feeds and
/// bundled datasets replace one series at a time. A write that changes nothing (by value) raises no event.
/// </summary>
public sealed class IndicatorStore
{
    private readonly SnapshotStore<IndicatorSnapshot> _store = new(IndicatorSnapshot.Empty);

    public IndicatorSnapshot Snapshot => _store.Snapshot;

    /// <summary>Raised on the writing thread after the snapshot has been replaced.</summary>
    public event EventHandler? Changed
    {
        add => _store.Changed += value;
        remove => _store.Changed -= value;
    }

    /// <summary>Replaces the series <paramref name="seriesId"/> (which must be <paramref name="series"/>' own id); others are untouched.</summary>
    public bool Replace(string seriesId, IndicatorSeries series)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(seriesId);
        ArgumentNullException.ThrowIfNull(series);
        if (series.Id != seriesId) throw new ArgumentException($"The series is {series.Id}, not {seriesId}.", nameof(series));
        return _store.Write(s =>
        {
            if (s.Get(seriesId) is { } old && old.Equals(series)) return null;
            var all = new Dictionary<string, IndicatorSeries>(s.Series, StringComparer.Ordinal) { [seriesId] = series };
            return s with { Series = all };
        });
    }

    /// <summary>Replaces several series in one write (one change event), as a feed's indicators arrive together.</summary>
    public bool ReplaceAll(IReadOnlyList<IndicatorSeries> series)
    {
        ArgumentNullException.ThrowIfNull(series);
        return _store.Write(s =>
        {
            var all = new Dictionary<string, IndicatorSeries>(s.Series, StringComparer.Ordinal);
            var changed = false;
            foreach (var x in series)
            {
                if (s.Get(x.Id) is { } old && old.Equals(x)) continue;
                all[x.Id] = x;
                changed = true;
            }
            return changed ? s with { Series = all } : null;
        });
    }

    /// <summary>
    /// Replaces the HNO rows and the series derived from them in one write, so the map and the panel never see the new rows
    /// with the old shares. A write that changes nothing raises no event.
    /// </summary>
    public bool ReplaceNeeds(IReadOnlyList<HumanitarianNeed> needs, IndicatorSeries series)
    {
        ArgumentNullException.ThrowIfNull(needs);
        ArgumentNullException.ThrowIfNull(series);
        return _store.Write(s =>
        {
            if (ListEquality.Equal(s.Needs, needs) && s.Get(series.Id) is { } old && old.Equals(series)) return null;
            var all = new Dictionary<string, IndicatorSeries>(s.Series, StringComparer.Ordinal) { [series.Id] = series };
            return s with { Series = all, Needs = [.. needs] };
        });
    }

    /// <summary>
    /// Replaces the ACAPS assessments and the access-score series in one write (one change event), so the map and the panel
    /// never see new rows with the old score. A write that changes nothing raises no event.
    /// </summary>
    public bool ReplaceAccess(IReadOnlyList<AccessAssessment> access, IndicatorSeries score)
    {
        ArgumentNullException.ThrowIfNull(access);
        ArgumentNullException.ThrowIfNull(score);
        return _store.Write(s =>
        {
            if (ListEquality.Equal(s.Access, access) && s.Get(score.Id) is { } old && old.Equals(score)) return null;
            var all = new Dictionary<string, IndicatorSeries>(s.Series, StringComparer.Ordinal) { [score.Id] = score };
            return s with { Series = all, Access = [.. access] };
        });
    }

    /// <summary>Replaces the FTS appeals; false (no event) when they are the same.</summary>
    public bool ReplaceAppeals(IReadOnlyList<AppealFunding> appeals)
    {
        ArgumentNullException.ThrowIfNull(appeals);
        return _store.Write(s => ListEquality.Equal(s.Appeals, appeals) ? null : s with { Appeals = [.. appeals] });
    }

    /// <summary>Takes these series out (a bundled extract that did not load, C5); false when none of them was there.</summary>
    public bool Remove(IEnumerable<string> seriesIds)
    {
        ArgumentNullException.ThrowIfNull(seriesIds);
        var ids = seriesIds.ToList();
        return _store.Write(s =>
        {
            if (!ids.Any(s.Series.ContainsKey)) return null;
            var all = new Dictionary<string, IndicatorSeries>(s.Series, StringComparer.Ordinal);
            foreach (var id in ids) all.Remove(id);
            return s with { Series = all };
        });
    }

    /// <summary>Replaces everything with a cached snapshot (at start-up).</summary>
    public bool Load(IndicatorSnapshot cached) => _store.Write(s => s.Equals(cached) ? null : cached);
}
