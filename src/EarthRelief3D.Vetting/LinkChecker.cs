using System.Net;

namespace EarthRelief3D.Vetting;

public enum LinkStatus
{
    /// <summary>The page answered (2xx, or a redirect).</summary>
    Ok,
    /// <summary>The site refuses scripts (HTTP 401, 403, 429 or a bot-challenge page): the link may be fine, so a person checks it in a browser.</summary>
    Blocked,
    /// <summary>Not found (404, 410), a server error (5xx), or the site could not be reached (DNS failure, refused, timed out).</summary>
    Broken,
}

/// <summary>
/// Checks that a link still works. A site that turns scripts away is "blocked", not "broken": a charity's donate page behind a bot
/// wall is normal. Charity Navigator and BBB Wise Giving Alliance (give.org) are never requested (their terms bar scripts and
/// scraping, ruling P4-8): <see cref="CheckAsync"/> refuses them.
/// </summary>
public sealed class LinkChecker : IDisposable
{
    public const string NotCheckedEvaluator = "not checked by the tool (evaluator's terms)";

    private readonly HttpClient _client;

    public LinkChecker(string userAgent, HttpMessageHandler? handler = null)
    {
        _client = new HttpClient(handler ?? new SocketsHttpHandler
        {
            AllowAutoRedirect = false,         // redirects are followed by hand so none can lead to an evaluator (CheckAsync)
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli,
        }, disposeHandler: true) { Timeout = TimeSpan.FromSeconds(25) };
        _client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);
    }

    /// <summary>True for charitynavigator.org and give.org, and any subdomain of either.</summary>
    public static bool IsEvaluatorHost(Uri url) => url.IsAbsoluteUri && (HostIs(url.Host, "charitynavigator.org") || HostIs(url.Host, "give.org"));

    private static bool HostIs(string host, string domain) =>
        Trim(host).Equals(domain, StringComparison.OrdinalIgnoreCase) || Trim(host).EndsWith("." + domain, StringComparison.OrdinalIgnoreCase);

    /// <summary>"give.org." (a fully qualified name) is give.org.</summary>
    private static string Trim(string host) => host.TrimEnd('.');

    /// <summary>How the report words a status.</summary>
    public static string Describe(LinkStatus s) => s switch
    {
        LinkStatus.Ok => "ok",
        LinkStatus.Blocked => "blocked to scripts: check in a browser",
        _ => "BROKEN",
    };

    /// <exception cref="InvalidOperationException">The link, or a redirect on the way (at most <see cref="MaxRedirects"/> are followed), is to an evaluator the tool must not request; nothing is requested from that host.</exception>
    public const int MaxRedirects = 10;

    public async Task<LinkStatus> CheckAsync(Uri url, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(url);
        if (!url.IsAbsoluteUri || url.Scheme is not ("http" or "https")) throw new ArgumentException("an http or https link is needed", nameof(url));
        if (IsEvaluatorHost(url)) throw new InvalidOperationException($"{url.Host}: the evaluator's terms bar scripts; the tool never requests it");
        try
        {
            var current = url;
            HttpResponseMessage resp;
            for (var hops = 0; ; hops++)
            {
                resp = await _client.GetAsync(current, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
                var status = (int)resp.StatusCode;
                if (status is not (301 or 302 or 303 or 307 or 308) || resp.Headers.Location is not { } next) break;
                resp.Dispose();
                if (hops >= MaxRedirects) return LinkStatus.Broken;
                current = next.IsAbsoluteUri ? next : new Uri(current, next);
                if (current.Scheme is not ("http" or "https")) return LinkStatus.Broken;
                if (IsEvaluatorHost(current)) throw new InvalidOperationException($"{url.Host} redirects to {current.Host}: the evaluator's terms bar scripts; the tool never requests it");
            }
            using var _ = resp;
            var code = (int)resp.StatusCode;
            if (code is 401 or 403 or 429) return LinkStatus.Blocked;
            if (resp.Headers.TryGetValues("cf-mitigated", out var mitigated) && mitigated.Any(v => v.Contains("challenge", StringComparison.OrdinalIgnoreCase)))
                return LinkStatus.Blocked;
            if (code is >= 200 and < 400 or 503)
            {
                // A challenge page can come with a 200 (or a 503): look at the start of a short page.
                if (await LooksLikeChallengeAsync(resp, ct).ConfigureAwait(false)) return LinkStatus.Blocked;
                return code == 503 ? LinkStatus.Broken : LinkStatus.Ok;
            }
            return LinkStatus.Broken;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or IOException)
        {
            return LinkStatus.Broken;        // DNS failure, refused, TLS failure or a timeout
        }
    }

    private static readonly string[] ChallengeMarkers =
        ["Just a moment...", "cf-challenge", "challenge-platform", "g-recaptcha", "hcaptcha", "Checking your browser", "Attention Required! | Cloudflare", "_Incapsula_Resource", "Pardon Our Interruption"];

    private static async Task<bool> LooksLikeChallengeAsync(HttpResponseMessage resp, CancellationToken ct)
    {
        var type = resp.Content.Headers.ContentType?.MediaType;
        if (type is not null && !type.Contains("html", StringComparison.OrdinalIgnoreCase)) return false;
        try
        {
            await using var s = await resp.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            var buf = new byte[16 * 1024];
            var n = 0;
            int r;
            while (n < buf.Length && (r = await s.ReadAsync(buf.AsMemory(n), ct).ConfigureAwait(false)) > 0) n += r;
            if (n >= buf.Length) return false;                 // a full page is not a challenge page
            var text = System.Text.Encoding.UTF8.GetString(buf, 0, n);
            return ChallengeMarkers.Any(m => text.Contains(m, StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or InvalidOperationException)
        {
            return false;
        }
    }

    public void Dispose() => _client.Dispose();
}

