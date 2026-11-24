using System.Collections.Immutable;
using EarthRelief3D.App.Presentation;
using EarthRelief3D.Core.Model;

namespace EarthRelief3D.App.Layers;

/// <summary>
/// The points of one marker layer drawn from a list, and where they come from: the legend names the citation's source and
/// as-of date, with <see cref="Filter"/> as its note (camps: why none are shown; GEM: the CC BY 4.0 credit).
/// </summary>
public sealed record PointSet(IReadOnlyList<PointItem> Items, SourceCitation Citation, string? Filter = null)
{
    /// <summary>
    /// What the legend line says the layer shows, from the data (GEM: "Power plants: operating plants of 500 MW or more
    /// (units summed per plant), coloured by its largest fuel", the filter P3-9 records); null: the layer's own wording.
    /// </summary>
    public string? Measures { get; init; }
}

/// <summary>
/// The point sets by marker layer (camps, Task 5; GEM power, oil and gas, and steel, Task 7). A layer with no set has no
/// data loaded yet (ruling R2): it draws nothing and the legend says so. Thread-safe; <see cref="Changed"/> is raised on the
/// writing thread with the layer that changed.
/// </summary>
public sealed class MarkerDataStore
{
    private ImmutableDictionary<MarkerLayer, PointSet> _sets = ImmutableDictionary<MarkerLayer, PointSet>.Empty;

    public event EventHandler<MarkerLayer>? Changed;

    /// <summary>The layer's set, or null while its data is not loaded.</summary>
    public PointSet? Get(MarkerLayer layer) => Volatile.Read(ref _sets).TryGetValue(layer, out var s) ? s : null;

    /// <summary>The layer's points (none while its data is not loaded).</summary>
    public IReadOnlyList<PointItem> Items(MarkerLayer layer) => Get(layer)?.Items ?? [];

    public void Replace(MarkerLayer layer, PointSet set)
    {
        ArgumentNullException.ThrowIfNull(set);
        ImmutableInterlocked.AddOrUpdate(ref _sets, layer, set, (_, _) => set);
        Changed?.Invoke(this, layer);
    }
}
