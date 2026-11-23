using System.Windows;
using System.Windows.Media;
using AstroCore.Earth;
using EarthRelief3D.App.Presentation;
using EarthRelief3D.Core.Feeds;
using EarthRelief3D.Core.Model;
using EarthRelief3D.Core.Store;
using GlobeCore.Earth;
using GlobeCore.Geo;
using GlobeCore.Imaging;
using GlobeCore.Logging;
using GlobeCore.Wpf.Layers;
using GlobeCore.Wpf.Rendering;

namespace EarthRelief3D.App.Layers;

/// <summary>One country on the food layer: its fill colour (the 20% rule's phase) and its outline colour (a worse phase with people).</summary>
public sealed record FoodArea(string Iso3, Color? Fill, Color? Outline);

/// <summary>
/// Food crises (IPC national figures, ruling P2-3, shading rule R17): each country with a current analysis is shaded in the
/// IPC colour of the worst phase that covers at least 20% of the people analysed (IPC's 20% rule, applied to the whole
/// country; Phases 3 to 5 only), and outlined in the colour of a worse phase that has people in it. Stale analyses (P2-4)
/// and projection-only records are neither shaded nor outlined. Drawn above the night shade, so the phase colours stay true
/// on the night side, and under the borders.
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
    private readonly CountryResolver _countries;
    private int _boundaryVersion;
    private readonly StaWorker _sta;
    private readonly IAppLog _log;
    private readonly Func<bool> _isClosed;
    private readonly Func<DateTime> _nowUtc;
    private string? _builtKey;
    private IImageLayer? _current;

    /// <param name="nowUtc">The display clock: an analysis that has aged past 12 months is not shaded (P2-4).</param>
    public FoodImageLayer(int width, int height, CrisisStore store, CountryResolver countries, StaWorker sta, IAppLog log, Func<bool> isClosed,
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
    /// The outline's width in pixels of the layer's image (the globe image, 4096 wide): about 2 px, fully opaque, drawn over
    /// the fill (R17).
    /// </summary>
    public const double OutlineWidthPx = 2.0;

    /// <summary>
    /// The countries to draw, by ISO3, with their fill and outline colours from <see cref="IpcAreaRule.MapStyle"/> (stale
    /// and projection-only analyses are left out). A country is in the plan when it has a fill, an outline or both.
    /// </summary>
    public static IReadOnlyList<FoodArea> Plan(IEnumerable<Crisis> crises) =>
    [
        .. crises.OfType<FoodCrisis>()
            .Select(f => (Iso3: f.CountryIso3.ToUpperInvariant(), Style: IpcAreaRule.MapStyle(f)))
            .Where(x => x.Style.Shaded is not null || x.Style.Outline is not null)
            .GroupBy(x => x.Iso3)
            .OrderBy(g => g.Key, StringComparer.Ordinal)
            .Select(g =>
            {
                // One analysis per country in practice; if more, the worst fill and the worst outline above it.
                int? shaded = g.Max(x => x.Style.Shaded);
                int? outline = g.Max(x => x.Style.Outline);
                if (outline <= shaded) outline = null;
                return new FoodArea(g.Key, shaded is { } s ? LayerStyle.IpcPhaseColor(s) : null,
                    outline is { } o ? LayerStyle.IpcPhaseColor(o) : null);
            }),
    ];

    /// <summary>The key of what is drawn: the boundary version and every country's fill and outline (a change rebuilds the image).</summary>
    public static string BuildKey(int boundaryVersion, IReadOnlyList<FoodArea> plan) =>
        boundaryVersion + "|" + string.Join(";", plan.Select(p => $"{p.Iso3}:{p.Fill}/{p.Outline}"));

    /// <summary>Draws the plan's countries (their polygons from <paramref name="countries"/>) at the image's size. STA thread.</summary>
    public static Bgra32Image Rasterize(IReadOnlyList<FoodArea> plan, CountryIndex countries, int width, int height)
    {
        var areas = new List<(GeoPolygon, Color?, Color?)>();
        foreach (var a in plan)
            if (countries.ByIso3(a.Iso3) is { } c)
                foreach (var poly in c.Polygons) areas.Add((poly, a.Fill, a.Outline));
        return RasterizeAreas(areas, width, height);
    }

    /// <summary>
    /// The fills (GlobeCore's area rasterizer, raised by <see cref="FillBoost"/>), then the outlines over them in one image:
    /// <see cref="OutlineWidthPx"/> wide and opaque, without the artificial edges along ±180°. STA thread.
    /// </summary>
    public static Bgra32Image RasterizeAreas(IReadOnlyList<(GeoPolygon Polygon, Color? Fill, Color? Outline)> areas, int width, int height)
    {
        var image = AreaRasterizer.Rasterize([.. areas.Where(a => a.Fill is not null).Select(a => (a.Polygon, a.Fill!.Value))], width, height);
        var px = image.Pixels;
        for (int i = 3; i < px.Length; i += 4)
            if (px[i] is > 0 and < 255) px[i] = (byte)Math.Min(255, Math.Round(px[i] * FillBoost));

        var outlines = areas.Where(a => a.Outline is not null).GroupBy(a => a.Outline!.Value)
            .Select(g => ((IEnumerable<IReadOnlyList<GeoPosition>>)g.SelectMany(a => OutlineLines(a.Polygon)).ToList(), g.Key))
            .ToList();
        if (outlines.Count == 0) return image;
        var lines = LineRasterizer.Rasterize(outlines, width, height, OutlineWidthPx);
        Over(image.Pixels, lines.Pixels);
        return image;
    }

    /// <summary>
    /// The polygon's edges as polylines, each ring closed, leaving out the edges that lie along the ±180° meridian: where a
    /// polygon was split at the dateline (Fiji, Russia) those are not borders, and are not outlined.
    /// </summary>
    public static IReadOnlyList<IReadOnlyList<GeoPosition>> OutlineLines(GeoPolygon polygon)
    {
        ArgumentNullException.ThrowIfNull(polygon);
        var result = new List<IReadOnlyList<GeoPosition>>();
        foreach (var ring in polygon.Rings)
        {
            int n = ring.Count;
            if (n > 1 && ring[0] == ring[n - 1]) n--;   // closed in the data: the last point repeats the first
            if (n < 2) continue;
            static bool Seam(GeoPosition a, GeoPosition b) => Math.Abs(a.LongitudeDeg) == 180 && Math.Abs(b.LongitudeDeg) == 180;
            // Start just after a seam edge if there is one, so a piece is never cut in two at the ring's first point.
            int start = 0;
            for (int i = 0; i < n; i++)
                if (Seam(ring[i], ring[(i + 1) % n])) { start = (i + 1) % n; break; }
            List<GeoPosition>? piece = null;
            for (int k = 0; k < n; k++)
            {
                var a = ring[(start + k) % n];
                var b = ring[(start + k + 1) % n];
                if (Seam(a, b))
                {
                    if (piece is { Count: >= 2 }) result.Add(piece);
                    piece = null;
                    continue;
                }
                piece ??= [a];
                piece.Add(b);
            }
            if (piece is { Count: >= 2 }) result.Add(piece);
        }
        return result;
    }

    // Straight-alpha "over": the outline image on top of the fill image, in place.
    private static void Over(byte[] dst, byte[] src)
    {
        for (int i = 0; i < dst.Length; i += 4)
        {
            int sa = src[i + 3];
            if (sa == 0) continue;
            if (sa == 255) { Buffer.BlockCopy(src, i, dst, i, 4); continue; }
            int da = dst[i + 3];
            int outA = sa * 255 + da * (255 - sa);   // x 255
            if (outA == 0) continue;
            for (int c = 0; c < 3; c++)
                dst[i + c] = (byte)Math.Min(255, (src[i + c] * sa * 255 + dst[i + c] * da * (255 - sa) + outA / 2) / outA);
            dst[i + 3] = (byte)((outA + 127) / 255);
        }
    }

    protected override async Task PrepareForComposeAsync()
    {
        var plan = Plan(_store.Snapshot.AsSeenAt(_nowUtc()).Crises);
        int version = Volatile.Read(ref _boundaryVersion);   // before the index is read: a swap meanwhile makes the next compose rebuild
        var key = BuildKey(version, plan);
        if (key == _builtKey) return;
        try
        {
            int w = _width, h = _height;
            var countries = _countries.Index;
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
        if (Picking.CountryAt(screen, projector, _countries.Index) is not { } country) return null;
        var food = _store.Snapshot.AsSeenAt(_nowUtc()).ByCountry(country.Iso3).OfType<FoodCrisis>().Where(f => !f.IsStale && !f.ProjectionOnly)
            .OrderByDescending(f => f.HighestPhase).FirstOrDefault();
        return food is null ? null
            : new PickResult(LayerId, new CountryPick(country.Iso3, CrisisLayer.Food), CrisisText.FoodHover(country.Name, food));
    }

    // Any thread: the host takes the request to the UI thread; the shading is rebuilt at the next compose.
    private void OnStoreChanged(object? sender, EventArgs e) => Invalidate();

    /// <summary>The boundaries changed (any thread): the shading is drawn again from the new polygons at the next compose.</summary>
    public void BoundariesChanged()
    {
        Interlocked.Increment(ref _boundaryVersion);
        Invalidate();
    }

    public void Close() => _store.Changed -= OnStoreChanged;
}
