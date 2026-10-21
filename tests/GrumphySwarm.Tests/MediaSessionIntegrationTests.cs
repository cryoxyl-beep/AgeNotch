using GrumphySwarm.Core.Abstractions;
using GrumphySwarm.Core.Models;
using GrumphySwarm.Core.Settings;
using GrumphySwarm.Core.State;
using GrumphySwarm.Tests.TestDoubles;
using GrumphySwarm.ViewModels;
using Xunit;

namespace GrumphySwarm.Tests;

public class MediaSessionIntegrationTests
{
    private class DummySettingsService : ISettingsService
    {
        public NotchSettings CurrentSettings { get; set; } = new();
        public NotchSettings LoadSettings() => CurrentSettings;
        public Task<NotchSettings> LoadSettingsAsync(CancellationToken cancellationToken = default) => Task.FromResult(CurrentSettings);
        public Task SaveSettingsAsync(NotchSettings settings, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public event EventHandler<NotchSettings>? SettingsChanged { add { } remove { } }
    }

    [Fact]
    public void InitialState_WhenNoMedia_ShowsEmptyIntentionalState()
    {
        var stateMachine = new NotchStateMachine();
        var settingsService = new DummySettingsService();
        var mediaService = new MockMediaSessionService { CurrentTrack = MediaTrackInfo.Empty };

        using var vm = new NotchViewModel(
            stateMachine,
            settingsService,
            mediaSessionService: mediaService);

        Assert.False(vm.HasMedia);
        Assert.Equal("No media playing", vm.MusicTrackTitle);
        Assert.Equal("Play music or video in any app", vm.MusicArtist);
        Assert.False(vm.MediaIsPlaying);
        Assert.False(vm.CanPlayPause);
    }

    [Fact]
    public void MediaTrackChanged_UpdatesViewModelAndMetadata()
    {
        var stateMachine = new NotchStateMachine();
        var settingsService = new DummySettingsService();
        var mediaService = new MockMediaSessionService();

        using var vm = new NotchViewModel(
            stateMachine,
            settingsService,
            mediaSessionService: mediaService);

        var realTrack = new MediaTrackInfo
        {
            HasMedia = true,
            Title = "Levitating",
            Artist = "Dua Lipa",
            Album = "Future Nostalgia",
            SourceAppId = "Spotify",
            IsPlaying = true,
            CanPlayPause = true,
            CanSkipNext = true,
            CanSkipPrevious = true
        };

        // Act
        mediaService.TriggerTrackChange(realTrack);

        // Assert
        Assert.True(vm.HasMedia);
        Assert.Equal("Levitating", vm.MusicTrackTitle);
        Assert.Equal("Dua Lipa", vm.MusicArtist);
        Assert.Equal("Future Nostalgia", vm.MusicAlbum);
        Assert.True(vm.MediaIsPlaying);
        Assert.True(vm.CanPlayPause);
        Assert.True(vm.CanSkipNext);
        Assert.True(vm.CanSkipPrevious);
    }

    [Fact]
    public async Task TransportControls_InvokeMediaServiceMethods()
    {
        var stateMachine = new NotchStateMachine();
        var settingsService = new DummySettingsService();
        var mediaService = new MockMediaSessionService();

        using var vm = new NotchViewModel(
            stateMachine,
            settingsService,
            mediaSessionService: mediaService);

        await vm.ToggleMediaPlayPauseAsync();
        Assert.True(mediaService.PlayPauseInvoked);

        await vm.MediaSkipNextAsync();
        Assert.True(mediaService.SkipNextInvoked);

        await vm.MediaSkipPreviousAsync();
        Assert.True(mediaService.SkipPreviousInvoked);
    }
}
