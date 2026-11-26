using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;
using EarthRelief3D.App.ViewModels;
using EarthRelief3D.Core.Help;
using GlobeCore.Wpf.Services;

namespace EarthRelief3D.App.Views;

/// <summary>The country panel (right). DataContext: CountryPanelViewModel. Scrolls to the section a marker click opened it at, and keeps its place when the open country is rebuilt.</summary>
public partial class CountryPanel : UserControl
{
    private CountryPanelViewModel? _vm;

    public CountryPanel()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    /// <summary>Opens the links; the real shell by default.</summary>
    public ILinkOpener Links { get; set; } = new ShellLinkOpener();

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (_vm is not null)
        {
            _vm.PropertyChanged -= OnVmChanged;
            _vm.ScrollRequested -= OnScrollRequested;
        }
        _vm = DataContext as CountryPanelViewModel;
        if (_vm is not null)
        {
            _vm.PropertyChanged += OnVmChanged;
            _vm.ScrollRequested += OnScrollRequested;
        }
    }

    // A rebuild of the open country (a unit or state chosen, a feed refreshed) keeps where the user scrolled to (final
    // review F1): the offset is put back after layout, in case the new sections made the list shorter for a moment.
    private void OnVmChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(CountryPanelViewModel.Sections)) return;
        double keep = Scroller.VerticalOffset;
        Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () => Scroller.ScrollToVerticalOffset(keep));
    }

    // Show (a new country or a new focus): to the focused section, or to the top. Queued after the offset-keeping callback of
    // the same rebuild, so this one wins.
    private void OnScrollRequested(object? sender, EventArgs e)
    {
        // After layout, so the section's container exists.
        Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () =>
        {
            if (_vm is not { FocusedSectionIndex: >= 0 } vm) { Scroller.ScrollToTop(); return; }
            if (SectionList.ItemContainerGenerator.ContainerFromIndex(vm.FocusedSectionIndex) is not FrameworkElement item) return;
            Scroller.UpdateLayout();   // the entries' own containers exist only after a layout pass (the first picture run found none)
            // The charities focus (P5-15): the first charity entry's top, not the section's.
            ScrollToTopOf(vm.FocusedEntryIndex >= 0 && EntryElement(item, vm.FocusedEntryIndex) is { } entry ? entry : item);
        });
    }

    /// <summary>The element of the section's <paramref name="index"/>-th help entry (the groups keep the entries' order), or null.</summary>
    private static FrameworkElement? EntryElement(DependencyObject section, int index)
    {
        var found = new List<FrameworkElement>();
        void Walk(DependencyObject d)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(d); i++)
            {
                var c = VisualTreeHelper.GetChild(d, i);
                if (c is StackPanel { DataContext: HelpEntry } sp) found.Add(sp);
                else Walk(c);
            }
        }
        Walk(section);
        return index < found.Count ? found[index] : null;
    }

    /// <summary>
    /// Scrolls so the section's top is at the top of the panel's view (or as far as the panel scrolls), for a scene's focus
    /// and a marker click alike: a section taller than the view opens at its heading, never mid-way.
    /// </summary>
    private void ScrollToTopOf(FrameworkElement item)
    {
        Scroller.UpdateLayout();
        var y = item.TranslatePoint(new Point(0, 0), Scroller).Y;
        Scroller.ScrollToVerticalOffset(Scroller.VerticalOffset + y);
    }

    private void OnSourceLinkClick(object sender, RoutedEventArgs e)
    {
        if (sender is Hyperlink { Tag: Uri link }) OpenInBrowser(link, Links);
    }

    /// <summary>Opens an http(s) page in the default browser; anything else, or a failure, is ignored (false).</summary>
    public static bool OpenInBrowser(Uri link, ILinkOpener opener)
    {
        if (link.Scheme != Uri.UriSchemeHttp && link.Scheme != Uri.UriSchemeHttps) return false;
        try { opener.Open(link.AbsoluteUri); return true; }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException) { return false; }
    }
}

public sealed record DetailRow(string Label, string Value);

/// <summary>Turns a section's (Label, Value) tuples, whose members are fields, into rows WPF can bind to.</summary>
public sealed class RowsConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is IReadOnlyList<(string Label, string Value)> rows ? rows.Select(r => new DetailRow(r.Label, r.Value)).ToList() : new List<DetailRow>();

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
}

/// <summary>One group of "Your one small step" ("Give", "Contact") with its entries, for binding.</summary>
public sealed record HelpGroup(string Name, IReadOnlyList<HelpEntry> Entries);

/// <summary>Groups a section's help entries by their group, in the order the groups first appear (Give, then Contact).</summary>
public sealed class HelpGroupsConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is IReadOnlyList<HelpEntry> entries
            ? entries.GroupBy(e => e.Group).Select(g => new HelpGroup(g.Key, [.. g])).ToList()
            : new List<HelpGroup>();

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
}

public sealed class NullToCollapsedConverter : IValueConverter
{
    public static readonly NullToCollapsedConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is null ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
}
