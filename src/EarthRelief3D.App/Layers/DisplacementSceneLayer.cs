using System.Windows;
using System.Windows.Media.Media3D;
using EarthRelief3D.App.Presentation;
using EarthRelief3D.Core.Model;
using EarthRelief3D.Core.Store;
using GlobeCore.Earth;
using GlobeCore.Wpf.Layers;
using GlobeCore.Wpf.Rendering;

namespace EarthRelief3D.App.Layers;

/// <summary>
/// People forced from home (UNHCR, by country of origin): a ring at the country's label point whose AREA is proportional
/// to the total displaced (refugees, asylum-seekers and internally displaced), so its radius grows with the square root.
/// The ring is drawn on the globe (it zooms with it); only its line width is kept in screen pixels. A layer value, not a
/// crisis (C11).
/// </summary>
public sealed class DisplacementSceneLayer : CrisisSceneLayer
{
    public const string LayerId = "displacement";
    public const int Order = 100;
    public const double Height = 1.003;
    /// <summary>Ring radius (Earth radii) for one million people: about 190 km; ten million is about 600 km.</summary>
    public const double RadiusPerSqrtMillion = 0.03;
    public const double LineWidthPx = 1.6;
    private const double PickSlopPx = 4;

    private readonly List<(RingPoints Ring, DisplacementFigure Figure)> _rings = [];

    private readonly record struct RingPoints(Point3D Centre, Point3D Edge);

    private readonly Func<DateTime> _nowUtc;

    /// <param name="nowUtc">The clock for the hover's date wording (no "as of" a date still to come).</param>
    public DisplacementSceneLayer(CrisisStore store, CountryIndex countries, Func<DateTime> nowUtc)
        : base(LayerId, "People forced from home", Order, store, countries) =>
        _nowUtc = nowUtc ?? throw new ArgumentNullException(nameof(nowUtc));

    /// <summary>Ring radius in Earth radii for <paramref name="total"/> people: area ∝ total.</summary>
    public static double RadiusFor(long total) => total <= 0 ? 0 : RadiusPerSqrtMillion * Math.Sqrt(total / 1e6);

    protected override void Rebuild(CrisisSnapshot s, double worldPerPx)
    {
        _rings.Clear();
        var mesh = new MarkerMeshes();
        // Largest first, so smaller rings are drawn over them and win a pick.
        foreach (var f in s.Displacement.OrderByDescending(CrisisText.Total))
        {
            double r = RadiusFor(CrisisText.Total(f));
            if (r <= 0 || Countries.ByIso3(f.CountryIso3) is not { } country) continue;
            mesh.AddRing(country.LabelPoint, r, LineWidthPx * worldPerPx, Height);
            var (n, e1, e2) = MarkerMeshes.Frame(country.LabelPoint);
            _rings.Add((new RingPoints(MarkerMeshes.Above(country.LabelPoint, Height), MarkerMeshes.On(n, e1, e2, r, 0, Height)), f));
        }
        SetModels([(mesh, LayerStyle.DisplacementColor)]);
        MarkerCount = _rings.Count;
    }

    /// <summary>A hit on the ring's line (not inside it: a big ring must not hide the countries under it).</summary>
    public override PickResult? HitTest(Point screen, Projector projector, bool hover)
    {
        for (int i = _rings.Count - 1; i >= 0; i--)
        {
            var (ring, f) = _rings[i];
            if (!Picking.FacesCamera(ring.Centre, projector.Eye)
                || !projector.TryProject(ring.Centre, out var c) || !projector.TryProject(ring.Edge, out var e)) continue;
            double rPx = Picking.Distance(c, e);
            if (Math.Abs(Picking.Distance(c, screen) - rPx) <= PickSlopPx)
                return new PickResult(LayerId, new CountryPick(f.CountryIso3, CrisisLayer.Displacement),
                    CrisisText.DisplacementHover(NameOf(f.CountryIso3), f, _nowUtc()));
        }
        return null;
    }
}
