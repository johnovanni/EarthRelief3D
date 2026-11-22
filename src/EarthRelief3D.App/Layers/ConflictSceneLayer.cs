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
/// Armed conflict (UCDP): a dot at every event of a conflict that meets UCDP's threshold, sized by the conflict's band
/// (C7, docs/severity-bands.md) and coloured by the event's age against the data's as-of date (newest: last 3 months).
/// </summary>
public sealed class ConflictSceneLayer : CrisisSceneLayer
{
    public const string LayerId = "conflict";
    public const int Order = 110;
    public const double Height = 1.002;
    /// <summary>Newer age buckets sit this much higher (Earth radii), so overlapping dots of different ages don't z-fight.</summary>
    public const double BucketLift = 0.0003;
    private const double PickSlopPx = 3;

    private readonly List<(Point3D Centre, double RadiusPx, ConflictCrisis Crisis)> _dots = [];

    public ConflictSceneLayer(CrisisStore store, CountryIndex countries) : base(LayerId, "Armed conflict", Order, store, countries) { }

    /// <summary>0 for the last 3 months before <paramref name="asOfUtc"/>, 1 for the 3 before those, 2 for older.</summary>
    public static int AgeBucket(DateTime eventUtc, DateTime asOfUtc)
    {
        double days = (asOfUtc - eventUtc).TotalDays;
        return days <= 91 ? 0 : days <= 182 ? 1 : 2;
    }

    /// <summary>Dot radius on screen for a conflict band (1 to 4; 5 is not used for conflict).</summary>
    public static double DotRadiusPx(int band) => 1.5 + Math.Clamp(band, 1, 5);

    protected override void Rebuild(CrisisSnapshot s, double worldPerPx)
    {
        _dots.Clear();
        var conflicts = s.Crises.OfType<ConflictCrisis>().ToList();
        var asOf = conflicts.Count == 0 ? default : conflicts.Max(CrisisText.NewestAsOf);
        var meshes = new MarkerMeshes[LayerStyle.ConflictAgeColors.Count];
        for (int i = 0; i < meshes.Length; i++) meshes[i] = new MarkerMeshes();
        // Bigger conflicts first, so a smaller conflict's dots are drawn (and picked) over them.
        foreach (var c in conflicts.OrderByDescending(c => c.Band))
        {
            double px = DotRadiusPx(c.Band);
            foreach (var e in c.Events)
            {
                int b = AgeBucket(e.StartUtc, asOf);
                double h = Height + (meshes.Length - 1 - b) * BucketLift;
                meshes[b].AddDisc(e.Position, px * worldPerPx, h);
                _dots.Add((MarkerMeshes.Above(e.Position, h), px, c));
            }
        }
        // Oldest first, so newer dots are drawn over older ones.
        SetModels(Enumerable.Range(0, meshes.Length).Reverse().Select(b => (meshes[b], LayerStyle.ConflictAgeColors[b])));
        MarkerCount = _dots.Count;
    }

    public override PickResult? HitTest(Point screen, Projector projector, bool hover)
    {
        ConflictCrisis? best = null;
        double bestDist = double.MaxValue;
        for (int i = _dots.Count - 1; i >= 0; i--)
        {
            var (centre, r, crisis) = _dots[i];
            if (!Picking.FacesCamera(centre, projector.Eye) || !projector.TryProject(centre, out var p)) continue;
            double d = Picking.Distance(p, screen);
            if (d <= r + PickSlopPx && d < bestDist) { best = crisis; bestDist = d; }
        }
        return best is null ? null
            : new PickResult(LayerId, new CountryPick(best.CountryIso3, CrisisLayer.Conflict), CrisisText.ConflictHover(NameOf(best.CountryIso3), best));
    }
}
