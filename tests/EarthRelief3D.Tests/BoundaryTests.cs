using System.IO;
using AstroCore.Earth;
using EarthRelief3D.App.Diagnostics;
using EarthRelief3D.App.ViewModels;
using EarthRelief3D.Core.Feeds;
using GlobeCore.Earth;
using GlobeCore.Feeds;
using GlobeCore.Regions;

namespace EarthRelief3D.Tests;

/// <summary>
/// The app's use of GlobeCore's updatable boundary sets: the rules and probes in <c>data/</c>, the resolver that follows
/// the live set, the legend note and the validator. No test touches the network.
/// </summary>
public class BoundaryTests
{
    private static string Data(string name) => Path.Combine(AppContext.BaseDirectory, "data", name);

    /// <summary>A resolver on its own live set (the shared default must never be swapped by a test).</summary>
    internal static CountryResolver FreshResolver() =>
        new(new CurrentBoundaries(NaturalEarthBoundaries.LoadBundled(BoundaryData.Rules)),
            CountryResolver.ReadAliases(Data(CountryResolver.AliasesFile)));

    /// <summary>A made-up later edition: Luxembourg joins Belgium and the first land line is gone.</summary>
    internal static BoundarySet NextEdition(BoundarySet set) => set with
    {
        Edition = "Natural Earth 9.9.9 (2030-01-01)",
        Tag = "v9.9.9",
        Regions = set.Regions.WithMerges(new Dictionary<string, string> { ["LUX"] = "BEL" }),
        Lines = [.. set.Lines.Skip(1)],
    };

    [Fact]
    public void BundledSetWithRulesMatchesBefore()
    {
        var r = CountryResolver.LoadDefault();
        // Crimea and Sevastopol are Ukraine's (UN General Assembly resolution 68/262), Somaliland is Somalia, Fiji is one
        // country on both sides of the date line, Gaza is Palestine's.
        Assert.Equal("UKR", r.Find(new GeoPosition(45.0, 34.1))?.Iso3);
        Assert.Equal("UKR", r.Find(new GeoPosition(44.6, 33.5))?.Iso3);
        Assert.Equal("SOM", r.Find(new GeoPosition(9.56, 44.06))?.Iso3);
        Assert.Equal("FJI", r.Find(new GeoPosition(-16.93, 179.95))?.Iso3);
        Assert.Equal("FJI", r.Find(new GeoPosition(-16.85, -179.95))?.Iso3);
        Assert.Equal("PSE", r.Find(new GeoPosition(31.5, 34.45))?.Iso3);
        Assert.Null(r.ByIso3("SOL"));
        Assert.Equal("SOM", r.Resolve("Somaliland")?.Iso3);
        // Somalia's polygons now include Somaliland's, Ukraine's include Crimea's: more parts than Natural Earth's own file.
        Assert.True(r.ByIso3("UKR")!.Polygons.Count > 1);

        // The shipped rules are the ones the set was built with, each with its reason, and both are listed on screen.
        var rules = BoundaryData.Rules.Rules;
        Assert.Equal(["moveAdmin1", "mergeUnits"], rules.Select(x => x.Kind));
        Assert.All(rules, x => Assert.False(string.IsNullOrWhiteSpace(x.Reason)));
        Assert.Contains("68/262", rules[0].Reason);
        Assert.Contains("(C16)", rules[1].Reason);
        Assert.Equal(["Crimea shown as part of Ukraine (UN General Assembly resolution 68/262)",
            "Somaliland shown as part of Somalia (UN humanitarian data)"], r.Boundaries.Set.RuleDescriptions);
    }

    /// <summary>Ruling R3: every rule that changes what the user sees is named in the legend note.</summary>
    [Fact]
    public void EveryRuleWithAVisibleEffectIsNamedOnScreen()
    {
        var rules = BoundaryData.Rules.Rules;
        Assert.NotEmpty(rules);
        // moveAdmin1 and mergeUnits change the map, the picks, the shading and the panels; only an alias (name matching) is invisible.
        var visible = rules.Where(x => x.Kind != "alias").ToList();
        Assert.All(visible, x => Assert.False(string.IsNullOrWhiteSpace(x.Description), $"rule {x.Name} changes what the user sees but has no description"));
        var note = LegendViewModel.NoteFor(CountryResolver.LoadDefault().Boundaries.Set);
        foreach (var x in visible) Assert.Contains(x.Description!, note);
    }

    [Fact]
    public void TheBundledSetPassesTheAppsValidator()
    {
        var rules = BoundaryData.Rules;
        var validator = BoundaryData.CreateValidator(rules, BoundaryData.DataDir);
        var result = validator.Validate(NaturalEarthBoundaries.LoadBundled(rules));
        Assert.True(result.IsValid, result.Reason);
        var probes = BoundaryData.ReadProbes(Data(BoundaryData.ProbesFile));
        Assert.Equal(["SDN", "UKR", "UKR", "UKR", "FJI", "FJI", "SOM", "PSE"], probes.Select(p => p.ExpectedId));
    }

    [Fact]
    public void LegendNamesBoundaryEdition()
    {
        var current = new CurrentBoundaries(NaturalEarthBoundaries.LoadBundled(BoundaryData.Rules));
        var store = new EarthRelief3D.Core.Store.CrisisStore();
        var legend = new LegendViewModel(store, new LayerStripViewModel(new MemorySettingsStore()), nowUtc: () => SampleStore.Now,
            boundaries: current);

        Assert.Equal("Borders: Natural Earth 5.1.2 (2022-05-13); Crimea shown as part of Ukraine (UN General Assembly resolution 68/262); "
            + "Somaliland shown as part of Somalia (UN humanitarian data). " + LegendViewModel.BoundaryNote, legend.BoundaryText);
        Assert.EndsWith("do not imply official endorsement or acceptance by the United Nations.", legend.BoundaryText);

        // A swap alone does not change the text: the scene names the new edition once its borders are drawn (no
        // dispatcher: at once).
        current.Swap(NextEdition(current.Set));
        Assert.StartsWith("Borders: Natural Earth 5.1.2 (2022-05-13)", legend.BoundaryText);
        legend.ShowBoundaries(current.Set);
        Assert.StartsWith("Borders: Natural Earth 9.9.9 (2030-01-01); Crimea shown as part of Ukraine", legend.BoundaryText);
        Assert.EndsWith(LegendViewModel.BoundaryNote, legend.BoundaryText);

        // Without a live set the legend still carries the UN sentence.
        Assert.Equal(LegendViewModel.BoundaryNote,
            new LegendViewModel(store, new LayerStripViewModel(new MemorySettingsStore()), nowUtc: () => SampleStore.Now).BoundaryText);
    }

    [Fact]
    public async Task ResolverFollowsASwapFromAnotherThread()
    {
        var r = FreshResolver();
        var before = r.Index;
        Assert.Equal("LUX", r.Resolve("Luxembourg")?.Iso3);
        int raised = 0;
        r.Changed += (_, _) => Interlocked.Increment(ref raised);

        await Task.Run(() => r.Boundaries.Swap(NextEdition(r.Boundaries.Set)));

        Assert.Equal(1, raised);
        Assert.NotSame(before, r.Index);
        Assert.Null(r.ByIso3("LUX"));
        Assert.Equal("BEL", r.Resolve("Luxembourg")?.Iso3);
        Assert.Equal("COD", r.Resolve("DR Congo (Zaire)")?.Iso3);   // the aliases are applied to the new set too
        Assert.Equal("SOM", r.Resolve("Somaliland")?.Iso3);
    }

    [Fact]
    public void ValidatorRejectsSetThatLosesAUcdpName()
    {
        var rules = BoundaryData.Rules;
        var validator = BoundaryData.CreateValidator(rules, BoundaryData.DataDir);
        var good = NaturalEarthBoundaries.LoadBundled(rules);
        Assert.True(validator.Validate(good).IsValid);

        // Haiti is no probe and one region less is well inside the ±10% count, so only the name check can catch this.
        var withoutHaiti = good with { Regions = new RegionIndex(good.Regions.All.Where(x => x.Id != "HTI")) };
        var result = validator.Validate(withoutHaiti);
        Assert.False(result.IsValid);
        Assert.Contains("Haiti", result.Reason);
        Assert.DoesNotContain("probe", result.Reason);
    }

    [Fact]
    public void ValidatorRejectsSetWhereAnAliasLosesItsCountry()
    {
        var rules = BoundaryData.Rules;
        var validator = BoundaryData.CreateValidator(rules, BoundaryData.DataDir);
        var good = NaturalEarthBoundaries.LoadBundled(rules);
        var withoutCongo = good with { Regions = new RegionIndex(good.Regions.All.Where(x => x.Id != "COD")) };
        var result = validator.Validate(withoutCongo);
        Assert.False(result.IsValid);
        Assert.Contains("Democratic Republic of the Congo (country COD is gone)", result.Reason);
    }

    [Fact]
    public void EveryUcdpNameTheAppKnowsResolvesToday()
    {
        var names = BoundaryData.ReadUcdpNames(Data(BoundaryData.UcdpNamesFile));
        var r = CountryResolver.LoadDefault();
        var lost = names.Where(n => r.Resolve(n) is null).ToList();
        Assert.True(lost.Count == 0, "UCDP countries that do not resolve: " + string.Join("; ", lost));
        // The sample files' country column is all in the list.
        foreach (var n in new[] { "DR Congo (Zaire)", "Haiti", "Myanmar (Burma)", "Sudan", "Ukraine" }) Assert.Contains(n, names);
    }

    [Fact]
    public void TheRejectionFingerprintIsOfTheShippedRulesAndProbes()
    {
        var expected = BoundaryUpdateFeed.Fingerprint(File.ReadAllBytes(Data(BoundaryData.RulesFile)), File.ReadAllBytes(Data(BoundaryData.ProbesFile)));
        Assert.Equal(expected, BoundaryData.Fingerprint(BoundaryData.DataDir));
        var tmp = Path.Combine(Path.GetTempPath(), "er3d-fp-" + Guid.NewGuid().ToString("N"));
        try
        {
            Assert.Equal(expected, BoundaryData.Open(BoundaryData.DataDir, tmp, new ListLog()).Fingerprint);
        }
        finally { try { Directory.Delete(tmp, recursive: true); } catch (IOException) { } }
    }

    [Fact]
    public async Task OfflineRunsAnswerTheBordersCheckWithTheBundledEdition()
    {
        // The offline handler (no network) tells the feed that v5.1.2 is the latest release, so the dot is green and
        // nothing is downloaded.
        var handler = OfflineSamples.CreateHandler(Path.Combine(AppContext.BaseDirectory, "Samples"));
        using var http = new FeedHttp("test", handler);
        var rules = BoundaryData.Rules;
        var tmp = Path.Combine(Path.GetTempPath(), "er3d-borders-" + Guid.NewGuid().ToString("N"));
        try
        {
            var set = NaturalEarthBoundaries.LoadBundled(rules);
            var feed = new BoundaryUpdateFeed(new NaturalEarthBoundarySource(http), rules,
                BoundaryData.CreateValidator(rules, BoundaryData.DataDir), new BoundaryStore(tmp), new CurrentBoundaries(set), new ListLog(),
                BoundaryData.Fingerprint(BoundaryData.DataDir));
            Assert.Equal("Borders", feed.Name);
            Assert.Equal(FeedRunResult.Ok, await feed.RunOnceAsync(default));
            Assert.Single(handler.Requests);
            Assert.Equal(NaturalEarthBoundarySource.LatestReleaseUrl.ToString(), handler.Requests[0]);
        }
        finally { try { Directory.Delete(tmp, recursive: true); } catch (IOException) { } }
    }
}
