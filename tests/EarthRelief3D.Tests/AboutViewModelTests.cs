using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Xml.Linq;
using EarthRelief3D.App;
using EarthRelief3D.App.ViewModels;
using EarthRelief3D.Core.Logic;
using EarthRelief3D.Core.Sources;
using GlobeCore.Wpf.Services;

namespace EarthRelief3D.Tests;

/// <summary>Plan 5 Task 5: the About card's credits, libraries, severity bands, boundary note and commands.</summary>
public sealed class AboutViewModelTests
{
    private static string Root => Repo.Root();
    private static SourceRegistry Real() => SourceRegistry.Load(Path.Combine(Root, "data"));

    private sealed class RecordingOpener : ILinkOpener
    {
        public List<string> Opened { get; } = [];
        public void Open(string url) => Opened.Add(url);
    }

    private sealed class ThrowingOpener : ILinkOpener
    {
        public void Open(string url) => throw new Win32Exception("no browser");
    }

    private static (AboutViewModel Vm, CardHost Cards) Make(ILinkOpener? opener = null, string folder = @"C:\Users\someone\AppData\Local\EarthRelief3D", string? notices = null)
    {
        var cards = new CardHost();
        cards.Register(AppCard.Data);
        cards.Register(AppCard.About);
        return (new AboutViewModel(Real(), opener ?? new RecordingOpener(), folder, cards, notices), cards);
    }

    [Fact]
    public void CreditsComeFromTheRegistry()
    {
        var registry = Real();
        var (vm, _) = Make();
        Assert.Equal(registry.Sources.Count, vm.Credits.Count);
        Assert.Equal(27, vm.Credits.Count);
        for (int i = 0; i < registry.Sources.Count; i++)
        {
            Assert.Equal(registry.Sources[i].Name, vm.Credits[i].Name);
            Assert.Equal(registry.Sources[i].CreditFor(null), vm.Credits[i].Text);
            Assert.Equal(registry.Sources[i].Page, vm.Credits[i].Url);
        }
    }

    [Fact]
    public void LibrariesListTheShippedPackages()
    {
        var packages = new List<(string Name, string Version)>();
        foreach (var proj in new[] { Path.Combine("src", "EarthRelief3D.App", "EarthRelief3D.App.csproj"), Path.Combine("src", "EarthRelief3D.Core", "EarthRelief3D.Core.csproj") })
            packages.AddRange(XDocument.Load(Path.Combine(Root, proj)).Descendants("PackageReference")
                .Select(p => ((string)p.Attribute("Include")!, (string)p.Attribute("Version")!)));
        var (vm, _) = Make();

        var listed = vm.Libraries.Select(l => l.Name).ToList();
        // Every package is listed, and the rest is the platform the app is built on.
        Assert.Equal(packages.Select(p => p.Name).Concat(["WPF", ".NET"]).Order().ToList(), listed.Order().ToList());
        Assert.All(vm.Libraries, l => Assert.Equal("MIT licence", l.Text));
        // The notices file the About card opens names each package at the version the app ships.
        var notices = File.ReadAllText(Path.Combine(Root, "THIRD-PARTY-NOTICES.txt"));
        foreach (var (name, version) in packages) Assert.Contains($"{name} {version}", notices);
    }

    [Fact]
    public void ShowsTheVersionAndFolder()
    {
        var (vm, _) = Make(folder: @"D:\ER3D");
        Assert.Equal("Version " + AppInfo.Version, vm.Version);
        Assert.Equal(@"D:\ER3D", vm.DataFolder);
        Assert.Equal(@"Your settings and the downloaded data are kept in D:\ER3D. The app sends requests only to the public sources listed under Data, and once a day to GitHub to look for a new version (you can turn that off in Settings).", vm.DataText);
        Assert.Equal("EarthRelief3D is free software under the MIT licence. Copyright (c) 2026 John M. Apolloni.", vm.AppLicence);
        Assert.Equal("https://github.com/johnovanni/EarthRelief3D/releases", vm.ReleasesUrl.AbsoluteUri);
        Assert.Equal("Every fact on screen shows where it comes from and how recent it is. EarthRelief3D never handles money, never ranks charities by its own opinion, shows no graphic images, makes no sound and collects no personal data.", vm.Principles);
    }

    /// <summary>The smallest deaths count that reaches <paramref name="band"/>, found from <see cref="SeverityBands.ForConflict"/> itself.</summary>
    private static int Boundary(int band)
    {
        int n = 0;
        while (SeverityBands.ForConflict(n) < band) n++;
        return n;
    }

    [Fact]
    public void BandLinesFollowSeverityBands()
    {
        var (vm, _) = Make();
        Assert.Equal(4, vm.BandLines.Count);
        var conflict = vm.BandLines[0];
        Assert.StartsWith("Armed conflict (UCDP deaths in one country in the data window)", conflict);
        foreach (var band in new[] { 2, 3, 4 })
            Assert.Contains(Boundary(band).ToString("N0", CultureInfo.InvariantCulture), conflict);
        Assert.Equal("Armed conflict (UCDP deaths in one country in the data window): band 1 under 1,000 (for a conflict that qualifies), 2 from 1,000, 3 from 10,000, 4 from 50,000", conflict);
        Assert.Equal("Food (IPC): the band is the IPC phase, 3 to 5", vm.BandLines[1]);
        Assert.Equal("Disease outbreaks (WHO): every report is band " + SeverityBands.ForOutbreak(), vm.BandLines[2]);
        // M1: for food the band is the phase, which sets the shade (docs/severity-bands.md: "how big and how dark").
        Assert.Equal("Bands only set a marker's size or shade; the source's own figure is always shown with it.", vm.BandLines[3]);
        Assert.Equal("https://github.com/johnovanni/EarthRelief3D/blob/main/docs/severity-bands.md", vm.BandsUrl.AbsoluteUri);
        // The food band is the phase itself.
        Assert.All(new[] { 3, 4, 5 }, p => Assert.Equal(p, SeverityBands.ForFood(p)));
    }

    [Fact]
    public void BoundaryNoteIsTheLegendsNote()
    {
        var (vm, _) = Make();
        Assert.Equal(LegendViewModel.BoundaryNote, vm.BoundaryNote);
    }

    [Fact]
    public void ShowDataOpensTheDataCard()
    {
        var (vm, cards) = Make();
        cards.Show(AppCard.About);
        vm.ShowDataCommand.Execute(null);
        Assert.Equal(AppCard.Data, cards.Open);
    }

    [Fact]
    public void ShowWelcomeWaitsForItsCard()
    {
        var (vm, cards) = Make();
        cards.Show(AppCard.About);
        vm.ShowWelcomeCommand.Execute(null);   // not registered yet (Task 6): nothing happens
        Assert.Equal(AppCard.About, cards.Open);
        cards.Register(AppCard.Welcome);
        vm.ShowWelcomeCommand.Execute(null);
        Assert.Equal(AppCard.Welcome, cards.Open);
    }

    [Fact]
    public void NoticesOpenThroughTheOpenerAndFailureShowsAMessage()
    {
        var dir = Path.Combine(Path.GetTempPath(), "er3d-about-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var file = Path.Combine(dir, "THIRD-PARTY-NOTICES.txt");
            File.WriteAllText(file, "notices");
            var rec = new RecordingOpener();
            var (vm, _) = Make(rec, dir, file);
            vm.OpenNoticesCommand.Execute(null);
            vm.OpenDataFolderCommand.Execute(null);
            Assert.Equal([file, dir], rec.Opened);
            Assert.Equal("", vm.Message);

            var (bad, _) = Make(new ThrowingOpener(), dir, file);
            bad.OpenNoticesCommand.Execute(null);
            Assert.Contains("licence texts could not be opened", bad.Message);
            Assert.Contains(file, bad.Message);
            bad.OpenDataFolderCommand.Execute(null);
            Assert.Contains("data folder could not be opened", bad.Message);

            // A file that is not there says so, and nothing is opened.
            var missing = Make(rec, dir, Path.Combine(dir, "nope.txt")).Vm;
            rec.Opened.Clear();
            missing.OpenNoticesCommand.Execute(null);
            Assert.Empty(rec.Opened);
            Assert.Contains("licence texts could not be opened", missing.Message);
        }
        finally { Directory.Delete(dir, true); }
    }

    [Fact]
    public void LinksOpenInTheBrowserAndAFailureShowsAMessage()
    {
        var rec = new RecordingOpener();
        var (vm, _) = Make(rec);
        vm.OpenLinkCommand.Execute(vm.Credits[0].Url);
        Assert.Equal([vm.Credits[0].Url.AbsoluteUri], rec.Opened);
        vm.OpenLinkCommand.Execute(new Uri("http://example.org/"));
        Assert.Single(rec.Opened);
        Assert.Contains("Only https links", vm.Message);
        var (bad, _) = Make(new ThrowingOpener());
        bad.OpenLinkCommand.Execute(bad.Credits[0].Url);
        Assert.Contains("could not be opened in your browser", bad.Message);
    }

    [Fact]
    public void EveryLinkIsHttps()
    {
        var (vm, _) = Make();
        var all = vm.Credits.Select(c => c.Url).Concat(vm.Libraries.Select(l => l.Url)).Concat([vm.ReleasesUrl, vm.BandsUrl]).ToList();
        Assert.All(all, u => { Assert.True(u.IsAbsoluteUri); Assert.Equal(Uri.UriSchemeHttps, u.Scheme); });
    }

    [Fact]
    public void CloseRequestedIsRaised()
    {
        var (vm, _) = Make();
        int n = 0;
        vm.CloseRequested += (_, _) => n++;
        vm.CloseCommand.Execute(null);
        Assert.Equal(1, n);
    }

    [Fact]
    public void TheStripsAboutButtonOpensAndClosesTheCard()
    {
        var vm = new MainViewModel(new EarthRelief3D.Core.Store.CrisisStore(), new MemorySettingsStore(), _ => null, dispatcher: null,
            registry: Real(), links: new RecordingOpener(), dataFolder: @"D:\ER3D");
        Assert.True(vm.Cards.IsAvailable(AppCard.About));
        Assert.True(vm.Layers.HasAbout);
        Assert.Equal(@"D:\ER3D", vm.About.DataFolder);
        vm.Layers.AboutCommand!.Execute(null);
        Assert.Equal(AppCard.About, vm.Cards.Open);
        vm.Layers.AboutCommand.Execute(null);
        Assert.Equal(AppCard.None, vm.Cards.Open);
        vm.Layers.AboutCommand.Execute(null);
        vm.About.CloseCommand.Execute(null);
        Assert.Equal(AppCard.None, vm.Cards.Open);
    }

    private sealed class FakeUpdates : EarthRelief3D.App.Services.IUpdateStatus
    {
        public string StatusText { get; set; } = "Checking for new versions is off (Settings)";
        public int Checks { get; private set; }
        public event EventHandler? StatusChanged;
        public void Change(string text) { StatusText = text; StatusChanged?.Invoke(this, EventArgs.Empty); }
        public Task CheckNowAsync() { Checks++; return Task.CompletedTask; }
    }

    /// <summary>P5-18: About shows the update check's status line, follows it, and "Check now" asks again.</summary>
    [Fact]
    public void ShowsTheUpdateStatus()
    {
        var (vm, _) = Make();
        Assert.False(vm.HasUpdateStatus);   // a picture or offline run has no update check
        Assert.Equal("", vm.UpdateStatus);
        var updates = new FakeUpdates();
        var changed = new List<string?>();
        vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName);
        vm.AttachUpdates(updates);
        Assert.True(vm.HasUpdateStatus);
        Assert.Equal("Checking for new versions is off (Settings)", vm.UpdateStatus);
        updates.Change("Checked for a new version on 25 Nov 2026: you have the newest (1.0.0)");
        Assert.Equal("Checked for a new version on 25 Nov 2026: you have the newest (1.0.0)", vm.UpdateStatus);
        Assert.Contains(nameof(AboutViewModel.UpdateStatus), changed);
        vm.CheckForUpdatesCommand.Execute(null);
        Assert.Equal(1, updates.Checks);
    }
}
