$code = @'
using System;
using System.Drawing;
using System.Runtime.InteropServices;

public class ScreenCapture {
    [DllImport("user32.dll")]
    public static extern IntPtr GetDesktopWindow();
    [DllImport("user32.dll")]
    public static extern IntPtr GetWindowDC(IntPtr hWnd);
    [DllImport("user32.dll")]
    public static extern IntPtr ReleaseDC(IntPtr hWnd, IntPtr hDC);
    [DllImport("gdi32.dll")]
    public static extern bool BitBlt(IntPtr hObject, int nXDest, int nYDest, int nWidth, int nHeight, IntPtr hObjectSource, int nXSrc, int nYSrc, int dwRop);

    public static void Capture(string path, int width, int height) {
        IntPtr hDesk = GetDesktopWindow();
        IntPtr hDC = GetWindowDC(hDesk);
        using (Bitmap bmp = new Bitmap(width, height)) {
            using (Graphics g = Graphics.FromImage(bmp)) {
                IntPtr hDest = g.GetHdc();
                BitBlt(hDest, 0, 0, width, height, hDC, 0, 0, 0x00CC0020);
                g.ReleaseHdc(hDest);
            }
            bmp.Save(path, System.Drawing.Imaging.ImageFormat.Png);
        }
        ReleaseDC(hDesk, hDC);
    }
}
'@

Add-Type -TypeDefinition $code -ReferencedAssemblies System.Drawing

[ScreenCapture]::Capture("c:\Users\SANTHOSH\Documents\antigravity\focused-darwin\current_screenshot.png", 1536, 350)
Write-Host "Captured successfully!"
