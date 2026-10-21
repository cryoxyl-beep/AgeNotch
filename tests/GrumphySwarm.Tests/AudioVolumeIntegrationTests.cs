using GrumphySwarm.Core.Abstractions;
using GrumphySwarm.Core.Settings;
using GrumphySwarm.Core.State;
using GrumphySwarm.Tests.TestDoubles;
using GrumphySwarm.ViewModels;
using Xunit;

namespace GrumphySwarm.Tests;

public class AudioVolumeIntegrationTests
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
    public void VolumeChanged_UpdatesViewModelProperties()
    {
        var stateMachine = new NotchStateMachine();
        var settingsService = new DummySettingsService();
        var audioService = new MockAudioVolumeService { CurrentVolume = 40, IsMuted = false };

        using var vm = new NotchViewModel(
            stateMachine,
            settingsService,
            audioVolumeService: audioService);

        Assert.Equal(40, vm.VolumePercent);
        Assert.False(vm.IsMuted);
        Assert.Equal("40%", vm.VolumeText);

        // Act: volume change to 75%
        audioService.TriggerVolumeChange(75, isMuted: false);

        // Assert
        Assert.Equal(75, vm.VolumePercent);
        Assert.False(vm.IsMuted);
        Assert.Equal("75%", vm.VolumeText);
        Assert.True(vm.IsTemporaryStatusActive);
        Assert.Equal("Volume", vm.TemporaryStatusTitle);
        Assert.Equal("75%", vm.TemporaryStatusValue);
    }

    [Fact]
    public void MuteAndUnmute_AreRepresentedAccurately()
    {
        var stateMachine = new NotchStateMachine();
        var settingsService = new DummySettingsService();
        var audioService = new MockAudioVolumeService { CurrentVolume = 60, IsMuted = false };

        using var vm = new NotchViewModel(
            stateMachine,
            settingsService,
            audioVolumeService: audioService);

        Assert.False(vm.IsMuted);

        // Act: Mute
        audioService.TriggerVolumeChange(60, isMuted: true);

        // Assert
        Assert.True(vm.IsMuted);
        Assert.Equal("Muted", vm.VolumeText);
        Assert.Equal("IconVolumeMuteGeometry", vm.VolumeIconKey);
        Assert.True(vm.IsTemporaryStatusActive);
        Assert.True(vm.IsTemporaryStatusMuted);

        // Act: Unmute
        audioService.TriggerVolumeChange(60, isMuted: false);

        // Assert
        Assert.False(vm.IsMuted);
        Assert.Equal("60%", vm.VolumeText);
        Assert.Equal("IconVolumeGeometry", vm.VolumeIconKey);
        Assert.False(vm.IsTemporaryStatusMuted);
    }

    [Fact]
    public void RepeatedVolumeChanges_DoNotLeaveStaleState()
    {
        var stateMachine = new NotchStateMachine();
        var settingsService = new DummySettingsService();
        var audioService = new MockAudioVolumeService { CurrentVolume = 30, IsMuted = false };

        using var vm = new NotchViewModel(
            stateMachine,
            settingsService,
            audioVolumeService: audioService);

        // Rapid volume changes
        for (int v = 30; v <= 70; v += 5)
        {
            audioService.TriggerVolumeChange(v, isMuted: false);
            Assert.Equal(v, vm.VolumePercent);
            Assert.True(vm.IsTemporaryStatusActive);
        }

        Assert.Equal(70, vm.VolumePercent);
        Assert.Equal("70%", vm.VolumeText);
        Assert.Equal("70%", vm.TemporaryStatusValue);
    }

    [Fact]
    public void TestRealCoreAudioActivation_IfDevicePresent()
    {
        try
        {
            var enumerator = (GrumphySwarm.Interop.CoreAudio.IMMDeviceEnumerator)new GrumphySwarm.Interop.CoreAudio.MMDeviceEnumeratorComObject();
            int hr = enumerator.GetDefaultAudioEndpoint(GrumphySwarm.Interop.CoreAudio.EDataFlow.eRender, GrumphySwarm.Interop.CoreAudio.ERole.eMultimedia, out var device);
            if (hr == 0 && device != null)
            {
                var iid = new Guid("5CDF2C82-841E-4546-9722-0CF74078229A");
                int hrActivate = device.Activate(ref iid, 1 /* CLSCTX_INPROC_SERVER */, IntPtr.Zero, out var volumeObj);
                Assert.True(hrActivate == 0, $"hrActivate was 0x{hrActivate:X8}");
                Assert.NotNull(volumeObj);
            }
        }
        catch (System.Runtime.InteropServices.COMException)
        {
            // Allowed on headless or containerized environments without active audio drivers
        }
    }
}
