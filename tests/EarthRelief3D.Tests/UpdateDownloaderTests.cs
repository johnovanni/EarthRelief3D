using System.Globalization;
using System.IO;
using System.Text;
using EarthRelief3D.App.Services;
using EarthRelief3D.Core.Updates;
using static EarthRelief3D.Tests.UpdateFakes;

namespace EarthRelief3D.Tests;

/// <summary>
/// Plan 5 Task 10 (P5-20; review focus 4): the installer is kept only when its size, SHA-256 and signature all match; any
/// failure deletes the file and says what did not match. Fakes only: nothing is downloaded from the network and nothing is run.
/// </summary>
public sealed class UpdateDownloaderTests : IDisposable
{
    private const string FileHost = "https://release-assets.example-files.net/abc/EarthRelief3D-Setup.exe?sig=1";
    private readonly string _dir = TempDir();
    private readonly FakeReleaseHandler _http = new();
    private readonly FakeSignatures _signatures = new();

    public void Dispose()
    {
        try { Directory.Delete(_dir, recursive: true); } catch (IOException) { }
    }

    private UpdateDownloader Make() => new(_http, _signatures, _dir);

    /// <summary>GitHub's address answers 302 to another https host, which serves <paramref name="bytes"/> (as GitHub does).</summary>
    private void Serve(byte[] bytes, string version = "1.1.0")
    {
        _http.Redirects[UpdateCheck.DownloadPrefix + "v" + version + "/" + UpdateCheck.SetupName] = FileHost;
        _http.Files[FileHost] = bytes;
    }

    private string Folder(string version = "1.1.0") => Path.Combine(_dir, version);
    private static string[] FilesIn(string dir) => Directory.Exists(dir) ? Directory.GetFiles(dir) : [];

    [Fact]
    public async Task GoodDownloadIsKept()
    {
        var bytes = Payload();
        Serve(bytes);
        var progress = new InlineProgress();
        var result = await Make().DownloadAsync(Release(bytes), AppExe, progress, CancellationToken.None);

        Assert.Null(result.Failure);
        Assert.Equal(Path.Combine(Folder(), UpdateCheck.SetupName), result.SetupPath);
        Assert.Equal(bytes, File.ReadAllBytes(result.SetupPath!));
        Assert.Equal(Sha(bytes), result.Sha256);
        Assert.Equal([Path.Combine(Folder(), UpdateCheck.SetupName)], FilesIn(Folder()));   // no .part left
        Assert.Equal(bytes.Length, progress.Reports[^1]);
        // The first request is GitHub's own address, the second the host it redirected to; each names the app.
        Assert.Equal(UpdateCheck.DownloadPrefix + "v1.1.0/" + UpdateCheck.SetupName, _http.Requests[0].RequestUri!.AbsoluteUri);
        Assert.Equal(FileHost, _http.Requests[1].RequestUri!.AbsoluteUri);
        Assert.All(_http.Requests, r => Assert.Contains("EarthRelief3D/", r.Headers.UserAgent.ToString()));
        // The signature was checked on the downloaded file.
        Assert.Contains(result.SetupPath!, _signatures.Checked);
        // Checked again just before it is run: still good.
        Assert.Null(Make().Verify(result.SetupPath!, Release(bytes), result.Sha256!, AppExe));
    }

    [Fact]
    public async Task WrongDigestIsDeleted()
    {
        var bytes = Payload();
        Serve(bytes);
        var other = new string('a', 64);
        var result = await Make().DownloadAsync(Release(bytes, sha: other), AppExe, new InlineProgress(), CancellationToken.None);
        Assert.Null(result.SetupPath);
        Assert.Equal($"The file's SHA-256 ({Sha(bytes)}) is not the release's ({other}).", result.Failure);
        Assert.Empty(FilesIn(Folder()));
    }

    [Fact]
    public async Task WrongSizeIsDeleted()
    {
        var bytes = Payload();
        Serve(bytes);
        var shorter = await Make().DownloadAsync(Release(bytes, size: bytes.Length + 10), AppExe, new InlineProgress(), CancellationToken.None);
        Assert.Equal(string.Create(CultureInfo.InvariantCulture, $"The file is {bytes.Length:N0} bytes, but the release says {bytes.Length + 10:N0} bytes."), shorter.Failure);
        Assert.Empty(FilesIn(Folder()));
        var longer = await Make().DownloadAsync(Release(bytes, size: bytes.Length - 10), AppExe, new InlineProgress(), CancellationToken.None);
        Assert.Equal(string.Create(CultureInfo.InvariantCulture, $"The file is larger than the release says ({bytes.Length - 10:N0} bytes)."), longer.Failure);
        Assert.Empty(FilesIn(Folder()));
    }

    [Fact]
    public async Task DifferentSignerIsRefused()
    {
        var bytes = Payload();
        Serve(bytes);
        _signatures.InstallerSubject = "CN=Someone Else, O=Someone Else, C=US";
        var result = await Make().DownloadAsync(Release(bytes), AppExe, new InlineProgress(), CancellationToken.None);
        Assert.Equal($"The installer is signed by \"CN=Someone Else, O=Someone Else, C=US\", not by \"{AppSigner}\", who signs EarthRelief3D.", result.Failure);
        Assert.Empty(FilesIn(Folder()));

        _signatures.InstallerSubject = null;
        var unsigned = await Make().DownloadAsync(Release(bytes), AppExe, new InlineProgress(), CancellationToken.None);
        Assert.Equal("The installer is not signed.", unsigned.Failure);
        Assert.Empty(FilesIn(Folder()));
    }

    [Fact]
    public async Task UntrustedSignatureIsRefused()
    {
        var bytes = Payload();
        Serve(bytes);
        _signatures.Trusted = false;
        var result = await Make().DownloadAsync(Release(bytes), AppExe, new InlineProgress(), CancellationToken.None);
        Assert.Equal("Windows does not trust the installer's signature.", result.Failure);
        Assert.Empty(FilesIn(Folder()));
    }

    [Fact]
    public async Task UnsignedAppOffersOnlyTheDownloadPage()
    {
        var bytes = Payload();
        Serve(bytes);
        _signatures.AppSubject = null;
        var result = await Make().DownloadAsync(Release(bytes), AppExe, new InlineProgress(), CancellationToken.None);
        Assert.Equal(UpdateDownloader.UnsignedAppText, result.Failure);
        Assert.Equal("This copy of EarthRelief3D is not signed (a developer build), so it does not install updates itself.", result.Failure);
        Assert.Empty(_http.Requests);   // no request is made
        Assert.Empty(FilesIn(Folder()));
    }

    [Fact]
    public async Task RefusedReleaseIsNeverRequested()
    {
        var bytes = Payload();
        Serve(bytes);
        var r = Release(bytes) with { Prerelease = true };
        var result = await Make().DownloadAsync(r, AppExe, new InlineProgress(), CancellationToken.None);
        Assert.Equal("The release is a pre-release.", result.Failure);
        Assert.Empty(_http.Requests);
    }

    [Fact]
    public async Task CancelDeletesThePartFile()
    {
        var bytes = Payload();
        Serve(bytes);
        using var cts = new CancellationTokenSource();
        var progress = new InlineProgress(done => { if (done > bytes.Length / 3) cts.Cancel(); });
        var result = await Make().DownloadAsync(Release(bytes), AppExe, progress, cts.Token);
        Assert.True(result.Cancelled);
        Assert.Null(result.SetupPath);
        Assert.Equal("The download was cancelled.", result.Failure);
        Assert.Empty(FilesIn(Folder()));
        Assert.True(progress.Reports[^1] < bytes.Length);
    }

    [Fact]
    public async Task BrokenConnectionDeletesThePartFile()
    {
        var bytes = Payload();
        Serve(bytes);
        _http.BreakAfter[FileHost] = bytes.Length / 2;
        var result = await Make().DownloadAsync(Release(bytes), AppExe, new InlineProgress(), CancellationToken.None);
        Assert.Null(result.SetupPath);
        Assert.False(result.Cancelled);
        Assert.Equal("The download stopped before the end: The connection was reset.", result.Failure);
        Assert.Empty(FilesIn(Folder()));
    }

    [Fact]
    public async Task RedirectsAreHttpsOnlyAndAtMostTen()
    {
        var bytes = Payload();
        var first = UpdateCheck.DownloadPrefix + "v1.1.0/" + UpdateCheck.SetupName;
        _http.Redirects[first] = "http://plain.example.net/EarthRelief3D-Setup.exe";
        _http.Files["http://plain.example.net/EarthRelief3D-Setup.exe"] = bytes;
        var plain = await Make().DownloadAsync(Release(bytes), AppExe, new InlineProgress(), CancellationToken.None);
        Assert.Equal("The download was sent to an address that is not https: http://plain.example.net/EarthRelief3D-Setup.exe", plain.Failure);
        Assert.DoesNotContain(_http.Requests, r => r.RequestUri!.Scheme == "http");

        // A loop of redirects: refused after ten.
        _http.Redirects.Clear();
        for (int i = 0; i < 12; i++) _http.Redirects[i == 0 ? first : $"https://loop.example.net/{i}"] = $"https://loop.example.net/{i + 1}";
        _http.Requests.Clear();
        var loop = await Make().DownloadAsync(Release(bytes), AppExe, new InlineProgress(), CancellationToken.None);
        Assert.Equal("The download was sent on more than 10 times.", loop.Failure);
        Assert.Equal(11, _http.Requests.Count);
        Assert.Empty(FilesIn(Folder()));

        // A plain error from GitHub.
        _http.Redirects.Clear();
        var missing = await Make().DownloadAsync(Release(bytes), AppExe, new InlineProgress(), CancellationToken.None);
        Assert.Equal("GitHub answered HTTP 404 for the installer.", missing.Failure);
    }

    [Fact]
    public async Task ChecksumAssetUsedWhenNoDigest()
    {
        var bytes = Payload();
        Serve(bytes);
        var sumUrl = UpdateCheck.DownloadPrefix + "v1.1.0/" + UpdateCheck.ChecksumName;
        _http.Files[sumUrl] = Encoding.ASCII.GetBytes(Sha(bytes).ToUpperInvariant() + "  " + UpdateCheck.SetupName);
        var good = await Make().DownloadAsync(Release(bytes, sha: null, withChecksumFile: true), AppExe, new InlineProgress(), CancellationToken.None);
        Assert.Null(good.Failure);
        Assert.Equal(Sha(bytes), good.Sha256);
        Assert.Contains(_http.Requests, r => r.RequestUri!.AbsoluteUri == sumUrl);

        _http.Files[sumUrl] = Encoding.ASCII.GetBytes(new string('b', 64) + "  " + UpdateCheck.SetupName);
        var bad = await Make().DownloadAsync(Release(bytes, sha: null, withChecksumFile: true), AppExe, new InlineProgress(), CancellationToken.None);
        Assert.Equal($"The file's SHA-256 ({Sha(bytes)}) is not the release's ({new string('b', 64)}).", bad.Failure);
        Assert.Empty(FilesIn(Folder()));

        _http.Files[sumUrl] = Encoding.ASCII.GetBytes("nothing here");
        var empty = await Make().DownloadAsync(Release(bytes, sha: null, withChecksumFile: true), AppExe, new InlineProgress(), CancellationToken.None);
        Assert.Equal("The release's checksum file holds no SHA-256.", empty.Failure);
    }

    [Fact]
    public async Task FileChangedAfterDownloadIsRefusedBeforeItIsRun()
    {
        var bytes = Payload();
        Serve(bytes);
        var result = await Make().DownloadAsync(Release(bytes), AppExe, new InlineProgress(), CancellationToken.None);
        File.WriteAllBytes(result.SetupPath!, Payload(1000));
        var failure = Make().Verify(result.SetupPath!, Release(bytes), result.Sha256!, AppExe);
        Assert.Equal("The file is 1,000 bytes, but the release says 300,000 bytes.", failure);
        Assert.False(File.Exists(result.SetupPath!));
    }

    [Fact]
    public void OldUpdateFoldersDeletedAtStart()
    {
        foreach (var v in new[] { "0.9.0", "1.0.0", "1.1.0", "junk" })
        {
            Directory.CreateDirectory(Path.Combine(_dir, v));
            File.WriteAllText(Path.Combine(_dir, v, UpdateCheck.SetupName), "x");
        }
        File.WriteAllText(Path.Combine(_dir, "stray.txt"), "x");
        Make().DeleteOldFolders(new Version(1, 0, 0));
        Assert.Equal(["1.1.0"], Directory.GetDirectories(_dir).Select(d => Path.GetFileName(d)!).ToArray());
        Assert.True(File.Exists(Path.Combine(_dir, "stray.txt")));   // only folders are the updater's
        // A missing folder is fine.
        new UpdateDownloader(_http, _signatures, Path.Combine(_dir, "none")).DeleteOldFolders(new Version(1, 0, 0));
    }
}
