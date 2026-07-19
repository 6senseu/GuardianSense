using System.Runtime.InteropServices;

namespace Guardian.Analysis.Native.WinTrust;

internal static class WinTrustNative
{
    [DllImport(
        "wintrust.dll",
        ExactSpelling = true,
        CharSet = CharSet.Unicode,
        SetLastError = true)]
    internal static extern int WinVerifyTrust(
        IntPtr hwnd,
        [MarshalAs(UnmanagedType.LPStruct)] Guid actionId,
        WinTrustData winTrustData);
}