using System.Runtime.InteropServices;
using System.Windows;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Win32;

namespace GrumphySwarm.Interop;

/// <summary>
/// Monitors display resolution, DPI changes, and multi-monitor setup to calculate
/// accurate notch placement on the primary display.
/// </summary>
public sealed class DisplayManager : IDisposable
{
    private readonly ILogger<DisplayManager> _logger;
    private bool _disposed;

    public event EventHandler? DisplayConfigurationChanged;

    public DisplayManager(ILogger<DisplayManager>? logger = null)
    {
        _logger = logger ?? NullLogger<DisplayManager>.Instance;
        SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
    }

    /// <summary>
    /// Gets the primary display width in WPF device-independent pixels (DIPs).
    /// </summary>
    public double PrimaryScreenWidthDips => SystemParameters.PrimaryScreenWidth;

    /// <summary>
    /// Gets the primary display height in WPF device-independent pixels (DIPs).
    /// </summary>
    public double PrimaryScreenHeightDips => SystemParameters.PrimaryScreenHeight;

    /// <summary>
    /// Calculates the horizontal Left position required to center a given width
    /// on the primary monitor in WPF DIPs.
    /// </summary>
    public double CalculateCenteredLeft(double targetWidth)
    {
        return CalculateLeft(targetWidth, GrumphySwarm.Core.Settings.NotchHorizontalPosition.Center, 0.0);
    }

    /// <summary>
    /// Calculates the horizontal Left position on the primary monitor in WPF DIPs
    /// based on specified alignment (Left, Center, Right, Custom) and horizontal offset.
    /// Enforces bounds to prevent the notch from slipping off-screen.
    /// </summary>
    public double CalculateLeft(double targetWidth, GrumphySwarm.Core.Settings.NotchHorizontalPosition position, double offsetDips)
    {
        var screenWidth = PrimaryScreenWidthDips;
        double maxLeft = Math.Max(0.0, screenWidth - targetWidth);

        double baseLeft = position switch
        {
            GrumphySwarm.Core.Settings.NotchHorizontalPosition.Left => 24.0,
            GrumphySwarm.Core.Settings.NotchHorizontalPosition.Right => Math.Max(0.0, screenWidth - targetWidth - 24.0),
            GrumphySwarm.Core.Settings.NotchHorizontalPosition.Custom => (screenWidth - targetWidth) / 2.0,
            _ => (screenWidth - targetWidth) / 2.0
        };

        double targetLeft = baseLeft + offsetDips;
        return Math.Clamp(targetLeft, 0.0, maxLeft);
    }

    /// <summary>
    /// Retrieves physical monitor metrics for the primary display via Win32.
    /// </summary>
    public NativeMethods.RECT GetPrimaryMonitorPhysicalBounds()
    {
        var hMonitor = NativeMethods.MonitorFromWindow(IntPtr.Zero, NativeMethods.MONITOR_DEFAULTTOPRIMARY);
        var info = new NativeMethods.MONITORINFO
        {
            cbSize = Marshal.SizeOf<NativeMethods.MONITORINFO>()
        };

        if (NativeMethods.GetMonitorInfo(hMonitor, ref info))
        {
            return info.rcMonitor;
        }

        return default;
    }

    private void OnDisplaySettingsChanged(object? sender, EventArgs e)
    {
        _logger.LogInformation("Display settings or resolution changed. Notifying listeners.");
        DisplayConfigurationChanged?.Invoke(this, EventArgs.Empty);
    }

    public void NotifyConfigurationChanged()
    {
        DisplayConfigurationChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
    }
}
