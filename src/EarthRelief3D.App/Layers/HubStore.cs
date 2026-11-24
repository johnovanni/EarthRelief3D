using EarthRelief3D.App.Curated;
using EarthRelief3D.App.Presentation;
using EarthRelief3D.Core.Feeds;
using EarthRelief3D.Core.Logic;

namespace EarthRelief3D.App.Layers;

/// <summary>
/// The hubs the nearest-hub rows and the arcs measure to (Plan 4 Task 5, <see cref="HubMarkers.Hubs"/>): the airport and port
/// extracts once they are set, and the UN depots the user sees. It is rebuilt whenever the curated store changes (an approval,
/// the drafts shown, a government chosen) and raises <see cref="Changed"/>, from whichever thread caused it. Until the extracts
/// are set the list holds only the depots.
/// </summary>
public sealed class HubStore
{
    private readonly CuratedStore _curated;
    private readonly object _gate = new();
    private IReadOnlyList<(MarkerLayer Layer, BundledPointsLoad Load)> _loads = [];
    private volatile IReadOnlyList<Hub> _hubs;

    public HubStore(CuratedStore curated)
    {
        _curated = curated ?? throw new ArgumentNullException(nameof(curated));
        _hubs = HubMarkers.Hubs(_loads, _curated.VisibleDepots);
        _curated.Changed += (_, _) => Rebuild();
    }

    /// <summary>The hubs now, in a stable order: the extracts' in file order, then the depots.</summary>
    public IReadOnlyList<Hub> Hubs => _hubs;

    public event EventHandler? Changed;

    /// <summary>Sets the airport and port extracts (the loads, loaded or not) and rebuilds the list.</summary>
    public void SetLoads(IReadOnlyList<(MarkerLayer Layer, BundledPointsLoad Load)> loads)
    {
        ArgumentNullException.ThrowIfNull(loads);
        lock (_gate) _loads = [.. loads];
        Rebuild();
    }

    private void Rebuild()
    {
        lock (_gate) _hubs = HubMarkers.Hubs(_loads, _curated.VisibleDepots);
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
