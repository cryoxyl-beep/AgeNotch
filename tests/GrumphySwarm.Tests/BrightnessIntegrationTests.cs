using GrumphySwarm.Core.Abstractions;
using GrumphySwarm.Core.Settings;
using GrumphySwarm.Core.State;
using GrumphySwarm.Tests.TestDoubles;
using GrumphySwarm.ViewModels;
using Xunit;

namespace GrumphySwarm.Tests;

public class BrightnessIntegrationTests
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
    public void BrightnessChanged_UpdatesViewModelAndShowsTemporaryHud()
    {
        var stateMachine = new NotchStateMachine();
        var settingsService = new DummySettingsService();
        var brightnessService = new MockBrightnessService { CurrentBrightness = 50, IsSupported = true };

        using var vm = new NotchViewModel(
            stateMachine,
            settingsService,
            brightnessService: brightnessService);

        Assert.True(vm.IsBrightnessSupported);
        Assert.Equal(50, vm.BrightnessPercent);
        Assert.Equal("50%", vm.BrightnessText);

        // Act
        brightnessService.TriggerBrightnessChange(80);

        // Assert
        Assert.Equal(80, vm.BrightnessPercent);
        Assert.Equal("80%", vm.BrightnessText);
        Assert.True(vm.IsTemporaryStatusActive);
        Assert.Equal("Brightness", vm.TemporaryStatusTitle);
        Assert.Equal("80%", vm.TemporaryStatusValue);
        Assert.Equal(0.8, vm.TemporaryStatusProgress);
    }

    [Fact]
    public void UnsupportedBrightness_HandledGracefully()
    {
        var stateMachine = new NotchStateMachine();
        var settingsService = new DummySettingsService();
        var brightnessService = new MockBrightnessService
        {
            CurrentBrightness = 0,
            IsSupported = false,
            HardwareDescription = "Unsupported Monitor"
        };

        using var vm = new NotchViewModel(
            stateMachine,
            settingsService,
            brightnessService: brightnessService);

        Assert.False(vm.IsBrightnessSupported);
        Assert.Equal("Unsupported Monitor", vm.BrightnessHardwareInfo);
    }
}
