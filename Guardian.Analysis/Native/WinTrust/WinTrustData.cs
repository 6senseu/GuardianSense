using System.Runtime.InteropServices;

namespace Guardian.Analysis.Native.WinTrust;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal sealed class WinTrustData : IDisposable
{
    public uint StructSize;

    public IntPtr PolicyCallbackData;

    public IntPtr SIPClientData;

    public WinTrustUiChoice UIChoice;

    public WinTrustRevocationChecks RevocationChecks;

    public WinTrustUnionChoice UnionChoice;

    public IntPtr FileInfoPtr;

    public WinTrustStateAction StateAction;

    public IntPtr StateData;

    public IntPtr URLReference;

    public WinTrustProviderFlags ProviderFlags;

    public WinTrustUiContext UIContext;

    public WinTrustData(WinTrustFileInfo fileInfo)
    {
        ArgumentNullException.ThrowIfNull(fileInfo);

        StructSize =
            (uint)Marshal.SizeOf(typeof(WinTrustData));

        PolicyCallbackData = IntPtr.Zero;
        SIPClientData = IntPtr.Zero;

        UIChoice = WinTrustUiChoice.None;

        RevocationChecks =
            WinTrustRevocationChecks.None;

        UnionChoice =
            WinTrustUnionChoice.File;

        FileInfoPtr =
            Marshal.AllocHGlobal(
                Marshal.SizeOf(typeof(WinTrustFileInfo)));

        Marshal.StructureToPtr(
            fileInfo,
            FileInfoPtr,
            false);

        StateAction =
            WinTrustStateAction.Ignore;

        StateData = IntPtr.Zero;
        URLReference = IntPtr.Zero;

        ProviderFlags =
            WinTrustProviderFlags.RevocationCheckNone;

        UIContext =
            WinTrustUiContext.Execute;
    }

    public void Dispose()
    {
        if (FileInfoPtr == IntPtr.Zero)
        {
            return;
        }

        Marshal.DestroyStructure<WinTrustFileInfo>(
            FileInfoPtr);

        Marshal.FreeHGlobal(FileInfoPtr);

        FileInfoPtr = IntPtr.Zero;

        GC.SuppressFinalize(this);
    }
}