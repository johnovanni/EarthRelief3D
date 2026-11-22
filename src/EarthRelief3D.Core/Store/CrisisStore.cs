using EarthRelief3D.Core.Model;
using GlobeCore.Stores;

namespace EarthRelief3D.Core.Store;

/// <summary>Thread-safe holder of the current <see cref="CrisisSnapshot"/>. A write that changes nothing (by value) raises no event.</summary>
public sealed class CrisisStore
{
    private readonly SnapshotStore<CrisisSnapshot> _store = new(CrisisSnapshot.Empty);

    public CrisisSnapshot Snapshot => _store.Snapshot;

    /// <summary>Raised on the writing thread after the snapshot has been replaced.</summary>
    public event EventHandler? Changed
    {
        add => _store.Changed += value;
        remove => _store.Changed -= value;
    }

    /// <summary>Replaces every crisis of <paramref name="kind"/>; other kinds are untouched. Crises of another kind in the list are ignored.</summary>
    public bool ReplaceKind(CrisisKind kind, IReadOnlyList<Crisis> crises) => _store.Write(s =>
    {
        var fresh = crises.Where(c => c.Kind == kind).ToList();
        var old = s.Crises.Where(c => c.Kind == kind).ToList();
        if (old.SequenceEqual(fresh)) return null;
        return s with { Crises = [.. s.Crises.Where(c => c.Kind != kind), .. fresh] };
    });

    public bool ReplaceDisplacement(IReadOnlyList<DisplacementFigure> figures) => _store.Write(s =>
        s.Displacement.SequenceEqual(figures) ? null : s with { Displacement = [.. figures] });

    /// <summary>Replaces the unplaced reports of one source.</summary>
    public bool ReplaceUnplaced(string source, IReadOnlyList<UnplacedReport> reports) => _store.Write(s =>
    {
        var fresh = reports.Where(r => r.Source == source).ToList();
        var old = s.Unplaced.Where(r => r.Source == source).ToList();
        if (old.SequenceEqual(fresh)) return null;
        return s with { Unplaced = [.. s.Unplaced.Where(r => r.Source != source), .. fresh] };
    });

    /// <summary>Replaces everything with a cached snapshot (at start-up).</summary>
    public bool Load(CrisisSnapshot cached) => _store.Write(s => s.Equals(cached) ? null : cached);
}
