using System.IO;
using EarthRelief3D.App.Services;
using EarthRelief3D.Core.Sources;

namespace EarthRelief3D.Tests;

/// <summary>Plan 5 Task 1: the one list of data sources (data/sources.json), its credits pinned to docs/data-sources.md and to the notices file.</summary>
public sealed class SourceRegistryTests
{
    private static readonly string[] ExpectedIds =
    [
        "ucdp", "ipc", "unhcr", "who-don", "who-gho", "ocha-hno", "faostat", "eia", "aqueduct", "gem-gipt", "gem-oilgas", "gem-steel",
        "acaps", "fts", "ocha-pooled-funds", "nga-wpi", "ourairports", "unhrd", "us-senate", "us-contacts", "logistics-cluster", "irs",
        "charity-evaluators", "curated", "natural-earth", "blue-marble",
    ];

    private static SourceRegistry Real() => SourceRegistry.Load(Path.Combine(Repo.Root(), "data"));
    private static string RepoFile(string name) => File.ReadAllText(Path.Combine(Repo.Root(), name));

    private static SourceInfo Row(string id = "x", string page = "https://example.org/", string credit = "Example credit") =>
        new(id, "Name " + id, "What", SourceUse.Live, "CC BY 4.0", new Uri("https://creativecommons.org/licenses/by/4.0/"), credit,
            new Uri(page), [], [], new DateOnly(2026, 11, 24));

    [Fact]
    public void RealFileLoadsAndValidates()
    {
        var r = Real();
        Assert.Empty(SourceRegistry.Validate(r));
        Assert.Equal(26, r.Sources.Count);
        Assert.Equal(ExpectedIds, r.Sources.Select(s => s.Id));
        Assert.Equal(r.Sources.Count, r.Sources.Select(s => s.Id).Distinct().Count());
    }

    [Fact]
    public void CreditsMatchDataSourcesDoc()
    {
        var doc = RepoFile(Path.Combine("docs", "data-sources.md"));
        foreach (var s in Real().Sources)
        {
            var fragments = SourceRegistry.CreditFragments(s.Credit);
            Assert.NotEmpty(fragments);
            foreach (var f in fragments)
                Assert.True(doc.Contains(f, StringComparison.Ordinal), $"{s.Id}: \"{f}\" is not in docs/data-sources.md");
        }
    }

    [Fact]
    public void CreditsAreInTheNoticesFile()
    {
        var notices = RepoFile("THIRD-PARTY-NOTICES.txt").ReplaceLineEndings("\n");
        foreach (var s in Real().Sources)
        {
            Assert.True(notices.Contains(s.Credit.Replace("{date}", "<date>"), StringComparison.Ordinal), $"{s.Id}: the credit is not in THIRD-PARTY-NOTICES.txt");
            Assert.True(notices.Contains(s.Licence, StringComparison.Ordinal), $"{s.Id}: the licence is not in THIRD-PARTY-NOTICES.txt");
        }
    }

    [Fact]
    public void EveryLinkIsHttps()
    {
        foreach (var s in Real().Sources)
        {
            Assert.Equal("https", s.Page.Scheme);
            if (s.LicenceUrl is { } u) Assert.Equal("https", u.Scheme);
        }
    }

    [Fact]
    public void CreditForFillsOrDropsTheDate()
    {
        var fts = Real().Sources.Single(s => s.Id == "fts");
        Assert.Equal("Financial Tracking Service (FTS), 25 Nov 2026, https://fts.unocha.org", fts.CreditFor("25 Nov 2026"));
        Assert.Equal("Financial Tracking Service (FTS), https://fts.unocha.org", fts.CreditFor(null));
        var eia = Real().Sources.Single(s => s.Id == "eia");
        Assert.Equal("Source: U.S. Energy Information Administration (Nov 2026)", eia.CreditFor("Nov 2026"));
        Assert.Equal("Source: U.S. Energy Information Administration", eia.CreditFor(null));
        var ucdp = Real().Sources.Single(s => s.Id == "ucdp");
        Assert.Equal(ucdp.Credit, ucdp.CreditFor("anything"));   // no {date}: nothing to fill
    }

    [Fact]
    public void ValidateNamesEachProblem()
    {
        Assert.Empty(SourceRegistry.Validate(new SourceRegistry([Row("a"), Row("b")])));

        var http = SourceRegistry.Validate(new SourceRegistry([Row("a"), Row("b", page: "http://example.org/")]));
        var m1 = Assert.Single(http);
        Assert.Contains("b", m1);

        var empty = SourceRegistry.Validate(new SourceRegistry([Row("a"), Row("b", credit: " ")]));
        var m2 = Assert.Single(empty);
        Assert.Contains("b", m2);

        var dup = SourceRegistry.Validate(new SourceRegistry([Row("a"), Row("a")]));
        var m3 = Assert.Single(dup);
        Assert.Contains("a", m3);
    }

    [Fact]
    public void ForDotFindsTheRowsOfAStatusDot()
    {
        var r = Real();
        Assert.Equal(["gem-gipt", "gem-oilgas", "gem-steel"], r.ForDot("GEM").Select(s => s.Id));
        Assert.Equal(["nga-wpi", "ourairports"], r.ForDot("Hubs").Select(s => s.Id));
        Assert.Empty(r.ForDot("No such dot"));
    }

    [Fact]
    public void EveryStatusDotHasASourceAndEveryRowDotIsReal()
    {
        var r = Real();
        foreach (var dot in AppServices.StatusDotNames)
            Assert.True(r.ForDot(dot).Count > 0, $"no source row for the status dot \"{dot}\"");
        foreach (var s in r.Sources)
            foreach (var dot in s.StatusDots)
                Assert.Contains(dot, AppServices.StatusDotNames);
    }

    [Fact]
    public void DatasetFoldersAreTheBundledFolders()
    {
        var onDisk = Directory.GetDirectories(Path.Combine(Repo.Root(), "data", "datasets")).Select(d => Path.GetFileName(d)!).Order().ToList();
        var named = Real().Sources.SelectMany(s => s.DatasetFolders).Order().ToList();
        Assert.Equal(onDisk, named);
    }

    [Fact]
    public void NoticesNameEveryShippedLibrary()
    {
        var notices = RepoFile("THIRD-PARTY-NOTICES.txt");
        foreach (var name in new[] { "HelixToolkit.Wpf 3.1.2", "CommunityToolkit.Mvvm 8.4.0", "ExcelDataReader 3.7.0", "Windows Presentation Foundation (WPF)",
                     ".NET runtime", "GlobeCore", "AstroCore", "THIRD-PARTY-NOTICES-DOTNET-RUNTIME.txt" })
            Assert.Contains(name, notices);
        Assert.Contains("Copyright (c) 2026 John M. Apolloni", notices);
        var astro = notices[notices.IndexOf("AstroCore", StringComparison.Ordinal)..];
        Assert.Contains("MIT", astro);
    }

    [Fact]
    public void NoticesShipWithTheApp()
    {
        // The App project links both files to its output; the tests' output folder receives them with the App.
        var csproj = File.ReadAllText(Path.Combine(Repo.Root(), "src", "EarthRelief3D.App", "EarthRelief3D.App.csproj"));
        foreach (var f in new[] { "THIRD-PARTY-NOTICES.txt", "THIRD-PARTY-NOTICES-DOTNET-RUNTIME.txt" })
        {
            Assert.Contains($"Link=\"{f}\" CopyToOutputDirectory=\"PreserveNewest\"", csproj);
            Assert.True(File.Exists(Path.Combine(AppContext.BaseDirectory, f)), $"{f} is not in the build output folder");
        }
        Assert.True(File.Exists(Path.Combine(AppContext.BaseDirectory, "data", SourceRegistry.FileName)), "data/sources.json is not in the build output folder");
    }
}
