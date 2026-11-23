using System.Globalization;
using System.Windows;
using System.Windows.Media;
using EarthRelief3D.App.Presentation;
using EarthRelief3D.Core.Feeds;
using EarthRelief3D.Core.Model;
using GlobeCore.Earth;
using GlobeCore.Imaging;
using GlobeCore.Logging;
using GlobeCore.Wpf.Layers;
using GlobeCore.Wpf.Rendering;

namespace EarthRelief3D.App.Layers;

/// <summary>One country to fill, and its class colour.</summary>
public sealed record ShadedCountry(string Iso3, Color Color);

/// <summary>
/// A country-value shading (Plan 3, P3-7: water, sanitation, health care, hunger, shelter need, oil production, water
/// stress): each country with a figure in the series is filled in its class colour, the way the food layer fills (GlobeCore's
/// area rasterizer raised by <see cref="FoodImageLayer.FillBoost"/>), above the night shade and under the borders. A
/// country with no figure (none in the series, or one below the first break: <see cref="HasFigure"/>) is not filled,
/// and its tooltip says "No figure from &lt;source&gt;", so "no figure" never looks
/// like "low need". While the series is not loaded nothing is drawn (ruling R2). Only one shading shows at a time (P3-6).
/// <para>
/// The store's Changed (feed threads) only asks for a recompose (<see cref="DataChanged"/>); the shading is rasterized on
/// the STA worker at the start of the next compose, and only when the countries or classes it shows have changed.
/// </para>
/// </summary>
public sealed class CountryValueLayer : ImageLayer, IPickable
{
    /// <summary>The shadings drawn by this class, in strip order (food is <see cref="FoodImageLayer"/>).</summary>
    public static IReadOnlyList<ShadingLayer> Layers { get; } = [.. LayerCatalog.NeedsShadings, .. LayerCatalog.ResourceShadings];

    /// <summary>The layer's z-order: just after the food layer (only one shading is visible, so they never compete).</summary>
    public static int OrderOf(ShadingLayer layer) => FoodImageLayer.Order + (int)layer;

    /// <summary>The blend order inside the AboveNight pass: the food layer's, under the borders.</summary>
    public const int ImageOrder = FoodImageLayer.ImageOrder;

    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    private readonly Func<IndicatorSeries?> _series;
    private readonly int _width, _height;
    private readonly CountryResolver _countries;
    private readonly StaWorker _sta;
    private readonly IAppLog _log;
    private readonly Func<bool> _isClosed;
    private int _boundaryVersion;
    private string? _builtKey;
    private IImageLayer? _current;

    /// <param name="series">The series to draw, read at each compose and hover; null while it is not loaded.</param>
    public CountryValueLayer(ShadingLayer which, Func<IndicatorSeries?> series, ShadingScale scale, int width, int height,
        CountryResolver countries, StaWorker sta, IAppLog log, Func<bool> isClosed)
        : base(LayerCatalog.Id(which), LayerCatalog.Label(which), OrderOf(which))
    {
        if (!Layers.Contains(which)) throw new ArgumentOutOfRangeException(nameof(which), which, "Not a country-value shading.");
        Which = which;
        _series = series ?? throw new ArgumentNullException(nameof(series));
        Scale = scale ?? throw new ArgumentNullException(nameof(scale));
        _width = width;
        _height = height;
        _countries = countries ?? throw new ArgumentNullException(nameof(countries));
        _sta = sta ?? throw new ArgumentNullException(nameof(sta));
        _log = log ?? throw new ArgumentNullException(nameof(log));
        _isClosed = isClosed ?? throw new ArgumentNullException(nameof(isClosed));
    }

    public ShadingLayer Which { get; }
    public ShadingScale Scale { get; }

    public ImagePass Pass => ImagePass.AboveNight;

    public override IImageLayer? Current => _current;

    /// <summary>
    /// The countries to fill, by ISO3 (upper case, in order): each country's newest figure (<see cref="IndicatorSeries.For"/>)
    /// in its class colour. Countries without a figure, or whose figure has no class (not a number, or below the first
    /// break), are left out. Empty while the series is not loaded.
    /// </summary>
    public static IReadOnlyList<ShadedCountry> Plan(IndicatorSeries? series, ShadingScale scale)
    {
        ArgumentNullException.ThrowIfNull(scale);
        if (series is null) return [];
        var plan = new List<ShadedCountry>();
        foreach (var iso in series.Values.Select(v => v.Iso3.ToUpperInvariant()).Distinct().Order(StringComparer.Ordinal))
            if (series.For(iso) is { } v && scale.ColorOf(v.Value) is { } color) plan.Add(new ShadedCountry(iso, color));
        return plan;
    }

    /// <summary>The key of what is drawn: the boundary version and every country's colour (a change rebuilds the image).</summary>
    public static string BuildKey(int boundaryVersion, IReadOnlyList<ShadedCountry> plan) =>
        boundaryVersion + "|" + string.Join(";", plan.Select(p => $"{p.Iso3}:{p.Color}"));

    /// <summary>Fills the plan's countries (their polygons from <paramref name="countries"/>) at the image's size, as the food layer fills. STA thread.</summary>
    public static Bgra32Image Rasterize(IReadOnlyList<ShadedCountry> plan, CountryIndex countries, int width, int height)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(countries);
        var areas = new List<FoodPolygon>();
        foreach (var p in plan)
            if (countries.ByIso3(p.Iso3) is { } c)
                foreach (var poly in c.Polygons) areas.Add(new FoodPolygon(poly, p.Color, null, null));
        return FoodImageLayer.RasterizeAreas(areas, width, height);
    }

    /// <summary>
    /// The hover text: "Iceland · Share of people without at least basic drinking water, 2024: 0.0% · WHO GHO"; for a
    /// country with no figure "Iceland · No figure from WHO GHO for the share of …: not shaded". Null while the series is
    /// not loaded (the borders layer answers).
    /// </summary>
    public static string? Tooltip(ShadingLayer which, string countryName, string iso3, IndicatorSeries? series, ShadingScale scale)
    {
        if (series is null) return null;
        var source = LayerCatalog.SourceName(which);
        if (series.For(iso3) is not { } v || !HasFigure(v, scale))
            return $"{countryName} · No figure from {source} for the {LayerCatalog.LowerFirst(series.Measures)}: not shaded";
        return $"{countryName} · {series.Measures}, {v.Year}: {ValueText(v, series.Unit, scale)} · {source}";
    }

    /// <summary>
    /// True when <paramref name="v"/> is a figure the map shows: the source qualified it (FAO's "&lt;2.5"), or it falls in
    /// a class of <paramref name="scale"/>. A value below the first break (WRI's -9999 "no data", a slightly negative
    /// 100 − x) or not a number is no figure: not filled, and the tooltip says "No figure from …".
    /// </summary>
    public static bool HasFigure(CountryValue v, ShadingScale scale)
    {
        ArgumentNullException.ThrowIfNull(v);
        ArgumentNullException.ThrowIfNull(scale);
        return v.Qualifier is not null || scale.ClassOf(v.Value) is not null;
    }

    /// <summary>
    /// The figure as shown: the source's qualifier when it has one ("&lt;2.5" → "under 2.5%", ruling R3; a WRI label as
    /// written), a category's label on a categorical scale, else the number with its unit ("0.0%", "9,612 thousand barrels
    /// per day", "47.2 thousand barrels per day").
    /// </summary>
    public static string ValueText(CountryValue v, string unit, ShadingScale scale)
    {
        ArgumentNullException.ThrowIfNull(v);
        string suffix = unit switch { "%" => "%", "" or "category" => "", _ => " " + unit };
        if (v.Qualifier is { Length: > 0 } q)
        {
            q = q.Trim();
            if (q.StartsWith('<')) return "under " + q[1..].Trim() + suffix;
            if (q.StartsWith('>')) return "over " + q[1..].Trim() + suffix;
            return q;
        }
        if (scale.Categorical && scale.ClassOf(v.Value) is { } c) return scale.ClassLabels[c];
        string number = unit == "%" ? v.Value.ToString("0.0", Inv)
            : Math.Abs(v.Value) >= 100 ? v.Value.ToString("N0", Inv) : v.Value.ToString("0.#", Inv);
        return number + suffix;
    }

    protected override async Task PrepareForComposeAsync()
    {
        var plan = Plan(_series(), Scale);
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
            // Keep what was shown; the next change or compose tries again.
            _log.Error($"{Title} shading could not be drawn", ex);
        }
    }

    public PickResult? HitTest(Point screen, Projector projector, bool hover)
    {
        if (Picking.CountryAt(screen, projector, _countries.Index) is not { } country) return null;
        return Tooltip(Which, country.Name, country.Iso3, _series(), Scale) is { } text
            ? new PickResult(Id, new CountryPick(country.Iso3, null), text) : null;
    }

    /// <summary>The series changed (any thread): the shading is rebuilt at the next compose if what it shows changed.</summary>
    public void DataChanged() => Invalidate();

    /// <summary>The boundaries changed (any thread): the shading is drawn again from the new polygons at the next compose.</summary>
    public void BoundariesChanged()
    {
        Interlocked.Increment(ref _boundaryVersion);
        Invalidate();
    }
}
