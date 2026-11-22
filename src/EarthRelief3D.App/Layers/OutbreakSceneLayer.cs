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
/// Disease outbreaks (WHO Disease Outbreak News, last 90 days): a cross at the country's label point, one per country
/// however many reports it has; the labels layer writes the disease beside it. Reports that name no recognisable country
/// are not drawn: the legend lists them under "Not placed on the map".
/// </summary>
public sealed class OutbreakSceneLayer : CrisisSceneLayer
{
    public const string LayerId = "outbreaks";
    public const int Order = 120;
    public const double Height = 1.004;
    public const double ArmPx = 8, ArmHalfWidthPx = 2.2;
    private const double PickRadiusPx = 10;

    private readonly List<(Point3D Centre, string Iso3, IReadOnlyList<OutbreakCrisis> Reports)> _crosses = [];

    public OutbreakSceneLayer(CrisisStore store, CountryIndex countries, Func<DateTime> nowUtc)
        : base(LayerId, "Disease outbreaks", Order, store, countries, nowUtc) { }

    protected override void Rebuild(CrisisSnapshot s, double worldPerPx)
    {
        _crosses.Clear();
        var mesh = new MarkerMeshes();
        foreach (var g in s.Crises.OfType<OutbreakCrisis>().GroupBy(o => o.CountryIso3).OrderBy(g => g.Key, StringComparer.Ordinal))
        {
            if (Countries.ByIso3(g.Key) is not { } country) continue;
            mesh.AddCross(country.LabelPoint, ArmPx * worldPerPx, ArmHalfWidthPx * worldPerPx, Height);
            _crosses.Add((MarkerMeshes.Above(country.LabelPoint, Height), g.Key, g.ToList()));
        }
        SetModels([(mesh, LayerStyle.OutbreakColor)]);
        MarkerCount = _crosses.Count;
    }

    public override PickResult? HitTest(Point screen, Projector projector, bool hover)
    {
        foreach (var (centre, iso, reports) in _crosses)
        {
            if (!Picking.FacesCamera(centre, projector.Eye) || !projector.TryProject(centre, out var p)) continue;
            if (Picking.Distance(p, screen) <= PickRadiusPx)
                return new PickResult(LayerId, new CountryPick(iso, CrisisLayer.Outbreaks), CrisisText.OutbreakHover(NameOf(iso), reports));
        }
        return null;
    }
}
