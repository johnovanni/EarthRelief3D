using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Media3D;
using EarthRelief3D.App.Presentation;
using EarthRelief3D.App.ViewModels;
using EarthRelief3D.Core.Model;
using EarthRelief3D.Core.Feeds;
using EarthRelief3D.Core.Store;
using GlobeCore.Earth;
using GlobeCore.Wpf.Layers;
using GlobeCore.Wpf.Rendering;

namespace EarthRelief3D.App.Layers;

/// <summary>
/// Names on the overlay canvas: each country with a crisis in a visible layer, and under it the diseases of its outbreak
/// cross. Countries with more kinds of crisis are placed first; a label that would overlap one already placed is left out.
/// </summary>
public sealed class LabelsOverlayLayer : OverlayLayer
{
    public const string LayerId = "labels";
    public const int Order = 200;
    public const double FontSize = 12;
    /// <summary>Labels within this much of the globe's edge (cosine of the angle to the camera) are left out.</summary>
    private const double LimbCosine = 0.25;

    private readonly CrisisStore _store;
    private readonly LayerStripViewModel _layers;
    private readonly CountryResolver _countries;
    private readonly LabelOverlay _labels;
    private readonly Func<DateTime> _nowUtc;

    /// <param name="nowUtc">The display clock: the snapshot is judged at it (WHO's 90 days, IPC's 12 months).</param>
    public LabelsOverlayLayer(CrisisStore store, LayerStripViewModel layers, CountryResolver countries, Canvas overlay, Func<DateTime> nowUtc)
        : base(LayerId, "Country and disease labels", Order)
    {
        _nowUtc = nowUtc ?? throw new ArgumentNullException(nameof(nowUtc));
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _layers = layers ?? throw new ArgumentNullException(nameof(layers));
        _countries = countries ?? throw new ArgumentNullException(nameof(countries));
        _labels = new LabelOverlay(overlay ?? throw new ArgumentNullException(nameof(overlay)));
        _store.Changed += OnInputChanged;
        _layers.PropertyChanged += OnLayersChanged;
    }

    /// <summary>The labels placed by the last render (tests, diagnostics).</summary>
    public int PlacedCount { get; private set; }

    public override void Render(Canvas overlay, Projector projector)
    {
        var s = _store.Snapshot.AsSeenAt(_nowUtc());
        var byCountry = new Dictionary<string, (int Kinds, List<string> Diseases)>(StringComparer.OrdinalIgnoreCase);
        void Note(string iso, string? disease = null)
        {
            if (!byCountry.TryGetValue(iso, out var e)) byCountry[iso] = e = (0, []);
            if (disease is null) byCountry[iso] = (e.Kinds + 1, e.Diseases);
            else if (!e.Diseases.Contains(disease)) e.Diseases.Add(disease);
        }
        if (_layers.ShowConflict) foreach (var iso in s.Crises.OfType<ConflictCrisis>().Select(c => c.CountryIso3).Distinct()) Note(iso);
        if (_layers.ShowFood) foreach (var iso in s.Crises.OfType<FoodCrisis>().Where(f => !f.IsStale && !f.ProjectionOnly).Select(c => c.CountryIso3).Distinct()) Note(iso);
        if (_layers.ShowOutbreaks)
        {
            var outbreaks = s.Crises.OfType<OutbreakCrisis>().OrderByDescending(o => o.PublishedUtc).ToList();
            foreach (var iso in outbreaks.Select(o => o.CountryIso3).Distinct(StringComparer.OrdinalIgnoreCase)) Note(iso);
            foreach (var o in outbreaks) Note(o.CountryIso3, o.Disease);
        }

        var index = _countries.Index;
        _labels.Begin();
        int placed = 0;
        var eye = (Vector3D)projector.Eye;
        foreach (var (iso, (kinds, diseases)) in byCountry.OrderByDescending(kv => kv.Value.Kinds).ThenBy(kv => kv.Key, StringComparer.Ordinal))
        {
            if (index.ByIso3(iso) is not { } country) continue;
            var p = MarkerMeshes.Above(country.LabelPoint, 1.0);
            var toEye = eye - (Vector3D)p;
            if (toEye.Length <= 0 || Vector3D.DotProduct((Vector3D)p, toEye) / toEye.Length < LimbCosine) continue;
            if (!projector.TryProject(p, out var at)) continue;
            if (_labels.TryAdd(country.Name, new Point(at.X + 4, at.Y - 9), LayerStyle.LabelColor, fontSize: FontSize)) placed++;
            if (diseases.Count > 0
                && _labels.TryAdd(string.Join(", ", diseases), new Point(at.X + 4, at.Y + 7), LayerStyle.OutbreakColor, fontSize: FontSize - 1)) placed++;
        }
        _labels.End();
        PlacedCount = placed;
    }

    protected override void Clear(Canvas overlay)
    {
        _labels.Begin();
        _labels.End();
        PlacedCount = 0;
    }

    private void OnInputChanged(object? sender, EventArgs e) => Invalidate();   // any thread: the host marshals

    /// <summary>The boundaries changed (any thread): the labels are placed again at the next render.</summary>
    public void BoundariesChanged() => Invalidate();

    private void OnLayersChanged(object? sender, PropertyChangedEventArgs e) => Invalidate();

    public void Close()
    {
        _store.Changed -= OnInputChanged;
        _layers.PropertyChanged -= OnLayersChanged;
    }
}
