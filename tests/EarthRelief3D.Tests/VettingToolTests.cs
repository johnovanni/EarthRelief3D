using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Linq;
using EarthRelief3D.Core.Curated;
using EarthRelief3D.Core.Help;
using EarthRelief3D.Vetting;

namespace EarthRelief3D.Tests;

/// <summary>
/// The vetting tool (spec 9.3; Plan 4 Task 7). The IRS samples in tests/Samples are real rows and one real Form 990 (US government
/// work); see the README there. Nothing here reaches the network: HTTP is a stub handler.
/// </summary>
public sealed class VettingToolTests
{
    private static string Sample(string name) => Path.Combine(AppContext.BaseDirectory, "Samples", name);

    private static byte[] ZipOf(string entry, byte[] content)
    {
        using var ms = new MemoryStream();
        using (var z = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            var e = z.CreateEntry(entry, CompressionLevel.Optimal);
            using var s = e.Open();
            s.Write(content);
        }
        return ms.ToArray();
    }

    private static MemoryStream Pub78Zip() => new(ZipOf("data-download-pub78.txt", File.ReadAllBytes(Sample("pub78_excerpt.txt"))));
    private static MemoryStream RevocationZip() => new(ZipOf("data-download-revocation.txt", File.ReadAllBytes(Sample("revocation_excerpt.txt"))));

    // ---------------------------------------------------------------- IRS files

    [Fact]
    public void Pub78Listed()
    {
        var all = Pub78Reader.Read(Pub78Zip());
        Assert.True(all.TryGetValue("530196605", out var redCross));
        Assert.Equal("American National Red Cross", redCross!.Name);
        Assert.Equal("DC", redCross.State);
        Assert.Equal("PC", redCross.DeductibilityCode);
        Assert.False(all.ContainsKey("000000000"));
        Assert.Equal("530196605", Ein.Normalize("53-0196605"));
        Assert.Equal("53-0196605", Ein.Format("530196605"));
        // Asking for some EINs keeps memory small on the real 100 MB file.
        var only = Pub78Reader.Read(Pub78Zip(), new HashSet<string> { "951831116" });
        Assert.Equal(["951831116"], only.Keys);
    }

    [Fact]
    public void RevokedIsFlagged()
    {
        var revoked = RevocationReader.Read(RevocationZip());
        Assert.Contains("000003154", revoked);
        Assert.DoesNotContain("530196605", revoked);
        // A row with an exemption reinstatement date is not a revocation any more.
        Assert.DoesNotContain("010028850", revoked);
    }

    [Fact]
    public void LatestReturnChosen()
    {
        using var csv = File.OpenRead(Sample("index_2026_excerpt.csv"));
        var latest = Form990Index.Latest(csv, "01-0211481");   // three periods: 202408, 202412, 202508
        Assert.NotNull(latest);
        Assert.Equal("202508", latest!.TaxPeriod);
        Assert.Equal("990", latest.ReturnType);
        Assert.Equal("202632799349300123", latest.ObjectId);
        Assert.Equal("2026_TEOS_XML_10A", latest.BatchId);
        Assert.Equal("010211481", latest.Ein);

        // A 990-T is an unrelated tax return: Medecins Sans Frontieres USA has only that in this index, so no 990 is found.
        using var csv2 = File.OpenRead(Sample("index_2026_excerpt.csv"));
        Assert.Null(Form990Index.Latest(csv2, "133433452"));
    }

    // ---------------------------------------------------------------- Form 990

    [Fact]
    public void ProgrammeShareFromPartIX()
    {
        var doc = Form990Reader.Parse(File.ReadAllBytes(Sample("irs990_sample.xml")));
        var r = Form990Reader.ProgrammeShare(doc);
        Assert.NotNull(r);
        Assert.Equal(2_985_109_298L, r!.Value.Programme);
        Assert.Equal(3_285_857_544L, r.Value.Total);
        Assert.Equal(2024, r.Value.TaxYear);                       // the return's own TaxYr (ruling R13); the period is 2024-07-01 to 2025-06-30
        Assert.Equal("2024-07-01", Form990Reader.TaxPeriodBegin(doc));
        Assert.Equal("2025-06-30", Form990Reader.TaxPeriodEnd(doc));
        Assert.Equal(0.9085, (double)r.Value.Programme / r.Value.Total, 4);
    }

    [Fact]
    public void TheSampleReturnHoldsNoPersonalData()
    {
        // Final review F5: the committed return keeps only what the tests read; no officer, employee or preparer is named, and
        // no compensation, PTIN or phone number is kept.
        var text = File.ReadAllText(Sample("irs990_sample.xml"));
        foreach (var element in new[] { "PersonNm", "PTIN", "PhoneNum", "PreparerFirmGrp", "PreparerPersonGrp", "BusinessOfficerGrp",
                     "Form990PartVIISectionAGrp", "ReportableCompFromOrgAmt", "CompensationAmt", "PrincipalOfcrBusinessName", "USAddress" })
            Assert.DoesNotContain("<" + element, text, StringComparison.Ordinal);
        var doc = Form990Reader.Parse(File.ReadAllBytes(Sample("irs990_sample.xml")));
        Assert.Equal("http://www.irs.gov/efile", doc.Root!.Name.NamespaceName);
        Assert.Equal("990", doc.Root.Descendants().Single(e => e.Name.LocalName == "ReturnTypeCd").Value);
        Assert.Equal("530196605", doc.Root.Descendants().Single(e => e.Name.LocalName == "EIN").Value);
    }

    private static XDocument Part9(params XElement[] header)
    {
        XNamespace ns = "http://www.irs.gov/efile";
        XElement N(XElement e) => new(ns + e.Name.LocalName, e.Value);
        return new XDocument(new XElement(ns + "Return",
            new XElement(ns + "ReturnHeader", header.Select(N)),
            new XElement(ns + "ReturnData", new XElement(ns + "IRS990", new XElement(ns + "TotalFunctionalExpensesGrp",
                new XElement(ns + "TotalAmt", "1000"), new XElement(ns + "ProgramServicesAmt", "800"))))));
    }

    [Fact]
    public void TaxYearFallsBackToPeriodBegin()
    {
        // No TaxYr: the calendar year of the period's first day (ruling R13).
        var r = Form990Reader.ProgrammeShare(Part9(new XElement("TaxPeriodBeginDt", "2024-07-01"), new XElement("TaxPeriodEndDt", "2025-06-30")));
        Assert.Equal(2024, r!.Value.TaxYear);
        // TaxYr wins over the dates.
        r = Form990Reader.ProgrammeShare(Part9(new XElement("TaxYr", "2023"), new XElement("TaxPeriodBeginDt", "2024-07-01")));
        Assert.Equal(2023, r!.Value.TaxYear);
        // Neither: no year, so no share rather than a guessed one.
        Assert.Null(Form990Reader.ProgrammeShare(Part9(new XElement("TaxPeriodEndDt", "2025-06-30"))));
    }

    [Fact]
    public void ShareDoesNotNeedPeriodEnd()
    {
        var r = Form990Reader.ProgrammeShare(Part9(new XElement("TaxYr", "2024")));
        Assert.Equal((800L, 1000L, 2024), r!.Value);
    }

    [Fact]
    public void Ez990HasNoShare()
    {
        XNamespace ns = "http://www.irs.gov/efile";
        XDocument Return(string form) => new(new XElement(ns + "Return",
            new XElement(ns + "ReturnHeader", new XElement(ns + "TaxPeriodEndDt", "2025-12-31"), new XElement(ns + "ReturnTypeCd", form)),
            new XElement(ns + "ReturnData", new XElement(ns + form,
                new XElement(ns + "TotalExpenses", "1000"), new XElement(ns + "ProgramServiceExpenses", "800")))));
        Assert.Null(Form990Reader.ProgrammeShare(Return("IRS990EZ")));
        Assert.Null(Form990Reader.ProgrammeShare(Return("IRS990PF")));
    }

    // ---------------------------------------------------------------- reading one entry out of a big zip

    /// <summary>Serves a zip, honouring (or ignoring) Range requests, and counts what it served.</summary>
    private sealed class ZipServer(byte[] data, bool honourRange) : HttpMessageHandler
    {
        public List<string> Ranges { get; } = [];
        public long BytesServed { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var range = request.Headers.Range?.Ranges.FirstOrDefault();
            Ranges.Add(request.Headers.Range?.ToString() ?? "(none)");
            if (!honourRange || range is null)
            {
                BytesServed += data.Length;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(data) });
            }
            long from, to;
            if (range.From is null) { to = data.Length - 1; from = Math.Max(0, data.Length - range.To!.Value); }   // a suffix range: the last N bytes
            else { from = range.From.Value; to = Math.Min(range.To ?? data.Length - 1, data.Length - 1); }
            var slice = data.AsSpan((int)from, (int)(to - from + 1)).ToArray();
            BytesServed += slice.Length;
            var resp = new HttpResponseMessage(HttpStatusCode.PartialContent) { Content = new ByteArrayContent(slice) };
            resp.Content.Headers.ContentRange = new ContentRangeHeaderValue(from, to, data.Length);
            return Task.FromResult(resp);
        }
    }

    /// <summary>A zip of 300 entries of random (incompressible) bytes, and one XML entry we want back.</summary>
    private static (byte[] Zip, byte[] Wanted) BigZip()
    {
        var rng = new Random(42);
        var wanted = Encoding.UTF8.GetBytes("<Return>" + string.Concat(Enumerable.Repeat("<x>programme</x>", 500)) + "</Return>");
        using var ms = new MemoryStream();
        using (var z = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            for (var i = 0; i < 300; i++)
            {
                var junk = new byte[4000];
                rng.NextBytes(junk);
                using var s = z.CreateEntry($"2026{i:D4}_public.xml", CompressionLevel.Optimal).Open();
                s.Write(junk);
            }
            using var w = z.CreateEntry("202640829349300109_public.xml", CompressionLevel.Optimal).Open();
            w.Write(wanted);
        }
        return (ms.ToArray(), wanted);
    }

    [Fact]
    public async Task ZipRangeReadsOneEntry()
    {
        var (zip, wanted) = BigZip();
        var server = new ZipServer(zip, honourRange: true);
        using var http = new VettingHttp("EarthRelief3D-Vetting/test", server);
        var got = await ZipRangeReader.ReadEntryAsync(http, new Uri("https://apps.irs.gov/pub/epostcard/990/xml/2026/2026_TEOS_XML_03A.zip"),
            "202640829349300109_public.xml", CancellationToken.None);
        Assert.Equal(wanted, got);
        Assert.All(server.Ranges, r => Assert.StartsWith("bytes=", r));
        Assert.True(server.BytesServed < zip.Length / 2, $"served {server.BytesServed} of {zip.Length} bytes: the zip was not read by ranges");
        await Assert.ThrowsAsync<FileNotFoundException>(() => ZipRangeReader.ReadEntryAsync(http, new Uri("https://apps.irs.gov/x.zip"),
            "missing.xml", CancellationToken.None));
    }

    [Fact]
    public async Task ZipRangeFallsBackToFullDownload()
    {
        var (zip, wanted) = BigZip();
        var server = new ZipServer(zip, honourRange: false);
        using var http = new VettingHttp("EarthRelief3D-Vetting/test", server);
        var cache = Path.Combine(Path.GetTempPath(), "er3d-vet-" + Guid.NewGuid().ToString("N"));
        try
        {
            var got = await ZipRangeReader.ReadEntryAsync(http, new Uri("https://apps.irs.gov/pub/epostcard/990/xml/2026/2026_TEOS_XML_03A.zip"),
                "202640829349300109_public.xml", CancellationToken.None, cache);
            Assert.Equal(wanted, got);
            Assert.Equal(zip.Length, server.BytesServed);          // the whole zip, once
            Assert.Single(Directory.GetFiles(cache));               // and kept for the next entry
            // A second entry from the same zip comes from the cached copy: no new request.
            var again = await ZipRangeReader.ReadEntryAsync(http, new Uri("https://apps.irs.gov/pub/epostcard/990/xml/2026/2026_TEOS_XML_03A.zip"),
                "202640829349300109_public.xml", CancellationToken.None, cache);
            Assert.Equal(wanted, again);
            Assert.Equal(zip.Length, server.BytesServed);
        }
        finally
        {
            if (Directory.Exists(cache)) Directory.Delete(cache, true);
        }
    }

    // ---------------------------------------------------------------- links

    private sealed class FixedHandler(Func<HttpRequestMessage, HttpResponseMessage> answer) : HttpMessageHandler
    {
        public List<Uri> Requested { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            lock (Requested) Requested.Add(request.RequestUri!);
            return Task.FromResult(answer(request));
        }
    }

    [Fact]
    public async Task BlockedLinkIsNotBroken()
    {
        var status = HttpStatusCode.OK;
        using var checker = new LinkChecker("EarthRelief3D-Vetting/test", new FixedHandler(_ => new HttpResponseMessage(status) { Content = new StringContent("<html>hi</html>") }));
        var u = new Uri("https://charity.example.org/donate");
        status = HttpStatusCode.Forbidden;
        Assert.Equal(LinkStatus.Blocked, await checker.CheckAsync(u));
        status = HttpStatusCode.TooManyRequests;
        Assert.Equal(LinkStatus.Blocked, await checker.CheckAsync(u));
        status = HttpStatusCode.Unauthorized;
        Assert.Equal(LinkStatus.Blocked, await checker.CheckAsync(u));
        status = HttpStatusCode.NotFound;
        Assert.Equal(LinkStatus.Broken, await checker.CheckAsync(u));
        status = HttpStatusCode.Gone;
        Assert.Equal(LinkStatus.Broken, await checker.CheckAsync(u));
        status = HttpStatusCode.BadGateway;
        Assert.Equal(LinkStatus.Broken, await checker.CheckAsync(u));
        status = HttpStatusCode.OK;
        Assert.Equal(LinkStatus.Ok, await checker.CheckAsync(u));
        status = HttpStatusCode.MovedPermanently;       // the handler stub does not follow; a redirect answer is a live page
        Assert.Equal(LinkStatus.Ok, await checker.CheckAsync(u));
        Assert.Contains("check in a browser", LinkChecker.Describe(LinkStatus.Blocked));

        // A 200 that is a bot challenge page is blocked, not fine.
        using var challenge = new LinkChecker("EarthRelief3D-Vetting/test",
            new FixedHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("<html><title>Just a moment...</title></html>", Encoding.UTF8, "text/html") }));
        Assert.Equal(LinkStatus.Blocked, await challenge.CheckAsync(u));
        // A server that is not there (DNS failure, refused connection) is broken.
        using var down = new LinkChecker("EarthRelief3D-Vetting/test", new FixedHandler(_ => throw new HttpRequestException("no such host")));
        Assert.Equal(LinkStatus.Broken, await down.CheckAsync(u));
    }

    [Fact]
    public async Task RedirectToEvaluatorIsNeverFollowed()
    {
        var handler = new FixedHandler(r => r.RequestUri!.Host == "charity.example.org"
            ? new HttpResponseMessage(HttpStatusCode.Found) { Headers = { Location = new Uri("https://www.charitynavigator.org/ein/530196605") } }
            : new HttpResponseMessage(HttpStatusCode.OK));
        using var checker = new LinkChecker("EarthRelief3D-Vetting/test", handler);
        await Assert.ThrowsAsync<InvalidOperationException>(() => checker.CheckAsync(new Uri("https://charity.example.org/donate")));
        Assert.Equal([new Uri("https://charity.example.org/donate")], handler.Requested);      // nothing reached the evaluator

        // A redirect chain to an ordinary page is followed (a relative Location too); a loop ends as broken.
        var hops = 0;
        var chain = new FixedHandler(r => r.RequestUri!.AbsolutePath == "/a"
            ? new HttpResponseMessage(HttpStatusCode.MovedPermanently) { Headers = { Location = new Uri("/b", UriKind.Relative) } }
            : new HttpResponseMessage(HttpStatusCode.OK));
        using var c2 = new LinkChecker("t", chain);
        Assert.Equal(LinkStatus.Ok, await c2.CheckAsync(new Uri("https://charity.example.org/a")));
        Assert.Equal(2, chain.Requested.Count);
        var loop = new FixedHandler(_ => { hops++; return new HttpResponseMessage(HttpStatusCode.Found) { Headers = { Location = new Uri("https://charity.example.org/") } }; });
        using var c3 = new LinkChecker("t", loop);
        Assert.Equal(LinkStatus.Broken, await c3.CheckAsync(new Uri("https://charity.example.org/")));
        Assert.Equal(LinkChecker.MaxRedirects + 1, hops);
    }

    [Fact]
    public void TrailingDotHostIsStillAnEvaluator()
    {
        Assert.True(LinkChecker.IsEvaluatorHost(new Uri("https://give.org./charity-reviews/x")));
        Assert.True(LinkChecker.IsEvaluatorHost(new Uri("https://www.charitynavigator.org./ein/1")));
        Assert.False(LinkChecker.IsEvaluatorHost(new Uri("https://notgive.org/")));
    }

    [Fact]
    public async Task RunReportsRedirectToEvaluatorAsNotChecked()
    {
        using var s = new Scratch(Draft("red-cross", "American Red Cross", "53-0196605", new DateOnly(2026, 1, 10), new DateOnly(2026, 10, 1)));
        var handler = new FixedHandler(r => r.RequestUri!.AbsolutePath == "/donate"
            ? new HttpResponseMessage(HttpStatusCode.Found) { Headers = { Location = new Uri("https://give.org/charity-reviews/x") } }
            : new HttpResponseMessage(HttpStatusCode.OK));
        var run = await Run(s, handler);
        Assert.Equal(0, run.Code);
        Assert.DoesNotContain(handler.Requested, u => IsEvaluatorHost(u.Host));
        Assert.Contains("https://relief.example.org/donate", run.Report);
        Assert.Contains("not checked by the tool (evaluator's terms)", run.Report);
    }

    // ---------------------------------------------------------------- whole runs

    private static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
    };

    private static Charity Draft(string id, string name, string ein, DateOnly reviewedOn, DateOnly evidenceOn, params Uri[] extraLinks) =>
        new(id, name, ein, new Uri("https://relief.example.org/"), new Uri("https://relief.example.org/donate"), null,
            [new CharityRating("Charity Navigator", new Uri("https://www.charitynavigator.org/ein/" + ein.Replace("-", "")), new DateOnly(2026, 1, 9), "owner")],
            null,
            [new CountryWork("SDN", new Uri("https://relief.example.org/sudan"), "Sudan programme", "charity's own page", evidenceOn)],
            new Review(Review.Draft, null), reviewedOn, "owner")
        { ProfileLinks = [new Uri("https://www.charitynavigator.org/ein/" + ein.Replace("-", "")), new Uri("https://give.org/charity-reviews/x"), .. extraLinks] };

    /// <summary>A scratch data folder: the real help tables and US profile, and the given charities.</summary>
    private sealed class Scratch : IDisposable
    {
        public string Root { get; } = Path.Combine(Path.GetTempPath(), "er3d-vet-" + Guid.NewGuid().ToString("N"));
        public string Data => Path.Combine(Root, "data");
        public string Offline => Path.Combine(Root, "irs");
        public string Out => Path.Combine(Root, "out", "report.md");

        public Scratch(params Charity[] charities)
        {
            var real = Path.Combine(AppContext.BaseDirectory, "data");
            Directory.CreateDirectory(Path.Combine(Data, "countries", "US"));
            Directory.CreateDirectory(Path.Combine(Data, "help"));
            foreach (var f in Directory.GetFiles(Path.Combine(real, "help"))) File.Copy(f, Path.Combine(Data, "help", Path.GetFileName(f)));
            File.Copy(Path.Combine(real, "countries", "US", "profile.json"), Path.Combine(Data, "countries", "US", "profile.json"));
            File.WriteAllText(Path.Combine(Data, "countries", "US", "charities.json"), JsonSerializer.Serialize(charities, Json));

            Directory.CreateDirectory(Offline);
            File.WriteAllBytes(Path.Combine(Offline, "data-download-pub78.zip"), Pub78Zip().ToArray());
            File.WriteAllBytes(Path.Combine(Offline, "data-download-revocation.zip"), RevocationZip().ToArray());
            File.Copy(Sample("index_2026_excerpt.csv"), Path.Combine(Offline, "index_2026.csv"));
            File.WriteAllBytes(Path.Combine(Offline, "2026_TEOS_XML_03A.zip"), ZipOf("202640829349300109_public.xml", File.ReadAllBytes(Sample("irs990_sample.xml"))));
        }

        public string[] Args(params string[] more) =>
            ["check", "--today", "2026-11-24", "--offline", Offline, "--data", Data, "--out", Out, .. more];

        public void Dispose()
        {
            if (Directory.Exists(Root)) Directory.Delete(Root, true);
        }
    }

    private static async Task<(int Code, string Out, string Err, string Report)> Run(Scratch s, HttpMessageHandler links, params string[] more)
    {
        var o = new StringWriter();
        var e = new StringWriter();
        var code = await VettingRun.RunAsync(s.Args(more), o, e, links);
        return (code, o.ToString(), e.ToString(), File.Exists(s.Out) ? File.ReadAllText(s.Out) : "");
    }

    [Fact]
    public void ExpiringWithinSixtyDays()
    {
        var today = new DateOnly(2026, 11, 24);
        var soon = Draft("soon", "Soon Relief", "53-0196605", reviewedOn: new DateOnly(2026, 1, 10), evidenceOn: new DateOnly(2026, 10, 1));
        var later = Draft("later", "Later Relief", "13-3433452", reviewedOn: new DateOnly(2026, 6, 1), evidenceOn: new DateOnly(2026, 6, 1));
        var passed = Draft("passed", "Passed Relief", "95-1831116", reviewedOn: new DateOnly(2025, 10, 1), evidenceOn: new DateOnly(2026, 10, 1));

        var items = VettingReport.Expiring([soon, later, passed], today);

        var s = Assert.Single(items, i => i.CharityName == "Soon Relief");
        Assert.Contains("review", s.What, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(new DateOnly(2027, 1, 10), s.ExpiresOn);          // 47 days from 24 Nov: inside 60
        Assert.DoesNotContain(items, i => i.CharityName == "Later Relief");
        var p = Assert.Single(items, i => i.CharityName == "Passed Relief");
        Assert.Equal(new DateOnly(2026, 10, 1), p.ExpiresOn);          // already passed: still listed
        Assert.DoesNotContain(items, i => i.What.Contains("Sudan") && i.CharityName == "Soon Relief");   // its evidence is good until October 2027
        // Evidence is checked too.
        var staleEvidence = Draft("ev", "Evidence Relief", "06-0726487", reviewedOn: new DateOnly(2026, 11, 1), evidenceOn: new DateOnly(2026, 1, 20));
        Assert.Contains(VettingReport.Expiring([staleEvidence], today), i => i.What.Contains("SDN") && i.ExpiresOn == new DateOnly(2027, 1, 20));
    }

    [Fact]
    public async Task EvaluatorLinksNotFetched()
    {
        using var s = new Scratch(Draft("red-cross", "American Red Cross", "53-0196605", new DateOnly(2026, 1, 10), new DateOnly(2026, 10, 1)));
        var handler = new FixedHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("<html>ok</html>") });

        var run = await Run(s, handler);

        Assert.Equal(0, run.Code);
        Assert.DoesNotContain(handler.Requested, u => IsEvaluatorHost(u.Host));
        Assert.Contains(handler.Requested, u => u.Host == "relief.example.org");     // the charity's own links are checked
        Assert.Contains("not checked by the tool (evaluator's terms)", run.Report);
        Assert.Contains("charitynavigator.org", run.Report);                          // listed, so the owner can open them
        await Assert.ThrowsAsync<InvalidOperationException>(() => new LinkChecker("t", handler).CheckAsync(new Uri("https://www.charitynavigator.org/ein/530196605")));
        await Assert.ThrowsAsync<InvalidOperationException>(() => new LinkChecker("t", handler).CheckAsync(new Uri("https://give.org/x")));
        Assert.DoesNotContain(handler.Requested, u => IsEvaluatorHost(u.Host));
    }

    private static bool IsEvaluatorHost(string host) =>
        host.Equals("charitynavigator.org", StringComparison.OrdinalIgnoreCase) || host.EndsWith(".charitynavigator.org", StringComparison.OrdinalIgnoreCase)
        || host.Equals("give.org", StringComparison.OrdinalIgnoreCase) || host.EndsWith(".give.org", StringComparison.OrdinalIgnoreCase);

    [Fact]
    public async Task ReportShowsEveryCheck()
    {
        using var s = new Scratch(
            Draft("red-cross", "American Red Cross", "53-0196605", new DateOnly(2026, 1, 10), new DateOnly(2026, 10, 1)),
            Draft("revoked", "Oakleaf Relief", "00-0003154", new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 1)),
            Draft("nobody", "Nobody Relief", "99-9999999", new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 1)));
        var handler = new FixedHandler(r => new HttpResponseMessage(r.RequestUri!.AbsolutePath.Contains("sudan") ? HttpStatusCode.NotFound : HttpStatusCode.Forbidden));

        var run = await Run(s, handler);

        Assert.Equal(0, run.Code);
        Assert.Contains("3 charities", run.Report);
        Assert.Contains("American National Red Cross", run.Report);          // the name as the IRS has it
        Assert.Contains("REVOKED", run.Report);
        Assert.Contains("NOT LISTED in Publication 78", run.Report);
        Assert.Contains("2025-06", run.Report);                                // the tax period of the latest return, as the IRS writes it
        Assert.Contains("202640829349300109", run.Report);                    // the object id
        Assert.Contains("check in a browser", run.Report);                    // the 403 links
        Assert.Contains("BROKEN", run.Report);                                // the 404 link
        Assert.Contains("expir", run.Report, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("2027-01-10", run.Report);
    }

    [Fact]
    public async Task EmptyListSaysSo()
    {
        using var s = new Scratch();
        var run = await Run(s, new FixedHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));
        Assert.Equal(0, run.Code);
        Assert.Contains("0 charities", run.Report);
    }

    [Fact]
    public async Task UsageErrorsAndRefusals()
    {
        using var s = new Scratch();
        var none = new FixedHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var o = new StringWriter();
        var e = new StringWriter();
        Assert.Equal(2, await VettingRun.RunAsync([], o, e, none));
        Assert.Equal(2, await VettingRun.RunAsync(["check", "--bogus"], o, e, none));
        Assert.Equal(2, await VettingRun.RunAsync(["check", "--today", "yesterday"], o, e, none));
        // The report may not be written into the data folder: the tool never edits it.
        Assert.Equal(2, await VettingRun.RunAsync(["check", "--offline", s.Offline, "--data", s.Data, "--out", Path.Combine(s.Data, "countries", "US", "report.md")], o, e, none));
        Assert.False(File.Exists(Path.Combine(s.Data, "countries", "US", "report.md")));
        // A missing IRS file is an error (exit 1), not a report that looks clean.
        File.Delete(Path.Combine(s.Offline, "data-download-pub78.zip"));
        Assert.Equal(1, await VettingRun.RunAsync(s.Args(), o, e, none));
    }

    [Fact]
    public async Task AMalformedListIsAClearError()
    {
        // m37: a charities.json that is not valid JSON (or not the list's shape) ends the run with exit 1 and a message naming
        // the problem, never an unhandled exception.
        using var s = new Scratch();
        var none = new FixedHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var list = Path.Combine(s.Data, "countries", "US", "charities.json");
        foreach (var bad in new[] { "[ { \"id\": ", "{ \"not\": \"a list\" }" })
        {
            File.WriteAllText(list, bad);
            var o = new StringWriter();
            var e = new StringWriter();
            Assert.Equal(1, await VettingRun.RunAsync(s.Args(), o, e, none));
            Assert.StartsWith("error: ", e.ToString(), StringComparison.Ordinal);
            Assert.Contains("charities.json", e.ToString(), StringComparison.Ordinal);
        }
    }

    private static string HashTree(string dir) =>
        string.Join("\n", Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories).Order(StringComparer.Ordinal)
            .Select(f => Path.GetRelativePath(dir, f) + " " + Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(f)))));

    [Fact]
    public async Task NeverEditsTheList()
    {
        using var s = new Scratch(Draft("red-cross", "American Red Cross", "53-0196605", new DateOnly(2026, 1, 10), new DateOnly(2026, 10, 1)));
        var realCountries = Path.Combine(AppContext.BaseDirectory, "data", "countries");
        var before = (Scratch: HashTree(Path.Combine(s.Data, "countries")), Real: HashTree(realCountries));
        Assert.NotEmpty(before.Scratch);

        var run = await Run(s, new FixedHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));
        // And once more aimed at the real folder (the list is empty today, which a run must leave as it is).
        var real = new StringWriter();
        await VettingRun.RunAsync(["check", "--today", "2026-11-24", "--offline", s.Offline, "--data", Path.Combine(AppContext.BaseDirectory, "data"), "--out", s.Out, "--skip-links"], real, real);

        Assert.Equal(0, run.Code);
        Assert.Equal(before.Scratch, HashTree(Path.Combine(s.Data, "countries")));
        Assert.Equal(before.Real, HashTree(realCountries));
        Assert.Equal(["report.md"], Directory.GetFiles(Path.GetDirectoryName(s.Out)!).Select(Path.GetFileName));
    }
}
