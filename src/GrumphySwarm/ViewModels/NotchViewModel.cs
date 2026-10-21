using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GrumphySwarm.Core.Abstractions;
using GrumphySwarm.Core.Models;
using GrumphySwarm.Core.Settings;
using GrumphySwarm.Core.State;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace GrumphySwarm.ViewModels;

/// <summary>
/// Main ViewModel driving the Notch user interface, live system integrations (Volume, Brightness, GSMTC Media),
/// and local Agent workspace management.
/// </summary>
public sealed partial class NotchViewModel : ObservableObject, IDisposable
{
    private readonly INotchStateMachine _stateMachine;
    private readonly ISettingsService _settingsService;
    private readonly IAudioVolumeService? _audioVolumeService;
    private readonly IBrightnessService? _brightnessService;
    private readonly IMediaSessionService? _mediaSessionService;
    private readonly IAgentProfileService? _agentProfileService;
    private readonly ISystemClock _clock;
    private readonly ILogger<NotchViewModel> _logger;

    private readonly DispatcherTimer _clockTimer;
    private readonly DispatcherTimer _collapseDelayTimer;
    private readonly DispatcherTimer _temporaryStatusTimer;
    private bool _disposed;

    [ObservableProperty]
    private NotchState _currentState;

    [ObservableProperty]
    private bool _isExpanded;

    [ObservableProperty]
    private bool _isPinned;

    [ObservableProperty]
    private bool _isPendingCollapse;

    [ObservableProperty]
    private string _currentTimeText = "12:00";

    [ObservableProperty]
    private string _currentDateMonthText = "Aug";

    [ObservableProperty]
    private string _currentDateDayText = "08";

    [ObservableProperty]
    private string _currentDateWeekdayText = "THU";

    [ObservableProperty]
    private string _activeTab = "Nook";

    [ObservableProperty]
    private string _calendarEventTitle = "Nothing for today";

    // --- Silhouette & Dimensions ---
    [ObservableProperty]
    private double _collapsedWidth;

    [ObservableProperty]
    private double _collapsedHeight;

    [ObservableProperty]
    private double _expandedWidth;

    [ObservableProperty]
    private double _expandedHeight;

    [ObservableProperty]
    private double _cornerRadiusCollapsed;

    [ObservableProperty]
    private double _cornerRadiusExpanded;

    [ObservableProperty]
    private double _flareRadiusCollapsed = 10.0;

    [ObservableProperty]
    private double _flareRadiusExpanded = 12.0;

    // --- Live System Volume ---
    [ObservableProperty]
    private int _volumePercent = 50;

    [ObservableProperty]
    private bool _isMuted;

    [ObservableProperty]
    private string _volumeText = "50%";

    [ObservableProperty]
    private string _volumeIconKey = "IconVolumeGeometry";

    // --- Live Screen Brightness ---
    [ObservableProperty]
    private int _brightnessPercent = 50;

    [ObservableProperty]
    private bool _isBrightnessSupported;

    [ObservableProperty]
    private string _brightnessText = "50%";

    [ObservableProperty]
    private string _brightnessHardwareInfo = string.Empty;

    // --- Contextual Temporary Status HUD (Volume / Brightness) ---
    [ObservableProperty]
    private bool _isTemporaryStatusActive;

    [ObservableProperty]
    private string _temporaryStatusTitle = "Volume";

    [ObservableProperty]
    private string _temporaryStatusValue = "50%";

    [ObservableProperty]
    private string _temporaryStatusIconKey = "IconVolumeGeometry";

    [ObservableProperty]
    private double _temporaryStatusProgress = 0.5;

    [ObservableProperty]
    private bool _isTemporaryStatusMuted;

    // --- Real System Media (GSMTC) ---
    [ObservableProperty]
    private bool _hasMedia;

    [ObservableProperty]
    private string _musicTrackTitle = "No media playing";

    [ObservableProperty]
    private string _musicArtist = "Play music or video in any app";

    [ObservableProperty]
    private string _musicAlbum = string.Empty;

    [ObservableProperty]
    private bool _mediaIsPlaying;

    [ObservableProperty]
    private bool _canPlayPause;

    [ObservableProperty]
    private bool _canSkipNext;

    [ObservableProperty]
    private bool _canSkipPrevious;

    [ObservableProperty]
    private ImageSource? _mediaThumbnailSource;

    // --- Agents Workspace ---
    public ObservableCollection<AgentItemViewModel> AgentProfiles { get; } = new();

    [ObservableProperty]
    private bool _hasNoAgents;

    [ObservableProperty]
    private bool _isEditingAgent;

    [ObservableProperty]
    private string _editorAgentId = string.Empty;

    [ObservableProperty]
    private string _editorAgentName = string.Empty;

    [ObservableProperty]
    private string _editorAgentDescription = string.Empty;

    [ObservableProperty]
    private AgentTaskType _editorAgentTaskType = AgentTaskType.DesktopAssistant;

    [ObservableProperty]
    private string _editorTitle = "Create Agent";

    public NotchViewModel(
        INotchStateMachine stateMachine,
        ISettingsService settingsService,
        IAudioVolumeService? audioVolumeService = null,
        IBrightnessService? brightnessService = null,
        IMediaSessionService? mediaSessionService = null,
        IAgentProfileService? agentProfileService = null,
        ISystemClock? clock = null,
        ILogger<NotchViewModel>? logger = null)
    {
        _stateMachine = stateMachine ?? throw new ArgumentNullException(nameof(stateMachine));
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _audioVolumeService = audioVolumeService;
        _brightnessService = brightnessService;
        _mediaSessionService = mediaSessionService;
        _agentProfileService = agentProfileService;
        _clock = clock ?? new SystemClock();
        _logger = logger ?? NullLogger<NotchViewModel>.Instance;

        // Collapse delay timer
        _collapseDelayTimer = new DispatcherTimer(DispatcherPriority.Normal)
        {
            Interval = TimeSpan.FromMilliseconds(_settingsService.CurrentSettings.HoverCollapseDelayMs)
        };
        _collapseDelayTimer.Tick += OnCollapseDelayTimerTick;

        // Clock timer: updates live time every second
        _clockTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _clockTimer.Tick += (_, _) => UpdateClock();
        _clockTimer.Start();
        UpdateClock();

        // Temporary Status Reset Timer (2.2 seconds)
        _temporaryStatusTimer = new DispatcherTimer(DispatcherPriority.Normal)
        {
            Interval = TimeSpan.FromMilliseconds(2200)
        };
        _temporaryStatusTimer.Tick += (_, _) =>
        {
            _temporaryStatusTimer.Stop();
            IsTemporaryStatusActive = false;
        };

        // Apply settings
        ApplySettings(_settingsService.CurrentSettings);
        _settingsService.SettingsChanged += (_, s) => ApplySettings(s);

        // Synchronize with state machine
        _currentState = _stateMachine.CurrentState;
        UpdateDerivedStateProperties(_currentState);

        _stateMachine.StateChanged += OnStateMachineStateChanged;
        _stateMachine.PendingCollapseChanged += OnStateMachinePendingCollapseChanged;

        // Bind Live Services
        InitializeAudioService();
        InitializeBrightnessService();
        InitializeMediaService();
        _ = LoadAgentProfilesAsync();
    }

    private void ApplySettings(NotchSettings settings)
    {
        CollapsedWidth = settings.CollapsedWidth;
        CollapsedHeight = settings.CollapsedHeight;
        ExpandedWidth = settings.ExpandedWidth;
        ExpandedHeight = settings.ExpandedHeight;
        CornerRadiusCollapsed = settings.CornerRadiusCollapsed;
        CornerRadiusExpanded = settings.CornerRadiusExpanded;
        FlareRadiusCollapsed = settings.FlareRadiusCollapsed;
        FlareRadiusExpanded = settings.FlareRadiusExpanded;

        if (_collapseDelayTimer != null)
        {
            _collapseDelayTimer.Interval = TimeSpan.FromMilliseconds(settings.HoverCollapseDelayMs);
        }
    }

    private void UpdateClock()
    {
        var now = _clock.Now;
        CurrentTimeText = now.ToString("HH:mm");
        CurrentDateMonthText = now.ToString("MMM");
        CurrentDateDayText = now.ToString("dd");
        CurrentDateWeekdayText = now.ToString("ddd").ToUpperInvariant();
    }

    // --- Audio Volume Integration ---
    private void InitializeAudioService()
    {
        if (_audioVolumeService == null) return;

        UpdateAudioState(_audioVolumeService.CurrentVolume, _audioVolumeService.IsMuted, showTemporaryHud: false);
        _audioVolumeService.VolumeChanged += (_, e) =>
        {
            SafeDispatch(() => UpdateAudioState(e.VolumePercent, e.IsMuted, showTemporaryHud: true));
        };
    }

    private void UpdateAudioState(int volumePercent, bool isMuted, bool showTemporaryHud)
    {
        VolumePercent = volumePercent;
        IsMuted = isMuted;
        VolumeText = isMuted ? "Muted" : $"{volumePercent}%";
        VolumeIconKey = isMuted ? "IconVolumeMuteGeometry" : "IconVolumeGeometry";

        if (showTemporaryHud)
        {
            ShowTemporaryStatus(
                title: "Volume",
                value: VolumeText,
                iconKey: VolumeIconKey,
                progress: isMuted ? 0.0 : volumePercent / 100.0,
                isMuted: isMuted);
        }
    }

    // --- Screen Brightness Integration ---
    private void InitializeBrightnessService()
    {
        if (_brightnessService == null) return;

        IsBrightnessSupported = _brightnessService.IsSupported;
        BrightnessPercent = _brightnessService.CurrentBrightness;
        BrightnessText = $"{BrightnessPercent}%";
        BrightnessHardwareInfo = _brightnessService.HardwareDescription;

        _brightnessService.BrightnessChanged += (_, e) =>
        {
            SafeDispatch(() =>
            {
                BrightnessPercent = e.BrightnessPercent;
                BrightnessText = $"{e.BrightnessPercent}%";

                ShowTemporaryStatus(
                    title: "Brightness",
                    value: BrightnessText,
                    iconKey: "IconBrightnessGeometry",
                    progress: e.BrightnessPercent / 100.0,
                    isMuted: false);
            });
        };
    }

    // --- Temporary HUD Display ---
    private void ShowTemporaryStatus(string title, string value, string iconKey, double progress, bool isMuted)
    {
        TemporaryStatusTitle = title;
        TemporaryStatusValue = value;
        TemporaryStatusIconKey = iconKey;
        TemporaryStatusProgress = Math.Clamp(progress, 0.0, 1.0);
        IsTemporaryStatusMuted = isMuted;

        IsTemporaryStatusActive = true;

        // Reset and restart dismissal timer cleanly
        _temporaryStatusTimer.Stop();
        _temporaryStatusTimer.Start();
    }

    // --- Real System Media Integration ---
    private void InitializeMediaService()
    {
        if (_mediaSessionService == null) return;

        ApplyMediaTrackInfo(_mediaSessionService.CurrentTrack);
        _mediaSessionService.MediaTrackChanged += (_, track) =>
        {
            SafeDispatch(() => ApplyMediaTrackInfo(track));
        };
    }

    private void ApplyMediaTrackInfo(MediaTrackInfo track)
    {
        HasMedia = track.HasMedia;
        MusicTrackTitle = track.Title;
        MusicArtist = track.Artist;
        MusicAlbum = track.Album;
        MediaIsPlaying = track.IsPlaying;
        CanPlayPause = track.CanPlayPause;
        CanSkipNext = track.CanSkipNext;
        CanSkipPrevious = track.CanSkipPrevious;
        MediaThumbnailSource = CreateBitmapFromBytes(track.ThumbnailBytes);
    }

    private static BitmapImage? CreateBitmapFromBytes(byte[]? bytes)
    {
        if (bytes == null || bytes.Length == 0) return null;
        try
        {
            using var stream = new MemoryStream(bytes);
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = stream;
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }
        catch
        {
            return null;
        }
    }

    [RelayCommand]
    public async Task ToggleMediaPlayPauseAsync()
    {
        if (_mediaSessionService != null)
        {
            await _mediaSessionService.TryPlayPauseAsync();
        }
    }

    [RelayCommand]
    public async Task MediaSkipNextAsync()
    {
        if (_mediaSessionService != null)
        {
            await _mediaSessionService.TrySkipNextAsync();
        }
    }

    [RelayCommand]
    public async Task MediaSkipPreviousAsync()
    {
        if (_mediaSessionService != null)
        {
            await _mediaSessionService.TrySkipPreviousAsync();
        }
    }

    // --- Agents Workspace Integration ---
    private async Task LoadAgentProfilesAsync()
    {
        if (_agentProfileService == null) return;

        var profiles = await _agentProfileService.GetProfilesAsync();
        SafeDispatch(() =>
        {
            AgentProfiles.Clear();
            foreach (var profile in profiles)
            {
                AgentProfiles.Add(new AgentItemViewModel(profile, OpenEditAgent, DeleteAgentProfile));
            }
            HasNoAgents = AgentProfiles.Count == 0;
        });

        _agentProfileService.ProfilesChanged += async (_, _) =>
        {
            var updated = await _agentProfileService.GetProfilesAsync();
            SafeDispatch(() =>
            {
                AgentProfiles.Clear();
                foreach (var profile in updated)
                {
                    AgentProfiles.Add(new AgentItemViewModel(profile, OpenEditAgent, DeleteAgentProfile));
                }
                HasNoAgents = AgentProfiles.Count == 0;
            });
        };
    }

    [RelayCommand]
    public void OpenCreateAgent()
    {
        EditorTitle = "Create New Agent Profile";
        EditorAgentId = string.Empty;
        EditorAgentName = string.Empty;
        EditorAgentDescription = string.Empty;
        EditorAgentTaskType = AgentTaskType.DesktopAssistant;
        IsEditingAgent = true;
    }

    public void OpenEditAgent(AgentItemViewModel item)
    {
        EditorTitle = "Edit Agent Profile";
        EditorAgentId = item.Id;
        EditorAgentName = item.Name;
        EditorAgentDescription = item.Description;
        EditorAgentTaskType = item.TaskType;
        IsEditingAgent = true;
    }

    [RelayCommand]
    public async Task SaveAgentProfileAsync()
    {
        if (string.IsNullOrWhiteSpace(EditorAgentName))
        {
            EditorAgentName = "Unnamed Agent";
        }

        var profile = new AgentProfile
        {
            Id = string.IsNullOrWhiteSpace(EditorAgentId) ? Guid.NewGuid().ToString("N") : EditorAgentId,
            Name = EditorAgentName.Trim(),
            Description = EditorAgentDescription.Trim(),
            TaskType = EditorAgentTaskType,
            StatusNote = "Standby (Execution Not Connected)"
        };

        if (_agentProfileService != null)
        {
            await _agentProfileService.SaveProfileAsync(profile);
        }

        IsEditingAgent = false;
    }

    [RelayCommand]
    public void CancelAgentEdit()
    {
        IsEditingAgent = false;
    }

    public void DeleteAgentProfile(AgentItemViewModel item)
    {
        if (_agentProfileService != null && item != null)
        {
            _ = _agentProfileService.DeleteProfileAsync(item.Id);
        }
    }

    // --- State Machine & UI Navigation ---
    private void OnStateMachineStateChanged(object? sender, NotchStateChangedEventArgs e)
    {
        CurrentState = e.NewState;
        UpdateDerivedStateProperties(e.NewState);
    }

    private void UpdateDerivedStateProperties(NotchState state)
    {
        IsExpanded = state is NotchState.Expanded or NotchState.Pinned or NotchState.EditMode;
        IsPinned = state == NotchState.Pinned;

        if (state != NotchState.Expanded)
        {
            _collapseDelayTimer?.Stop();
            IsPendingCollapse = false;
        }
    }

    private void OnStateMachinePendingCollapseChanged(object? sender, bool isPending)
    {
        IsPendingCollapse = isPending;
        if (isPending)
        {
            _collapseDelayTimer.Stop();
            _collapseDelayTimer.Start();
        }
        else
        {
            _collapseDelayTimer.Stop();
        }
    }

    private void OnCollapseDelayTimerTick(object? sender, EventArgs e)
    {
        _collapseDelayTimer.Stop();
        _stateMachine.Fire(NotchTransitionTrigger.CollapseDelayElapsed);
    }

    [RelayCommand]
    public void HandleMouseEnter()
    {
        _stateMachine.Fire(NotchTransitionTrigger.MouseEnter);
    }

    [RelayCommand]
    public void HandleMouseLeave()
    {
        _stateMachine.Fire(NotchTransitionTrigger.MouseLeave);
    }

    [RelayCommand]
    public void TogglePin()
    {
        _stateMachine.Fire(NotchTransitionTrigger.TogglePin);
    }

    [RelayCommand]
    public void ToggleExpand()
    {
        if (IsExpanded)
        {
            _stateMachine.Fire(NotchTransitionTrigger.ForceCollapse);
        }
        else
        {
            _stateMachine.Fire(NotchTransitionTrigger.ForceExpand);
        }
    }

    [RelayCommand]
    public void ForceCollapse()
    {
        _stateMachine.Fire(NotchTransitionTrigger.ForceCollapse);
    }

    [RelayCommand]
    public void ForceExpand()
    {
        _stateMachine.Fire(NotchTransitionTrigger.ForceExpand);
    }

    [RelayCommand]
    public void SelectTab(string tab)
    {
        ActiveTab = tab;
        _logger.LogInformation("Tab selected: {Tab}", tab);
    }

    [RelayCommand]
    public void Exit()
    {
        Application.Current?.Shutdown();
    }

    private static void SafeDispatch(Action action)
    {
        if (Application.Current?.Dispatcher is Dispatcher dispatcher && !dispatcher.CheckAccess())
        {
            dispatcher.InvokeAsync(action);
        }
        else
        {
            action();
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _clockTimer.Stop();
        _collapseDelayTimer.Stop();
        _temporaryStatusTimer.Stop();

        _stateMachine.StateChanged -= OnStateMachineStateChanged;
        _stateMachine.PendingCollapseChanged -= OnStateMachinePendingCollapseChanged;

        _audioVolumeService?.Dispose();
        _brightnessService?.Dispose();
        _mediaSessionService?.Dispose();
    }
}
