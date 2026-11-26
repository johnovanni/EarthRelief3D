using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace EarthRelief3D.App.Services;

/// <summary>An Authenticode check of a file (P5-20). Only reads the file; never runs it.</summary>
public interface ISignatureCheck
{
    /// <summary>The subject of the certificate that signed <paramref name="path"/>; null for an unsigned (or missing) file.</summary>
    string? SignerSubject(string path);

    /// <summary>True when Windows trusts the file's Authenticode signature (WinVerifyTrust).</summary>
    bool IsTrusted(string path);
}

/// <summary>
/// The real check: <c>WinVerifyTrust</c> in <c>wintrust.dll</c> with <c>WINTRUST_ACTION_GENERIC_VERIFY_V2</c> and no UI, and the
/// signer's subject from <see cref="X509Certificate.CreateFromSignedFile"/>. Revocation is not checked (decision D10-3 in the
/// task report): the installer's SHA-256 is pinned by GitHub's answer over https first, and a revocation server that cannot be
/// reached would otherwise stop every update.
/// </summary>
public sealed class WinTrustSignatureCheck : ISignatureCheck
{
    public string? SignerSubject(string path)
    {
        if (!File.Exists(path)) return null;
        try
        {
#pragma warning disable SYSLIB0057 // the documented way to read an Authenticode signer; the file is only read
            using var cert = X509Certificate.CreateFromSignedFile(path);
#pragma warning restore SYSLIB0057
            return string.IsNullOrEmpty(cert.Subject) ? null : cert.Subject;
        }
        catch (CryptographicException) { return null; }
    }

    public bool IsTrusted(string path)
    {
        if (!File.Exists(path)) return false;
        var pathPtr = Marshal.StringToHGlobalUni(path);
        var fileInfo = new WintrustFileInfo { cbStruct = (uint)Marshal.SizeOf<WintrustFileInfo>(), pcwszFilePath = pathPtr };
        var filePtr = Marshal.AllocHGlobal(Marshal.SizeOf<WintrustFileInfo>());
        try
        {
            Marshal.StructureToPtr(fileInfo, filePtr, false);
            var data = new WintrustData
            {
                cbStruct = (uint)Marshal.SizeOf<WintrustData>(),
                dwUIChoice = WtdUiNone,
                fdwRevocationChecks = WtdRevokeNone,
                dwUnionChoice = WtdChoiceFile,
                pFile = filePtr,
                dwStateAction = WtdStateActionVerify,
                dwProvFlags = WtdRevocationCheckNone | WtdDisableMd2Md4,
            };
            var action = GenericVerifyV2;
            int result = WinVerifyTrust(InvalidHandle, ref action, ref data);
            data.dwStateAction = WtdStateActionClose;   // frees the state the verify kept
            WinVerifyTrust(InvalidHandle, ref action, ref data);
            return result == 0;
        }
        finally
        {
            Marshal.FreeHGlobal(filePtr);
            Marshal.FreeHGlobal(pathPtr);
        }
    }

    private static readonly IntPtr InvalidHandle = new(-1);
    private static readonly Guid GenericVerifyV2 = new("00AAC56B-CD44-11d0-8CC2-00C04FC295EE");
    private const uint WtdUiNone = 2, WtdRevokeNone = 0, WtdChoiceFile = 1, WtdStateActionVerify = 1, WtdStateActionClose = 2;
    private const uint WtdRevocationCheckNone = 0x10, WtdDisableMd2Md4 = 0x2000;

    [StructLayout(LayoutKind.Sequential)]
    private struct WintrustFileInfo
    {
        public uint cbStruct;
        public IntPtr pcwszFilePath;
        public IntPtr hFile;
        public IntPtr pgKnownSubject;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WintrustData
    {
        public uint cbStruct;
        public IntPtr pPolicyCallbackData;
        public IntPtr pSIPClientData;
        public uint dwUIChoice;
        public uint fdwRevocationChecks;
        public uint dwUnionChoice;
        public IntPtr pFile;
        public uint dwStateAction;
        public IntPtr hWVTStateData;
        public IntPtr pwszURLReference;
        public uint dwProvFlags;
        public uint dwUIContext;
        public IntPtr pSignatureSettings;
    }

    [DllImport("wintrust.dll", ExactSpelling = true, SetLastError = false)]
    private static extern int WinVerifyTrust(IntPtr hwnd, ref Guid pgActionId, ref WintrustData pWvtData);
}
