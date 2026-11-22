using System.Windows;
using System.Windows.Media;
using EarthRelief3D.App.Presentation;
using EarthRelief3D.Core.Model;
using EarthRelief3D.Core.Store;
using GlobeCore.Earth;
using GlobeCore.Geo;
using GlobeCore.Imaging;
using GlobeCore.Logging;
using GlobeCore.Wpf.Layers;
using GlobeCore.Wpf.Rendering;

namespace EarthRelief3D.App.Layers;

/// <summary>
/// Food crises (IPC national figures, ruling P2-3): each country whose current analysis has people in Phase 3 or higher is
/// shaded in the IPC's colour for its highest such phase. Stale analyses (P2-4) are not shaded. Drawn above the night
/// shade, so the phase colours stay true on the night side, and under the borders.
/// <para>
/// The store's Changed (feed threads) only asks for a recompose; the shading is re-rasterized on the STA worker at the
/// start of the next compose, and only when the countries or phases it shows have changed.
/// </para>
/// </summary>
public sealed class FoodImageLayer : ImageLayer, IPickable
{
    public const string LayerId = "food";
    public const int Order = 20;
    /// <summary>The blend order inside the AboveNight pass: under the borders (10).</summary>
    public const int ImageOrder = 5;
    /// <summary>The rasterizer fills at 35%; the IPC colours are raised to about 60% so the phases read on Blue Marble.</summary>
    public const double FillBoost = 153.0 / 89.0;

    private readonly int _width, _height;
    private readonly CrisisStore _store;
    private readonly CountryIndex _countries;
    private readonly StaWorker _sta;
    private readonly IAppLog _log;
    private readonly Func<bool> _isClosed;
    private readonly Func<DateTime> _nowUtc;
    private string? _builtKey;
    private IImageLayer? _current;

    /// <param name="nowUtc">The display clock: an analysis that has aged past 12 months is not shaded (P2-4).</param>
    public FoodImageLayer(int width, int height, CrisisStore store, CountryIndex countries, StaWorker sta, IAppLog log, Func<bool> isClosed,
        Func<DateTime> nowUtc)
        : base(LayerId, "Food crisis", Order)
    {
        _nowUtc = nowUtc ?? throw new ArgumentNullException(nameof(nowUtc));
        _width = width;
        _height = height;
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _countries = countries ?? throw new ArgumentNullException(nameof(countries));
        _sta = sta ?? throw new ArgumentNullException(nameof(sta));
        _log = log ?? throw new ArgumentNullException(nameof(log));
        _isClosed = isClosed ?? throw new ArgumentNullException(nameof(isClosed));
        _store.Changed += OnStoreChanged;
    }

    public ImagePass Pass => ImagePass.AboveNight;

    public override IImageLayer? Current => _current;

    /// <summary>
    /// The countries to shade and their IPC colours, by ISO3: the highest phase of each current analysis (not stale, not
    /// projection only).
    /// </summary>
    public static IReadOnlyList<(string Iso3, Color Color)> Plan(IEnumerable<Crisis> crises) =>
    [
        .. crises.OfType<FoodCrisis>().Where(f => !f.IsStale && !f.ProjectionOnly && f.HighestPhase is >= 3 and <= 5)
            .GroupBy(f => f.CountryIso3.ToUpperInvariant())
            .OrderBy(g => g.Key, StringComparer.Ordinal)
            .Select(g => (g.Key, LayerStyle.IpcPhaseColor(g.Max(f => f.HighestPhase)))),
    ];

    /// <summary>Draws the plan's countries (their polygons from <paramref name="countries"/>) at the image's size. STA thread.</summary>
    public static Bgra32Image Rasterize(IReadOnlyList<(string Iso3, Color Color)> plan, CountryIndex countries, int width, int height)
    {
        var areas = new List<(GeoPolygon, Color)>();
        foreach (var (iso, color) in plan)
            if (countries.ByIso3(iso) is { } c)
                foreach (var poly in c.Polygons) areas.Add((poly, color));
        var image = AreaRasterizer.Rasterize(areas, width, height);
        var px = image.Pixels;
        for (int i = 3; i < px.Length; i += 4)
            if (px[i] is > 0 and < 255) px[i] = (byte)Math.Min(255, Math.Round(px[i] * FillBoost));
        return image;
    }

    protected override async Task PrepareForComposeAsync()
    {
        var plan = Plan(_store.Snapshot.AsSeenAt(_nowUtc()).Crises);
        var key = string.Join(";", plan.Select(p => p.Iso3 + ":" + p.Color));
        if (key == _builtKey) return;
        try
        {
            int w = _width, h = _height;
            var countries = _countries;
            var image = plan.Count == 0 ? null : await _sta.InvokeAsync(() => Rasterize(plan, countries, w, h));
            if (_isClosed()) return;
            _current = image is null ? null : new BitmapBlendLayer(image, Pass, ImageOrder);
            _builtKey = key;
        }
        catch (OperationCanceledException) when (_isClosed()) { }
        catch (Exception ex)
        {
            // Keep what was shown; the next store change or compose tries again.
            _log.Error("Food crisis shading could not be drawn", ex);
        }
    }

    public PickResult? HitTest(Point screen, Projector projector, bool hover)
    {
        if (Picking.CountryAt(screen, projector, _countries) is not { } country) return null;
        var food = _store.Snapshot.AsSeenAt(_nowUtc()).ByCountry(country.Iso3).OfType<FoodCrisis>().Where(f => !f.IsStale && !f.ProjectionOnly)
            .OrderByDescending(f => f.HighestPhase).FirstOrDefault();
        return food is null ? null
            : new PickResult(LayerId, new CountryPick(country.Iso3, CrisisLayer.Food), CrisisText.FoodHover(country.Name, food));
    }

    // Any thread: the host takes the request to the UI thread; the shading is rebuilt at the next compose.
    private void OnStoreChanged(object? sender, EventArgs e) => Invalidate();

    public void Close() => _store.Changed -= OnStoreChanged;
}
