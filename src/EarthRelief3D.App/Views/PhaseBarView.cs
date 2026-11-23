using System.Globalization;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Media;
using EarthRelief3D.App.Presentation;

namespace EarthRelief3D.App.Views;

/// <summary>
/// The IPC phase bar (R17): Phases 1 to 5, left to right, stacked to 100% in IPC colours (a phase with people at least
/// <see cref="PhaseBarLayout.MinPx"/> wide), and a dashed marker at 80% of the width labelled "worst 20%". Drawn, not
/// templated; the same facts are in AutomationProperties.Name (set by the panel) and in the rows under it.
/// </summary>
public sealed class PhaseBarView : FrameworkElement
{
    public const double LabelHeight = 14, BarHeight = 16;

    public static readonly DependencyProperty BarProperty = DependencyProperty.Register(nameof(Bar), typeof(PhaseBar), typeof(PhaseBarView),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    private static readonly Brush LabelBrush = Freeze(new SolidColorBrush(Color.FromRgb(0xC8, 0xCD, 0xD8)));
    private static readonly Pen Frame = Freeze(new Pen(Freeze(new SolidColorBrush(Color.FromRgb(0x3A, 0x42, 0x56))), 1));
    private static readonly Pen MarkerUnder = Freeze(new Pen(Freeze(new SolidColorBrush(Color.FromRgb(0x10, 0x16, 0x24))), 3.5));
    private static readonly Pen Marker = Freeze(new Pen(Brushes.White, 1.5) { DashStyle = new DashStyle([2.0, 1.5], 0) });

    public PhaseBarView() => SnapsToDevicePixels = true;

    public PhaseBar? Bar
    {
        get => (PhaseBar?)GetValue(BarProperty);
        set => SetValue(BarProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize) =>
        new(double.IsInfinity(availableSize.Width) ? 300 : availableSize.Width, LabelHeight + BarHeight + 2);

    protected override void OnRender(DrawingContext dc)
    {
        if (Bar is not { HasBar: true } bar) return;
        double w = Math.Floor(ActualWidth), top = LabelHeight;
        if (w <= 0) return;
        var widths = PhaseBarLayout.Widths([.. bar.Segments.Select(s => s.People)], w);
        double x = 0;
        for (int i = 0; i < widths.Length; i++)
        {
            if (widths[i] <= 0) continue;
            dc.DrawRectangle(LayerStyle.Frozen(bar.Segments[i].Color), null, new Rect(x, top, widths[i], BarHeight));
            x += widths[i];
        }
        dc.DrawRectangle(null, Frame, new Rect(0.5, top + 0.5, w - 1, BarHeight - 1));

        // The marker, dark under white so it reads on every phase colour, and its label just right of it.
        double mx = Math.Round(w * PhaseBar.MarkerAt) + 0.5;
        dc.DrawLine(MarkerUnder, new Point(mx, top - 3), new Point(mx, top + BarHeight + 2));
        dc.DrawLine(Marker, new Point(mx, top - 3), new Point(mx, top + BarHeight + 2));
        var label = new FormattedText(PhaseBar.MarkerLabel, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            new Typeface("Segoe UI"), 11, LabelBrush, VisualTreeHelper.GetDpi(this).PixelsPerDip);
        double lx = Math.Min(mx + 3, w - label.Width);
        dc.DrawText(label, new Point(lx, -1));
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new PhaseBarPeer(this);

    private static T Freeze<T>(T f) where T : Freezable
    {
        f.Freeze();
        return f;
    }

    /// <summary>Exposes the bar as an image whose name (AutomationProperties.Name) states the shares in words.</summary>
    private sealed class PhaseBarPeer(PhaseBarView owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Image;
        protected override string GetClassNameCore() => nameof(PhaseBarView);
    }
}
