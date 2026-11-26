using System.IO;
using System.Text.RegularExpressions;

namespace EarthRelief3D.Tests;

/// <summary>Plan 5 Task 9: the release workflow, the installer script, the icon and the version agree with each other (P5-1, P5-4, P5-5, P5-6, P5-16).</summary>
public sealed class ReleasePackagingTests
{
    private static readonly string[] SignedNames =
    [
        "EarthRelief3D.App.exe", "EarthRelief3D.App.dll", "EarthRelief3D.Core.dll", "AstroCore.dll", "GlobeCore.dll", "GlobeCore.Wpf.dll", "GlobeCore.Earth.dll",
    ];

    private static string Read(params string[] parts) => File.ReadAllText(Path.Combine([Repo.Root(), .. parts]));
    private static string Workflow() => Read(".github", "workflows", "release.yml");
    private static string Iss() => Read("packaging", "installer", "EarthRelief3D.iss");

    /// <summary>The text of the step called <paramref name="name"/>, up to the next step.</summary>
    private static string Step(string name)
    {
        var text = Workflow();
        var start = text.IndexOf($"- name: {name}", StringComparison.Ordinal);
        Assert.True(start >= 0, $"No step '{name}'");
        var next = text.IndexOf("\n      - ", start + 1, StringComparison.Ordinal);
        return next < 0 ? text[start..] : text[start..next];
    }

    private static List<string> PublishFiles(string text) =>
        Regex.Matches(text, @"publish\\([A-Za-z0-9.]+)").Select(m => m.Groups[1].Value).Order().ToList();

    [Fact]
    public void TagMustMatchVersion()
    {
        var step = Step("Read version");
        Assert.Contains("Directory.Build.props", step);
        Assert.Contains("github.ref_type }}' -eq 'tag'", step);
        Assert.Contains("'${{ github.ref_name }}' -ne \"v$v\"", step);
        Assert.Contains("throw", step);
    }

    [Fact]
    public void VerifiedFilesAreTheSignedFiles()
    {
        var signed = PublishFiles(Step("Sign app files"));
        var verified = PublishFiles(Step("Verify signatures"));
        Assert.Equal(SignedNames.Order().ToList(), signed);
        Assert.Equal(signed, verified);
        Assert.Contains("env.SETUP", Step("Sign installer"));
        Assert.Contains(@"out\$env:SETUP",Step("Verify signatures"));
    }

    [Fact]
    public void PullRequestsDoNotSign()
    {
        var text = Workflow();
        Assert.Contains("SIGN: ${{ github.event_name != 'pull_request' }}", text);
        Assert.Contains("environment: ${{ github.event_name != 'pull_request' && 'release' || '' }}", text);
        foreach (var name in new[] { "Sign in to Azure", "Sign app files", "Sign installer", "Verify signatures" })
            Assert.Contains("if: env.SIGN == 'true'", Step(name));
        // Nothing but those steps names Azure.
        var azureSteps = Regex.Matches(text, @"uses: (azure|Azure)/").Count;
        Assert.Equal(3, azureSteps);
        Assert.Contains("paths: ['.github/workflows/release.yml', 'packaging/**', 'src/**']", text);
    }

    [Fact]
    public void CheckoutFallsBackToTheBuiltInToken()
    {
        var text = Workflow();
        Assert.Contains("submodules: recursive", text);
        Assert.Contains("token: ${{ secrets.SUBMODULES_TOKEN || github.token }}", text);
    }

    [Fact]
    public void TestsRunBeforePublish()
    {
        var text = Workflow();
        var test = text.IndexOf("dotnet test EarthRelief3D.sln -c Release", StringComparison.Ordinal);
        var publish = text.IndexOf("dotnet publish src/EarthRelief3D.App", StringComparison.Ordinal);
        var setup = text.IndexOf("actions/setup-dotnet", StringComparison.Ordinal);
        Assert.True(setup >= 0 && setup < test, "tests must run after setup-dotnet");
        Assert.True(test < publish, "tests must run before publish");
        Assert.True(text.IndexOf("dotnet publish", StringComparison.Ordinal) == publish);
    }

    [Fact]
    public void InstallerNamesTheBuiltExe()
    {
        var iss = Iss();
        var csproj = Read("src", "EarthRelief3D.App", "EarthRelief3D.App.csproj");
        var assembly = Regex.Match(csproj, "<AssemblyName>([^<]+)</AssemblyName>") is { Success: true } m ? m.Groups[1].Value : "EarthRelief3D.App";
        Assert.Equal(assembly + ".exe", Regex.Match(iss, @"#define AppExe ""([^""]+)""").Groups[1].Value);
        var baseName = Regex.Match(iss, @"OutputBaseFilename=(\S+)").Groups[1].Value;
        Assert.Equal(baseName + ".exe", Regex.Match(Workflow(), @"SETUP: (\S+)").Groups[1].Value);
        var id = Regex.Match(iss, @"AppId=\{\{([0-9A-Fa-f-]+)\}").Groups[1].Value;
        Assert.True(Guid.TryParse(id, out _), "AppId is not a GUID");
        Assert.NotEqual("F5ED128E-15B1-403B-9C11-F81F253842FE", id.ToUpperInvariant());
        Assert.Contains(@"#define AppName ""EarthRelief3D""", iss);
        Assert.Contains(@"DefaultDirName={autopf}\Qapow\{#AppName}", iss);
        Assert.Contains("AppPublisher=John Apolloni", iss);
        Assert.True(File.Exists(Path.Combine(Repo.Root(), "src", "EarthRelief3D.App", "app.ico")));
    }

    [Fact]
    public void IconHasEverySize()
    {
        var bytes = File.ReadAllBytes(Path.Combine(Repo.Root(), "src", "EarthRelief3D.App", "app.ico"));
        Assert.Equal(0, BitConverter.ToUInt16(bytes, 0));
        Assert.Equal(1, BitConverter.ToUInt16(bytes, 2));
        var count = BitConverter.ToUInt16(bytes, 4);
        var sizes = Enumerable.Range(0, count).Select(i => bytes[6 + i * 16] == 0 ? 256 : bytes[6 + i * 16]).Order().ToList();
        Assert.Equal([16, 24, 32, 48, 64, 128, 256], sizes);
    }

    [Fact]
    public void VersionIsOneZeroZero()
    {
        var props = Read("Directory.Build.props");
        Assert.Matches(@"<Version>1\.0\.0</Version>", props);
        Assert.Contains("installer", props);
    }

    /// <summary>P5-21: a silent update (the in-app updater) starts the app again as the user; an interactive install keeps its "Launch" box.</summary>
    [Fact]
    public void SilentUpdateRestartsTheAppAsTheUser()
    {
        var text = Iss().Replace("\r\n", "\n", StringComparison.Ordinal);
        var exe = Regex.Match(text, @"#define AppExe ""([^""]+)""").Groups[1].Value;
        Assert.Equal("EarthRelief3D.App.exe", exe);
        var start = text.IndexOf("[Run]", StringComparison.Ordinal);
        Assert.True(start >= 0, "No [Run] section");
        var next = text.IndexOf("\n[", start + 1, StringComparison.Ordinal);
        var lines = (next < 0 ? text[start..] : text[start..next]).Split('\n').Select(l => l.Trim())
            .Where(l => l.StartsWith("Filename:", StringComparison.Ordinal)).Select(l => l.Replace("{#AppExe}", exe, StringComparison.Ordinal)).ToList();
        Assert.Equal(2, lines.Count);
        static string[] Flags(string line) => Regex.Match(line, @"Flags: ([^;]+)").Groups[1].Value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        Assert.StartsWith(@"Filename: ""{app}\EarthRelief3D.App.exe""", lines[0]);
        Assert.Contains("skipifsilent", Flags(lines[0]));
        Assert.Contains("postinstall", Flags(lines[0]));
        Assert.StartsWith(@"Filename: ""{app}\EarthRelief3D.App.exe""", lines[1]);
        Assert.Equal(["nowait", "runasoriginaluser", "skipifnotsilent"], Flags(lines[1]).Order().ToArray());
    }

    /// <summary>
    /// M5: Restart Manager closes the app but does not restart it (RestartApplications=no), so the only restart is the [Run] entry,
    /// as the user. M8: each install starts from a fresh {app}\data tree (the user's data is in %LOCALAPPDATA%, never in {app}).
    /// </summary>
    [Fact]
    public void UpdatesRestartOnlyThroughRunAndReplaceTheDataTree()
    {
        var text = Iss().Replace("\r\n", "\n", StringComparison.Ordinal);
        static string Section(string iss, string name)
        {
            var start = iss.IndexOf("\n[" + name + "]\n", StringComparison.Ordinal);
            Assert.True(start >= 0, $"No [{name}] section");
            var next = iss.IndexOf("\n[", start + 1, StringComparison.Ordinal);
            return next < 0 ? iss[start..] : iss[start..next];
        }
        var setup = Section(text, "Setup");
        Assert.Matches(@"(?m)^CloseApplications=yes$", setup);
        Assert.Matches(@"(?m)^RestartApplications=no$", setup);
        var delete = Section(text, "InstallDelete").Split('\n').Select(l => l.Trim()).Where(l => l.StartsWith("Type:", StringComparison.Ordinal)).ToList();
        Assert.Equal([@"Type: filesandordirs; Name: ""{app}\data"""], delete);
        // Nothing the user keeps is under {app}: settings, caches and logs are in %LOCALAPPDATA%.
        Assert.Contains(@"%LOCALAPPDATA%\EarthRelief3D", text);
    }

    /// <summary>The updater's rules are in the release notes for the owner: never delete or rename the two assets of a published release.</summary>
    [Fact]
    public void ReleaseDocNamesTheUpdatersAssets()
    {
        var doc = Read("docs", "release.md");
        Assert.Contains("## How the app finds a new version", doc);
        Assert.Contains("Never delete or rename the two assets of a published release", doc);
        Assert.Contains("EarthRelief3D-Setup.exe.sha256", doc);
        Assert.Contains(EarthRelief3D.Core.Updates.UpdateCheck.LatestUrl, doc);
    }
}
