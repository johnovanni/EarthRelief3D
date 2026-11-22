using System.IO;
using EarthRelief3D.Core.Feeds;
using EarthRelief3D.Core.Model;
using EarthRelief3D.Core.Store;
using GlobeCore.Feeds;

namespace EarthRelief3D.Tests;

public class UcdpFeedFixTests
{
    private static readonly DateTime Now = new(2026, 11, 22, 0, 0, 0, DateTimeKind.Utc);
    private static readonly string[] Header = CsvReader.Read(new StringReader(Samples.Text("ucdp_candidate_monthly.csv"))).First();

    private static readonly (double Lat, double Lon) Khartoum = (15.5, 32.5), Juba = (4.85, 31.6);

    // The point defaults to Khartoum; South Sudan rows get Juba, so the rows stay consistent with their names.
    private static string Row(int id, int best, string conflict, string country, (double Lat, double Lon)? at = null)
    {
        var (lat, lon) = at ?? (country == "South Sudan" ? Juba : Khartoum);
        var f = new string[Header.Length];
        Array.Fill(f, "");
        void Set(string col, string v) => f[Array.IndexOf(Header, col)] = v;
        Set("id", id.ToString()); Set("type_of_violence", "1"); Set("conflict_new_id", conflict); Set("conflict_name", "Test: A vs B");
        Set("side_a", "A"); Set("side_b", "B"); Set("country", country); Set("latitude", lat.ToString(System.Globalization.CultureInfo.InvariantCulture));
        Set("longitude", lon.ToString(System.Globalization.CultureInfo.InvariantCulture));
        Set("date_start", "2026-10-01 00:00:00.000"); Set("date_end", "2026-10-01 00:00:00.000"); Set("best", best.ToString());
        return string.Join(",", f);
    }

    private static (UcdpCandidateFeed Feed, CrisisStore Store, SampleHttpHandler H, FixedClock Clock) Make(string monthlyCsv)
    {
        var head = string.Join(",", Header);
        var h = new SampleHttpHandler();
        h.Map(Samples.UcdpPage, Samples.Path("ucdp_downloads.html"));
        h.MapText(Samples.UcdpMonthly, monthlyCsv);
        h.MapText(Samples.UcdpQuarterly, head + "\n");
        var store = new CrisisStore();
        var clock = new FixedClock(Now);
        return (new UcdpCandidateFeed(new FeedHttp("test", h), store, clock, new ListLog()), store, h, clock);
    }

    private static string Csv(params string[] rows) => string.Join(",", Header) + "\n" + string.Join("\n", rows) + "\n";

    [Fact]
    public async Task ThresholdIsOnTheWholeConflictAcrossCountries()
    {
        // 20 + 20 deaths in two countries: 40 in the conflict, so a crisis in each country
        var (feed, store, _, _) = Make(Csv(Row(1, 20, "9100", "Sudan"), Row(2, 20, "9100", "South Sudan"),
            Row(3, 500, "9200", "Sudan"), Row(4, 3, "9200", "South Sudan"),
            Row(5, 10, "9300", "Sudan"), Row(6, 10, "9300", "South Sudan")));   // 20 in total: no crisis
        await feed.RunOnceAsync(default);
        var c = store.Snapshot.Crises.OfType<ConflictCrisis>().Select(x => (x.UcdpConflictId, x.CountryIso3)).ToHashSet();
        Assert.Contains((9100, "SDN"), c);
        Assert.Contains((9100, "SSD"), c);
        Assert.Contains((9200, "SDN"), c);
        Assert.Contains((9200, "SSD"), c);
        Assert.DoesNotContain(c, x => x.UcdpConflictId == 9300);
        var ssd = store.Snapshot.Crises.OfType<ConflictCrisis>().Single(x => x.UcdpConflictId == 9200 && x.CountryIso3 == "SSD");
        Assert.Equal(3, ssd.Events.Sum(e => e.BestDeaths));   // each crisis holds only its own country's events
        Assert.Null(ssd.Began);
        Assert.Contains("since Oct 2026", ssd.Severity.Text);
    }

    [Fact]
    // Ruling R14b rule 1: a point in a territory UCDP does not list (PSE) overrides UCDP's country name.
    public async Task GazaEventCodedIsraelGoesToPalestine()
    {
        var gaza = (31.5, 34.45);   // Gaza City
        Assert.Equal("PSE", CountryResolver.LoadDefault().Find(new AstroCore.Earth.GeoPosition(gaza.Item1, gaza.Item2))?.Iso3);   // the PSE polygon is in the index
        Assert.Contains("PSE", CountryResolver.LoadDefaultUcdpTerritories());
        var (feed, store, _, _) = Make(Csv(Row(1, 30, "9400", "Israel", gaza), Row(2, 5, "9400", "Israel", (32.08, 34.78))));   // and Tel Aviv
        await feed.RunOnceAsync(default);
        var byCountry = store.Snapshot.Crises.OfType<ConflictCrisis>().ToDictionary(c => c.CountryIso3);
        Assert.Equal(30, byCountry["PSE"].Events.Sum(e => e.BestDeaths));
        Assert.Equal(5, byCountry["ISR"].Events.Sum(e => e.BestDeaths));   // the conflict (35 deaths) qualifies as a whole (R9)
        Assert.Empty(store.Snapshot.Unplaced);
    }

    [Fact]
    // R14b: UCDP's name wins over the coarse 1:50m outline near a border. One of the sample's Sumy oblast events (id and
    // point from tests/Samples), coded Ukraine, whose point the 1:50m outline puts in Russia.
    public async Task SumyBorderEventCodedUkraineStaysInUkraine()
    {
        var sumy = (52.18886, 34.03613);   // Seredyna-Buda community, Sumy oblast
        Assert.Equal("RUS", CountryResolver.LoadDefault().Find(new AstroCore.Earth.GeoPosition(sumy.Item1, sumy.Item2))?.Iso3);
        var (feed, store, _, _) = Make(Csv(Row(1, 30, "9700", "Ukraine", sumy)));
        await feed.RunOnceAsync(default);
        var c = Assert.Single(store.Snapshot.Crises.OfType<ConflictCrisis>());
        Assert.Equal("UKR", c.CountryIso3);
    }

    [Fact]
    // R14b rule 2: an unknown name is placed by the country containing the point.
    public async Task UnknownNameOnLandUsesTheContainingCountry()
    {
        var (feed, store, _, _) = Make(Csv(Row(1, 30, "9800", "Atlantis")));   // Khartoum
        await feed.RunOnceAsync(default);
        Assert.Equal("SDN", Assert.Single(store.Snapshot.Crises.OfType<ConflictCrisis>()).CountryIso3);
        Assert.Empty(store.Snapshot.Unplaced);
    }

    [Fact]
    public async Task EventAtSeaIsPlacedByNameOrListedAsUnplaced()
    {
        var sea = (20.0, 38.5);   // the middle of the Red Sea
        Assert.Null(CountryResolver.LoadDefault().Find(new AstroCore.Earth.GeoPosition(sea.Item1, sea.Item2)));
        var (feed, store, _, _) = Make(Csv(Row(1, 30, "9500", "Sudan"),
            Row(2, 4, "9500", "Yemen (North Yemen)", sea),     // at sea, but the name is known: Yemen
            Row(3, 7, "9500", "Atlantis", sea), Row(4, 2, "9500", "Atlantis", sea)));   // at sea and the name is unknown
        await feed.RunOnceAsync(default);
        var isos = store.Snapshot.Crises.OfType<ConflictCrisis>().Select(c => c.CountryIso3).Order().ToList();
        Assert.Equal(["SDN", "YEM"], isos);
        var u = Assert.Single(store.Snapshot.Unplaced);
        Assert.Equal("UCDP", u.Source);
        Assert.Contains("\"Atlantis\"", u.Title);
        Assert.Equal("2 events (9 deaths) at a point in no country, and the country name \"Atlantis\" is not recognised", u.Reason);
        Assert.Equal("CC BY 4.0", u.Citation.Licence);
    }

    [Fact]
    public async Task UnplacedEventsOfAConflictBelowTheThresholdAreNotListed()
    {
        var (feed, store, _, _) = Make(Csv(Row(1, 30, "9100", "Sudan"), Row(2, 3, "9600", "Atlantis", (20.0, 38.5))));
        await feed.RunOnceAsync(default);
        Assert.Empty(store.Snapshot.Unplaced);   // 3 deaths: not a crisis, so nothing would be drawn anyway
    }

    [Fact]
    public void PlaceOfFollowsRulingR14b()
    {
        var r = CountryResolver.LoadDefault();
        IReadOnlySet<string> t = new HashSet<string> { "PSE" };
        Assert.Equal("UKR", UcdpReader.PlaceOf(r.ByIso3("UKR"), () => r.ByIso3("RUS"), t, out var h1)?.Iso3);
        Assert.Equal(UcdpReader.Placement.Name, h1);
        Assert.Equal("PSE", UcdpReader.PlaceOf(r.ByIso3("ISR"), () => r.ByIso3("PSE"), t, out var h2)?.Iso3);
        Assert.Equal(UcdpReader.Placement.Territory, h2);
        Assert.Equal("PSE", UcdpReader.PlaceOf(r.ByIso3("PSE"), () => r.ByIso3("PSE"), t, out var h3)?.Iso3);
        Assert.Equal(UcdpReader.Placement.Name, h3);
        Assert.Equal("SDN", UcdpReader.PlaceOf(null, () => r.ByIso3("SDN"), t, out var h4)?.Iso3);
        Assert.Equal(UcdpReader.Placement.Location, h4);
        Assert.Null(UcdpReader.PlaceOf(null, () => null, t, out var h5));
        Assert.Equal(UcdpReader.Placement.None, h5);
        Assert.Equal("ISR", UcdpReader.PlaceOf(r.ByIso3("ISR"), () => null, t, out _)?.Iso3);   // at sea: the name
    }

    [Fact]
    public async Task NewestMonthlyAndQuarterlyFilesExpireAfter24Hours()
    {
        var (feed, store, h, clock) = Make(Csv(Row(1, 30, "9100", "Sudan")));
        await feed.RunOnceAsync(default);
        Assert.Equal(2, h.Requests.Count(u => u.EndsWith(".csv")));
        clock.UtcNow = Now.AddHours(23);
        await feed.RunOnceAsync(default);
        Assert.Equal(2, h.Requests.Count(u => u.EndsWith(".csv")));

        h.MapText(Samples.UcdpMonthly, Csv(Row(1, 30, "9100", "Sudan"), Row(2, 40, "9100", "Sudan")));
        clock.UtcNow = Now.AddHours(25);
        await feed.RunOnceAsync(default);
        Assert.Equal(4, h.Requests.Count(u => u.EndsWith(".csv")));
        Assert.Equal(70, store.Snapshot.Crises.OfType<ConflictCrisis>().Single().Events.Sum(e => e.BestDeaths));
    }

    [Fact]
    public async Task ZeroEventsIsFormatChangedAndKeepsData()
    {
        var (feed2, store2, _, _) = Make(Csv());
        var e = await Assert.ThrowsAsync<FeedFormatException>(() => feed2.RunOnceAsync(default));
        Assert.StartsWith("format changed: no events", e.Message);
        Assert.Empty(store2.Snapshot.Crises);
    }

    [Fact]
    public async Task LatestCompleteMonthComesFromTheFileName()
    {
        // events run into November, but the monthly file is the October release: the trend must end at October
        var (feed, store, _, _) = Make(Csv(Row(1, 30, "9100", "Sudan")));
        await feed.RunOnceAsync(default);
        var files = UcdpReader.FindFiles(Samples.Text("ucdp_downloads.html"), new Uri(Samples.UcdpPage));
        Assert.Equal(new DateOnly(2026, 10, 1), files.LatestCompleteMonth);
        Assert.Single(store.Snapshot.Crises);
    }
}
