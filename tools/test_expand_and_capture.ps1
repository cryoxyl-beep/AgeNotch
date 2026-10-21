Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

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
        CaptureRegion(path, 0, 0, width, height);
    }

    public static void CaptureRegion(string path, int x, int y, int width, int height) {
        IntPtr hDesk = GetDesktopWindow();
        IntPtr hDC = GetWindowDC(hDesk);
        using (Bitmap bmp = new Bitmap(width, height)) {
            using (Graphics g = Graphics.FromImage(bmp)) {
                IntPtr hDest = g.GetHdc();
                BitBlt(hDest, 0, 0, width, height, hDC, x, y, 0x00CC0020);
                g.ReleaseHdc(hDest);
            }
            bmp.Save(path, System.Drawing.Imaging.ImageFormat.Png);
        }
        ReleaseDC(hDesk, hDC);
    }
}
'@

if (-not ([System.Management.Automation.PSTypeName]'ScreenCapture').Type) {
    Add-Type -TypeDefinition $code -ReferencedAssemblies System.Drawing
}

# Kill any existing instance
Get-Process -Name GrumphySwarm -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 500

# Start published app
$proc = Start-Process -FilePath "c:\Users\SANTHOSH\Documents\antigravity\focused-darwin\publish\win-x64\GrumphySwarm.exe" -PassThru
Start-Sleep -Seconds 2

# Move mouse to top-center of primary screen to expand the notch
$bounds = [System.Windows.Forms.Screen]::PrimaryScreen.Bounds
$centerX = [int]($bounds.Width / 2)
[System.Windows.Forms.Cursor]::Position = New-Object System.Drawing.Point($centerX, 10)
Start-Sleep -Milliseconds 800

# Move down slightly inside the expanded notch body to keep it expanded
[System.Windows.Forms.Cursor]::Position = New-Object System.Drawing.Point($centerX, 50)
Start-Sleep -Milliseconds 600

# Capture screenshot
$fullPath = "c:\Users\SANTHOSH\Documents\antigravity\focused-darwin\desktop_notch_full.png"
[ScreenCapture]::Capture($fullPath, $bounds.Width, 260)

$cropX = [Math]::Max(0, [int]($centerX - 410))
$outPath = "c:\Users\SANTHOSH\Documents\antigravity\focused-darwin\current_screenshot.png"
[ScreenCapture]::CaptureRegion($outPath, $cropX, 0, 820, 200)
Write-Host "Captured desktop notch to $outPath (X: $cropX, Width: 820, Height: 200)"
