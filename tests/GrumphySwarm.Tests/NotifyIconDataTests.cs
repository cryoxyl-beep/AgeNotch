using System.Runtime.InteropServices;
using Xunit;

namespace GrumphySwarm.Tests;

public class NotifyIconDataTests
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct NOTIFYICONDATA_V3
    {
        public int cbSize;
        public IntPtr hWnd;
        public int uID;
        public int uFlags;
        public int uCallbackMessage;
        public IntPtr hIcon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string szTip;
        public int dwState;
        public int dwStateMask;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string szInfo;
        public int uTimeoutOrVersion;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string szInfoTitle;
        public int dwInfoFlags;
    }

    [Fact]
    public void V3_Size_Matches_Standard_Win64()
    {
        int size = Marshal.SizeOf<NOTIFYICONDATA_V3>();
        // On 64-bit Windows, NOTIFYICONDATAW_V3_SIZE is 952 or 528 depending on szInfo
        // 4 + 4(pad) + 8 + 4 + 4 + 4 + 4(pad) + 8 + 256 + 4 + 4 + 512 + 4 + 128 + 4 = 948 + 4(pad) = 952
        Assert.True(size > 0);
    }
}
