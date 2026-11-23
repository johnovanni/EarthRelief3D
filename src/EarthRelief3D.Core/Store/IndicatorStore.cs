using EarthRelief3D.Core.Model;
using GlobeCore.Stores;

namespace EarthRelief3D.Core.Store;

/// <summary>The country-value series by id. Compares by value, so the store can skip writes that change nothing.</summary>
public sealed record IndicatorSnapshot(IReadOnlyDictionary<string, IndicatorSeries> Series)
{
    public static readonly IndicatorSnapshot Empty = new(new Dictionary<string, IndicatorSeries>(StringComparer.Ordinal));

    /// <summary>The series, or null while it has not been loaded (ruling R2: its layer says "Data not loaded yet").</summary>
    public IndicatorSeries? Get(string seriesId) => Series.TryGetValue(seriesId, out var s) ? s : null;

    public bool Equals(IndicatorSnapshot? o)
    {
        if (o is null) return false;
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
            return new IndicatorSnapshot(all);
        });
    }

    /// <summary>Replaces everything with a cached snapshot (at start-up).</summary>
    public bool Load(IndicatorSnapshot cached) => _store.Write(s => s.Equals(cached) ? null : cached);
}
