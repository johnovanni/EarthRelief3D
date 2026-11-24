using System.Windows;
using System.Windows.Media.Media3D;
using EarthRelief3D.App.Curated;
using EarthRelief3D.App.Presentation;
using EarthRelief3D.Core.Curated;
using EarthRelief3D.Core.Feeds;
using GlobeCore.Wpf.Layers;
using GlobeCore.Wpf.Rendering;

namespace EarthRelief3D.App.Layers;

/// <summary>
/// Genocide and mass atrocities (spec §10.2, C14): a hollow triangle, near white with a dark edge, at the label point of
/// each country with a current finding the user sees: a court judgment, UN body finding or government determination, with a
/// statement dated within the last 12 months (by the designating body or in a UN report) that the situation continues.
/// Interim steps (provisional measures, warrants, pending cases) never draw a triangle on their own; with a current finding
/// they are listed in its tooltip. Other designations are history: the country panel lists them, the map does not. The
/// tooltip names each listed designation's type, authority and year. Data comes from
/// the <see cref="CuratedStore"/> (its Changed may come on any thread: only a flag crosses) and is judged at the display time.
/// </summary>
public sealed class AtrocityLayer : SceneLayer, IPickable
{
    public const string LayerId = "atrocities";
    /// <summary>After the outbreak crosses (120), before the point markers (130+).</summary>
    public const int Order = 125;
    /// <summary>Above the outbreak crosses (1.004), below the point markers (1.005).</summary>
    public const double Height = 1.0045;
    public const double RadiusPx = 10, LinePx = 2, EdgePx = 4.5;
    private const double PickRadiusPx = 12;
    private const double Lift = 0.00004;   // the light outline sits just above its dark edge

    private readonly CuratedStore _store;
    private readonly CountryResolver _countries;
    private readonly Func<DateTime> _nowUtc;
    private readonly List<(Point3D Centre, string Iso3, string Tooltip)> _glyphs = [];
    private int _dataDirty = 1;
    private double _builtWorldPerPx = double.NaN;
    private bool _closed;

    /// <param name="nowUtc">The display clock: a continuing statement counts for 12 months from its date.</param>
    public AtrocityLayer(CuratedStore store, CountryResolver countries, Func<DateTime> nowUtc)
        : base(LayerId, LayerCatalog.Label(MarkerLayer.Atrocities), Order)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _countries = countries ?? throw new ArgumentNullException(nameof(countries));
        _nowUtc = nowUtc ?? throw new ArgumentNullException(nameof(nowUtc));
        _store.Changed += OnChanged;
    }

    /// <summary>How many glyphs are drawn (one per country; tests, diagnostics).</summary>
    public int MarkerCount { get; private set; }

    /// <summary>
    /// The countries on the map, each with what its tooltip lists, newest first (<see cref="CountryDesignations.Current"/>):
    /// a country is on the map only with a current finding; its interim steps with a recent statement are listed with it.
    /// </summary>
    public static IReadOnlyDictionary<string, IReadOnlyList<Designation>> CurrentByCountry(CuratedStore store, DateTime nowUtc) =>
        store.Visible.GroupBy(d => d.Iso3.ToUpperInvariant(), StringComparer.OrdinalIgnoreCase)
            .Select(g => (g.Key, CountryDesignations.Of(g, nowUtc).Current))
            .Where(x => x.Current.Count > 0)
            .ToDictionary(x => x.Key, x => x.Current, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// The hover text: one entry per current designation, newest first ("Myanmar · UN body finding: ... (..., 2018)"). A
    /// long entry (Palestine's ICC warrants name every charge) is wrapped (<see cref="TooltipText.Wrap"/>), so the tooltip
    /// stays well inside the window.
    /// </summary>
    public static string Tooltip(string country, IReadOnlyList<Designation> current, IReadOnlyList<Government> governments) =>
        string.Join(Environment.NewLine, current.Select(d => TooltipText.Wrap(DesignationText.Hover(d, country, governments))));

    public override void Update(SceneContext ctx)
    {
        if (_closed) return;
        double wpp = Picking.WorldPerPixel(ctx.Projector);
        bool data = Interlocked.Exchange(ref _dataDirty, 0) == 1;
        bool rescale = double.IsNaN(_builtWorldPerPx) || wpp > _builtWorldPerPx * CrisisSceneLayer.RescaleRatio
                       || wpp * CrisisSceneLayer.RescaleRatio < _builtWorldPerPx;
        if (!data && !rescale) return;
        _builtWorldPerPx = wpp;
        Rebuild(wpp);
    }

    private void Rebuild(double wpp)
    {
        _glyphs.Clear();
        var edges = new MarkerMeshes();
        var fills = new MarkerMeshes();
        var index = _countries.Index;
        var governments = _store.Tables.Governments;
        foreach (var (iso, current) in CurrentByCountry(_store, _nowUtc()).OrderBy(kv => kv.Key, StringComparer.Ordinal))
        {
            if (index.ByIso3(iso) is not { } country) continue;   // not a unit of the boundary set: listed in the panel only
            var at = country.LabelPoint;
            edges.AddTriangleOutline(at, RadiusPx * wpp, EdgePx * wpp, Height);
            fills.AddTriangleOutline(at, RadiusPx * wpp, LinePx * wpp, Height + Lift);
            _glyphs.Add((MarkerMeshes.Above(at, Height), iso, Tooltip(country.Name, current, governments)));
        }
        var group = new Model3DGroup();
        if (!edges.IsEmpty) group.Children.Add(new GeometryModel3D(edges.ToMesh(), MarkerMeshes.Material(LayerStyle.MarkerEdgeColor)));
        if (!fills.IsEmpty) group.Children.Add(new GeometryModel3D(fills.ToMesh(), MarkerMeshes.Material(LayerStyle.AtrocityColor)));
        group.Freeze();
        Root.Content = group;
        MarkerCount = _glyphs.Count;
    }

    public PickResult? HitTest(Point screen, Projector projector, bool hover)
    {
        foreach (var (centre, iso, tooltip) in _glyphs)
        {
            if (!Picking.FacesCamera(centre, projector.Eye) || !projector.TryProject(centre, out var p)) continue;
            if (Picking.Distance(p, screen) <= PickRadiusPx)
                return new PickResult(LayerId, new CountryPick(iso, null) { Marker = MarkerLayer.Atrocities }, tooltip);
        }
        return null;
    }

    // Any thread: only a flag crosses; the host marshals the invalidation to the UI thread.
    private void OnChanged(object? sender, EventArgs e) => DataChanged();

    /// <summary>The data, the boundaries or the display day changed (any thread): rebuilt at the next update.</summary>
    public void DataChanged()
    {
        Volatile.Write(ref _dataDirty, 1);
        Invalidate();
    }

    /// <summary>Stops following the store (the window has closed).</summary>
    public void Close()
    {
        _closed = true;
        _store.Changed -= OnChanged;
    }
}
