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
                }

                _currentSession = newSession;

                if (_currentSession != null)
                {
                    _currentSession.MediaPropertiesChanged += OnMediaPropertiesChanged;
                    _currentSession.PlaybackInfoChanged += OnPlaybackInfoChanged;
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

            if (mediaProperties == null || (string.IsNullOrWhiteSpace(mediaProperties.Title) && string.IsNullOrWhiteSpace(mediaProperties.Artist)))
            {
                SetCurrentTrack(new MediaTrackInfo
                {
                    HasMedia = false,
                    Title = "No active track",
                    Artist = session.SourceAppUserModelId ?? "Media paused",
                    Album = string.Empty,
                    SourceAppId = session.SourceAppUserModelId ?? string.Empty,
                    IsPlaying = isPlaying,
                    CanPlayPause = controls?.IsPlayEnabled == true || controls?.IsPauseEnabled == true || controls?.IsPlayPauseToggleEnabled == true,
                    CanSkipNext = controls?.IsNextEnabled == true,
                    CanSkipPrevious = controls?.IsPreviousEnabled == true,
                    ThumbnailBytes = null
                });
                return;
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
                IsPlaying = isPlaying,
                CanPlayPause = controls?.IsPlayEnabled == true || controls?.IsPauseEnabled == true || controls?.IsPlayPauseToggleEnabled == true,
                CanSkipNext = controls?.IsNextEnabled == true,
                CanSkipPrevious = controls?.IsPreviousEnabled == true,
                ThumbnailBytes = thumbBytes
            };

            SetCurrentTrack(trackInfo);
            _logger.LogDebug("GSMTC Track updated: '{Title}' by '{Artist}', Playing={IsPlaying}", trackInfo.Title, trackInfo.Artist, trackInfo.IsPlaying);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while refreshing media properties.");
        }
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

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        lock (_lock)
        {
            if (_currentSession != null)
            {
                _currentSession.MediaPropertiesChanged -= OnMediaPropertiesChanged;
                _currentSession.PlaybackInfoChanged -= OnPlaybackInfoChanged;
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
