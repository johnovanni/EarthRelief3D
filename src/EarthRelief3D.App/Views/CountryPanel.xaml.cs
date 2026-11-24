using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Threading;
using EarthRelief3D.App.ViewModels;
using GlobeCore.Wpf.Services;

namespace EarthRelief3D.App.Views;

/// <summary>The country panel (right). DataContext: CountryPanelViewModel. Scrolls to the section a marker click opened it at.</summary>
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
        if (_vm is not null) _vm.PropertyChanged -= OnVmChanged;
        _vm = DataContext as CountryPanelViewModel;
        if (_vm is not null) _vm.PropertyChanged += OnVmChanged;
    }

    private void OnVmChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(CountryPanelViewModel.Sections) && e.PropertyName != nameof(CountryPanelViewModel.FocusedSectionIndex)) return;
        // After layout, so the section's container exists.
        Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () =>
        {
            if (_vm is not { FocusedSectionIndex: >= 0 } vm) { Scroller.ScrollToTop(); return; }
            if (SectionList.ItemContainerGenerator.ContainerFromIndex(vm.FocusedSectionIndex) is FrameworkElement item)
                ScrollToTopOf(item);
        });
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

public sealed class NullToCollapsedConverter : IValueConverter
{
    public static readonly NullToCollapsedConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is null ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
}
