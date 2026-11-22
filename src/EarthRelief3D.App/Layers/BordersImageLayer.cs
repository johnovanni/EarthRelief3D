using System.Windows;
using System.Windows.Media;
using AstroCore.Earth;
using EarthRelief3D.App.Presentation;
using EarthRelief3D.Core.Store;
using GlobeCore.Earth;
using GlobeCore.Imaging;
using GlobeCore.Wpf.Layers;
using GlobeCore.Wpf.Rendering;

namespace EarthRelief3D.App.Layers;

/// <summary>
/// Coastlines and country borders (Natural Earth 1:50m, rasterized once at load), drawn above the night shade. Also the
/// bottom of picking: any country under the mouse, so a click anywhere on land opens that country's panel. Countries are
/// found in the resolver's index, with units applied (C16): a click in Somaliland opens Somalia.
/// </summary>
public sealed class BordersImageLayer : ImageLayer, IPickable
{
    public const string LayerId = "borders";
    public const int Order = 10;

    private readonly IImageLayer _image;
    private readonly CrisisStore _store;
    private readonly CountryIndex _countries;
    private readonly Func<DateTime> _nowUtc;

    /// <param name="coastAndBorders">The lines at the globe image's size (<see cref="RasterizeCoastAndBorders"/>).</param>
    /// <param name="nowUtc">The clock for the hover's date wording.</param>
    public BordersImageLayer(SparseOverlay coastAndBorders, CrisisStore store, CountryIndex countries, Func<DateTime> nowUtc)
        : base(LayerId, "Coastlines and borders", Order)
    {
        _nowUtc = nowUtc ?? throw new ArgumentNullException(nameof(nowUtc));
        ArgumentNullException.ThrowIfNull(coastAndBorders);
        _image = new SparseOverlayLayer(coastAndBorders, Pass, Order);
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _countries = countries ?? throw new ArgumentNullException(nameof(countries));
        Source = "Natural Earth 1:50m";
    }

    public ImagePass Pass => ImagePass.AboveNight;

    public override IImageLayer? Current => _image;

    /// <summary>The coastlines and borders at the globe image's size, kept as line pixels only (UI or STA thread).</summary>
    public static SparseOverlay RasterizeCoastAndBorders(IEnumerable<IReadOnlyList<GeoPosition>> coast,
                                                         IEnumerable<IReadOnlyList<GeoPosition>> borders, int w, int h) =>
        SparseOverlay.FromImage(LineRasterizer.Rasterize(
            [(borders, Color.FromArgb(140, 225, 225, 225)), (coast, Color.FromArgb(170, 255, 255, 255))], w, h, 1.0));

    public PickResult? HitTest(Point screen, Projector projector, bool hover)
    {
        if (Picking.CountryAt(screen, projector, _countries) is not { } country) return null;
        return new PickResult(LayerId, new CountryPick(country.Iso3, null),
            CrisisText.CountryHover(country.Name, country.Iso3, _store.Snapshot, _nowUtc()));
    }
}
