using GrumphySwarm.Core.Abstractions;
using GrumphySwarm.Core.Models;

namespace GrumphySwarm.Tests.TestDoubles;

public sealed class MockAudioVolumeService : IAudioVolumeService
{
    public event EventHandler<VolumeChangedEventArgs>? VolumeChanged;

    public int CurrentVolume { get; set; } = 50;
    public bool IsMuted { get; set; } = false;

    public bool IsDisposed { get; private set; }

    public void TriggerVolumeChange(int volume, bool isMuted)
    {
        CurrentVolume = volume;
        IsMuted = isMuted;
        VolumeChanged?.Invoke(this, new VolumeChangedEventArgs(volume, isMuted));
    }

    public Task SetVolumeAsync(int volumePercent)
    {
        TriggerVolumeChange(volumePercent, IsMuted);
        return Task.CompletedTask;
    }

    public Task ToggleMuteAsync()
    {
        TriggerVolumeChange(CurrentVolume, !IsMuted);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        IsDisposed = true;
    }
}

public sealed class MockBrightnessService : IBrightnessService
{
    public event EventHandler<BrightnessChangedEventArgs>? BrightnessChanged;

    public int CurrentBrightness { get; set; } = 60;
    public bool IsSupported { get; set; } = true;
    public string HardwareDescription { get; set; } = "Mock Internal Display";
    public bool IsDisposed { get; private set; }

    public void TriggerBrightnessChange(int brightness)
    {
        CurrentBrightness = brightness;
        BrightnessChanged?.Invoke(this, new BrightnessChangedEventArgs(brightness));
    }

    public Task SetBrightnessAsync(int brightnessPercent)
    {
        TriggerBrightnessChange(brightnessPercent);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        IsDisposed = true;
    }
}

public sealed class MockMediaSessionService : IMediaSessionService
{
    public event EventHandler<MediaTrackInfo>? MediaTrackChanged;

    public MediaTrackInfo CurrentTrack { get; set; } = MediaTrackInfo.Empty;
    public bool PlayPauseInvoked { get; private set; }
    public bool SkipNextInvoked { get; private set; }
    public bool SkipPreviousInvoked { get; private set; }
    public bool IsDisposed { get; private set; }

    public void TriggerTrackChange(MediaTrackInfo track)
    {
        CurrentTrack = track;
        MediaTrackChanged?.Invoke(this, track);
    }

    public Task<bool> TryPlayPauseAsync()
    {
        PlayPauseInvoked = true;
        return Task.FromResult(true);
    }

    public Task<bool> TrySkipNextAsync()
    {
        SkipNextInvoked = true;
        return Task.FromResult(true);
    }

    public Task<bool> TrySkipPreviousAsync()
    {
        SkipPreviousInvoked = true;
        return Task.FromResult(true);
    }

    public bool SeekInvoked { get; private set; }
    public TimeSpan LastSeekPosition { get; private set; }

    public Task<bool> TrySeekAsync(TimeSpan position)
    {
        SeekInvoked = true;
        LastSeekPosition = position;
        return Task.FromResult(true);
    }

    public void Dispose()
    {
        IsDisposed = true;
    }
}
