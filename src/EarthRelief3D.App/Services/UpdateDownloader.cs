using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using EarthRelief3D.Core.Updates;

namespace EarthRelief3D.App.Services;

/// <summary>
/// What <see cref="UpdateDownloader.DownloadAsync"/> gives: the checked installer's path, or a sentence saying what did not match
/// (the file is then deleted). <see cref="Sha256"/> is the digest the file was checked against (checked again before it is run).
/// </summary>
public sealed record DownloadResult(string? SetupPath, string? Failure)
{
    public string? Sha256 { get; init; }

    /// <summary>The user cancelled (the part file is deleted; the banner goes back to the offer).</summary>
    public bool Cancelled { get; init; }
}

/// <summary>
/// Downloads a release's installer (P5-20) into <c>&lt;updatesDir&gt;\&lt;version&gt;\EarthRelief3D-Setup.exe</c>, only after the
/// user chose "Download and install", and keeps it only when every check passes, in this order: the release itself
/// (<see cref="UpdateCheck.RefuseReason"/>); the running app is signed (a developer build never downloads); the first address is
/// GitHub's own, redirects stay https and number at most <see cref="MaxRedirects"/>; the size is exactly the release's (and never
/// more than 300 MB); the SHA-256 is GitHub's digest (or the release's checksum file's); Windows trusts the file's signature; and
/// its signer is the running app's signer. Any failure, a cancel or a broken connection deletes the file.
/// <para>
/// The handler must not follow redirects itself (<see cref="CreateHandler"/>), so each hop is checked here. This is not GlobeCore's
/// <c>FeedHttp</c> (which caps downloads at 50 MB and is not changed).
/// </para>
/// </summary>
public sealed class UpdateDownloader
{
    public const int MaxRedirects = 10;
    public const string UnsignedAppText = "This copy of EarthRelief3D is not signed (a developer build), so it does not install updates itself.";
    /// <summary>A download that receives nothing for this long stops (the user may also cancel at any time).</summary>
    public static readonly TimeSpan IdleTimeout = TimeSpan.FromSeconds(60);

    private readonly HttpClient _http;
    private readonly ISignatureCheck _signatures;
    private readonly string _dir;

    public UpdateDownloader(HttpMessageHandler handler, ISignatureCheck signatures, string updatesDir)
    {
        ArgumentNullException.ThrowIfNull(handler);
        if (handler is SocketsHttpHandler { AllowAutoRedirect: true } or HttpClientHandler { AllowAutoRedirect: true })
            throw new ArgumentException("The handler must not follow redirects itself: each one is checked.", nameof(handler));
        _signatures = signatures ?? throw new ArgumentNullException(nameof(signatures));
        ArgumentException.ThrowIfNullOrWhiteSpace(updatesDir);
        _dir = Path.GetFullPath(updatesDir);
        _http = new HttpClient(handler, disposeHandler: false) { Timeout = Timeout.InfiniteTimeSpan };
    }

    /// <summary>The real handler: https over sockets, redirects left to <see cref="UpdateDownloader"/>, no decompression.</summary>
    public static SocketsHttpHandler CreateHandler() => new()
    {
        AllowAutoRedirect = false,
        ConnectTimeout = TimeSpan.FromSeconds(30),
        AutomaticDecompression = System.Net.DecompressionMethods.None,
    };

    /// <summary>The folder updates are kept in (<c>%LOCALAPPDATA%\EarthRelief3D\updates</c> in the app).</summary>
    public string UpdatesDir => _dir;

    /// <summary>The folder for one version's installer.</summary>
    public string FolderFor(Version v) => Path.Combine(_dir, UpdateCheck.Normalise(v).ToString(3));

    /// <summary>At start: deletes every update folder that is not for a version above <paramref name="running"/> (installed, or not a version at all).</summary>
    public void DeleteOldFolders(Version running)
    {
        if (!Directory.Exists(_dir)) return;
        foreach (var dir in Directory.GetDirectories(_dir))
        {
            if (Version.TryParse(Path.GetFileName(dir), out var v) && UpdateCheck.Normalise(v) > UpdateCheck.Normalise(running)) continue;
            try { Directory.Delete(dir, recursive: true); }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
        }
    }

    public async Task<DownloadResult> DownloadAsync(ReleaseInfo r, string runningExePath, IProgress<long> progress, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(r);
        ArgumentNullException.ThrowIfNull(progress);
        if (UpdateCheck.RefuseReason(r) is { } refused) return Fail(refused);
        // A developer build is not signed: it never downloads (nothing could be compared with its signer).
        if (_signatures.SignerSubject(runningExePath) is not { } appSigner) return Fail(UnsignedAppText);
        var setup = r.Setup!;
        var folder = FolderFor(r.Version);
        var final = Path.Combine(folder, UpdateCheck.SetupName);
        var part = final + ".part";
        bool receiving = false;
        try
        {
            Directory.CreateDirectory(folder);
            DeleteQuietly(final);
            DeleteQuietly(part);
            string expected;
            if (setup.Sha256 is { } digest) expected = digest;
            else
            {
                var (sum, sumFailure) = await ReadChecksumAsync(r, ct).ConfigureAwait(false);
                if (sum is null) return Fail(sumFailure!);
                expected = sum;
            }

            var (response, failure) = await GetAsync(new Uri(UpdateCheck.ExpectedUrl(r, UpdateCheck.SetupName)), "the installer", ct).ConfigureAwait(false);
            if (response is null) return Fail(failure!);
            using (response)
            {
                if (response.Content.Headers.ContentLength is long announced && announced != setup.Size) return Fail(SizeText(announced, setup.Size));
                using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
                long total = 0;
                bool tooLong = false;
                var buffer = new byte[81920];
                receiving = true;
                await using (var body = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false))
                await using (var file = new FileStream(part, FileMode.CreateNew, FileAccess.Write, FileShare.None, buffer.Length, FileOptions.Asynchronous))
                {
                    while (true)
                    {
                        ct.ThrowIfCancellationRequested();
                        int n;
                        using (var idle = CancellationTokenSource.CreateLinkedTokenSource(ct))
                        {
                            idle.CancelAfter(IdleTimeout);
                            n = await body.ReadAsync(buffer, idle.Token).ConfigureAwait(false);
                        }
                        if (n == 0) break;
                        total += n;
                        if (total > setup.Size) { tooLong = true; break; }   // closed first, then deleted
                        hash.AppendData(buffer, 0, n);
                        await file.WriteAsync(buffer.AsMemory(0, n), ct).ConfigureAwait(false);
                        progress.Report(total);
                    }
                }
                receiving = false;
                if (tooLong || total != setup.Size) return Delete(part, SizeText(total, setup.Size));
                var actual = Convert.ToHexStringLower(hash.GetHashAndReset());
                if (actual != expected) return Delete(part, ShaText(actual, expected));
            }
            File.Move(part, final, overwrite: true);
            if (CheckSignature(final, appSigner) is { } signatureFailure) return Delete(final, signatureFailure);
            return new DownloadResult(final, null) { Sha256 = expected };
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            DeleteQuietly(part);
            DeleteQuietly(final);
            return new DownloadResult(null, "The download was cancelled.") { Cancelled = true };
        }
        catch (OperationCanceledException)
        {
            DeleteQuietly(part);
            DeleteQuietly(final);
            return Fail($"Nothing arrived for {IdleTimeout.TotalSeconds:0} seconds, so the download stopped.");
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or UnauthorizedAccessException)
        {
            DeleteQuietly(part);
            DeleteQuietly(final);
            return Fail((receiving ? "The download stopped before the end: " : "The update could not be downloaded: ") + Sentence(ex.Message));
        }
    }

    /// <summary>
    /// Checks the downloaded file again just before it is run (P5-20, review focus 4): it is in our folder, its size and SHA-256 are
    /// still the release's, Windows trusts its signature and its signer is the running app's. Null when all pass; otherwise the file
    /// is deleted and the sentence says what did not match.
    /// </summary>
    public string? Verify(string path, ReleaseInfo r, string sha256, string runningExePath)
    {
        var failure = VerifyCore(path, r, sha256, runningExePath);
        if (failure is not null) DeleteQuietly(path);
        return failure;
    }

    private string? VerifyCore(string path, ReleaseInfo r, string sha256, string runningExePath)
    {
        if (UpdateCheck.RefuseReason(r) is { } refused) return refused;
        if (_signatures.SignerSubject(runningExePath) is not { } appSigner) return UnsignedAppText;
        var full = Path.GetFullPath(path);
        if (!string.Equals(full, Path.Combine(FolderFor(r.Version), UpdateCheck.SetupName), StringComparison.OrdinalIgnoreCase))
            return "The installer is not in EarthRelief3D's updates folder.";
        if (!File.Exists(full)) return "The downloaded installer is no longer there.";
        try
        {
            // Read with others allowed only to read, so the file cannot change while it is checked.
            using var file = new FileStream(full, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (file.Length != r.Setup!.Size) return SizeText(file.Length, r.Setup.Size);
            var actual = Convert.ToHexStringLower(SHA256.HashData(file));
            if (actual != sha256) return ShaText(actual, sha256);
            return CheckSignature(full, appSigner);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return "The downloaded installer could not be read: " + Sentence(ex.Message);
        }
    }

    private string? CheckSignature(string path, string appSigner)
    {
        if (!_signatures.IsTrusted(path)) return "Windows does not trust the installer's signature.";
        if (_signatures.SignerSubject(path) is not { } signer) return "The installer is not signed.";
        return string.Equals(signer, appSigner, StringComparison.Ordinal)
            ? null : $"The installer is signed by \"{signer}\", not by \"{appSigner}\", who signs EarthRelief3D.";
    }

    private async Task<(string? Sha, string? Failure)> ReadChecksumAsync(ReleaseInfo r, CancellationToken ct)
    {
        var (response, failure) = await GetAsync(new Uri(UpdateCheck.ExpectedUrl(r, UpdateCheck.ChecksumName)), "the checksum file", ct).ConfigureAwait(false);
        if (response is null) return (null, failure);
        using (response)
        {
            var buffer = new byte[UpdateCheck.MaxChecksumBytes + 1];
            int read = 0;
            await using var body = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            while (read < buffer.Length && await body.ReadAsync(buffer.AsMemory(read), ct).ConfigureAwait(false) is var n && n > 0) read += n;
            if (read > UpdateCheck.MaxChecksumBytes) return (null, "The release's checksum file is longer than one line.");
            return UpdateCheck.ParseChecksumFile(Encoding.ASCII.GetString(buffer, 0, read)) is { } sha
                ? (sha, null) : (null, "The release's checksum file holds no SHA-256.");
        }
    }

    /// <summary>GET with each redirect checked: https only, at most <see cref="MaxRedirects"/>. The response is the caller's to dispose.</summary>
    private async Task<(HttpResponseMessage? Response, string? Failure)> GetAsync(Uri first, string what, CancellationToken ct)
    {
        var url = first;
        for (int hop = 0; ; hop++)
        {
            if (!url.IsAbsoluteUri || url.Scheme != Uri.UriSchemeHttps) return (null, "The download was sent to an address that is not https: " + url);
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.UserAgent.ParseAdd(AppInfo.UserAgent);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/octet-stream"));
            var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
            int code = (int)response.StatusCode;
            if (code is 301 or 302 or 303 or 307 or 308)
            {
                var location = response.Headers.Location;
                response.Dispose();
                if (location is null) return (null, $"The download was sent on without an address (HTTP {code}).");
                if (hop >= MaxRedirects) return (null, $"The download was sent on more than {MaxRedirects} times.");
                url = location.IsAbsoluteUri ? location : new Uri(url, location);
                continue;
            }
            if (!response.IsSuccessStatusCode)
            {
                response.Dispose();
                return (null, $"GitHub answered HTTP {code} for {what}.");
            }
            return (response, null);
        }
    }

    private static string SizeText(long actual, long expected) => actual > expected
        ? string.Create(CultureInfo.InvariantCulture, $"The file is larger than the release says ({expected:N0} bytes).")
        : string.Create(CultureInfo.InvariantCulture, $"The file is {actual:N0} bytes, but the release says {expected:N0} bytes.");

    private static string ShaText(string actual, string expected) => $"The file's SHA-256 ({actual}) is not the release's ({expected}).";

    private static string Sentence(string message) => message.Trim().TrimEnd('.') + ".";

    private static DownloadResult Fail(string failure) => new(null, failure);

    private static DownloadResult Delete(string path, string failure)
    {
        DeleteQuietly(path);
        return Fail(failure);
    }

    private static void DeleteQuietly(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
    }
}
