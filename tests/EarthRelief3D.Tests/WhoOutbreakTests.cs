using System.Text.Json;
using EarthRelief3D.Core.Feeds;
using EarthRelief3D.Core.Model;
using EarthRelief3D.Core.Store;
using GlobeCore.Feeds;

namespace EarthRelief3D.Tests;

public class WhoOutbreakTests
{
    private static readonly DateTime Now = new(2026, 11, 22, 0, 0, 0, DateTimeKind.Utc);
    private const string Sample = "who_don_synthetic.json";

    private static async Task<CrisisStore> Run(string json, DateTime? now = null)
    {
        var h = new SampleHttpHandler();
        h.MapText("https://www.who.int/api/news/diseaseoutbreaknews", json);
        var store = new CrisisStore();
        var feed = new WhoOutbreakFeed(new FeedHttp("test", h), store, new FixedClock(now ?? Now), new ListLog());
        Assert.Equal(FeedRunResult.Ok, await feed.RunOnceAsync(default));
        return store;
    }

    [Fact]
    public async Task TitleCountriesResolve()
    {
        var store = await Run(Samples.Text(Sample));
        var multi = store.Snapshot.Crises.OfType<OutbreakCrisis>().Where(c => c.DonId == "2026-DON901").Select(c => c.CountryIso3).Order().ToList();
        Assert.Equal(["COD", "KEN", "NGA"], multi);
        var all = store.Snapshot.Crises.OfType<OutbreakCrisis>().ToList();
        Assert.Contains(all, c => c.CountryIso3 == "CIV");
        Assert.Contains(all, c => c.CountryIso3 == "TUR");
        Assert.Contains(all, c => c.CountryIso3 == "PSE");
        Assert.Contains(all, c => c.CountryIso3 == "VNM");
        var kenya = all.Single(c => c.CountryIso3 == "KEN");
        Assert.Equal(3, kenya.Band);
        Assert.Equal(new Uri("https://www.who.int/emergencies/disease-outbreak-news/item/2026-DON901"), kenya.ReportUrl);
        Assert.Equal("Zorvian fever", kenya.Disease);
        var cite = Assert.Single(kenya.Citations);
        Assert.Equal("CC BY-NC-SA 3.0 IGO", cite.Licence);
        Assert.Equal(new DateTime(2026, 11, 18, 9, 0, 0, DateTimeKind.Utc), cite.AsOfUtc);
    }

    [Fact]
    public async Task UnknownCountryIsListedNotDropped()
    {
        var doc = JsonDocument.Parse(Samples.Text(Sample));
        var first = doc.RootElement.GetProperty("value")[0].GetRawText().Replace("Kenya and Nigeria", "Kenya and Atlantis");
        var store = await Run("{\"value\":[" + first + "]}");
        Assert.Equal(["COD", "KEN"], store.Snapshot.Crises.Select(c => c.CountryIso3).Order());
        var un = Assert.Single(store.Snapshot.Unplaced);
        Assert.Equal("WHO", un.Source);
        Assert.Equal("country name not recognised: Atlantis", un.Reason);
    }

    [Fact]
    public async Task OlderThan90DaysIsIgnored()
    {
        var store = await Run(Samples.Text(Sample));
        Assert.DoesNotContain(store.Snapshot.Crises, c => ((OutbreakCrisis)c).DonId == "2025-DON777");
        Assert.DoesNotContain(store.Snapshot.Crises, c => c.CountryIso3 == "MDG");
        // The window follows the injected clock: a year later nothing is recent.
        var later = await Run(Samples.Text(Sample), Now.AddDays(400));
        Assert.Empty(later.Snapshot.Crises);
    }

    [Theory]
    [InlineData("Zorvian fever - Democratic Republic of the Congo, Kenya and Nigeria", "Zorvian fever", "Democratic Republic of the Congo|Kenya|Nigeria")]
    [InlineData("Quillfox virus disease - Côte d'Ivoire", "Quillfox virus disease", "Côte d'Ivoire")]
    [InlineData("Marnet syndrome - Türkiye", "Marnet syndrome", "Türkiye")]
    [InlineData("Brellick cholera-like illness - occupied Palestinian territory", "Brellick cholera-like illness", "occupied Palestinian territory")]
    [InlineData("Tarvane influenza – Viet Nam – update 2", "Tarvane influenza", "Viet Nam")]
    [InlineData("Z - Kenya; Uganda", "Z", "Kenya|Uganda")]
    [InlineData("Glenmoor plague - Madagascar", "Glenmoor plague", "Madagascar")]
    [InlineData("Only a disease", "Only a disease", "")]
    // Live titles also have no space before the dash ("Disease- Place"); a dash inside a word is not a separator.
    [InlineData("X virus- Cambodia", "X virus", "Cambodia")]
    [InlineData("cholera-like illness - Chad", "cholera-like illness", "Chad")]
    [InlineData("Avian influenza A(H5N1)- Cambodia - update 3", "Avian influenza A(H5N1)", "Cambodia")]
    [InlineData("COVID-19- Global", "COVID-19", "Global")]
    [InlineData("cholera-like illness", "cholera-like illness", "")]
    public void ParsesDiseaseAndPlaces(string title, string disease, string places)
    {
        var (d, p) = WhoTitleParser.Parse(title);
        Assert.Equal(disease, d);
        Assert.Equal(places, string.Join("|", p));
    }

    [Fact]
    public async Task NotNewestFirstIsAFormatChangeAndKeepsTheStore()
    {
        // The sample is newest first; swapping its first two items breaks the order the query asked for.
        var doc = JsonDocument.Parse(Samples.Text(Sample));
        var items = doc.RootElement.GetProperty("value").EnumerateArray().Select(i => i.GetRawText()).ToList();
        (items[0], items[1]) = (items[1], items[0]);
        var h = new SampleHttpHandler();
        h.MapText("https://www.who.int/api/news/diseaseoutbreaknews", "{\"value\":[" + string.Join(",", items) + "]}");
        var store = new CrisisStore();
        var ex = await Assert.ThrowsAsync<FeedFormatException>(() =>
            new WhoOutbreakFeed(new FeedHttp("test", h), store, new FixedClock(Now), new ListLog()).RunOnceAsync(default));
        Assert.Equal("format changed: not sorted by publication date", ex.Message);
        Assert.Same(CrisisSnapshot.Empty, store.Snapshot);
    }

    [Fact]
    public async Task ReshapedDocumentIsAFormatChangeAndKeepsTheStore()
    {
        var h = new SampleHttpHandler();
        h.MapText("https://www.who.int/api/news/diseaseoutbreaknews", "{\"items\":[]}");
        var store = new CrisisStore();
        var ex = await Assert.ThrowsAsync<FeedFormatException>(() =>
            new WhoOutbreakFeed(new FeedHttp("test", h), store, new FixedClock(Now), new ListLog()).RunOnceAsync(default));
        Assert.StartsWith("format changed:", ex.Message);
    }
}
