using GrumphySwarm.Core.Models;

namespace GrumphySwarm.Core.Abstractions;

/// <summary>
/// Service interface for event-driven Windows system media playback and transport control monitoring.
/// </summary>
public interface IMediaSessionService : IDisposable
{
    /// <summary>
    /// Occurs when active media session or metadata (track, artist, playback status) changes.
    /// </summary>
    event EventHandler<MediaTrackInfo>? MediaTrackChanged;

    /// <summary>
    /// Current live media track snapshot.
    /// </summary>
    MediaTrackInfo CurrentTrack { get; }

    /// <summary>
    /// Toggles play/pause on the active media session.
    /// </summary>
    Task<bool> TryPlayPauseAsync();

    /// <summary>
    /// Skips to the next track on the active media session.
    /// </summary>
    Task<bool> TrySkipNextAsync();

    /// <summary>
    /// Skips to the previous track on the active media session.
    /// </summary>
    Task<bool> TrySkipPreviousAsync();
}
