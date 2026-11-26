using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace EarthRelief3D.Core.Updates;

/// <summary>
/// The rules for EarthRelief3D's own update check (P5-18 to P5-20): where the newest release is asked for, how GitHub's answer is
/// read, when a release counts as newer (by version, never by date), and which releases may be downloaded at all. Only the asset
/// named exactly <see cref="SetupName"/> of a release tagged <c>v&lt;version&gt;</c>, at
/// <see cref="DownloadPrefix"/><c>v&lt;version&gt;/</c><see cref="SetupName"/> (https), between 1 byte and 300 MB, with a SHA-256
/// from GitHub's digest or from the release's <see cref="ChecksumName"/> file, is ever downloaded.
/// </summary>
public static partial class UpdateCheck
{
    public const string LatestUrl = "https://api.github.com/repos/johnovanni/EarthRelief3D/releases/latest";
    public const string DownloadPrefix = "https://github.com/johnovanni/EarthRelief3D/releases/download/";
    public const string SetupName = "EarthRelief3D-Setup.exe";
    /// <summary>The release's checksum file (Task 9's workflow writes "&lt;hex&gt;  EarthRelief3D-Setup.exe"), read only when GitHub gives no digest.</summary>
    public const string ChecksumName = SetupName + ".sha256";
    /// <summary>300 MB (300 × 1,048,576 bytes): the most the updater downloads.</summary>
    public const long MaxSetupBytes = 314_572_800;
    /// <summary>The most a checksum file may hold (it is one line).</summary>
    public const long MaxChecksumBytes = 4096;
    /// <summary>The list of releases: "Open the download page" when a release's own page is not on it.</summary>
    public const string ReleasesPage = "https://github.com/johnovanni/EarthRelief3D/releases";

    [GeneratedRegex(@"^v(\d{1,9})\.(\d{1,9})\.(\d{1,9})$", RegexOptions.CultureInvariant)]
    private static partial Regex TagPattern();

    [GeneratedRegex("^[0-9a-fA-F]{64}$", RegexOptions.CultureInvariant)]
    private static partial Regex HexPattern();

    /// <summary>
    /// Reads GitHub's <c>/releases/latest</c> answer: <c>tag_name</c>, <c>draft</c>, <c>prerelease</c>, <c>published_at</c>,
    /// <c>html_url</c> and, per asset, <c>name</c>, <c>size</c>, <c>browser_download_url</c> and <c>digest</c>. Throws
    /// <see cref="FormatException"/> for an answer it cannot read.
    /// </summary>
    public static ReleaseInfo Parse(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object) throw new FormatException("GitHub's answer is not a release.");
            string tag = root.GetProperty("tag_name").GetString() ?? throw new FormatException("The release has no tag.");
            bool draft = root.GetProperty("draft").GetBoolean();
            bool prerelease = root.GetProperty("prerelease").GetBoolean();
            var when = root.TryGetProperty("published_at", out var p) && p.ValueKind == JsonValueKind.String ? p.GetString()
                : root.TryGetProperty("created_at", out var c) && c.ValueKind == JsonValueKind.String ? c.GetString() : null;
            if (when is null || !DateTime.TryParse(when, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var published))
                throw new FormatException("The release has no date.");
            var page = AbsoluteUri(root.GetProperty("html_url").GetString(), "html_url");
            ReleaseAsset? setup = null, checksum = null;
            if (root.TryGetProperty("assets", out var assets) && assets.ValueKind == JsonValueKind.Array)
            {
                foreach (var a in assets.EnumerateArray())
                {
                    string name = a.GetProperty("name").GetString() ?? "";
                    if (name != SetupName && name != ChecksumName) continue;
                    long size = a.GetProperty("size").GetInt64();
                    var url = AbsoluteUri(a.GetProperty("browser_download_url").GetString(), "browser_download_url");
                    string? digest = a.TryGetProperty("digest", out var d) && d.ValueKind == JsonValueKind.String ? d.GetString() : null;
                    string? sha = digest is not null && digest.StartsWith("sha256:", StringComparison.Ordinal) && HexPattern().IsMatch(digest[7..])
                        ? digest[7..].ToLowerInvariant() : null;
                    var asset = new ReleaseAsset(name, size, url, sha) { RawDigest = digest is not null && sha is null ? digest : null };
                    if (name == SetupName) setup ??= asset; else checksum ??= asset;
                }
            }
            var m = TagPattern().Match(tag);
            var version = m.Success
                ? new Version(int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture), int.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture),
                              int.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture))
                : new Version(0, 0, 0);
            return new ReleaseInfo(version, tag, published, page, draft, prerelease, setup, checksum);
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException or OverflowException or ArgumentException)
        {
            throw new FormatException("GitHub's answer could not be read: " + ex.Message, ex);
        }
    }

    private static Uri AbsoluteUri(string? text, string field) =>
        Uri.TryCreate(text, UriKind.Absolute, out var u) ? u : throw new FormatException($"The release's {field} is not an address.");

    /// <summary>True when the release's tag is <c>v&lt;major&gt;.&lt;minor&gt;.&lt;patch&gt;</c> (only then is its version known).</summary>
    public static bool HasVersionTag(ReleaseInfo r) => TagPattern().IsMatch(r.Tag);

    /// <summary>The version as three numbers (1.0 and 1.0.0.0 are 1.0.0), so versions compare by their numbers alone.</summary>
    public static Version Normalise(Version v) => new(v.Major, Math.Max(0, v.Minor), Math.Max(0, v.Build));

    /// <summary>True when <paramref name="r"/> has a version tag and its version is above <paramref name="running"/> (by number: 1.0.10 is above 1.0.9).</summary>
    public static bool IsNewer(ReleaseInfo r, Version running) => HasVersionTag(r) && Normalise(r.Version) > Normalise(running);

    /// <summary>The one address a release's file may be downloaded from first: <see cref="DownloadPrefix"/>, the tag, the file's name.</summary>
    public static string ExpectedUrl(ReleaseInfo r, string name) => DownloadPrefix + r.Tag + "/" + name;

    /// <summary>Null when the release may be downloaded; otherwise a plain sentence saying why not.</summary>
    public static string? RefuseReason(ReleaseInfo r)
    {
        ArgumentNullException.ThrowIfNull(r);
        if (!HasVersionTag(r)) return $"The release's tag \"{r.Tag}\" is not \"v\" and a version such as v1.1.0.";
        if (r.Draft) return "The release is a draft.";
        if (r.Prerelease) return "The release is a pre-release.";
        if (r.Setup is not { } setup) return $"The release has no file named {SetupName}.";
        if (!IsExpected(setup.DownloadUrl, ExpectedUrl(r, SetupName)))
            return $"The installer is not at GitHub's https address for this release: {setup.DownloadUrl}";
        if (setup.Size <= 0 || setup.Size > MaxSetupBytes)
            return $"The installer's size ({setup.Size.ToString("N0", CultureInfo.InvariantCulture)} bytes) is not between 1 byte and 300 MB.";
        if (setup.RawDigest is not null) return $"The release's checksum for the installer is not a SHA-256: \"{setup.RawDigest}\".";
        if (setup.Sha256 is null)
        {
            if (r.Checksum is not { } sum) return "The release gives no SHA-256 checksum for the installer.";
            if (!IsExpected(sum.DownloadUrl, ExpectedUrl(r, ChecksumName)))
                return $"The checksum file is not at GitHub's https address for this release: {sum.DownloadUrl}";
            if (sum.Size <= 0 || sum.Size > MaxChecksumBytes)
                return $"The checksum file's size ({sum.Size.ToString("N0", CultureInfo.InvariantCulture)} bytes) is not that of one line.";
        }
        return null;
    }

    // Exactly the expected https address (the scheme and host compare without case, the path with case).
    private static bool IsExpected(Uri actual, string expected) =>
        actual.IsAbsoluteUri && actual.Scheme == Uri.UriSchemeHttps && string.IsNullOrEmpty(actual.Query) && string.IsNullOrEmpty(actual.Fragment)
        && Uri.Compare(actual, new Uri(expected), UriComponents.SchemeAndServer | UriComponents.Path, UriFormat.UriEscaped, StringComparison.Ordinal) == 0;

    /// <summary>The release's own page when it is an https page under <see cref="ReleasesPage"/>; otherwise the list of releases.</summary>
    public static Uri SafePage(ReleaseInfo? r) =>
        r?.Page is { IsAbsoluteUri: true, Scheme: "https" } page && page.AbsoluteUri.StartsWith(ReleasesPage + "/", StringComparison.Ordinal)
            ? page : new Uri(ReleasesPage);

    /// <summary>The SHA-256 in a checksum file ("&lt;64 hex&gt;  EarthRelief3D-Setup.exe", or the hex alone), lower case; null when it holds none.</summary>
    public static string? ParseChecksumFile(string text)
    {
        var first = (text ?? "").Trim().Split([' ', '\t', '\r', '\n', '*'], 2, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        return first is not null && HexPattern().IsMatch(first) ? first.ToLowerInvariant() : null;
    }

    /// <summary>"3 Feb 2027": how release and check dates are written.</summary>
    public static string DateText(DateTime date) => date.ToString("d MMM yyyy", CultureInfo.InvariantCulture);

    /// <summary>"12 of 53 MB" (MB of 1,048,576 bytes, rounded down for the part done and up for the whole).</summary>
    public static string ProgressText(long done, long total)
    {
        const double Mb = 1_048_576;
        long whole = (long)Math.Ceiling(Math.Max(0, total) / Mb);
        long part = Math.Min(whole, (long)Math.Floor(Math.Max(0, done) / Mb));
        return $"{part.ToString(CultureInfo.InvariantCulture)} of {whole.ToString(CultureInfo.InvariantCulture)} MB";
    }
}
