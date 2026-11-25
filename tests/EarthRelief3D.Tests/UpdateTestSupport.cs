using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using EarthRelief3D.App.Services;
using EarthRelief3D.Core.Updates;

namespace EarthRelief3D.Tests;

/// <summary>Fakes for the updater's tests (Plan 5 Task 10): nothing here ever runs a file or reaches the network.</summary>
internal static class UpdateFakes
{
    public const string AppSigner = "CN=John Apolloni, O=John Apolloni, L=Portland, S=Maine, C=US";
    public const string AppExe = @"C:\Program Files\Qapow\EarthRelief3D\EarthRelief3D.App.exe";
    public static readonly DateTime Published = new(2027, 2, 3, 9, 30, 58, DateTimeKind.Utc);

    public static string Sha(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));

    /// <summary>Bytes standing in for an installer (never run: they are not a program).</summary>
    public static byte[] Payload(int length = 300_000)
    {
        var b = new byte[length];
        new Random(42).NextBytes(b);
        return b;
    }

    /// <summary>A release 1.1.0 whose installer is <paramref name="payload"/> (its size and digest unless given).</summary>
    public static ReleaseInfo Release(byte[] payload, string? sha = "", long? size = null, string version = "1.1.0", bool withChecksumFile = false) =>
        new(Version.Parse(version), "v" + version, Published, new Uri($"https://github.com/johnovanni/EarthRelief3D/releases/tag/v{version}"), false, false,
            new ReleaseAsset(UpdateCheck.SetupName, size ?? payload.Length, new Uri(UpdateCheck.DownloadPrefix + "v" + version + "/" + UpdateCheck.SetupName),
                sha == "" ? Sha(payload) : sha),
            withChecksumFile ? new ReleaseAsset(UpdateCheck.ChecksumName, 89, new Uri(UpdateCheck.DownloadPrefix + "v" + version + "/" + UpdateCheck.ChecksumName), null) : null);

    public static string TempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "er3d-updates-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }
}

/// <summary>Signatures by path: the running app's signer, and whatever <see cref="InstallerSubject"/> and <see cref="Trusted"/> say for any other file.</summary>
internal sealed class FakeSignatures : ISignatureCheck
{
    public string? AppSubject { get; set; } = UpdateFakes.AppSigner;
    public string? InstallerSubject { get; set; } = UpdateFakes.AppSigner;
    public bool Trusted { get; set; } = true;
    public List<string> Checked { get; } = [];

    public string? SignerSubject(string path)
    {
        if (path == UpdateFakes.AppExe) return AppSubject;
        Checked.Add(path);
        return File.Exists(path) ? InstallerSubject : null;
    }

    public bool IsTrusted(string path)
    {
        Checked.Add(path);
        return File.Exists(path) && Trusted;
    }
}

/// <summary>
/// Answers by address: <see cref="Files"/> with 200, <see cref="Redirects"/> with 302 to their target, anything else 404. A file in
/// <see cref="BreakAfter"/> stops with an <see cref="IOException"/> after that many bytes. Every request is recorded.
/// </summary>
internal sealed class FakeReleaseHandler : HttpMessageHandler
{
    public Dictionary<string, byte[]> Files { get; } = [];
    public Dictionary<string, string> Redirects { get; } = [];
    public Dictionary<string, int> BreakAfter { get; } = [];
    public HttpStatusCode? Status { get; set; }
    public List<HttpRequestMessage> Requests { get; } = [];
    /// <summary>When set, every answer waits for it (or for the request to be cancelled).</summary>
    public TaskCompletionSource? Gate { get; set; }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        Requests.Add(request);
        if (Gate is { } gate) await gate.Task.WaitAsync(ct);
        return Answer(request);
    }

    private HttpResponseMessage Answer(HttpRequestMessage request)
    {
        var url = request.RequestUri!.AbsoluteUri;
        if (Status is { } s) return new HttpResponseMessage(s) { Content = new StringContent("{\"message\":\"API rate limit exceeded\"}") };
        if (Redirects.TryGetValue(url, out var to))
        {
            var r = new HttpResponseMessage(HttpStatusCode.Found);
            r.Headers.Location = new Uri(to, UriKind.RelativeOrAbsolute);
            return r;
        }
        if (Files.TryGetValue(url, out var bytes))
        {
            Stream body = BreakAfter.TryGetValue(url, out var n) ? new BreakingStream(bytes, n) : new MemoryStream(bytes);
            var content = new StreamContent(body, 8192);
            content.Headers.ContentLength = bytes.Length;
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
        }
        return new HttpResponseMessage(HttpStatusCode.NotFound);
    }

    private sealed class BreakingStream(byte[] bytes, int breakAt) : MemoryStream(bytes)
    {
        public override int Read(byte[] buffer, int offset, int count) =>
            Position >= breakAt ? throw new IOException("The connection was reset.") : base.Read(buffer, offset, Math.Min(count, breakAt - (int)Position));

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken ct = default) =>
            Position >= breakAt ? throw new IOException("The connection was reset.") : base.ReadAsync(buffer[..Math.Min(buffer.Length, breakAt - (int)Position)], ct);

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken ct) =>
            ReadAsync(buffer.AsMemory(offset, count), ct).AsTask();
    }
}

/// <summary>Records what it was asked to start; starts nothing.</summary>
internal sealed class FakeLauncher(List<string>? log = null) : IInstallerLauncher
{
    public List<(string Path, string Arguments)> Started { get; } = [];
    public string? Refuse { get; set; }
    public bool FileExistedWhenStarted { get; private set; }

    public bool TryStart(string path, string arguments, out string? error)
    {
        log?.Add("launch");
        FileExistedWhenStarted = File.Exists(path);
        if (Refuse is { } r) { error = r; return false; }
        Started.Add((path, arguments));
        error = null;
        return true;
    }
}

/// <summary>An <see cref="IProgress{T}"/> that reports at once, on the caller's thread (so a test can cancel half-way).</summary>
internal sealed class InlineProgress(Action<long>? onReport = null) : IProgress<long>
{
    public List<long> Reports { get; } = [];
    public void Report(long value)
    {
        Reports.Add(value);
        onReport?.Invoke(value);
    }
}
