using System.Runtime.InteropServices;
using Xunit;

namespace GrumphySwarm.Tests;

public class NativeMethodsTests
{
    [Fact]
    public void PInvoke_User32_Exports_Exist()
    {
        var h = NativeMethodsTestsHelper.GetModuleHandle("user32.dll");
        Assert.NotEqual(IntPtr.Zero, h);

        var pGetWithoutW = NativeMethodsTestsHelper.GetProcAddress(h, "GetWindowLongPtr");
        // "GetWindowLongPtr" does NOT exist in user32.dll!
        Assert.Equal(IntPtr.Zero, pGetWithoutW);

        var pGet = NativeMethodsTestsHelper.GetProcAddress(h, "GetWindowLongPtrW");
        Assert.NotEqual(IntPtr.Zero, pGet);

        var pSet = NativeMethodsTestsHelper.GetProcAddress(h, "SetWindowLongPtrW");
        Assert.NotEqual(IntPtr.Zero, pSet);
    }
}

internal static class NativeMethodsTestsHelper
{
    [DllImport("kernel32.dll", CharSet = CharSet.Ansi)]
    public static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
    public static extern IntPtr GetModuleHandle(string lpModuleName);
}
