namespace EarthRelief3D.Core.Updates;

/// <summary>
/// One file of a GitHub release, as <c>/releases/latest</c> lists it (P5-20). <paramref name="Sha256"/> is the lower-case hex of
/// GitHub's <c>digest</c> ("sha256:…"), or null when the release gives none or gives one that is not a SHA-256 (then
/// <see cref="RawDigest"/> holds what it gave, and the release is refused).
/// </summary>
public sealed record ReleaseAsset(string Name, long Size, Uri DownloadUrl, string? Sha256)
{
    /// <summary>The <c>digest</c> text as GitHub gave it, when it is not "sha256:" and 64 hex digits; null otherwise.</summary>
    public string? RawDigest { get; init; }
}

/// <summary>
/// GitHub's newest release of EarthRelief3D (P5-18, P5-20). <paramref name="Version"/> comes from the tag <c>v&lt;major&gt;.&lt;minor&gt;.&lt;patch&gt;</c>,
/// and is 0.0.0 when the tag is not of that form (<see cref="UpdateCheck.RefuseReason"/> then says so). <paramref name="Setup"/> is the
/// asset named exactly <c>EarthRelief3D-Setup.exe</c>, <paramref name="Checksum"/> the one named <c>EarthRelief3D-Setup.exe.sha256</c>;
/// each is null when the release lacks it.
/// </summary>
public sealed record ReleaseInfo(Version Version, string Tag, DateTime PublishedUtc, Uri Page, bool Draft, bool Prerelease,
                                 ReleaseAsset? Setup, ReleaseAsset? Checksum);
