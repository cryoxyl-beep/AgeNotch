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
        var screenWidth = PrimaryScreenWidthDips;
        var left = (screenWidth - targetWidth) / 2.0;
        return Math.Max(0.0, left);
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
