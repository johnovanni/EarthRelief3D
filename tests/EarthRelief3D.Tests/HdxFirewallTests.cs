using System.Net;
using System.Net.Http;
using System.Text;
using EarthRelief3D.Core.Feeds;
using EarthRelief3D.Core.Store;
using GlobeCore.Feeds;

namespace EarthRelief3D.Tests;

/// <summary>
/// HDX's firewall (AWS WAF) challenged the app on 26 Nov 2026 after a burst of restarts: 202 Accepted,
/// <c>x-amzn-waf-action: challenge</c>, an empty HTML body. The four HDX feeds said "format changed" and went red.
/// Now it is a blue "paused" dot with the reason, the data is kept, and HDX is left alone for the pause.
/// </summary>
public class HdxFirewallTests
{
    private static readonly DateTime Now = new(2026, 11, 26, 12, 0, 0, DateTimeKind.Utc);

    /// <summary>Challenges every request to data.humdata.org, as HDX's firewall did.</summary>
    private sealed class HdxChallenge : HttpMessageHandler
    {
        public int Requests;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Interlocked.Increment(ref Requests);
            Assert.Equal("data.humdata.org", request.RequestUri!.Host);
            var r = new HttpResponseMessage(HttpStatusCode.Accepted) { Content = new StringContent("", Encoding.UTF8, "text/html") };
            r.Headers.TryAddWithoutValidation(FeedHttp.WafActionHeader, "challenge");
            return Task.FromResult(r);
        }
    }

    [Fact]
    public async Task TheHdxFeedsArePausedNotFormatChanged()
    {
        var clock = new FixedClock(Now);
        var log = new ListLog();
        var h = new HdxChallenge();
        using var http = new FeedHttp("test", h, pauses: new HostPauses(clock));
        var indicators = new IndicatorStore();
        var crises = new CrisisStore();
        IFeed[] feeds =
        [
            new HnoFeed(http, indicators, crises, clock, log),
            new FtsFundingFeed(http, indicators, crises, clock, log),
            new AcapsAccessFeed(http, indicators, crises, clock, log),
            new IpcNationalFeed(http, crises, clock, log),
        ];
        using var scheduler = new FeedScheduler(feeds, clock, log, "idle");
        await scheduler.TickAsync(CancellationToken.None);

        Assert.All(scheduler.Statuses, s =>
        {
            Assert.Equal(FeedState.Paused, s.State);
            Assert.Equal(0, s.ConsecutiveFailures);
            Assert.Equal(Now.AddHours(1), s.NextRunUtc);
            Assert.Equal("data.humdata.org is refusing requests for now (a firewall check); trying again after 26 Nov 13:00 UTC", s.Note);
        });
        Assert.DoesNotContain(log.Lines, l => l.Contains("format changed", StringComparison.OrdinalIgnoreCase) || l.Contains("result.resources", StringComparison.Ordinal));
        Assert.Contains(log.Lines, l => l.StartsWith("WARN FTS: data.humdata.org is refusing requests", StringComparison.Ordinal));
        // The feeds run at once, so up to all four may have reached HDX before the first challenge was recorded; never more.
        Assert.InRange(h.Requests, 1, feeds.Length);
        var asked = h.Requests;

        // During the pause nothing goes to HDX, even when a feed is asked to run.
        clock.UtcNow = Now.AddMinutes(30);
        scheduler.RunSoon(FtsFundingFeed.FeedName);
        await scheduler.TickAsync(CancellationToken.None);
        Assert.Equal(asked, h.Requests);
        Assert.Equal(FeedState.Paused, scheduler.Statuses[1].State);
    }

    [Fact]
    public async Task TheIpcAreaStepStopsAskingAfterAChallenge()
    {
        var h = new HdxChallenge();
        var log = new ListLog();
        var feed = new IpcAreaFeed(new FeedHttp("test", h), new CrisisStore(), new FixedClock(Now), log, pause: TimeSpan.Zero);
        var three = new[] { "NGA", "SOM", "SDN" }.Select(iso => IpcAreaFixture.National() with { Id = "ipc-" + iso, CountryIso3 = iso }).ToList();
        await feed.RunAsync(three, CancellationToken.None);
        Assert.Equal(1, h.Requests);   // the first country's search was refused; the others were not asked
        Assert.Contains(log.Lines, l => l.Contains("and the countries after it keep the 20% rule for now", StringComparison.Ordinal));
        Assert.Contains(log.Lines, l => l.Contains("3 keep the 20% rule (HDX refused requests during this run)", StringComparison.Ordinal));
    }
}
