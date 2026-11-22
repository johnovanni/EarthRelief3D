using System.Windows;
using System.Windows.Media.Media3D;
using EarthRelief3D.Core.Feeds;
using EarthRelief3D.Core.Model;
using EarthRelief3D.Core.Store;
using GlobeCore.Earth;
using GlobeCore.Wpf.Layers;
using GlobeCore.Wpf.Rendering;

namespace EarthRelief3D.App.Layers;

/// <summary>
/// A layer of markers drawn from the crisis store. The store's Changed comes on feed threads: the layer only sets a flag
/// and calls <see cref="SceneLayer.Invalidate"/>, which <see cref="LayerHost"/> takes to the UI thread; the snapshot is read
/// in <see cref="Update"/> there. Marker sizes are kept in screen pixels, so the meshes are rebuilt when the zoom changes
/// the size of a pixel by more than <see cref="RescaleRatio"/>.
/// </summary>
public abstract class CrisisSceneLayer : SceneLayer, IPickable
{
    public const double RescaleRatio = 1.15;

    private readonly CrisisStore _store;
    private readonly Func<string, string?> _countryName;
    private readonly Func<DateTime> _nowUtc;
    private int _dataDirty = 1;
    private double _builtWorldPerPx = double.NaN;
    private bool _closed;

    /// <param name="nowUtc">The display clock: the snapshot is judged at it (WHO's 90 days, IPC's 12 months).</param>
    protected CrisisSceneLayer(string id, string title, int zOrder, CrisisStore store, CountryResolver countries, Func<DateTime> nowUtc)
        : base(id, title, zOrder)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _nowUtc = nowUtc ?? throw new ArgumentNullException(nameof(nowUtc));
        _resolver = countries ?? throw new ArgumentNullException(nameof(countries));
        _countryName = iso => Countries.ByIso3(iso)?.Name;
        _store.Changed += OnStoreChanged;
    }

    private readonly CountryResolver _resolver;

    /// <summary>The current countries (follows a boundary swap); take it once per operation.</summary>
    protected CountryIndex Countries => _resolver.Index;

    /// <summary>The snapshot the markers were last built from, judged at the display time (UI thread); empty before the first update.</summary>
    protected CrisisSnapshot Snapshot { get; private set; } = CrisisSnapshot.Empty;

    /// <summary>How many markers are drawn (tests, diagnostics).</summary>
    public int MarkerCount { get; protected set; }

    /// <summary>World length (Earth radii) of a screen pixel when the markers were last built.</summary>
    protected double WorldPerPixel => double.IsNaN(_builtWorldPerPx) ? 1e-3 : _builtWorldPerPx;

    protected string NameOf(string iso3) => _countryName(iso3) ?? iso3;

    public override void Update(SceneContext ctx)
    {
        if (_closed) return;
        double wpp = Picking.WorldPerPixel(ctx.Projector);
        bool data = Interlocked.Exchange(ref _dataDirty, 0) == 1;
        bool rescale = double.IsNaN(_builtWorldPerPx) || wpp > _builtWorldPerPx * RescaleRatio || wpp * RescaleRatio < _builtWorldPerPx;
        if (!data && !rescale) return;
        if (data) Snapshot = _store.Snapshot.AsSeenAt(_nowUtc());
        _builtWorldPerPx = wpp;
        Rebuild(Snapshot, wpp);
    }

    /// <summary>Rebuilds the meshes in <see cref="SceneLayer.Root"/> from <paramref name="s"/> (UI thread).</summary>
    protected abstract void Rebuild(CrisisSnapshot s, double worldPerPx);

    public abstract PickResult? HitTest(Point screen, Projector projector, bool hover);

    /// <summary>Replaces the root's content with one model per (mesh, colour); empty meshes are left out.</summary>
    protected void SetModels(IEnumerable<(MarkerMeshes Mesh, System.Windows.Media.Color Color)> parts)
    {
        var group = new Model3DGroup();
        foreach (var (mesh, color) in parts)
            if (!mesh.IsEmpty) group.Children.Add(new GeometryModel3D(mesh.ToMesh(), MarkerMeshes.Material(color)));
        group.Freeze();
        Root.Content = group;
    }

    // Any thread: only a flag crosses; the host marshals the invalidation to the UI thread.
    private void OnStoreChanged(object? sender, EventArgs e)
    {
        Volatile.Write(ref _dataDirty, 1);
        Invalidate();
    }

    /// <summary>The boundaries changed (any thread): marker positions are taken from the new countries at the next update.</summary>
    public void BoundariesChanged()
    {
        Volatile.Write(ref _dataDirty, 1);
        Invalidate();
    }

    /// <summary>Stops following the store (the window has closed).</summary>
    public void Close()
    {
        _closed = true;
        _store.Changed -= OnStoreChanged;
    }
}
