using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EarthRelief3D.App.Presentation;
using EarthRelief3D.Core.Feeds;
using EarthRelief3D.Core.Model;
using EarthRelief3D.Core.Store;

namespace EarthRelief3D.App.ViewModels;

/// <summary>One part of the country panel: a heading, label/value rows, the source line and a link to the source.</summary>
/// <param name="SourceLine">"Source: &lt;publisher&gt;, &lt;dataset&gt; · as of &lt;date&gt;" (food: the IPC analysis period instead).</param>
public sealed record PanelSection(string Heading, IReadOnlyList<(string Label, string Value)> Rows, string SourceLine, Uri? Link);

/// <summary>
/// The country panel (C1: per country, a section per crisis), the "What is happening" part of spec §9.1. Sections come in
/// this order: armed conflict, food crisis, disease outbreaks, people forced from home; a kind with nothing for the country
/// has no section. A country with no crisis says <see cref="NoCrisisText"/> and lists the sources checked, with their
/// dates. While open it follows the store (marshalled to the dispatcher).
/// </summary>
public sealed partial class CountryPanelViewModel : ObservableObject
{
    public const string ConflictHeading = "Armed conflict";
    public const string FoodHeading = "Food crisis";
    /// <summary>An IPC analysis whose period ended more than 12 months ago: listed, not a crisis (P2-4).</summary>
    public const string PastFoodHeading = "Food security: last IPC analysis";
    public const string OutbreakHeading = "Disease outbreaks";
    public const string DisplacementHeading = "People forced from home";
    public const string NoCrisisText = "No crisis recorded by these sources";

    private static readonly IReadOnlyList<PanelSection> NoSections = [];
    private readonly CrisisStore _store;
    private readonly Func<string, string?> _nameOf;
    private readonly Dispatcher? _dispatcher;
    private int _refreshPending;

    [ObservableProperty] private bool _isOpen;
    [ObservableProperty] private string _iso3 = "";
    [ObservableProperty] private string _countryName = "";
    [ObservableProperty] private IReadOnlyList<PanelSection> _sections = NoSections;
    [ObservableProperty] private bool _hasNoCrisis;
    [ObservableProperty] private IReadOnlyList<string> _sourcesChecked = [];
    /// <summary>The section the panel was opened at (a marker click, C1), or -1.</summary>
    [ObservableProperty] private int _focusedSectionIndex = -1;

    private CrisisLayer? _focus;

    /// <param name="countryName">The country's name for an ISO3 code (null: the code is shown).</param>
    /// <param name="dispatcher">The UI thread's; store changes from other threads are queued onto it. Null: handled at once.</param>
    public CountryPanelViewModel(CrisisStore store, Func<string, string?> countryName, Dispatcher? dispatcher = null)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _nameOf = countryName ?? throw new ArgumentNullException(nameof(countryName));
        _dispatcher = dispatcher;
        _store.Changed += OnStoreChanged;
    }

    /// <summary>Opens the panel for <paramref name="iso3"/>, at the <paramref name="focus"/> layer's section if given.</summary>
    public void Show(string iso3, CrisisLayer? focus = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(iso3);
        _focus = focus;
        Build(iso3.Trim().ToUpperInvariant());
        IsOpen = true;
    }

    [RelayCommand]
    private void Close()
    {
        IsOpen = false;
        Iso3 = "";
        CountryName = "";
        Sections = NoSections;
        SourcesChecked = [];
        HasNoCrisis = false;
        FocusedSectionIndex = -1;
        _focus = null;
    }

    private void Build(string iso3)
    {
        var s = _store.Snapshot;
        var crises = s.ByCountry(iso3);
        var sections = new List<PanelSection>();
        var focusIndex = -1;
        void Add(PanelSection? section, CrisisLayer layer)
        {
            if (section is null) return;
            if (_focus == layer) focusIndex = sections.Count;
            sections.Add(section);
        }

        var conflicts = crises.OfType<ConflictCrisis>().OrderByDescending(c => c.Severity.Value).ToList();
        Add(ConflictSection(conflicts), CrisisLayer.Conflict);
        var food = crises.OfType<FoodCrisis>().OrderByDescending(f => f.PeriodTo).FirstOrDefault();
        Add(food is null ? null : FoodSection(food), CrisisLayer.Food);
        var outbreaks = crises.OfType<OutbreakCrisis>().OrderByDescending(o => o.PublishedUtc).ToList();
        Add(OutbreakSection(outbreaks), CrisisLayer.Outbreaks);
        var displaced = s.Displacement.Where(d => string.Equals(d.CountryIso3, iso3, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(d => d.Year).FirstOrDefault();
        Add(displaced is null ? null : DisplacementSection(displaced), CrisisLayer.Displacement);

        var noCrisis = conflicts.Count == 0 && outbreaks.Count == 0 && food is not { IsStale: false };
        Iso3 = iso3;
        CountryName = _nameOf(iso3) ?? iso3;
        Sections = sections;
        HasNoCrisis = noCrisis;
        SourcesChecked = noCrisis ? [.. SourceDates.All(s).Select(SourceCheckedLine)] : [];
        FocusedSectionIndex = focusIndex;
    }

    /// <summary>"UCDP (armed conflict) · as of 6 Nov 2026".</summary>
    public static string SourceCheckedLine(SourceSummary s)
    {
        var date = s.DateText ?? LegendViewModel.NoData;
        if (s.Layer == CrisisLayer.Food && s.DateText is not null) date += " (newest IPC analysis)";
        if (s.Layer == CrisisLayer.Displacement && s.DateText is not null) date = s.Source.Replace("UNHCR, ", "", StringComparison.Ordinal) + ", " + date;
        return $"{s.ShortName} ({s.What}) · {date}";
    }

    // ---------------------------------------------------------------- sections

    private static PanelSection? ConflictSection(IReadOnlyList<ConflictCrisis> conflicts)
    {
        if (conflicts.Count == 0) return null;
        var rows = new List<(string, string)>();
        foreach (var c in conflicts)
        {
            rows.Add(("Conflict", ConflictName(c)));
            if (!string.IsNullOrWhiteSpace(c.SideA) || !string.IsNullOrWhiteSpace(c.SideB)) rows.Add(("Sides", $"{c.SideA} vs {c.SideB}"));
            rows.Add(("Deaths", c.Severity.Text));   // UCDP's own wording, shown as is
            if (c.Trend is { } t) rows.Add(("Trend", $"{CrisisText.Number(t.Recent)} deaths in the last 3 months, {CrisisText.Number(t.Previous)} in the 3 before"));
        }
        var newest = conflicts.SelectMany(c => c.Citations).MaxBy(x => x.AsOfUtc)!;
        return new PanelSection(ConflictHeading, rows, CrisisText.SourceLine(newest, CrisisText.AsOf(newest.AsOfUtc)), newest.Url);
    }

    // The title is "<country>: <UCDP conflict name>"; the panel already names the country.
    private static string ConflictName(ConflictCrisis c)
    {
        var colon = c.Title.IndexOf(": ", StringComparison.Ordinal);
        return colon >= 0 ? c.Title[(colon + 2)..] : c.Title;
    }

    private static PanelSection FoodSection(FoodCrisis f)
    {
        var cite = f.Citations.Count > 0 ? f.Citations[0]
            : new SourceCitation(IpcNationalReader.Publisher, IpcNationalReader.Dataset, null, IpcNationalReader.PageUrl, "CC0", IpcNationalReader.Attribution, default, default);
        var period = CrisisText.Period(f.PeriodFrom, f.PeriodTo);
        List<(string, string)> rows = f.IsStale
            ? [("Last IPC analysis", period), ("Note", "Ended more than 12 months ago, so it is not shown as a current crisis")]
            :
            [
                ("Highest phase", f.Severity.Text),
                ("People in Phase 3 or higher", $"{CrisisText.Number(f.Phase3PlusPeople)} ({CrisisText.Percent(f.Phase3PlusShare)} of the people analysed)"),
                ("Analysis period", period),
            ];
        return new PanelSection(f.IsStale ? PastFoodHeading : FoodHeading, rows, CrisisText.SourceLine(cite, CrisisText.FoodDate(f)), cite.Url);
    }

    private static PanelSection? OutbreakSection(IReadOnlyList<OutbreakCrisis> outbreaks)
    {
        if (outbreaks.Count == 0) return null;
        var rows = new List<(string, string)>();
        foreach (var o in outbreaks)
        {
            rows.Add(("Disease", o.Disease));
            rows.Add(("Reported", CrisisText.Date(o.PublishedUtc)));
        }
        rows.Add(("Window", $"WHO Outbreak News reports in the last {WhoOutbreakFeed.Window}"));
        var newest = outbreaks[0];
        var cite = newest.Citations.MaxBy(x => x.AsOfUtc)!;
        return new PanelSection(OutbreakHeading, rows, CrisisText.SourceLine(cite, CrisisText.AsOf(cite.AsOfUtc)), newest.ReportUrl);
    }

    private static PanelSection DisplacementSection(DisplacementFigure d) => new(DisplacementHeading,
        [
            ("Refugees", CrisisText.Number(d.Refugees)),
            ("Asylum-seekers", CrisisText.Number(d.AsylumSeekers)),
            ("Internally displaced", CrisisText.Number(d.Idps)),
            ("Total", CrisisText.Number(CrisisText.Total(d))),
            ("Counted", "by country of origin"),
        ],
        CrisisText.SourceLine(d.Citation, CrisisText.AsOf(d.Citation.AsOfUtc)), d.Citation.Url);

    // ---------------------------------------------------------------- store

    private void OnStoreChanged(object? sender, EventArgs e)
    {
        if (_dispatcher is null || _dispatcher.CheckAccess()) { RefreshIfOpen(); return; }
        if (Interlocked.Exchange(ref _refreshPending, 1) == 1) return;
        _dispatcher.BeginInvoke(() =>
        {
            Volatile.Write(ref _refreshPending, 0);
            RefreshIfOpen();
        });
    }

    private void RefreshIfOpen()
    {
        if (IsOpen && Iso3.Length > 0) Build(Iso3);
    }
}
