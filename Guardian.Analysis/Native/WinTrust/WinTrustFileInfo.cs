using System.Runtime.InteropServices;

namespace Guardian.Analysis.Native.WinTrust;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal sealed class WinTrustFileInfo
{
    public uint StructSize;

    [MarshalAs(UnmanagedType.LPWStr)]
    public string FilePath;

    public IntPtr FileHandle;

    public IntPtr KnownSubject;

    public WinTrustFileInfo(string filePath)
    {
        StructSize = (uint)Marshal.SizeOf(typeof(WinTrustFileInfo));

        FilePath = filePath;

        FileHandle = IntPtr.Zero;

        KnownSubject = IntPtr.Zero;
    }
}