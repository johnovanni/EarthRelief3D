using System.ComponentModel;
using System.Diagnostics;
using System.IO;

namespace EarthRelief3D.App.Services;

/// <summary>Starts a downloaded, checked installer (P5-21, ruling R11). Tests use a fake: nothing downloaded is ever run by a test.</summary>
public interface IInstallerLauncher
{
    /// <summary>
    /// True once Windows has started <paramref name="path"/>, with <paramref name="exited"/> completing with the installer's exit code
    /// when it ends (null when Windows gives no process to watch); false, with a sentence in <paramref name="error"/>, when it has not.
    /// </summary>
    bool TryStart(string path, string arguments, out Task<int>? exited, out string? error);
}

/// <summary>
/// The real launcher (ruling R11): an ordinary shell start, without the <c>runas</c> verb. The installer (Inno Setup,
/// <c>PrivilegesRequired=admin</c>) then asks Windows for permission itself, so "the user that started Setup" is the user, not an
/// administrator, and its <c>runasoriginaluser</c> entry starts EarthRelief3D again as that user. Its exit is watched: if it ends
/// while the app is still open, the update was not installed.
/// </summary>
public sealed class ShellInstallerLauncher : IInstallerLauncher
{
    /// <summary>How the installer is started: the shell, no verb (so not elevated by the app), in its own folder.</summary>
    internal static ProcessStartInfo StartInfo(string path, string arguments) => new(path, arguments)
    {
        UseShellExecute = true,
        WorkingDirectory = Path.GetDirectoryName(path) ?? "",
    };

    public bool TryStart(string path, string arguments, out Task<int>? exited, out string? error)
    {
        try
        {
            var process = Process.Start(StartInfo(path, arguments));
            exited = process is null ? null : ExitCodeAsync(process);
            error = null;
            return true;
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException or IOException)
        {
            exited = null;
            error = "Windows could not start the installer: " + ex.Message.Trim().TrimEnd('.') + ".";
            return false;
        }
    }

    private static async Task<int> ExitCodeAsync(Process process)
    {
        using (process)
        {
            await process.WaitForExitAsync().ConfigureAwait(false);
            return process.ExitCode;
        }
    }
}
