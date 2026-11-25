using System.IO;
using EarthRelief3D.App.Services;

namespace EarthRelief3D.Tests;

/// <summary>Plan 5 Task 10 (P5-20): the real Authenticode check (WinVerifyTrust) and signer subject. Files are only read, never run.</summary>
public sealed class WinTrustSignatureCheckTests
{
    [Fact]
    public void UnsignedFileHasNoSigner()
    {
        var copy = Path.Combine(Path.GetTempPath(), "er3d-unsigned-" + Guid.NewGuid().ToString("N") + ".dll");
        File.Copy(typeof(WinTrustSignatureCheckTests).Assembly.Location, copy);
        try
        {
            var check = new WinTrustSignatureCheck();
            Assert.Null(check.SignerSubject(copy));
            Assert.False(check.IsTrusted(copy));
            Assert.Null(check.SignerSubject(copy + ".missing"));
            Assert.False(check.IsTrusted(copy + ".missing"));
        }
        finally { File.Delete(copy); }
    }

    [Fact]
    public void MicrosoftSignedFileIsTrusted()
    {
        var dotnet = DotnetExe();
        var check = new WinTrustSignatureCheck();
        Assert.True(check.IsTrusted(dotnet), dotnet);
        Assert.Contains("Microsoft Corporation", check.SignerSubject(dotnet));
    }

    /// <summary>The dotnet.exe running the tests (or the one DOTNET_ROOT or the default install names).</summary>
    private static string DotnetExe()
    {
        var candidates = new List<string?>();
        if (Environment.ProcessPath is { } p && Path.GetFileName(p).Equals("dotnet.exe", StringComparison.OrdinalIgnoreCase)) candidates.Add(p);
        if (Environment.GetEnvironmentVariable("DOTNET_ROOT") is { Length: > 0 } root) candidates.Add(Path.Combine(root, "dotnet.exe"));
        if (Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") is { Length: > 0 } host) candidates.Add(host);
        candidates.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "dotnet", "dotnet.exe"));
        return candidates.FirstOrDefault(File.Exists) ?? throw new FileNotFoundException("dotnet.exe not found");
    }
}
