using System.ComponentModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EarthRelief3D.Core.Updates;
using GlobeCore.Wpf.Services;

namespace EarthRelief3D.App.ViewModels;

/// <summary>What the update banner shows (P5-19 to P5-22).</summary>
public enum UpdateBannerState { Hidden, Offer, Downloading, Ready, Failed, Stale }

/// <summary>
/// The update banner at the top centre of the window (P5-19): a banner, not a pop-up and not a card; no sound, nothing modal. It
/// offers a newer release ("Download and install", "What's new", "Later"), shows the download ("12 of 53 MB", "Cancel"), offers to
/// install it ("Install now", "Later"), says why an update could not be used ("Open the download page", "Later"), or says the app
/// has not been able to check for 30 days ("Check now", "Later"). The <see cref="Services.UpdateService"/> sets its state and
/// handles its commands; "What's new" and "Open the download page" open the release's page (https, on GitHub) in the browser.
/// </summary>
public sealed partial class UpdateBannerViewModel : ObservableObject
{
    public const string StaleText = "EarthRelief3D has not been able to check for a new version for 30 days. Its included data may be out of date: Data shows each source's date.";
    /// <summary>On the stale banner while its "Check now" runs.</summary>
    public const string CheckingText = "Checking for a new version…";
    /// <summary>The reason a "failed" picture shows (scene field "update": "failed").</summary>
    public const string PictureFailureText = "The file's SHA-256 is not the release's.";

    private readonly ILinkOpener _opener;
    private readonly Version _running;

    public UpdateBannerViewModel(ILinkOpener opener, Version running)
    {
        _opener = opener ?? throw new ArgumentNullException(nameof(opener));
        _running = UpdateCheck.Normalise(running ?? throw new ArgumentNullException(nameof(running)));
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsVisible), nameof(ShowDownload), nameof(ShowWhatsNew), nameof(ShowLater), nameof(ShowCancel),
        nameof(ShowInstallNow), nameof(ShowOpenDownloadPage), nameof(ShowCheckNow))]
    private UpdateBannerState _state;

    /// <summary>"Install now" was chosen and the installer has started: the Ready banner stays, without its buttons, until the installer closes the app (R11).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowInstallNow), nameof(ShowLater))]
    private bool _isInstalling;

    /// <summary>The stale banner's "Check now" is running (M6): the banner says "Checking…" and hides "Check now" until the result.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowCheckNow))]
    private bool _isChecking;

    /// <summary>The banner's sentence(s) for the state.</summary>
    [ObservableProperty] private string _text = "";

    /// <summary>While downloading: "12 of 53 MB".</summary>
    [ObservableProperty] private string _progressText = "";

    /// <summary>Why the release page could not be opened; empty otherwise.</summary>
    [ObservableProperty] private string _message = "";

    /// <summary>The release the banner is about (null for the stale banner).</summary>
    public ReleaseInfo? Release { get; private set; }

    public bool IsVisible => State != UpdateBannerState.Hidden;
    public bool ShowDownload => State == UpdateBannerState.Offer;
    public bool ShowWhatsNew => State == UpdateBannerState.Offer;
    public bool ShowLater => !IsInstalling && State is UpdateBannerState.Offer or UpdateBannerState.Ready or UpdateBannerState.Failed or UpdateBannerState.Stale;
    public bool ShowCancel => State == UpdateBannerState.Downloading;
    public bool ShowInstallNow => State == UpdateBannerState.Ready && !IsInstalling;
    public bool ShowOpenDownloadPage => State == UpdateBannerState.Failed;
    public bool ShowCheckNow => State == UpdateBannerState.Stale && !IsChecking;

    // Set by the update service (none in a picture run: the buttons then do nothing).
    internal Func<Task>? DownloadRequested { get; set; }
    internal Action? LaterRequested { get; set; }
    internal Action? CancelRequested { get; set; }
    internal Action? InstallRequested { get; set; }
    internal Func<Task>? CheckNowRequested { get; set; }

    public static string OfferText(ReleaseInfo r, Version running) =>
        $"A new version of EarthRelief3D is available: {VersionText(r.Version)} (released {UpdateCheck.DateText(r.PublishedUtc)}). You have {VersionText(running)}. New versions bring updated data and fixes.";

    public static string DownloadingText(ReleaseInfo r, string progress) => $"Downloading EarthRelief3D {VersionText(r.Version)}: {progress}";

    public static string ReadyText(ReleaseInfo r) =>
        $"Ready to install {VersionText(r.Version)}. EarthRelief3D will close, the installer will run (Windows will ask for your permission), and EarthRelief3D will start again.";

    /// <summary>
    /// A newer release, while the app runs as administrator: the installer would inherit that, with no Windows prompt, and restart
    /// the app as administrator, so the app does not install it itself (only "Open the download page" and "Later").
    /// </summary>
    public static string ElevatedText(ReleaseInfo r) =>
        $"A new version of EarthRelief3D is available: {VersionText(r.Version)} (released {UpdateCheck.DateText(r.PublishedUtc)}). EarthRelief3D is running as administrator, so it will not install updates itself. Download the new version from its page instead.";

    public static string FailedText(string reason) => $"The update could not be used: {reason.Trim()} You can download it from its release page instead.";

    /// <summary>
    /// The installer ended while the app was still open (ruling R11). Exit code 0 is the installer's "success", yet it did not close
    /// EarthRelief3D, so the update may or may not be in place (M7); any other code: it was not installed.
    /// </summary>
    public static string NotInstalledText(int exitCode) => exitCode == 0
        ? "The update may not have been installed: the installer stopped (exit code 0) without closing EarthRelief3D."
        : $"The update was not installed: the installer stopped (exit code {exitCode.ToString(System.Globalization.CultureInfo.InvariantCulture)}). If Windows asked for your permission and it was not given, that is why.";

    /// <summary>"1.1.0".</summary>
    public static string VersionText(Version v) => UpdateCheck.Normalise(v).ToString(3);

    internal void ShowOffer(ReleaseInfo r) => Set(UpdateBannerState.Offer, r, OfferText(r, _running));

    internal void ShowDownloading(ReleaseInfo r, long done)
    {
        ProgressText = UpdateCheck.ProgressText(done, r.Setup?.Size ?? 0);
        Set(UpdateBannerState.Downloading, r, DownloadingText(r, ProgressText));
    }

    /// <summary>A progress report: ignored once the download has ended (reports can arrive after it).</summary>
    internal void ReportProgress(long done)
    {
        if (State == UpdateBannerState.Downloading && Release is { } r) ShowDownloading(r, done);
    }

    internal void ShowReady(ReleaseInfo r) => Set(UpdateBannerState.Ready, r, ReadyText(r));
    internal void ShowFailed(ReleaseInfo? r, string reason) => Set(UpdateBannerState.Failed, r, FailedText(reason));
    internal void ShowNotInstalled(ReleaseInfo r, int exitCode) => Set(UpdateBannerState.Failed, r, NotInstalledText(exitCode));

    /// <summary>The installer has started: the Ready text stays true ("EarthRelief3D will close, ..."), its buttons go.</summary>
    internal void ShowInstalling(ReleaseInfo r)
    {
        ShowReady(r);
        IsInstalling = true;
    }
    /// <summary>The stale banner; with <paramref name="lastResult"/> (a failed "Check now") on its own line below (M6).</summary>
    internal void ShowStale(string? lastResult = null) =>
        Set(UpdateBannerState.Stale, null, string.IsNullOrEmpty(lastResult) ? StaleText : StaleText + "\n" + lastResult);

    /// <summary>The stale banner while its "Check now" runs (M6): "Checking…", without "Check now".</summary>
    internal void ShowStaleChecking()
    {
        Set(UpdateBannerState.Stale, null, CheckingText);
        IsChecking = true;
    }

    /// <summary>A newer release while the app runs as administrator (F3): only "Open the download page" and "Later".</summary>
    internal void ShowElevated(ReleaseInfo r) => Set(UpdateBannerState.Failed, r, ElevatedText(r));
    internal void Hide() => Set(UpdateBannerState.Hidden, Release, "");

    private void Set(UpdateBannerState state, ReleaseInfo? r, string text)
    {
        Release = r;
        Message = "";
        IsInstalling = false;
        IsChecking = false;
        if (state != UpdateBannerState.Downloading) ProgressText = "";
        Text = text;
        State = state;
    }

    /// <summary>For a picture (scene field "update"): the banner in <paramref name="s"/> for <paramref name="r"/>, as the app would show it. Nothing is downloaded.</summary>
    public void ShowForPicture(ReleaseInfo r, UpdateBannerState s)
    {
        ArgumentNullException.ThrowIfNull(r);
        switch (s)
        {
            case UpdateBannerState.Offer: ShowOffer(r); break;
            case UpdateBannerState.Downloading:
                Set(UpdateBannerState.Downloading, r, "");
                ShowDownloading(r, 12L * 1_048_576);
                break;
            case UpdateBannerState.Ready: ShowReady(r); break;
            case UpdateBannerState.Failed: ShowFailed(r, PictureFailureText); break;
            case UpdateBannerState.Stale: ShowStale(); break;
            default: Hide(); break;
        }
    }

    [RelayCommand]
    private Task DownloadAndInstall() => State == UpdateBannerState.Offer && DownloadRequested is { } d ? d() : Task.CompletedTask;

    /// <summary>"What's new": the release's page on GitHub.</summary>
    [RelayCommand]
    private void WhatsNew() => OpenPage();

    [RelayCommand]
    private void Later()
    {
        if (!ShowLater) return;   // also while the installer runs
        if (LaterRequested is { } l) l(); else Hide();
    }

    [RelayCommand]
    private void Cancel()
    {
        if (State == UpdateBannerState.Downloading) CancelRequested?.Invoke();
    }

    [RelayCommand]
    private void InstallNow()
    {
        if (State == UpdateBannerState.Ready && !IsInstalling) InstallRequested?.Invoke();
    }

    /// <summary>"Open the download page": the release's page (or the list of releases).</summary>
    [RelayCommand]
    private void OpenDownloadPage() => OpenPage();

    [RelayCommand]
    private Task CheckNow() => CheckNowRequested is { } c ? c() : Task.CompletedTask;

    private void OpenPage()
    {
        var page = UpdateCheck.SafePage(Release);
        try
        {
            _opener.Open(page.AbsoluteUri);
            Message = "";
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException or IOException)
        {
            Message = "The page could not be opened in your browser: " + page.AbsoluteUri;
        }
    }
}
