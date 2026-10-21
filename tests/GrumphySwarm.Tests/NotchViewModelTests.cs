using GrumphySwarm.Core.Abstractions;
using GrumphySwarm.Core.Settings;
using GrumphySwarm.Core.State;
using GrumphySwarm.ViewModels;
using Xunit;

namespace GrumphySwarm.Tests;

public class NotchViewModelTests
{
    private class DummySettingsService : ISettingsService
    {
        public NotchSettings CurrentSettings { get; set; } = new();

        public NotchSettings LoadSettings() => CurrentSettings;

        public Task<NotchSettings> LoadSettingsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(CurrentSettings);

        public Task SaveSettingsAsync(NotchSettings settings, CancellationToken cancellationToken = default)
        {
            CurrentSettings = settings;
            SettingsChanged?.Invoke(this, settings);
            return Task.CompletedTask;
        }

        public event EventHandler<NotchSettings>? SettingsChanged;
    }

    private class MockClock : ISystemClock
    {
        public DateTimeOffset Now => new(2026, 8, 8, 12, 0, 0, TimeSpan.Zero);
        public DateTimeOffset UtcNow => new(2026, 8, 8, 12, 0, 0, TimeSpan.Zero);
    }

    [Fact]
    public void Constructor_AppliesSettingsAndDimensions()
    {
        var stateMachine = new NotchStateMachine();
        var settingsService = new DummySettingsService
        {
            CurrentSettings = new NotchSettings
            {
                CollapsedWidth = 230,
                CollapsedHeight = 34,
                ExpandedWidth = 660,
                ExpandedHeight = 175,
                CornerRadiusCollapsed = 16,
                CornerRadiusExpanded = 24,
                FlareRadiusCollapsed = 10,
                FlareRadiusExpanded = 12
            }
        };

        using var vm = new NotchViewModel(stateMachine, settingsService, clock: new MockClock());

        Assert.Equal(230, vm.CollapsedWidth);
        Assert.Equal(34, vm.CollapsedHeight);
        Assert.Equal(660, vm.ExpandedWidth);
        Assert.Equal(175, vm.ExpandedHeight);
        Assert.Equal(16, vm.CornerRadiusCollapsed);
        Assert.Equal(24, vm.CornerRadiusExpanded);
        Assert.Equal(10, vm.FlareRadiusCollapsed);
        Assert.Equal(12, vm.FlareRadiusExpanded);
        Assert.False(vm.IsExpanded);
    }

    [Fact]
    public void HandleMouseEnter_ExpandsViewModel()
    {
        var stateMachine = new NotchStateMachine();
        var settingsService = new DummySettingsService();
        using var vm = new NotchViewModel(stateMachine, settingsService, clock: new MockClock());

        vm.HandleMouseEnterCommand.Execute(null);

        Assert.True(vm.IsExpanded);
        Assert.Equal(NotchState.Expanded, vm.CurrentState);
    }

    [Fact]
    public void TogglePin_PinsAndPreventsCollapse()
    {
        var stateMachine = new NotchStateMachine();
        var settingsService = new DummySettingsService();
        using var vm = new NotchViewModel(stateMachine, settingsService, clock: new MockClock());

        vm.HandleMouseEnterCommand.Execute(null);
        Assert.True(vm.IsExpanded);

        vm.TogglePinCommand.Execute(null);
        Assert.True(vm.IsPinned);

        // When pinned, MouseLeave does not collapse
        vm.HandleMouseLeaveCommand.Execute(null);
        Assert.True(vm.IsExpanded);
        Assert.Equal(NotchState.Pinned, vm.CurrentState);

        // Unpin
        vm.TogglePinCommand.Execute(null);
        Assert.False(vm.IsPinned);
    }

    [Fact]
    public void ForceCollapse_CollapsesEvenWhenPinned()
    {
        var stateMachine = new NotchStateMachine();
        var settingsService = new DummySettingsService();
        using var vm = new NotchViewModel(stateMachine, settingsService, clock: new MockClock());

        vm.HandleMouseEnterCommand.Execute(null);
        vm.TogglePinCommand.Execute(null);
        Assert.True(vm.IsPinned);

        vm.ForceCollapseCommand.Execute(null);
        Assert.False(vm.IsExpanded);
        Assert.False(vm.IsPinned);
        Assert.Equal(NotchState.Collapsed, vm.CurrentState);
    }

    [Fact]
    public void SelectTab_UpdatesActiveTab()
    {
        var stateMachine = new NotchStateMachine();
        var settingsService = new DummySettingsService();
        using var vm = new NotchViewModel(stateMachine, settingsService, clock: new MockClock());

        Assert.Equal("Nook", vm.ActiveTab);

        vm.SelectTabCommand.Execute("Agents");
        Assert.Equal("Agents", vm.ActiveTab);
    }

    [Fact]
    public void AgentWorkspace_OpenCreateAndCancel_TogglesEditingState()
    {
        var stateMachine = new NotchStateMachine();
        var settingsService = new DummySettingsService();
        using var vm = new NotchViewModel(stateMachine, settingsService, clock: new MockClock());

        Assert.False(vm.IsEditingAgent);

        vm.OpenCreateAgentCommand.Execute(null);
        Assert.True(vm.IsEditingAgent);
        Assert.Equal("Create New Agent Profile", vm.EditorTitle);

        vm.CancelAgentEditCommand.Execute(null);
        Assert.False(vm.IsEditingAgent);
    }
}
