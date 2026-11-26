using System.ComponentModel;
using System.Globalization;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EarthRelief3D.Core.Logic;
using EarthRelief3D.Core.Sources;
using GlobeCore.Wpf.Services;

namespace EarthRelief3D.App.ViewModels;

/// <summary>One line of the About card's lists: a name that links to <paramref name="Url"/>, and what it is credited for.</summary>
public sealed record Credit(string Name, string Text, Uri Url);

/// <summary>
/// The About card (Plan 5 Task 5): the version, the app's licence, the principles, every data credit as the source list gives it,
/// how the severity bands size markers, the boundary note, the libraries' licences and where the app keeps its data. Links open
/// in the user's browser (https only); one that cannot be opened shows a <see cref="Message"/> instead.
/// </summary>
public sealed partial class AboutViewModel : ObservableObject
{
    public const string NoticesFileName = "THIRD-PARTY-NOTICES.txt";

    public const string HeadingText = "About EarthRelief3D";
    public const string AppLicenceText = "EarthRelief3D is free software under the MIT licence. Copyright (c) 2026 John M. Apolloni.";
    public const string PrinciplesText = "Every fact on screen shows where it comes from and how recent it is. EarthRelief3D never handles money, never ranks charities by its own opinion, shows no graphic images, makes no sound and collects no personal data.";
    public const string BandsNote = "Bands only set a marker's size or shade; the source's own figure is always shown with it.";

    public static readonly Uri Releases = new("https://github.com/johnovanni/EarthRelief3D/releases");
    public static readonly Uri BandsPage = new("https://github.com/johnovanni/EarthRelief3D/blob/main/docs/severity-bands.md");

    private readonly ILinkOpener _opener;
    private readonly CardHost _cards;
    private readonly string _noticesPath;

    /// <param name="noticesPath">The file "Licence texts" opens; the installed <c>THIRD-PARTY-NOTICES.txt</c> next to the exe when not given.</param>
    public AboutViewModel(SourceRegistry registry, ILinkOpener opener, string dataFolder, CardHost cards, string? noticesPath = null)
    {
        ArgumentNullException.ThrowIfNull(registry);
        _opener = opener ?? throw new ArgumentNullException(nameof(opener));
        _cards = cards ?? throw new ArgumentNullException(nameof(cards));
        DataFolder = dataFolder ?? "";
        _noticesPath = noticesPath ?? Path.Combine(AppContext.BaseDirectory, NoticesFileName);
        Credits = [.. registry.Sources.Select(s => new Credit(s.Name, s.CreditFor(null), s.Page))];
    }

    public string Heading => HeadingText;
    public string Version { get; } = "Version " + AppInfo.Version;
    public string AppLicence => AppLicenceText;
    public string Principles => PrinciplesText;
    public Uri ReleasesUrl => Releases;
    public Uri BandsUrl => BandsPage;
    public string BoundaryNote => LegendViewModel.BoundaryNote;
    public string DataFolder { get; }

    /// <summary>One line per source in the source list, with its credit as the list gives it (no date: the Data card has the dates).</summary>
    public IReadOnlyList<Credit> Credits { get; }

    /// <summary>The software the app is built on (the packages it ships, and the platform), each under the MIT licence; the full texts are in the notices file.</summary>
    public IReadOnlyList<Credit> Libraries { get; } =
    [
        new("HelixToolkit.Wpf", "MIT licence", new Uri("https://github.com/helix-toolkit/helix-toolkit")),
        new("CommunityToolkit.Mvvm", "MIT licence", new Uri("https://github.com/CommunityToolkit/dotnet")),
        new("ExcelDataReader", "MIT licence", new Uri("https://github.com/ExcelDataReader/ExcelDataReader")),
        new("WPF", "MIT licence", new Uri("https://github.com/dotnet/wpf")),
        new(".NET", "MIT licence", new Uri("https://github.com/dotnet/runtime")),
    ];

    /// <summary>The paragraph about what the app keeps and where it sends requests.</summary>
    public string DataText =>
        $"Your settings and the downloaded data are kept in {DataFolder}. The app sends requests only to the public sources listed under Data, and once a day to GitHub to look for a new version (you can turn that off in Settings).";

    /// <summary>
    /// How markers are sized: one line per kind, then the note that bands only set size. The conflict boundaries are read from
    /// <see cref="SeverityBands"/>, not typed here, so the card cannot drift from the code (and docs/severity-bands.md says the same).
    /// </summary>
    public IReadOnlyList<string> BandLines { get; } = BuildBandLines();

    private static List<string> BuildBandLines()
    {
        static string N(int n) => n.ToString("N0", CultureInfo.InvariantCulture);
        // The smallest deaths count each band starts at (the bands grow with the count).
        static int From(int band)
        {
            int lo = 0, hi = 10_000_000;
            while (lo < hi)
            {
                int mid = lo + (hi - lo) / 2;
                if (SeverityBands.ForConflict(mid) >= band) hi = mid; else lo = mid + 1;
            }
            return lo;
        }
        int b2 = From(2), b3 = From(3), b4 = From(4);
        return
        [
            $"Armed conflict (UCDP deaths in one country in the data window): band 1 under {N(b2)} (for a conflict that qualifies), 2 from {N(b2)}, 3 from {N(b3)}, 4 from {N(b4)}",
            "Food (IPC): the band is the IPC phase, 3 to 5",
            $"Disease outbreaks (WHO): every report is band {SeverityBands.ForOutbreak()}",
            BandsNote,
        ];
    }

    private Services.IUpdateStatus? _updates;

    /// <summary>The update check's status line (P5-18), such as "Checked for a new version on 25 Nov 2026: you have the newest (1.0.0)"; empty when the app has no update check (a picture or offline run).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasUpdateStatus))]
    private string _updateStatus = "";

    /// <summary>True when the card shows the status line and "Check now".</summary>
    public bool HasUpdateStatus => _updates is not null;

    /// <summary>Shows <paramref name="updates"/>' status line, following it, with "Check now".</summary>
    public void AttachUpdates(Services.IUpdateStatus updates)
    {
        ArgumentNullException.ThrowIfNull(updates);
        if (_updates is not null) _updates.StatusChanged -= OnUpdateStatusChanged;
        _updates = updates;
        updates.StatusChanged += OnUpdateStatusChanged;
        UpdateStatus = updates.StatusText;
        OnPropertyChanged(nameof(HasUpdateStatus));
    }

    private void OnUpdateStatusChanged(object? sender, EventArgs e) => UpdateStatus = _updates?.StatusText ?? "";

    /// <summary>"Check now": asks GitHub at once (even when the daily check is off: the user asked).</summary>
    [RelayCommand]
    private Task CheckForUpdates() => _updates?.CheckNowAsync() ?? Task.CompletedTask;

    /// <summary>Why the last link, folder or file could not be opened; empty otherwise.</summary>
    [ObservableProperty] private string _message = "";

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
        TryOpen(link.AbsoluteUri, "The link could not be opened in your browser: " + link.AbsoluteUri);
    }

    /// <summary>Opens the installed notices file (the licence texts of the software the app is built on).</summary>
    [RelayCommand]
    private void OpenNotices()
    {
        string failed = "The licence texts could not be opened: " + _noticesPath;
        if (!File.Exists(_noticesPath)) { Message = failed; return; }
        TryOpen(_noticesPath, failed);
    }

    [RelayCommand]
    private void OpenDataFolder()
    {
        string failed = "The data folder could not be opened: " + DataFolder;
        if (!Directory.Exists(DataFolder)) { Message = failed; return; }
        TryOpen(DataFolder, failed);
    }

    /// <summary>"See dates and licences": the Data card.</summary>
    [RelayCommand]
    private void ShowData() => _cards.Show(AppCard.Data);

    /// <summary>"Show the welcome again". Does nothing until the Welcome card is registered.</summary>
    [RelayCommand]
    private void ShowWelcome()
    {
        if (_cards.IsAvailable(AppCard.Welcome)) _cards.Show(AppCard.Welcome);
    }

    // The target stays in the message so the user can copy it into a browser or the file manager.
    private void TryOpen(string target, string failed)
    {
        try
        {
            _opener.Open(target);
            Message = "";
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException or IOException)
        {
            Message = failed;
        }
    }
}
