using Windows.Media.Control;
using Windows.Storage.Streams;
using GrumphySwarm.Core.Abstractions;
using GrumphySwarm.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace GrumphySwarm.Services;

/// <summary>
/// Event-driven Windows System Media Transport Controls (GSMTC) service.
/// Connects to real media playback sessions across Spotify, Apple Music, Edge, Chrome, Firefox, and media players.
/// </summary>
public sealed class WindowsMediaSessionService : IMediaSessionService
{
    private readonly ILogger<WindowsMediaSessionService> _logger;
    private readonly object _lock = new();

    private GlobalSystemMediaTransportControlsSessionManager? _sessionManager;
    private GlobalSystemMediaTransportControlsSession? _currentSession;
    private MediaTrackInfo _currentTrack = MediaTrackInfo.Empty;
    private Timer? _playbackProgressTimer;
    private bool _disposed;

    public event EventHandler<MediaTrackInfo>? MediaTrackChanged;

    public MediaTrackInfo CurrentTrack
    {
        get
        {
            lock (_lock) return _currentTrack;
        }
        private set
        {
            lock (_lock) _currentTrack = value;
        }
    }

    public WindowsMediaSessionService(ILogger<WindowsMediaSessionService>? logger = null)
    {
        _logger = logger ?? NullLogger<WindowsMediaSessionService>.Instance;
        _ = InitializeSessionManagerAsync();

        // 1-second progress advancement timer for smooth timeline updates during active playback
        _playbackProgressTimer = new Timer(OnProgressTimerTick, null, 1000, 1000);
    }

    private async Task InitializeSessionManagerAsync()
    {
        try
        {
            _sessionManager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
            if (_sessionManager == null)
            {
                _logger.LogWarning("GlobalSystemMediaTransportControlsSessionManager is not available.");
                return;
            }

            _sessionManager.CurrentSessionChanged += OnCurrentSessionChanged;
            _sessionManager.SessionsChanged += OnSessionsChanged;

            _logger.LogInformation("GSMTC SessionManager initialized.");
            await UpdateCurrentSessionAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize GSMTC SessionManager.");
        }
    }

    private void OnCurrentSessionChanged(GlobalSystemMediaTransportControlsSessionManager sender, CurrentSessionChangedEventArgs args)
    {
        _ = UpdateCurrentSessionAsync();
    }

    private void OnSessionsChanged(GlobalSystemMediaTransportControlsSessionManager sender, SessionsChangedEventArgs args)
    {
        _ = UpdateCurrentSessionAsync();
    }

    private async Task UpdateCurrentSessionAsync()
    {
        if (_disposed || _sessionManager == null) return;

        try
        {
            GlobalSystemMediaTransportControlsSession? newSession = null;
            try
            {
                newSession = _sessionManager.GetCurrentSession();
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Could not acquire current GSMTC session.");
            }

            lock (_lock)
            {
                if (_currentSession != null)
                {
                    _currentSession.MediaPropertiesChanged -= OnMediaPropertiesChanged;
                    _currentSession.PlaybackInfoChanged -= OnPlaybackInfoChanged;
                    _currentSession.TimelinePropertiesChanged -= OnTimelinePropertiesChanged;
                }

                _currentSession = newSession;

                if (_currentSession != null)
                {
                    _currentSession.MediaPropertiesChanged += OnMediaPropertiesChanged;
                    _currentSession.PlaybackInfoChanged += OnPlaybackInfoChanged;
                    _currentSession.TimelinePropertiesChanged += OnTimelinePropertiesChanged;
                }
            }

            await RefreshMediaPropertiesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating current GSMTC session.");
        }
    }

    private void OnMediaPropertiesChanged(GlobalSystemMediaTransportControlsSession sender, MediaPropertiesChangedEventArgs args)
    {
        _ = RefreshMediaPropertiesAsync();
    }

    private void OnPlaybackInfoChanged(GlobalSystemMediaTransportControlsSession sender, PlaybackInfoChangedEventArgs args)
    {
        _ = RefreshMediaPropertiesAsync();
    }

    private void OnTimelinePropertiesChanged(GlobalSystemMediaTransportControlsSession sender, TimelinePropertiesChangedEventArgs args)
    {
        _ = RefreshMediaPropertiesAsync();
    }

    private async Task RefreshMediaPropertiesAsync()
    {
        if (_disposed) return;

        try
        {
            GlobalSystemMediaTransportControlsSession? session;
            lock (_lock) session = _currentSession;

            if (session == null)
            {
                SetCurrentTrack(MediaTrackInfo.Empty);
                return;
            }

            var playbackInfo = session.GetPlaybackInfo();
            var controls = playbackInfo?.Controls;
            bool isPlaying = playbackInfo?.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;

            GlobalSystemMediaTransportControlsSessionMediaProperties? mediaProperties = null;
            try
            {
                mediaProperties = await session.TryGetMediaPropertiesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "TryGetMediaPropertiesAsync failed or session closed.");
            }

            // Map source application identifier
            var (sourceAppName, sourceAppBadgeKey) = ResolveSourceAppInfo(session.SourceAppUserModelId);

            if (mediaProperties == null || (string.IsNullOrWhiteSpace(mediaProperties.Title) && string.IsNullOrWhiteSpace(mediaProperties.Artist)))
            {
                SetCurrentTrack(new MediaTrackInfo
                {
                    HasMedia = false,
                    Title = "No active track",
                    Artist = sourceAppName,
                    Album = string.Empty,
                    SourceAppId = session.SourceAppUserModelId ?? string.Empty,
                    SourceAppName = sourceAppName,
                    SourceAppBadgeKey = sourceAppBadgeKey,
                    IsPlaying = isPlaying,
                    CanPlayPause = controls?.IsPlayEnabled == true || controls?.IsPauseEnabled == true || controls?.IsPlayPauseToggleEnabled == true,
                    CanSkipNext = controls?.IsNextEnabled == true,
                    CanSkipPrevious = controls?.IsPreviousEnabled == true,
                    HasTimeline = false,
                    ThumbnailBytes = null
                });
                return;
            }

            // Timeline extraction
            bool hasTimeline = false;
            TimeSpan position = TimeSpan.Zero;
            TimeSpan duration = TimeSpan.Zero;
            string posText = "--:--";
            string remText = "--:--";
            double progress = 0.0;

            try
            {
                var timeline = session.GetTimelineProperties();
                if (timeline != null && timeline.EndTime > TimeSpan.Zero)
                {
                    hasTimeline = true;
                    position = timeline.Position;
                    duration = timeline.EndTime;
                    posText = FormatTimeSpan(position);
                    var remaining = duration > position ? duration - position : TimeSpan.Zero;
                    remText = "-" + FormatTimeSpan(remaining);
                    progress = Math.Clamp(position.TotalSeconds / Math.Max(1.0, duration.TotalSeconds), 0.0, 1.0);
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Could not acquire timeline properties.");
            }

            byte[]? thumbBytes = null;
            if (mediaProperties.Thumbnail != null)
            {
                try
                {
                    using var stream = await mediaProperties.Thumbnail.OpenReadAsync();
                    if (stream != null && stream.Size > 0)
                    {
                        thumbBytes = new byte[stream.Size];
                        using var dataReader = new DataReader(stream.GetInputStreamAt(0));
                        await dataReader.LoadAsync((uint)stream.Size);
                        dataReader.ReadBytes(thumbBytes);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Failed to read album art thumbnail stream.");
                }
            }

            var trackInfo = new MediaTrackInfo
            {
                HasMedia = true,
                Title = string.IsNullOrWhiteSpace(mediaProperties.Title) ? "Unknown Title" : mediaProperties.Title,
                Artist = string.IsNullOrWhiteSpace(mediaProperties.Artist) ? "Unknown Artist" : mediaProperties.Artist,
                Album = mediaProperties.AlbumTitle ?? string.Empty,
                SourceAppId = session.SourceAppUserModelId ?? string.Empty,
                SourceAppName = sourceAppName,
                SourceAppBadgeKey = sourceAppBadgeKey,
                IsPlaying = isPlaying,
                CanPlayPause = controls?.IsPlayEnabled == true || controls?.IsPauseEnabled == true || controls?.IsPlayPauseToggleEnabled == true,
                CanSkipNext = controls?.IsNextEnabled == true,
                CanSkipPrevious = controls?.IsPreviousEnabled == true,
                HasTimeline = hasTimeline,
                Position = position,
                Duration = duration,
                PositionText = posText,
                RemainingText = remText,
                Progress = progress,
                ThumbnailBytes = thumbBytes
            };

            SetCurrentTrack(trackInfo);
            _logger.LogDebug("GSMTC Track updated: '{Title}' by '{Artist}' ({App}), Playing={IsPlaying}, Time={Pos}/{Dur}",
                trackInfo.Title, trackInfo.Artist, trackInfo.SourceAppName, trackInfo.IsPlaying, trackInfo.PositionText, trackInfo.Duration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while refreshing media properties.");
        }
    }

    private void OnProgressTimerTick(object? state)
    {
        if (_disposed) return;

        MediaTrackInfo current;
        lock (_lock) current = _currentTrack;

        if (current.HasMedia && current.IsPlaying && current.HasTimeline && current.Duration > TimeSpan.Zero)
        {
            var nextPos = current.Position + TimeSpan.FromSeconds(1);
            if (nextPos > current.Duration) nextPos = current.Duration;

            var remaining = current.Duration > nextPos ? current.Duration - nextPos : TimeSpan.Zero;
            double progress = Math.Clamp(nextPos.TotalSeconds / Math.Max(1.0, current.Duration.TotalSeconds), 0.0, 1.0);

            var updated = new MediaTrackInfo
            {
                HasMedia = current.HasMedia,
                Title = current.Title,
                Artist = current.Artist,
                Album = current.Album,
                SourceAppId = current.SourceAppId,
                SourceAppName = current.SourceAppName,
                SourceAppBadgeKey = current.SourceAppBadgeKey,
                IsPlaying = current.IsPlaying,
                CanPlayPause = current.CanPlayPause,
                CanSkipNext = current.CanSkipNext,
                CanSkipPrevious = current.CanSkipPrevious,
                HasTimeline = true,
                Position = nextPos,
                Duration = current.Duration,
                PositionText = FormatTimeSpan(nextPos),
                RemainingText = "-" + FormatTimeSpan(remaining),
                Progress = progress,
                ThumbnailBytes = current.ThumbnailBytes
            };

            SetCurrentTrack(updated);
        }
    }

    private static (string Name, string BadgeKey) ResolveSourceAppInfo(string? appId)
    {
        if (string.IsNullOrWhiteSpace(appId))
        {
            return ("Media Player", "IconGenericMusicBadge");
        }

        var lower = appId.ToLowerInvariant();
        if (lower.Contains("spotify"))
        {
            return ("Spotify", "IconSpotifyBadge");
        }
        if (lower.Contains("apple") || lower.Contains("itunes"))
        {
            return ("Apple Music", "IconAppleMusicBadge");
        }
        if (lower.Contains("chrome"))
        {
            return ("Google Chrome", "IconBrowserBadge");
        }
        if (lower.Contains("edge"))
        {
            return ("Microsoft Edge", "IconBrowserBadge");
        }
        if (lower.Contains("firefox"))
        {
            return ("Firefox", "IconBrowserBadge");
        }

        var clean = appId.Replace(".exe", "", StringComparison.OrdinalIgnoreCase);
        int bang = clean.IndexOf('!');
        if (bang >= 0 && bang < clean.Length - 1)
        {
            clean = clean[(bang + 1)..];
        }

        return (clean, "IconGenericMusicBadge");
    }

    private static string FormatTimeSpan(TimeSpan ts)
    {
        if (ts.TotalHours >= 1)
        {
            return $"{(int)ts.TotalHours}:{ts.Minutes:D2}:{ts.Seconds:D2}";
        }
        return $"{ts.Minutes}:{ts.Seconds:D2}";
    }

    private void SetCurrentTrack(MediaTrackInfo track)
    {
        CurrentTrack = track;
        MediaTrackChanged?.Invoke(this, track);
    }

    public async Task<bool> TryPlayPauseAsync()
    {
        try
        {
            GlobalSystemMediaTransportControlsSession? session;
            lock (_lock) session = _currentSession;
            if (session != null)
            {
                return await session.TryTogglePlayPauseAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "TryPlayPauseAsync failed.");
        }
        return false;
    }

    public async Task<bool> TrySkipNextAsync()
    {
        try
        {
            GlobalSystemMediaTransportControlsSession? session;
            lock (_lock) session = _currentSession;
            if (session != null)
            {
                return await session.TrySkipNextAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "TrySkipNextAsync failed.");
        }
        return false;
    }

    public async Task<bool> TrySkipPreviousAsync()
    {
        try
        {
            GlobalSystemMediaTransportControlsSession? session;
            lock (_lock) session = _currentSession;
            if (session != null)
            {
                return await session.TrySkipPreviousAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "TrySkipPreviousAsync failed.");
        }
        return false;
    }

    public async Task<bool> TrySeekAsync(TimeSpan position)
    {
        try
        {
            GlobalSystemMediaTransportControlsSession? session;
            lock (_lock) session = _currentSession;
            if (session != null)
            {
                return await session.TryChangePlaybackPositionAsync(position.Ticks);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "TrySeekAsync failed.");
        }
        return false;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _playbackProgressTimer?.Dispose();
        _playbackProgressTimer = null;

        lock (_lock)
        {
            if (_currentSession != null)
            {
                _currentSession.MediaPropertiesChanged -= OnMediaPropertiesChanged;
                _currentSession.PlaybackInfoChanged -= OnPlaybackInfoChanged;
                _currentSession.TimelinePropertiesChanged -= OnTimelinePropertiesChanged;
                _currentSession = null;
            }

            if (_sessionManager != null)
            {
                _sessionManager.CurrentSessionChanged -= OnCurrentSessionChanged;
                _sessionManager.SessionsChanged -= OnSessionsChanged;
                _sessionManager = null;
            }
        }
    }
}
