namespace GrumphySwarm.Core.Abstractions;

/// <summary>
/// Event arguments for real-time audio volume and mute notifications.
/// </summary>
public sealed class VolumeChangedEventArgs : EventArgs
{
    public int VolumePercent { get; }
    public bool IsMuted { get; }

    public VolumeChangedEventArgs(int volumePercent, bool isMuted)
    {
        VolumePercent = Math.Clamp(volumePercent, 0, 100);
        IsMuted = isMuted;
    }
}

/// <summary>
/// Service interface for event-driven Windows system master volume and mute monitoring/control.
/// </summary>
public interface IAudioVolumeService : IDisposable
{
    /// <summary>
    /// Occurs in real-time when the master volume or mute state changes via keys, mixer, or apps.
    /// </summary>
    event EventHandler<VolumeChangedEventArgs>? VolumeChanged;

    /// <summary>
    /// Current volume level percentage [0 - 100].
    /// </summary>
    int CurrentVolume { get; }

    /// <summary>
    /// Whether the master audio output is currently muted.
    /// </summary>
    bool IsMuted { get; }

    /// <summary>
    /// Sets the master output volume level percentage [0 - 100].
    /// </summary>
    Task SetVolumeAsync(int volumePercent);

    /// <summary>
    /// Toggles the master output mute state.
    /// </summary>
    Task ToggleMuteAsync();
}
