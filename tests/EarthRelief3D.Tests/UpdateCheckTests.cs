using System.Text.Json.Nodes;
using EarthRelief3D.Core.Updates;

namespace EarthRelief3D.Tests;

/// <summary>Plan 5 Task 10 (P5-18, P5-20): reading GitHub's newest release, and which releases may be downloaded.</summary>
public sealed class UpdateCheckTests
{
    internal static string Newer() => Samples.Text("github_release_latest_newer.json");

    /// <summary>The newer sample with <paramref name="edit"/> applied to its JSON.</summary>
    internal static ReleaseInfo Edited(Action<JsonObject> edit)
    {
        var root = JsonNode.Parse(Newer())!.AsObject();
        edit(root);
        return UpdateCheck.Parse(root.ToJsonString());
    }

    private static JsonObject Asset(JsonObject root, string name) =>
        root["assets"]!.AsArray().Select(a => a!.AsObject()).First(a => (string?)a["name"] == name);

    [Fact]
    public void ParsesGithubRelease()
    {
        var r = UpdateCheck.Parse(Newer());
        Assert.Equal(new Version(1, 1, 0), r.Version);
        Assert.Equal("v1.1.0", r.Tag);
        Assert.Equal(new DateTime(2027, 2, 3, 9, 30, 58, DateTimeKind.Utc), r.PublishedUtc);
        Assert.Equal(DateTimeKind.Utc, r.PublishedUtc.Kind);
        Assert.Equal("3 Feb 2027", UpdateCheck.DateText(r.PublishedUtc));
        Assert.Equal(new Uri("https://github.com/johnovanni/EarthRelief3D/releases/tag/v1.1.0"), r.Page);
        Assert.False(r.Draft);
        Assert.False(r.Prerelease);
        var setup = Assert.IsType<ReleaseAsset>(r.Setup);
        Assert.Equal("EarthRelief3D-Setup.exe", setup.Name);
        Assert.Equal(55_574_528, setup.Size);
        Assert.Equal("fe47cc5ecd60be7bb4c160857d48f1c865f493839680388927d9e7d286692599", setup.Sha256);
        Assert.Equal(new Uri("https://github.com/johnovanni/EarthRelief3D/releases/download/v1.1.0/EarthRelief3D-Setup.exe"), setup.DownloadUrl);
        Assert.Equal("EarthRelief3D-Setup.exe.sha256", r.Checksum?.Name);
        Assert.Null(UpdateCheck.RefuseReason(r));
        Assert.Equal(r.Page, UpdateCheck.SafePage(r));

        var same = UpdateCheck.Parse(Samples.Text("github_release_latest_same.json"));
        Assert.Equal(new Version(1, 0, 0), same.Version);
        Assert.Null(UpdateCheck.RefuseReason(same));
    }

    [Fact]
    public void NewerByVersionNotByDate()
    {
        ReleaseInfo Tagged(string tag, string published) => Edited(r => { r["tag_name"] = tag; r["published_at"] = published; });
        Assert.True(UpdateCheck.IsNewer(Tagged("v1.0.10", "2020-01-01T00:00:00Z"), new Version(1, 0, 9)));
        Assert.False(UpdateCheck.IsNewer(Tagged("v1.0.0", "2030-01-01T00:00:00Z"), new Version(1, 0, 0)));
        Assert.False(UpdateCheck.IsNewer(Tagged("v1.0.0", "2030-01-01T00:00:00Z"), new Version(1, 0, 0, 0)));
        Assert.False(UpdateCheck.IsNewer(Tagged("v0.9.0", "2030-01-01T00:00:00Z"), new Version(1, 0, 0)));
        Assert.True(UpdateCheck.IsNewer(Tagged("v2.0.0", "2020-01-01T00:00:00Z"), new Version(1, 9, 9)));
    }

    [Theory]
    [InlineData("1.1.0")]
    [InlineData("v1.1")]
    [InlineData("v1.1.0-beta")]
    [InlineData("V1.1.0")]
    [InlineData("v1.1.0 ")]
    public void RefusesBadTags(string tag)
    {
        var r = Edited(j => j["tag_name"] = tag);
        Assert.False(UpdateCheck.HasVersionTag(r));
        Assert.False(UpdateCheck.IsNewer(r, new Version(0, 0, 1)));
        Assert.Equal($"The release's tag \"{tag}\" is not \"v\" and a version such as v1.1.0.", UpdateCheck.RefuseReason(r));
    }

    [Fact]
    public void RefusesDraftOrPrerelease()
    {
        Assert.Equal("The release is a draft.", UpdateCheck.RefuseReason(Edited(j => j["draft"] = true)));
        Assert.Equal("The release is a pre-release.", UpdateCheck.RefuseReason(Edited(j => j["prerelease"] = true)));
    }

    [Theory]
    [InlineData("https://github.com/someone/EarthRelief3D/releases/download/v1.1.0/EarthRelief3D-Setup.exe")]
    [InlineData("https://github.com/johnovanni/EarthWatch3D/releases/download/v1.1.0/EarthRelief3D-Setup.exe")]
    [InlineData("http://github.com/johnovanni/EarthRelief3D/releases/download/v1.1.0/EarthRelief3D-Setup.exe")]
    [InlineData("https://github.com/johnovanni/EarthRelief3D/releases/download/v1.0.9/EarthRelief3D-Setup.exe")]
    [InlineData("https://example.com/johnovanni/EarthRelief3D/releases/download/v1.1.0/EarthRelief3D-Setup.exe")]
    [InlineData("https://github.com/johnovanni/EarthRelief3D/releases/download/v1.1.0/EarthRelief3D-Setup.exe?x=1")]
    public void RefusesForeignOrPlainHttpUrl(string url)
    {
        var r = Edited(j => Asset(j, "EarthRelief3D-Setup.exe")["browser_download_url"] = url);
        Assert.Equal("The installer is not at GitHub's https address for this release: " + new Uri(url), UpdateCheck.RefuseReason(r));
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(-1L)]
    [InlineData(314_572_801L)]
    public void RefusesOversizeOrEmpty(long size)
    {
        var r = Edited(j => Asset(j, "EarthRelief3D-Setup.exe")["size"] = size);
        var reason = UpdateCheck.RefuseReason(r);
        Assert.NotNull(reason);
        Assert.StartsWith("The installer's size (", reason);
        Assert.EndsWith("bytes) is not between 1 byte and 300 MB.", reason);
        Assert.Null(UpdateCheck.RefuseReason(Edited(j => Asset(j, "EarthRelief3D-Setup.exe")["size"] = UpdateCheck.MaxSetupBytes)));
    }

    [Fact]
    public void MissingSetupAssetIsRefused()
    {
        var r = Edited(j => Asset(j, "EarthRelief3D-Setup.exe")["name"] = "EarthRelief3D-Setup-1.1.0.exe");
        Assert.Null(r.Setup);
        Assert.Equal("The release has no file named EarthRelief3D-Setup.exe.", UpdateCheck.RefuseReason(r));
        // It is still newer, so the banner offers it, with its release page.
        Assert.True(UpdateCheck.IsNewer(r, new Version(1, 0, 0)));
        Assert.Equal(new Uri("https://github.com/johnovanni/EarthRelief3D/releases/tag/v1.1.0"), UpdateCheck.SafePage(r));
        // A page that is not on the releases list is never opened: the list is.
        var foreign = Edited(j => j["html_url"] = "https://example.com/johnovanni/EarthRelief3D/releases/tag/v1.1.0");
        Assert.Equal(new Uri(UpdateCheck.ReleasesPage), UpdateCheck.SafePage(foreign));
    }

    [Theory]
    [InlineData("md5:0123456789abcdef0123456789abcdef")]
    [InlineData("sha256:xyz")]
    [InlineData("sha256:fe47cc5ecd60be7bb4c160857d48f1c865f493839680388927d9e7d28669259")]
    [InlineData("SHA256:fe47cc5ecd60be7bb4c160857d48f1c865f493839680388927d9e7d286692599")]
    public void BadDigestFormatIsRefused(string digest)
    {
        var r = Edited(j => Asset(j, "EarthRelief3D-Setup.exe")["digest"] = digest);
        Assert.Null(r.Setup!.Sha256);
        Assert.Equal($"The release's checksum for the installer is not a SHA-256: \"{digest}\".", UpdateCheck.RefuseReason(r));
    }

    [Fact]
    public void NoDigestUsesTheChecksumFileOrIsRefused()
    {
        var r = Edited(j => Asset(j, "EarthRelief3D-Setup.exe").Remove("digest"));
        Assert.Null(r.Setup!.Sha256);
        Assert.Null(UpdateCheck.RefuseReason(r));   // the .sha256 file gives it
        var none = Edited(j =>
        {
            Asset(j, "EarthRelief3D-Setup.exe")["digest"] = null;
            j["assets"]!.AsArray().Remove(Asset(j, "EarthRelief3D-Setup.exe.sha256"));
        });
        Assert.Equal("The release gives no SHA-256 checksum for the installer.", UpdateCheck.RefuseReason(none));
        var foreignSum = Edited(j =>
        {
            Asset(j, "EarthRelief3D-Setup.exe").Remove("digest");
            Asset(j, "EarthRelief3D-Setup.exe.sha256")["browser_download_url"] = "https://example.com/EarthRelief3D-Setup.exe.sha256";
        });
        Assert.StartsWith("The checksum file is not at GitHub's https address", UpdateCheck.RefuseReason(foreignSum));
        Assert.Equal("fe47cc5ecd60be7bb4c160857d48f1c865f493839680388927d9e7d286692599",
            UpdateCheck.ParseChecksumFile("FE47CC5ECD60BE7BB4C160857D48F1C865F493839680388927D9E7D286692599  EarthRelief3D-Setup.exe"));
        Assert.Null(UpdateCheck.ParseChecksumFile("not a checksum"));
        Assert.Null(UpdateCheck.ParseChecksumFile(""));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("[]")]
    [InlineData("{}")]
    [InlineData("{\"tag_name\":\"v1.1.0\"}")]
    [InlineData("{\"message\":\"API rate limit exceeded\",\"documentation_url\":\"https://docs.github.com\"}")]
    public void UnreadableJsonThrowsFormatException(string json) =>
        Assert.Throws<FormatException>(() => UpdateCheck.Parse(json));

    [Fact]
    public void ProgressIsInWholeMegabytes()
    {
        Assert.Equal("12 of 53 MB", UpdateCheck.ProgressText(12L * 1_048_576 + 5, 55_574_528));
        Assert.Equal("0 of 1 MB", UpdateCheck.ProgressText(0, 10));
        Assert.Equal("53 of 53 MB", UpdateCheck.ProgressText(55_574_528, 55_574_528));
    }
}
