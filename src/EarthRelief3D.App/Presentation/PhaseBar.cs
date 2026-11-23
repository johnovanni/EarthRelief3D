using System.Windows.Media;
using EarthRelief3D.Core.Model;

namespace EarthRelief3D.App.Presentation;

/// <summary>One piece of the bar: a phase (1 to 5), or 0 for the people analysed who are in no reported phase.</summary>
/// <param name="Fraction">Its share of the denominator D (the pieces add up to 1).</param>
public sealed record PhaseBarSegment(int Phase, long People, double Fraction, Color Color);

/// <summary>
/// One row under the bar: swatch, name, people and percent of D ("not reported" for a blank cell). Phase 0 is the row for
/// the people in no reported phase.
/// </summary>
public sealed record PhaseBarRow(int Phase, string Name, string People, string Percent, Color Color)
{
    /// <summary>For the view: one shared frozen brush per colour (computed, so it takes no part in the record's equality).</summary>
    public SolidColorBrush Brush => LayerStyle.Frozen(Color);
}

/// <summary>
/// The country panel's phase bar (ruling R17): the people analysed (D, the rule's own denominator) as one bar, the people in
/// no reported phase first in grey, then Phases 1 to 5 in IPC colours, so the right-hand 20% are always the worst-off. A
/// dashed marker at 80% of the width, labelled "worst 20%", falls in the rule's line phase (<see cref="MarkerIndex"/>).
/// A caption says where it falls and what the map shows; a row per phase gives the numbers. <see cref="AutomationName"/>
/// carries the same facts as text, so colour is never the only carrier of meaning.
/// </summary>
/// <param name="HasBar">False when there is nothing per phase to stack (only the rows and the caption show).</param>
/// <param name="OfAnalysed">"of 47,535,794 people analysed", what D is, or "share unknown".</param>
/// <param name="MarkerIndex">The segment the marker falls in (worked out in whole people); -1 without a bar.</param>
/// <param name="Note">The famine note when the country is shaded Phase 5; null otherwise.</param>
public sealed record PhaseBar(IReadOnlyList<PhaseBarSegment> Segments, IReadOnlyList<PhaseBarRow> Rows, bool HasBar, string OfAnalysed,
    string Caption, string AutomationName, int MarkerIndex, string? Note)
{
    /// <summary>The marker's place along the bar: the right 20% are the worst-off people.</summary>
    public const double MarkerAt = 1 - IpcAreaRule.Threshold;

    public const string MarkerLabel = "worst 20%";
    public const string NotInAPhaseName = "Not in a reported phase";

    /// <summary>The grey of the people in no reported phase: neutral, and unlike every IPC colour.</summary>
    public static Color NotInAPhaseColor { get; } = Color.FromRgb(0x6B, 0x73, 0x85);

    public bool HasNote => Note is not null;

    /// <summary>The bar for <paramref name="f"/>'s current-period counts (stale analyses too; never for a projection-only record).</summary>
    public static PhaseBar For(string country, FoodCrisis f)
    {
        ArgumentNullException.ThrowIfNull(f);
        var c = IpcAreaRule.Classify(f);
        var segments = new List<PhaseBarSegment>();
        if (c.ShareKnown && c.NotInAPhase > 0)
            segments.Add(new PhaseBarSegment(0, c.NotInAPhase, c.NotInAPhaseShare, NotInAPhaseColor));
        for (int p = 1; p <= 5; p++)
            segments.Add(new PhaseBarSegment(p, IpcAreaRule.People(f, p), c.ShareKnown ? c.Share(p) : 0, LayerStyle.IpcPhaseColor(p)));

        var rows = Enumerable.Range(1, 5).Select(p => new PhaseBarRow(p, LayerStyle.IpcPhaseLabel(p),
            f.IsReported(p) ? CrisisText.Number(IpcAreaRule.People(f, p)) : CrisisText.NotReported,
            !f.IsReported(p) ? CrisisText.NotReported : c.ShareKnown ? CrisisText.FoodPercent(c.Share(p)) : "",
            LayerStyle.IpcPhaseColor(p))).ToList();
        if (c.ShareKnown && c.NotInAPhase > 0)
            rows.Add(new PhaseBarRow(0, NotInAPhaseName, CrisisText.Number(c.NotInAPhase), CrisisText.FoodPercent(c.NotInAPhaseShare),
                NotInAPhaseColor));

        bool hasBar = c.CanStack;
        int marker = -1;
        if (hasBar)
        {
            // The piece holding the point 80% along, in whole people: the first whose running total passes 4/5 of D.
            long total = segments.Sum(s => s.People), running = 0;
            for (int i = 0; i < segments.Count && marker < 0; i++)
            {
                running += segments[i].People;
                if (running * 5 > total * 4) marker = i;
            }
        }

        var of = CrisisText.OfAnalysed(f, c);
        var caption = CrisisText.BarCaption(country, f, c);
        string? note = !f.IsStale && !f.ProjectionOnly && c.ShadedPhase == 5 ? CrisisText.FamineNote : null;
        var parts = rows.Select(r => r.People == CrisisText.NotReported ? $"{r.Name} {CrisisText.NotReported}"
            : r.Percent.Length > 0 ? $"{r.Name} {r.Percent}, {r.People} people" : $"{r.Name} {r.People} people");
        var name = $"IPC phase bar for {country}, {of}: {string.Join("; ", parts)}. "
                   + (hasBar ? "A dashed line marks the worst 20%. " : "") + caption + (note is null ? "" : " " + note);
        return new PhaseBar(segments, rows, hasBar, of, caption, name, marker, note);
    }
}

/// <summary>The widths of the bar's pieces, and where the marker is drawn.</summary>
public static class PhaseBarLayout
{
    /// <summary>A piece with people in it is never thinner than this (a 0.3% Phase 5 stays visible).</summary>
    public const double MinPx = 3;

    /// <summary>
    /// Widths that fill <paramref name="width"/> in proportion to <paramref name="people"/>, with every piece that has more
    /// than 0 people at least <paramref name="minPx"/> wide (taken from the wider ones), and 0 for a piece with nobody.
    /// </summary>
    public static double[] Widths(IReadOnlyList<long> people, double width, double minPx = MinPx)
    {
        ArgumentNullException.ThrowIfNull(people);
        var w = new double[people.Count];
        var present = Enumerable.Range(0, people.Count).Where(i => people[i] > 0).ToList();
        if (present.Count == 0 || width <= 0) return w;
        if (present.Count * minPx >= width)
        {
            foreach (var i in present) w[i] = width / present.Count;
            return w;
        }
        var atMin = new HashSet<int>();
        while (true)
        {
            var free = present.Where(i => !atMin.Contains(i)).ToList();
            double room = width - atMin.Count * minPx;
            double total = free.Sum(i => (double)people[i]);
            bool changed = false;
            foreach (var i in free)
            {
                w[i] = room * people[i] / total;
                if (w[i] < minPx) { atMin.Add(i); changed = true; }
            }
            if (!changed) break;
        }
        foreach (var i in atMin) w[i] = minPx;
        return w;
    }

    /// <summary>
    /// The marker's x: 80% of <paramref name="width"/>, moved just inside piece <paramref name="index"/> when the minimum
    /// widths have shifted that piece's edges past it, so the drawn marker always sits in the rule's phase.
    /// </summary>
    public static double MarkerX(IReadOnlyList<double> widths, int index, double width)
    {
        ArgumentNullException.ThrowIfNull(widths);
        double x = width * PhaseBar.MarkerAt;
        if (index < 0 || index >= widths.Count) return x;
        double start = widths.Take(index).Sum(), end = start + widths[index];
        double inset = Math.Min(1, widths[index] / 2);
        return Math.Clamp(x, start + inset, end - inset);
    }
}
