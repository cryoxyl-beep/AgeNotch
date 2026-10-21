namespace GrumphySwarm.Core.Models;

/// <summary>
/// Snapshot of live media metadata, playback timeline, and transport controls
/// from the Windows System Media Manager.
/// </summary>
public sealed class MediaTrackInfo
{
    public static readonly MediaTrackInfo Empty = new()
    {
        HasMedia = false,
        Title = "No media playing",
        Artist = "Play music or video in any app",
        Album = string.Empty,
        SourceAppId = string.Empty,
        SourceAppName = "Media Player",
        SourceAppBadgeKey = "IconGenericMusicBadge",
        IsPlaying = false,
        CanPlayPause = false,
        CanSkipNext = false,
        CanSkipPrevious = false,
        HasTimeline = false,
        Position = TimeSpan.Zero,
        Duration = TimeSpan.Zero,
        PositionText = "--:--",
        RemainingText = "--:--",
        Progress = 0.0,
        ThumbnailBytes = null
    };

    public bool HasMedia { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Artist { get; init; } = string.Empty;
    public string Album { get; init; } = string.Empty;
    public string SourceAppId { get; init; } = string.Empty;
    public string SourceAppName { get; init; } = "Media Player";
    public string SourceAppBadgeKey { get; init; } = "IconGenericMusicBadge";
    public bool IsPlaying { get; init; }
    public bool CanPlayPause { get; init; }
    public bool CanSkipNext { get; init; }
    public bool CanSkipPrevious { get; init; }
    public bool HasTimeline { get; init; }
    public TimeSpan Position { get; init; } = TimeSpan.Zero;
    public TimeSpan Duration { get; init; } = TimeSpan.Zero;
    public string PositionText { get; init; } = "--:--";
    public string RemainingText { get; init; } = "--:--";
    public double Progress { get; init; } = 0.0;
    public byte[]? ThumbnailBytes { get; init; }
}
