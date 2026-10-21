namespace GrumphySwarm.Core.Models;

/// <summary>
/// Snapshot of live media metadata and transport controls from the Windows System Media Manager.
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
        IsPlaying = false,
        CanPlayPause = false,
        CanSkipNext = false,
        CanSkipPrevious = false,
        ThumbnailBytes = null
    };

    public bool HasMedia { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Artist { get; init; } = string.Empty;
    public string Album { get; init; } = string.Empty;
    public string SourceAppId { get; init; } = string.Empty;
    public bool IsPlaying { get; init; }
    public bool CanPlayPause { get; init; }
    public bool CanSkipNext { get; init; }
    public bool CanSkipPrevious { get; init; }
    public byte[]? ThumbnailBytes { get; init; }
}
