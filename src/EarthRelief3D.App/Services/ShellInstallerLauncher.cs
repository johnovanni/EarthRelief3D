using System.ComponentModel;
using System.Diagnostics;
using System.IO;

namespace EarthRelief3D.App.Services;

/// <summary>Starts a downloaded, checked installer (P5-21). Tests use a fake: nothing downloaded is ever run by a test.</summary>
public interface IInstallerLauncher
{
    /// <summary>True once Windows has started <paramref name="path"/>; false (with a sentence in <paramref name="error"/>) when it has not.</summary>
    bool TryStart(string path, string arguments, out string? error);
}

/// <summary>
/// The real launcher: the shell with the verb <c>runas</c>, so Windows asks the user's permission (the installer needs administrator
/// rights). A refused permission (Win32 error 1223, "cancelled by the user") is <see cref="PermissionRefusedText"/>.
/// </summary>
public sealed class ShellInstallerLauncher : IInstallerLauncher
{
    public const string PermissionRefusedText = "Windows did not give permission, so the update was not installed.";
    private const int ErrorCancelled = 1223;

    public bool TryStart(string path, string arguments, out string? error)
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo(path, arguments)
            {
                UseShellExecute = true,
                Verb = "runas",
                WorkingDirectory = Path.GetDirectoryName(path) ?? "",
            });
            error = null;
            return true;
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == ErrorCancelled)
        {
            error = PermissionRefusedText;
            return false;
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException or IOException)
        {
            error = "Windows could not start the installer: " + ex.Message.TrimEnd('.') + ".";
            return false;
        }
    }
}
