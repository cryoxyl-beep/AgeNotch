namespace GrumphySwarm.Core.Abstractions;

/// <summary>
/// Event arguments for screen brightness notifications.
/// </summary>
public sealed class BrightnessChangedEventArgs : EventArgs
{
    public int BrightnessPercent { get; }

    public BrightnessChangedEventArgs(int brightnessPercent)
    {
        BrightnessPercent = Math.Clamp(brightnessPercent, 0, 100);
    }
}

/// <summary>
/// Service interface for screen brightness detection and notifications on supported displays.
/// </summary>
public interface IBrightnessService : IDisposable
{
    /// <summary>
    /// Occurs when hardware screen brightness changes.
    /// </summary>
    event EventHandler<BrightnessChangedEventArgs>? BrightnessChanged;

    /// <summary>
    /// Current screen brightness percentage [0 - 100].
    /// </summary>
    int CurrentBrightness { get; }

    /// <summary>
    /// Whether screen brightness querying and control is supported on this machine's display hardware.
    /// </summary>
    bool IsSupported { get; }

    /// <summary>
    /// Hardware device or interface descriptor (e.g. WMI Monitor, DDC/CI, or Unsupported).
    /// </summary>
    string HardwareDescription { get; }

    /// <summary>
    /// Sets the screen brightness percentage [0 - 100] on supported hardware.
    /// </summary>
    Task SetBrightnessAsync(int brightnessPercent);
}
