using System.Windows.Media;
using EarthRelief3D.Core.Model;

namespace EarthRelief3D.App.Presentation;

/// <summary>One phase's piece of the bar: its share of the bar (the five counts stacked to 100%).</summary>
public sealed record PhaseBarSegment(int Phase, long People, double Fraction, Color Color);

/// <summary>One row under the bar: swatch, IPC name, people and percent of the people analysed ("not reported" for a blank cell).</summary>
public sealed record PhaseBarRow(int Phase, string Name, string People, string Percent, Color Color)
{
    /// <summary>For the view: one shared frozen brush per colour (computed, so it takes no part in the record's equality).</summary>
    public SolidColorBrush Brush => LayerStyle.Frozen(Color);
}

/// <summary>
/// The country panel's phase bar (ruling R17): one 100%-stacked bar of Phases 1 to 5 in IPC colours, a dashed marker at 80%
/// of its width labelled "worst 20%", a caption that says where that line falls and what the map shows, and a row per phase.
/// <see cref="AutomationName"/> carries the same facts as text, so colour is never the only carrier of meaning.
/// </summary>
/// <param name="HasBar">False when every count is blank or 0: there is nothing to stack, and only the rows and caption show.</param>
/// <param name="OfAnalysed">"of 47,535,794 people analysed", or "share unknown".</param>
public sealed record PhaseBar(IReadOnlyList<PhaseBarSegment> Segments, IReadOnlyList<PhaseBarRow> Rows, bool HasBar, string OfAnalysed,
    string Caption, string AutomationName)
{
    /// <summary>The marker's place along the bar: the right 20% are the worst-off people.</summary>
    public const double MarkerAt = 1 - IpcAreaRule.Threshold;

    public const string MarkerLabel = "worst 20%";

    /// <summary>The bar for <paramref name="f"/>'s current-period counts (stale analyses too; never for a projection-only record).</summary>
    public static PhaseBar For(string country, FoodCrisis f)
    {
        ArgumentNullException.ThrowIfNull(f);
        var c = IpcAreaRule.Classify(f);
        var people = Enumerable.Range(1, 5).Select(p => IpcAreaRule.People(f, p)).ToArray();
        long sum = people.Sum();
        var segments = Enumerable.Range(1, 5)
            .Select(p => new PhaseBarSegment(p, people[p - 1], sum > 0 ? (double)people[p - 1] / sum : 0, LayerStyle.IpcPhaseColor(p)))
            .ToList();
        var rows = Enumerable.Range(1, 5).Select(p => new PhaseBarRow(p, LayerStyle.IpcPhaseLabel(p),
            f.IsReported(p) ? CrisisText.Number(people[p - 1]) : CrisisText.NotReported,
            !f.IsReported(p) ? CrisisText.NotReported : c.ShareKnown ? CrisisText.FoodPercent(c.Share(p)) : "",
            LayerStyle.IpcPhaseColor(p))).ToList();
        var of = CrisisText.OfAnalysed(f, c);
        var caption = CrisisText.BarCaption(country, f, c);
        var parts = rows.Select(r => !f.IsReported(r.Phase) ? $"{r.Name} {CrisisText.NotReported}"
            : c.ShareKnown ? $"{r.Name} {r.Percent}" : $"{r.Name} {r.People} people");
        var name = $"IPC phase bar for {country}, {of}: {string.Join(", ", parts)}. "
                   + (sum > 0 ? "A dashed line marks the worst 20%. " : "") + caption;
        return new PhaseBar(segments, rows, sum > 0, of, caption, name);
    }
}

/// <summary>The widths of the bar's five pieces.</summary>
public static class PhaseBarLayout
{
    /// <summary>A phase with people in it is never thinner than this (a 0.3% Phase 5 stays visible).</summary>
    public const double MinPx = 3;

    /// <summary>
    /// Widths that fill <paramref name="width"/> in proportion to <paramref name="people"/>, with every phase that has more
    /// than 0 people at least <paramref name="minPx"/> wide (taken from the wider ones), and 0 for a phase with nobody.
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
}
