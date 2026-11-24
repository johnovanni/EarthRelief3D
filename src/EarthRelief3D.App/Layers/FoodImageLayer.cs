using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
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

/// <summary>
/// One country on the food layer: its fill colour (the 20% rule's phase) and its outline colour (a worse phase with
/// people); or, for a country drawn by IPC area (P3-2), no country colours and its <paramref name="Areas"/>.
/// </summary>
public sealed record FoodArea(string Iso3, Color? Fill, Color? Outline, Color? Faint = null, IpcAreaSet? Areas = null);

/// <summary>One polygon to draw: its fill, its outline, and its faint tint (an outline-only country, owner's decision round 2).</summary>
public sealed record FoodPolygon(GeoPolygon Polygon, Color? Fill, Color? Outline, Color? Faint);

/// <summary>
/// Food crises (IPC national figures, ruling P2-3, shading rule R17; IPC areas, P3-2). Where IPC publishes current areas
/// that belong to the country's national analysis (ruling R4), each area of Phase 3 to 5 is filled in its IPC colour with a
/// 1 px edge in that colour, in IPC's own area classification, and the country is not shaded by the rule. Elsewhere each
/// country with a current analysis is shaded in the
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
    /// <summary>
    /// The faint tint's opacity (46 of 255, about 18%): about 30% of a shaded fill's (153, about 60%), so an outline-only
    /// country reads as tinted but never as shaded (owner's decision, round 2).
    /// </summary>
    public const int FaintAlpha = 46;

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
    /// The outline's width in pixels of the layer's image (the globe image, 4096 wide), fully opaque, drawn over the fill
    /// (R17): a band this wide just inside the country's edge. The 1 px border line drawn above covers half a pixel of it,
    /// so about 2 px show.
    /// </summary>
    public const double OutlineWidthPx = 2.5;

    /// <summary>
    /// The countries to draw, by ISO3, with their fill and outline colours from <see cref="IpcAreaRule.MapStyle"/>. One
    /// record per country, the one the panel shows (<see cref="FoodCrisis.Latest"/>), whose fill and outline go together;
    /// when that record is stale or projection only, the country is not drawn. A country is in the plan when it has a
    /// fill, an outline or both.
    /// </summary>
    public static IReadOnlyList<FoodArea> Plan(IEnumerable<Crisis> crises) => Plan(crises, []);

    /// <summary>
    /// The plan for a snapshot: a country whose latest national record has a matching area set
    /// (<see cref="CrisisSnapshot.AreasFor"/>) is drawn by its areas instead of by the rule.
    /// </summary>
    public static IReadOnlyList<FoodArea> Plan(CrisisSnapshot s)
    {
        ArgumentNullException.ThrowIfNull(s);
        return Plan(s.Crises, s.IpcAreas);
    }

    private static IReadOnlyList<FoodArea> Plan(IEnumerable<Crisis> crises, IReadOnlyList<IpcAreaSet> areaSets)
    {
        var withAreas = new CrisisSnapshot([], [], []) { IpcAreas = areaSets };
        var plan = new List<FoodArea>();
        foreach (var g in crises.OfType<FoodCrisis>().GroupBy(f => f.CountryIso3.ToUpperInvariant()).OrderBy(g => g.Key, StringComparer.Ordinal))
        {
            var latest = FoodCrisis.Latest(g)!;
            if (withAreas.AreasFor(latest) is { } areas)
            {
                plan.Add(new FoodArea(g.Key, null, null, null, areas));
                continue;
            }
            var style = IpcAreaRule.MapStyle(latest);
            if (!style.IsEmpty) plan.Add(new FoodArea(g.Key, PhaseColor(style.Shaded), PhaseColor(style.Outline), PhaseColor(style.Faint)));
        }
        return plan;
    }

    /// <summary>The areas IPC classified in Phase 3 to 5 are drawn; Phase 1 and 2 and unclassified areas are not (as R17's phases).</summary>
    public static bool IsDrawn(IpcArea a) => a.Phase is >= 3 and <= 5;

    private static Color? PhaseColor(int? phase) => phase is { } p ? LayerStyle.IpcPhaseColor(p) : null;

    /// <summary>
    /// The width of <paramref name="polygon"/>'s outline band in image pixels: <see cref="OutlineWidthPx"/>, narrowed to a
    /// quarter of the smaller side of the polygon's bounding box so a small country keeps some fill inside it, and never
    /// under 1 px.
    /// </summary>
    public static double OutlineBandPx(GeoPolygon polygon, int width, int height)
    {
        ArgumentNullException.ThrowIfNull(polygon);
        var points = polygon.Rings.SelectMany(r => r).ToList();
        if (points.Count == 0) return OutlineWidthPx;
        double w = (points.Max(p => p.LongitudeDeg) - points.Min(p => p.LongitudeDeg)) / 360 * width;
        double h = (points.Max(p => p.LatitudeDeg) - points.Min(p => p.LatitudeDeg)) / 180 * height;
        return Math.Max(1.0, Math.Min(OutlineWidthPx, Math.Min(w, h) / 4));
    }

    /// <summary>
    /// The key of what is drawn: the boundary version and every country's fill, outline and faint tint, or its areas'
    /// analysis, period and drawn phases (a change rebuilds the image).
    /// </summary>
    public static string BuildKey(int boundaryVersion, IReadOnlyList<FoodArea> plan) =>
        boundaryVersion + "|" + string.Join(";", plan.Select(p => p.Areas is { } a
            ? $"{p.Iso3}:areas/{a.AnalysisId}/{a.From:yyyyMMdd}-{a.To:yyyyMMdd}/{a.Areas.Count}/{AreaHash(a)}"
            : $"{p.Iso3}:{p.Fill}/{p.Outline}/{p.Faint}"));

    // The areas' names, drawn phases and part counts, so a reclassified area rebuilds the image.
    private static string AreaHash(IpcAreaSet set)
    {
        var h = new HashCode();
        foreach (var a in set.Areas)
        {
            h.Add(a.Name, StringComparer.Ordinal);
            h.Add(IsDrawn(a) ? a.Phase : 0);
            h.Add(a.Polygons.Count);
        }
        return h.ToHashCode().ToString("x8", System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Draws the plan's countries (their polygons from <paramref name="countries"/>) and, for a country drawn by area, its
    /// Phase 3 to 5 areas: filled as the countries are, with the area rasterizer's 1 px edge in the phase colour (no
    /// outline band; no edge along ±180°), at the image's size. STA thread.
    /// </summary>
    public static Bgra32Image Rasterize(IReadOnlyList<FoodArea> plan, CountryIndex countries, int width, int height)
    {
        var areas = new List<FoodPolygon>();
        foreach (var a in plan)
        {
            if (a.Areas is { } set)
            {
                foreach (var area in set.Areas.Where(IsDrawn))
                {
                    var color = LayerStyle.IpcPhaseColor(area.Phase!.Value);
                    foreach (var poly in area.Polygons) areas.Add(new FoodPolygon(poly, color, null, null));
                }
                continue;
            }
            if (countries.ByIso3(a.Iso3) is { } c)
                foreach (var poly in c.Polygons) areas.Add(new FoodPolygon(poly, a.Fill, a.Outline, a.Faint));
        }
        return RasterizeAreas(areas, width, height);
    }

    /// <summary>Fills and outlines with no faint tint (see the <see cref="FoodPolygon"/> overload).</summary>
    public static Bgra32Image RasterizeAreas(IReadOnlyList<(GeoPolygon Polygon, Color? Fill, Color? Outline)> areas, int width, int height) =>
        RasterizeAreas([.. areas.Select(a => new FoodPolygon(a.Polygon, a.Fill, a.Outline, null))], width, height);

    /// <summary>
    /// The fills (GlobeCore's area rasterizer, raised by <see cref="FillBoost"/>) and the faint tints (the same, lowered to
    /// <see cref="FaintAlpha"/>), then the outlines over them in one image: opaque bands up to <see cref="OutlineWidthPx"/>
    /// wide just inside each country's edge, without the artificial edges along ±180°. STA thread.
    /// </summary>
    public static Bgra32Image RasterizeAreas(IReadOnlyList<FoodPolygon> areas, int width, int height)
    {
        ArgumentNullException.ThrowIfNull(areas);
        var image = AreaRasterizer.Rasterize([.. areas.Where(a => a.Fill is not null).Select(a => (a.Polygon, a.Fill!.Value))], width, height);
        var px = image.Pixels;
        for (int i = 3; i < px.Length; i += 4)
            if (px[i] is > 0 and < 255) px[i] = (byte)Math.Min(255, Math.Round(px[i] * FillBoost));

        var faint = areas.Where(a => a.Faint is not null).Select(a => (a.Polygon, a.Faint!.Value)).ToList();
        if (faint.Count > 0)
        {
            // The area rasterizer fills at 89 (35%); the tint keeps that coverage and is lowered to FaintAlpha. Countries do
            // not overlap, so the tints go over the fills without mixing.
            var tint = AreaRasterizer.Rasterize(faint, width, height);
            var tp = tint.Pixels;
            for (int i = 3; i < tp.Length; i += 4)
                if (tp[i] is > 0 and < 255) tp[i] = (byte)Math.Min(255, Math.Round(tp[i] * (FaintAlpha / 89.0)));
            Over(px, tp);
        }

        var outlined = areas.Where(a => a.Outline is not null).Select(a => (a.Polygon, a.Outline!.Value)).ToList();
        if (outlined.Count == 0) return image;
        Over(px, RasterizeOutlines(outlined, width, height));
        return image;
    }

    /// <summary>
    /// Each polygon's outline as a band just inside its edge: a pen twice <see cref="OutlineBandPx"/> wide along the edges
    /// (<see cref="OutlineLines"/>, so not along ±180°), clipped to the polygon. Inside, so two neighbours outlined in
    /// different phases each keep their colour on their own side of the shared border, and the 1 px border line drawn above
    /// covers little of it. Straight-alpha BGRA pixels. STA thread.
    /// </summary>
    private static byte[] RasterizeOutlines(IReadOnlyList<(GeoPolygon Polygon, Color Color)> areas, int width, int height)
    {
        Point P(GeoPosition g) => new((g.LongitudeDeg + 180) / 360 * width, (90 - g.LatitudeDeg) / 180 * height);
        var visual = new DrawingVisual();
        var pens = new Dictionary<(Color, double), Pen>();
        using (var dc = visual.RenderOpen())
        {
            foreach (var (polygon, color) in areas)
            {
                double band = OutlineBandPx(polygon, width, height);
                if (!pens.TryGetValue((color, band), out var pen))
                {
                    var brush = new SolidColorBrush(color);
                    brush.Freeze();
                    pens[(color, band)] = pen = new Pen(brush, 2 * band) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round, LineJoin = PenLineJoin.Round };
                    pen.Freeze();
                }
                var clip = new StreamGeometry { FillRule = FillRule.EvenOdd };
                using (var ctx = clip.Open())
                    foreach (var ring in polygon.Rings)
                    {
                        if (ring.Count < 3) continue;
                        ctx.BeginFigure(P(ring[0]), true, true);
                        for (int i = 1; i < ring.Count; i++) ctx.LineTo(P(ring[i]), true, false);
                    }
                clip.Freeze();
                var edges = new StreamGeometry();
                using (var ctx = edges.Open())
                    foreach (var line in OutlineLines(polygon))
                    {
                        ctx.BeginFigure(P(line[0]), false, false);
                        for (int i = 1; i < line.Count; i++) ctx.LineTo(P(line[i]), true, false);
                    }
                edges.Freeze();
                dc.PushClip(clip);
                dc.DrawGeometry(null, pen, edges);
                dc.Pop();
            }
        }
        var rtb = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        rtb.Render(visual);
        var px = new byte[checked(width * height * 4)];
        rtb.CopyPixels(px, width * 4, 0);
        for (int i = 0; i < px.Length; i += 4)   // premultiplied -> straight alpha, as the composer expects
        {
            byte a = px[i + 3];
            if (a is 0 or 255) continue;
            for (int c = 0; c < 3; c++) px[i + c] = (byte)Math.Min(255, (px[i + c] * 255 + a / 2) / a);
        }
        return px;
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
        var plan = Plan(_store.Snapshot.AsSeenAt(_nowUtc()));
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
        if (Picking.PositionAt(screen, projector) is not { } g || _countries.Index.Find(g) is not { } country) return null;
        var text = HoverAt(_store.Snapshot.AsSeenAt(_nowUtc()), country.Iso3, country.Name, g);
        return text is null ? null : new PickResult(LayerId, new CountryPick(country.Iso3, CrisisLayer.Food), text);
    }

    /// <summary>
    /// The food tooltip at <paramref name="g"/> in the country <paramref name="iso3"/>, from the record the map and the panel
    /// use; null for a stale or projection-only one, which is not on this layer (the borders layer answers). For a country
    /// drawn by area (P3-2): the area under the point, in its IPC area classification, or between areas the country's line in
    /// area terms.
    /// </summary>
    public static string? HoverAt(CrisisSnapshot s, string iso3, string country, GeoPosition g)
    {
        ArgumentNullException.ThrowIfNull(s);
        var food = FoodCrisis.Latest(s.ByCountry(iso3).OfType<FoodCrisis>());
        if (food is null || food.IsStale || food.ProjectionOnly) return null;
        if (s.AreasFor(food) is not { } set) return CrisisText.FoodHover(country, food);
        return set.AreaAt(g) is { } area ? CrisisText.AreaHover(area, country) : CrisisText.FoodHoverByArea(country, food);
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
