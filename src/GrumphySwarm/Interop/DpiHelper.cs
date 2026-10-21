using System.Windows;
using System.Windows.Media;

namespace GrumphySwarm.Interop;

/// <summary>
/// Provides utility methods for converting coordinates and sizes between
/// Windows physical pixels and WPF device-independent pixels (DIPs).
/// </summary>
public static class DpiHelper
{
    public const double StandardDpi = 96.0;

    /// <summary>
    /// Gets the DPI scale for a visual element or defaults to 1.0 (96 DPI).
    /// </summary>
    public static (double ScaleX, double ScaleY) GetDpiScale(Visual? visual)
    {
        if (visual == null) return (1.0, 1.0);

        try
        {
            var dpi = VisualTreeHelper.GetDpi(visual);
            return (dpi.DpiScaleX, dpi.DpiScaleY);
        }
        catch
        {
            return (1.0, 1.0);
        }
    }

    /// <summary>
    /// Converts physical pixels to device-independent pixels (DIPs).
    /// </summary>
    public static double PixelsToDips(int pixels, double scale)
    {
        if (scale <= 0.0) return pixels;
        return pixels / scale;
    }

    /// <summary>
    /// Converts device-independent pixels (DIPs) to physical pixels.
    /// </summary>
    public static int DipsToPixels(double dips, double scale)
    {
        return (int)Math.Round(dips * scale);
    }
}
