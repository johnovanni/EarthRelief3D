using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace EarthRelief3D.App.Views;

/// <summary>
/// Text size (C8; P5-8) for one card: scales the card's inside with a <see cref="ScaleTransform"/> as a layout transform, so the
/// card grows to fit. The transform goes on the card's content, not on the card, so the card's own Margin, Width, MaxWidth and
/// MaxHeight (set by <c>MainWindow</c> from <see cref="Presentation.TextScaleLayout"/>) stay on-screen sizes. A new card
/// (Welcome, About, Data) is made to follow the Settings choice by adding it to the list in <c>MainWindow</c>.
/// </summary>
public static class CardScale
{
    /// <summary>The element a transform goes on: a user control's content, or a border's child. Null for any other element.</summary>
    public static FrameworkElement? Inside(FrameworkElement card) => card switch
    {
        ContentControl c => c.Content as FrameworkElement,
        Decorator d => d.Child as FrameworkElement,
        _ => null,
    };

    /// <summary>Scales the card's inside by <paramref name="scale"/> (1.0 takes the transform off). Returns false when the card has no inside to scale.</summary>
    public static bool Apply(FrameworkElement card, double scale)
    {
        ArgumentNullException.ThrowIfNull(card);
        if (Inside(card) is not { } inside) return false;
        if (scale == 1.0) inside.ClearValue(FrameworkElement.LayoutTransformProperty);
        else inside.LayoutTransform = new ScaleTransform(scale, scale);
        return true;
    }
}
