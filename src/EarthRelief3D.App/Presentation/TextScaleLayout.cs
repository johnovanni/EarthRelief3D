using System.Windows;

namespace EarthRelief3D.App.Presentation;

/// <summary>
/// Text size (C8; P5-8): the sizes the cards take at 100%, 125% and 150%, in one place, so that <c>MainWindow</c> and the
/// tests use the same numbers. Every figure here is on screen (after the text scale), in device-independent pixels.
/// <para>
/// A card's text is scaled by a <c>ScaleTransform</c> layout transform on the card's inside (<c>Views.CardScale</c>); the
/// card's own size, margin and maximum are on-screen sizes. So a card that must stay clear of the others is given its
/// width, height and margin from here. The 3D globe is not scaled.
/// </para>
/// <para>
/// When the window is too narrow for the explainer beside the country panel (at 150% in a 1,000-wide window it is), the
/// explainer goes under the panel, in the panel's column, and the two share its height; each scrolls.
/// </para>
/// </summary>
public static class TextScaleLayout
{
    /// <summary>The sizes offered in Settings, "Text size": 100% (the default), 125%, 150%.</summary>
    public static IReadOnlyList<double> Choices { get; } = [1.0, 1.25, 1.5];

    /// <summary>The country panel's width at 100%.</summary>
    public const double PanelBaseWidth = 420;
    /// <summary>The panel never takes more than this share of the window's width.</summary>
    public const double PanelShare = 0.45;
    /// <summary>The gap between cards and the window's edge.</summary>
    public const double Gap = 12;
    /// <summary>The widest the layer strip (with its Settings flyout open) gets at 100%.</summary>
    public const double StripBaseWidth = 320;
    /// <summary>The legend's width at most, at 100%.</summary>
    public const double LegendBaseWidth = 560;
    /// <summary>The explainer card's width at most, at 100%.</summary>
    public const double ExplainerBaseWidth = 420;
    /// <summary>Under this width (at 100%) beside the panel, the explainer goes under the panel.</summary>
    public const double ExplainerMinBaseWidth = 240;
    /// <summary>The "Sources" card at 100% (its text and the status dots). Used where the card has not been measured (tests).</summary>
    public const double SourcesBaseWidth = 170, SourcesBaseHeight = 30;
    /// <summary>The folded legend at 100% (a header row and the UN note, wrapped). Used where the card has not been measured (tests).</summary>
    public const double LegendClosedBaseHeight = 80;
    /// <summary>The share of the window's height the layer strip may take before it scrolls, while the legend is open.</summary>
    public const double StripShare = 0.62;
    /// <summary>The share of the window's height an open legend may take before it scrolls (P5-24).</summary>
    public const double LegendShare = 0.5;

    /// <summary>A saved text size as the app reads it: one of <see cref="Choices"/>, or 1.0.</summary>
    public static double Normalise(double saved) => Choices.Contains(saved) ? saved : 1.0;

    /// <summary>The country panel's width on screen: its 100% width times the scale, but at most 45% of the window.</summary>
    public static double PanelWidth(double windowWidth, double scale) => Math.Min(PanelBaseWidth * scale, PanelShare * windowWidth);

    /// <summary>The left edge of the explainer beside the panel: right of the layer strip.</summary>
    public static double ExplainerSideLeft(double stripWidth) => stripWidth + 2 * Gap;

    /// <summary>True when the explainer goes under the panel: beside it there is less than <see cref="ExplainerMinBaseWidth"/> times the scale.</summary>
    public static bool IsStacked(double windowWidth, double scale, double stripWidth) =>
        windowWidth - 2 * Gap - PanelWidth(windowWidth, scale) - ExplainerSideLeft(stripWidth) < ExplainerMinBaseWidth * scale;

    /// <summary>How tall the layer strip may be before it scrolls: a share of the window while the legend is open, else all but the folded legend's height.</summary>
    public static double StripMaxHeight(double windowHeight, bool legendOpen, double legendHeight) =>
        Math.Max(200, legendOpen ? windowHeight * StripShare - 2 * Gap : windowHeight - 4 * Gap - legendHeight);

    /// <summary>How tall an open legend may be: at most <see cref="LegendShare"/> of the window, and never over the strip above it.</summary>
    public static double LegendMaxHeight(double windowHeight, double stripHeight) =>
        Math.Max(120, Math.Min(windowHeight * LegendShare - 2 * Gap, windowHeight - stripHeight - 4 * Gap));

    /// <summary>How wide the legend may be: not over the right-hand column (the panel, or the explainer under it) or, with neither, the Sources card.</summary>
    public static double LegendMaxWidth(double windowWidth, double scale, bool columnOpen, double sourcesWidth) =>
        Math.Max(100, Math.Min(LegendBaseWidth * scale, windowWidth - 3 * Gap - (columnOpen ? PanelWidth(windowWidth, scale) : sourcesWidth)));

    /// <summary>The bottom of the panel's column: above the Sources card.</summary>
    public static double ColumnBottom(double windowHeight, double sourcesHeight) => windowHeight - 2 * Gap - sourcesHeight;

    /// <summary>How tall the panel may be: the column's height, or half of it (less a gap) when the explainer is under it.</summary>
    public static double PanelMaxHeight(double windowHeight, double sourcesHeight, bool explainerUnder)
    {
        double column = ColumnBottom(windowHeight, sourcesHeight) - Gap;
        return Math.Max(100, explainerUnder ? (column - Gap) / 2 : column);
    }

    /// <summary>
    /// Where the explainer card is, at most: beside the panel (right of the strip, left of the panel, above the legend), or
    /// under the panel in its column. <paramref name="panelHeight"/> is the panel's height now (when it is open).
    /// </summary>
    public static Rect ExplainerBounds(Size window, double scale, double stripWidth, bool panelOpen, double panelHeight, double legendHeight, double sourcesHeight)
    {
        double panelWidth = PanelWidth(window.Width, scale);
        if (IsStacked(window.Width, scale, stripWidth))
        {
            double top = panelOpen ? Gap + panelHeight + Gap : Gap;
            return new Rect(window.Width - Gap - panelWidth, top, panelWidth, Math.Max(0, ColumnBottom(window.Height, sourcesHeight) - top));
        }
        double right = window.Width - 2 * Gap - panelWidth, left = ExplainerSideLeft(stripWidth);
        double width = Math.Max(0, Math.Min(ExplainerBaseWidth * scale, right - left));
        return new Rect(right - width, Gap, width, Math.Max(0, window.Height - 3 * Gap - legendHeight));
    }

    /// <summary>
    /// The on-screen rectangles of the layer strip, the legend, the country panel, the explainer card and the Sources card, in
    /// that order, at their largest (each card takes no more), from the numbers <c>MainWindow</c> gives the cards. A card that
    /// is not shown is <see cref="Rect.Empty"/>.
    /// </summary>
    public static Rect[] CardBounds(Size window, double scale, bool panelOpen, bool explainerOpen, bool legendOpen)
    {
        double sourcesW = SourcesBaseWidth * scale, sourcesH = SourcesBaseHeight * scale;
        double stripW = StripBaseWidth * scale;
        double legendClosedH = LegendClosedBaseHeight * scale;
        double stripH = StripMaxHeight(window.Height, legendOpen, legendClosedH);
        double legendH = legendOpen ? LegendMaxHeight(window.Height, stripH) : legendClosedH;
        bool stacked = IsStacked(window.Width, scale, stripW);
        bool columnOpen = panelOpen || (explainerOpen && stacked);
        double panelH = PanelMaxHeight(window.Height, sourcesH, explainerOpen && stacked);
        double panelW = PanelWidth(window.Width, scale);
        double legendW = LegendMaxWidth(window.Width, scale, columnOpen, sourcesW);
        return
        [
            new Rect(Gap, Gap, stripW, stripH),
            new Rect(Gap, window.Height - Gap - legendH, legendW, legendH),
            panelOpen ? new Rect(window.Width - Gap - panelW, Gap, panelW, panelH) : Rect.Empty,
            explainerOpen ? ExplainerBounds(window, scale, stripW, panelOpen, panelH, legendH, sourcesH) : Rect.Empty,
            new Rect(window.Width - Gap - sourcesW, window.Height - Gap - sourcesH, sourcesW, sourcesH),
        ];
    }
}
