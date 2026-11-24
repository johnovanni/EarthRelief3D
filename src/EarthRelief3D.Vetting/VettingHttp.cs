using System.Net.Http.Headers;

namespace EarthRelief3D.Vetting;

/// <summary>
/// The HTTP client the IRS downloads use. It is not GlobeCore's FeedHttp because the IRS files need two things that one lacks: a
/// <c>Range</c> header (<see cref="ZipRangeReader"/>), and bodies larger than FeedHttp's 50 MB cap (the 990 index is 64 MB and the
/// revocation zip 48 MB). The User-Agent names the tool and the project page and nothing about the person running it. https only.
/// </summary>
public sealed class VettingHttp : IDisposable
{
    private readonly HttpClient _client;

    /// <param name="handler">A stub in tests; owned and disposed by this object.</param>
    public VettingHttp(string userAgent, HttpMessageHandler? handler = null)
    {
        _client = new HttpClient(handler ?? new SocketsHttpHandler { AutomaticDecompression = System.Net.DecompressionMethods.None }, disposeHandler: true)
        {
            // This covers waiting for the response headers; the body is read under the caller's token.
            Timeout = TimeSpan.FromSeconds(90),
        };
        _client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);
    }

    /// <summary>GETs with the headers read and the body left to the caller (dispose the response). A <paramref name="range"/> asks for part of the file.</summary>
    public Task<HttpResponseMessage> GetAsync(Uri url, RangeHeaderValue? range, CancellationToken ct)
    {
        if (url.Scheme != Uri.UriSchemeHttps) throw new ArgumentException("https only", nameof(url));
        var req = new HttpRequestMessage(HttpMethod.Get, url);
        if (range is not null) req.Headers.Range = range;
        return _client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
    }

    /// <summary>Downloads <paramref name="url"/> to <paramref name="path"/> (through a .part file) and returns the server's Last-Modified, if it sent one.</summary>
    public async Task<DateTimeOffset?> DownloadAsync(Uri url, string path, CancellationToken ct)
    {
        using var resp = await GetAsync(url, null, ct).ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();
        await SaveAsync(resp, path, ct).ConfigureAwait(false);
        return resp.Content.Headers.LastModified;
    }

    /// <summary>Streams a response body to <paramref name="path"/> through a .part file, so a failed download never leaves a half file under its real name.</summary>
    internal static async Task SaveAsync(HttpResponseMessage resp, string path, CancellationToken ct)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        var part = path + ".part";
        try
        {
            await using (var body = await resp.Content.ReadAsStreamAsync(ct).ConfigureAwait(false))
            await using (var file = new FileStream(part, FileMode.Create, FileAccess.Write, FileShare.None, 1 << 16, useAsync: true))
                await body.CopyToAsync(file, ct).ConfigureAwait(false);
            File.Move(part, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(part)) File.Delete(part);
        }
    }

    public void Dispose() => _client.Dispose();
}
