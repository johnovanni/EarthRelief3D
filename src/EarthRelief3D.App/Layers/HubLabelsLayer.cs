using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using EarthRelief3D.App.Presentation;
using EarthRelief3D.Core.Logic;
using GlobeCore.Wpf.Layers;
using GlobeCore.Wpf.Rendering;

namespace EarthRelief3D.App.Layers;

/// <summary>
/// The distance labels of the hub arcs (Plan 4 Task 5): "&lt;hub name&gt; · &lt;distance&gt; straight-line" at the middle of
/// each arc, in its hub's colour, left out when that point is on the far side of the globe (the same limb test as the country
/// labels). The distance is in the unit chosen in Settings (<see cref="Changed"/> says it changed). Labels that would overlap
/// are moved a line apart, never dropped (<see cref="Place"/>).
/// </summary>
public sealed class HubLabelsLayer : OverlayLayer
{
    public const int Order = 201;
    public const double FontSize = 12;
    /// <summary>Labels within this much of the globe's edge (cosine of the angle to the camera) are left out, as <see cref="LabelsOverlayLayer"/>'s.</summary>
    private const double LimbCosine = 0.25;

    private readonly LabelOverlay _labels;
    private readonly Func<IReadOnlyList<HubArcSpec>> _specs;
    private readonly Func<DistanceUnit> _units;
    private readonly Func<double> _textScale;

    /// <param name="textScale">The text size chosen in Settings (1.0, 1.25 or 1.5), read at each render; null: 100%. <see cref="Changed"/> says it changed.</param>
    public HubLabelsLayer(Func<IReadOnlyList<HubArcSpec>> specs, Func<DistanceUnit> units, Canvas overlay, Func<double>? textScale = null)
        : base(LayerCatalog.Id(MarkerLayer.HubLines) + "-labels", LayerCatalog.Label(MarkerLayer.HubLines) + ": distances", Order)
    {
        _specs = specs ?? throw new ArgumentNullException(nameof(specs));
        _units = units ?? throw new ArgumentNullException(nameof(units));
        _textScale = textScale ?? (() => 1.0);
        _labels = new LabelOverlay(overlay ?? throw new ArgumentNullException(nameof(overlay)));
    }

    /// <summary>The labels placed by the last render (tests, diagnostics).</summary>
    public int PlacedCount { get; private set; }

    /// <summary>The panel, the toggle, the hubs, the unit or the text size changed (any thread): the labels are placed again at the next render.</summary>
    public void Changed() => Invalidate();

    /// <summary>"Port Sudan New International Airport · 812 km straight-line"; a draft depot (a <c>--show-drafts</c> run) "UNHRD Dubai (draft) · ...".</summary>
    public static string LabelText(HubLink link, DistanceUnit unit)
    {
        ArgumentNullException.ThrowIfNull(link);
        return $"{link.Hub.Name}{(link.Hub.IsDraft ? " (draft)" : "")} · {CrisisText.Distance(link.DistanceKm, unit)} straight-line";
    }

    public override void Render(Canvas overlay, Projector projector)
    {
        _labels.Begin();
        int placed = 0;
        var eye = (Vector3D)projector.Eye;
        var unit = _units();
        double k = _textScale();
        var wanted = new List<(string, Point, Color)>();
        foreach (var spec in _specs() ?? [])
        {
            var path = HubArcLayer.ArcPoints(spec.From, spec.Link.Hub.Position, HubArcLayer.LiftFactor(spec.Link.Hub.Kind));
            if (path.Count < 2) continue;
            var p = HubArcLayer.MidPoint(path);
            var toEye = eye - (Vector3D)p;
            if (toEye.Length <= 0 || Vector3D.DotProduct((Vector3D)p, toEye) / toEye.Length < LimbCosine) continue;
            if (!projector.TryProject(p, out var at)) continue;
            wanted.Add((LabelText(spec.Link, unit), new Point(at.X + 4, at.Y - 9 * k), LayerStyle.HubColor(spec.Link.Hub.Kind)));
        }
        placed = Place(_labels, wanted, FontSize * k);
        _labels.End();
        PlacedCount = placed;
    }

    /// <summary>
    /// Places the arc labels so none is dropped (final review F2, m24): each goes at its point if that is free, otherwise a line
    /// or more below or above it, and if every nearby line is taken it is placed at its point anyway (forced). Only a label
    /// off the canvas is left out. Returns how many were placed.
    /// </summary>
    public static int Place(LabelOverlay labels, IEnumerable<(string Text, Point At, Color Color)> wanted, double fontSize = FontSize)
    {
        ArgumentNullException.ThrowIfNull(labels);
        ArgumentNullException.ThrowIfNull(wanted);
        double line = fontSize + 4;   // LabelOverlay's label height
        int placed = 0;
        foreach (var (text, at, color) in wanted)
        {
            bool done = false;
            foreach (var step in NudgeSteps)
                if (labels.TryAdd(text, new Point(at.X, at.Y + step * line), color, fontSize: fontSize)) { done = true; break; }
            if (done || labels.TryAdd(text, at, color, force: true, fontSize: fontSize)) placed++;
        }
        return placed;
    }

    // At the point, then one line below, one above, two below, two above, three below, three above.
    private static readonly int[] NudgeSteps = [0, 1, -1, 2, -2, 3, -3];

    protected override void Clear(Canvas overlay)
    {
        _labels.Begin();
        _labels.End();
        PlacedCount = 0;
    }
}
