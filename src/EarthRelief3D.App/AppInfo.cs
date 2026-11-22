using System.Reflection;

namespace EarthRelief3D.App;

public static class AppInfo
{
    /// <summary>The app's name as shown to the user (message boxes, the crash message) and its data folder name.</summary>
    public const string Name = "EarthRelief3D";

    public const string ProjectPage = "https://github.com/johnovanni/EarthRelief3D";

    /// <summary>The informational version without the "+commit" build metadata, e.g. "0.1.0".</summary>
    public static string Version { get; } = GetVersion();

    /// <summary>Sent with every request. It names the app and its project page only: no personal data.</summary>
    public static string UserAgent { get; } = $"{Name}/{Version} (+{ProjectPage})";

    /// <summary>Start of each day's log file name in the logs folder (earthrelief3d-yyyyMMdd.log).</summary>
    public const string LogFilePrefix = "earthrelief3d-";

    /// <summary>The tooltip note on an idle feed (none of the Plan 2 feeds skips runs, but the scheduler needs one).</summary>
    public const string FeedIdleNote = "Not polled right now";

    private static string GetVersion()
    {
        var info = typeof(AppInfo).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        var plus = info?.IndexOf('+') ?? -1;
        return plus >= 0 ? info![..plus] : info ?? "0.0.0";
    }
}
