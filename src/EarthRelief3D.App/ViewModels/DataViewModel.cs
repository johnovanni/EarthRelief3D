using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EarthRelief3D.App.Presentation;
using EarthRelief3D.Core.Sources;
using GlobeCore.Wpf.Services;

namespace EarthRelief3D.App.ViewModels;

/// <summary>One line of the Data card: a source, how the app uses it, its date, licence and credit.</summary>
/// <param name="Use">"fetched by the app", "included with the app", "written for the app, each entry cited" or "links only".</param>
/// <param name="AsOf">The source's date and, for a fetched source, when it was last checked (<see cref="SourceAsOf"/>).</param>
/// <param name="LicenceUrl">Where the licence is read; null when the source gives none.</param>
/// <param name="Credit">The source's credit, with its date filled in.</param>
/// <param name="Page">The source's page.</param>
public sealed record DataRow(string Name, string What, string Use, string AsOf, string Licence, Uri? LicenceUrl, string Credit, Uri Page);

/// <summary>
/// The Data card (Plan 5 Task 4): every source the app uses, from <c>data/sources.json</c>, each with what it gives, how the app
/// uses it, its date, its licence and its credit. The dates are read again by <see cref="Refresh"/> (a feed finished, or the card
/// was opened). Links open in the user's browser; one that cannot be opened shows a message instead.
/// </summary>
public sealed partial class DataViewModel : ObservableObject
{
    public const string HeadingText = "Data sources";
    public const string IntroText = "Every fact in EarthRelief3D comes from one of these sources. Each line on the map and in the panel names its source and date.";
    /// <summary>Shown when the source list could not be read (the log says why).</summary>
    public const string NotReadMessage = "The source list could not be read; see the log";

    private readonly SourceRegistry _registry;
    private readonly Func<SourceAsOfInputs> _inputs;
    private readonly Func<DateTime> _nowUtc;
    private readonly ILinkOpener _links;

    [ObservableProperty] private IReadOnlyList<DataRow> _rows = [];
    /// <summary>A line under the heading: empty normally; why the list is empty, or that a link could not be opened.</summary>
    [ObservableProperty] private string _message = "";

    public DataViewModel(SourceRegistry registry, Func<SourceAsOfInputs> inputs, Func<DateTime> nowUtc, ILinkOpener links)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _inputs = inputs ?? throw new ArgumentNullException(nameof(inputs));
        _nowUtc = nowUtc ?? throw new ArgumentNullException(nameof(nowUtc));
        _links = links ?? throw new ArgumentNullException(nameof(links));
#pragma warning disable MVVMTK0034 // the start value
        if (_registry.Sources.Count == 0) _message = NotReadMessage;
#pragma warning restore MVVMTK0034
        Refresh();
    }

    public string Heading => HeadingText;
    public string Intro => IntroText;

    /// <summary>Reads every row's date again from the stores.</summary>
    public void Refresh()
    {
        if (_registry.Sources.Count == 0) { Rows = []; return; }
        var inputs = _inputs();
        var now = _nowUtc();
        Rows = [.. _registry.Sources.Select(s => new DataRow(s.Name, s.What, UseLabel(s.Use), SourceAsOf.For(s, inputs, now), s.Licence, s.LicenceUrl,
            s.CreditFor(SourceAsOf.CreditDate(s, inputs, now)), s.Page))];
    }

    /// <summary>How the app uses a source, in plain words.</summary>
    public static string UseLabel(SourceUse use) => use switch
    {
        SourceUse.Live => "fetched by the app",
        SourceUse.Bundled => "included with the app",
        SourceUse.Curated => "written for the app, each entry cited",
        _ => "links only",
    };

    /// <summary>Raised when the card's close button is pressed.</summary>
    public event EventHandler? CloseRequested;

    [RelayCommand]
    private void Close() => CloseRequested?.Invoke(this, EventArgs.Empty);

    /// <summary>Opens an https page in the user's browser; anything else, or a failure, shows a message.</summary>
    [RelayCommand]
    private void OpenLink(Uri? link)
    {
        if (link is null) return;
        if (!link.IsAbsoluteUri || link.Scheme != Uri.UriSchemeHttps)
        {
            Message = "Only https links are opened: " + link;
            return;
        }
        try
        {
            _links.Open(link.AbsoluteUri);
            Message = "";
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
        {
            Message = "The link could not be opened in your browser: " + link.AbsoluteUri;
        }
    }
}
