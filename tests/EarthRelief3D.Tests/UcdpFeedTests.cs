using System.Globalization;
using System.IO;
using EarthRelief3D.Core.Feeds;
using EarthRelief3D.Core.Model;
using EarthRelief3D.Core.Store;
using GlobeCore.Feeds;

namespace EarthRelief3D.Tests;

public class UcdpFeedTests
{
    private static readonly DateTime Now = new(2026, 11, 22, 0, 0, 0, DateTimeKind.Utc);

    private static (UcdpCandidateFeed Feed, CrisisStore Store, ListLog Log) Make(FeedHttp http)
    {
        var store = new CrisisStore();
        var log = new ListLog();
        return (new UcdpCandidateFeed(http, store, new FixedClock(Now), log), store, log);
    }

    private static List<string[]> ReadSample(string name) => CsvReader.Read(new StringReader(Samples.Text(name))).ToList();

    [Fact]
    public void FindsNewestFilesFromPage()
    {
        var files = UcdpReader.FindFiles(Samples.Text("ucdp_downloads.html"), new Uri(Samples.UcdpPage));
        Assert.Equal(Samples.UcdpQuarterly, files.Quarterly!.Url.ToString());
        Assert.Equal([Samples.UcdpMonthly], files.MonthliesAfterQuarterly.Select(m => m.Url.ToString()));
        Assert.Equal("26.0.10", files.Version);
    }

    [Fact]
    public async Task PageWithoutLinksIsFormatChanged()
    {
        var h = new SampleHttpHandler();
        h.MapText(Samples.UcdpPage, "<html><body>moved</body></html>");
        var (feed, store, _) = Make(new FeedHttp("test", h));
        var e = await Assert.ThrowsAsync<FeedFormatException>(() => feed.RunOnceAsync(default));
        Assert.StartsWith("format changed:", e.Message);
        Assert.Empty(store.Snapshot.Crises);
    }

    [Fact]
    public void ParsesQuotedSourceColumns()
    {
        var csv = "a,b,c\n1,\"x, \"\"quoted\"\" y\nline two\",3\n4,5,6\n";
        var rows = CsvReader.Read(new StringReader(csv)).ToList();
        Assert.Equal(3, rows.Count);
        Assert.Equal("x, \"quoted\" y\nline two", rows[1][1]);
        Assert.Equal(["4", "5", "6"], rows[2]);

        // every row of the real sample has the header's column count, so the quoted source_* columns shifted nothing
        var sample = ReadSample("ucdp_candidate_monthly.csv");
        Assert.All(sample, r => Assert.Equal(sample[0].Length, r.Length));
        Assert.Equal(457, sample.Count - 1);
    }

    [Fact]
    public async Task SudanConflictIsACrisisWithEventsAndTrend()
    {
        var (http, _) = Samples.UcdpHttp();
        var (feed, store, log) = Make(http);
        Assert.Equal(FeedRunResult.Ok, await feed.RunOnceAsync(default));

        // expected event counts per conflict, computed from the raw samples (an id in both files counts once). Ruling R14:
        // an event is Sudan's when its point is in Sudan, or when it is in no country and UCDP's country is "Sudan".
        var countries = CountryResolver.LoadDefault();
        var ids = new HashSet<string>();
        var expected = new Dictionary<string, int>();
        foreach (var name in new[] { "ucdp_candidate_monthly.csv", "ucdp_candidate_quarterly.csv" })
        {
            var rows = ReadSample(name);
            int iId = Array.IndexOf(rows[0], "id"), iCountry = Array.IndexOf(rows[0], "country"), iConf = Array.IndexOf(rows[0], "conflict_new_id");
            int iLat = Array.IndexOf(rows[0], "latitude"), iLon = Array.IndexOf(rows[0], "longitude");
            foreach (var r in rows.Skip(1))
            {
                if (!ids.Add(r[iId])) continue;
                var at = new AstroCore.Earth.GeoPosition(double.Parse(r[iLat], CultureInfo.InvariantCulture), double.Parse(r[iLon], CultureInfo.InvariantCulture));
                var iso = (countries.Find(at) ?? countries.Resolve(r[iCountry]))?.Iso3;
                if (iso == "SDN") expected[r[iConf]] = expected.GetValueOrDefault(r[iConf]) + 1;
            }
        }

        var sudan = store.Snapshot.Crises.OfType<ConflictCrisis>().Where(c => c.CountryIso3 == "SDN").ToList();
        Assert.NotEmpty(sudan);
        Assert.All(sudan, c =>
        {
            Assert.Equal(CrisisKind.ArmedConflict, c.Kind);
            Assert.Equal(expected[c.UcdpConflictId.ToString()], c.Events.Count);
            Assert.Equal(c.Events.Count, c.Events.Select(e => e.Id).Distinct().Count());
            Assert.NotNull(c.Trend);
            Assert.InRange(c.Band, 1, 4);
            Assert.Equal(c.Events.Sum(e => e.BestDeaths), c.DeathsByMonth.Values.Sum());
            Assert.Equal("UCDP Candidate Events Dataset", c.Citations[0].Dataset);
            Assert.Equal("CC BY 4.0", c.Citations[0].Licence);
            Assert.Equal("26.0.10", c.Citations[0].Version);
            Assert.Equal(Now, c.Citations[0].RetrievedUtc);
            for (var m = new DateOnly(2026, 1, 1); m <= new DateOnly(2026, 10, 1); m = m.AddMonths(1))
                Assert.True(c.DeathsByMonth.ContainsKey(m), m.ToString());
        });
        Assert.Single(log.Lines, l => l.Contains("UCDP") && l.Contains("events not placed"));
    }

    [Fact]
    public async Task SampleCountriesResolveToTheirIsoCodes()
    {
        var (http, _) = Samples.UcdpHttp();
        var (feed, store, log) = Make(http);
        await feed.RunOnceAsync(default);
        var isos = store.Snapshot.Crises.Select(c => c.CountryIso3).ToHashSet();
        foreach (var iso in new[] { "COD", "MMR", "UKR", "HTI", "SDN" }) Assert.Contains(iso, isos);
        Assert.DoesNotContain("SSD", isos);
        Assert.Contains(log.Lines, l => l.Contains("events not placed: none"));
        Assert.DoesNotContain(store.Snapshot.Unplaced, u => u.Source == "UCDP");
    }

    [Fact]
    public void EveryUcdpCountryNameSeenIn2026Resolves()
    {
        // The distinct `country` values of the full 2026 quarterly Candidate file, read 2026-11-22.
        string[] names = ["Afghanistan", "Algeria", "Angola", "Argentina", "Bahrain", "Bangladesh", "Belize", "Benin", "Brazil", "Burkina Faso", "Burundi", "Cameroon", "Canada", "Central African Republic", "Chad", "Chile", "Colombia", "Costa Rica", "DR Congo (Zaire)", "Djibouti", "Dominican Republic", "Ecuador", "El Salvador", "Ethiopia", "Ghana", "Guatemala", "Haiti", "Honduras", "India", "Indonesia", "Iran", "Iraq", "Israel", "Italy", "Ivory Coast", "Jamaica", "Jordan", "Kenya", "Kuwait", "Lebanon", "Libya", "Mali", "Mexico", "Moldova", "Morocco", "Mozambique", "Myanmar (Burma)", "Nepal", "Niger", "Nigeria", "Oman", "Pakistan", "Papua New Guinea", "Peru", "Philippines", "Russia (Soviet Union)", "Saudi Arabia", "Somalia", "South Africa", "South Sudan", "Spain", "Sri Lanka", "Sudan", "Sweden", "Syria", "Tajikistan", "Tanzania", "Thailand", "Togo", "Trinidad and Tobago", "Tunisia", "Turkey", "Uganda", "Ukraine", "United Arab Emirates", "United States of America", "Venezuela", "Yemen (North Yemen)", "Zambia"];
        var r = CountryResolver.LoadDefault();
        var missing = names.Where(n => r.Resolve(n) is null).ToList();
        Assert.True(missing.Count == 0, "unresolved: " + string.Join(", ", missing));
        Assert.Equal("SSD", r.Resolve("South Sudan")!.Iso3);
        Assert.Equal("SDN", r.Resolve("Sudan")!.Iso3);
        Assert.Equal("COD", r.Resolve("DR Congo (Zaire)")!.Iso3);
        Assert.Equal("MMR", r.Resolve("Myanmar (Burma)")!.Iso3);
    }

    [Fact]
    public async Task ConflictBelow25DeathsIsNotACrisis()
    {
        var header = ReadSample("ucdp_candidate_monthly.csv")[0];
        string Row(int id, int best, string conflict)
        {
            var f = new string[header.Length];
            Array.Fill(f, "");
            void Set(string col, string v) => f[Array.IndexOf(header, col)] = v;
            Set("id", id.ToString()); Set("type_of_violence", "1"); Set("conflict_new_id", conflict); Set("conflict_name", "Test: A vs B");
            Set("side_a", "A"); Set("side_b", "B"); Set("country", "Sudan"); Set("latitude", "15.5"); Set("longitude", "32.5");
            Set("date_start", "2026-10-01 00:00:00.000"); Set("date_end", "2026-10-01 00:00:00.000"); Set("best", best.ToString());
            return string.Join(",", f);
        }
        var head = string.Join(",", header);
        var csv = string.Join("\n", [head, Row(1, 10, "9001"), Row(2, 14, "9001"), Row(3, 25, "9002")]) + "\n";
        var h = new SampleHttpHandler();
        h.Map(Samples.UcdpPage, Samples.Path("ucdp_downloads.html"));
        h.MapText(Samples.UcdpMonthly, csv);
        h.MapText(Samples.UcdpQuarterly, head + "\n");
        var (feed, store, _) = Make(new FeedHttp("test", h));
        await feed.RunOnceAsync(default);
        var ids = store.Snapshot.Crises.OfType<ConflictCrisis>().Select(c => c.UcdpConflictId).ToList();
        Assert.DoesNotContain(9001, ids);   // 24 deaths in the year
        Assert.Contains(9002, ids);         // exactly 25 is a crisis
    }

    [Fact]
    public async Task DoesNotRedownloadCachedFiles()
    {
        var (http, h) = Samples.UcdpHttp();
        var (feed, _, _) = Make(http);
        await feed.RunOnceAsync(default);
        Assert.Equal(2, h.Requests.Count(u => u.EndsWith(".csv")));
        await feed.RunOnceAsync(default);
        Assert.Equal(2, h.Requests.Count(u => u.EndsWith(".csv")));
        Assert.Equal(2, h.Requests.Count(u => u == Samples.UcdpPage));
    }

    [Fact]
    public async Task FormatChangeKeepsPreviousData()
    {
        var (http, h) = Samples.UcdpHttp();
        var (feed, store, _) = Make(http);
        await feed.RunOnceAsync(default);
        var before = store.Snapshot.Crises.Count;
        Assert.True(before > 0);
        h.MapText(Samples.UcdpPage, "<html>nothing</html>");
        await Assert.ThrowsAsync<FeedFormatException>(() => feed.RunOnceAsync(default));
        Assert.Equal(before, store.Snapshot.Crises.Count);
    }

    [Fact]
    public async Task MissingColumnIsFormatChanged()
    {
        var h = new SampleHttpHandler();
        h.Map(Samples.UcdpPage, Samples.Path("ucdp_downloads.html"));
        h.MapText(Samples.UcdpMonthly, "id,best\n1,5\n");
        h.MapText(Samples.UcdpQuarterly, "id,best\n");
        var (feed, _, _) = Make(new FeedHttp("test", h));
        var e = await Assert.ThrowsAsync<FeedFormatException>(() => feed.RunOnceAsync(default));
        Assert.StartsWith("format changed:", e.Message);
    }
}
