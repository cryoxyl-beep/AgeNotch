using GrumphySwarm.Core.State;
using Xunit;

namespace GrumphySwarm.Tests;

public class NotchStateMachineTests
{
    [Fact]
    public void InitialState_IsCollapsed_ByDefault()
    {
        var sm = new NotchStateMachine();

        Assert.Equal(NotchState.Collapsed, sm.CurrentState);
        Assert.False(sm.IsPendingCollapse);
        Assert.False(sm.IsMouseOver);
    }

    [Fact]
    public void InitialState_CanBeSpecified()
    {
        var sm = new NotchStateMachine(NotchState.Pinned);

        Assert.Equal(NotchState.Pinned, sm.CurrentState);
    }

    [Fact]
    public void MouseEnter_FromCollapsed_TransitionsToExpanded()
    {
        var sm = new NotchStateMachine();
        NotchStateChangedEventArgs? eventArgs = null;
        sm.StateChanged += (_, args) => eventArgs = args;

        var result = sm.Fire(NotchTransitionTrigger.MouseEnter);

        Assert.True(result);
        Assert.Equal(NotchState.Expanded, sm.CurrentState);
        Assert.True(sm.IsMouseOver);
        Assert.False(sm.IsPendingCollapse);
        Assert.NotNull(eventArgs);
        Assert.Equal(NotchState.Collapsed, eventArgs.PreviousState);
        Assert.Equal(NotchState.Expanded, eventArgs.NewState);
        Assert.Equal(NotchTransitionTrigger.MouseEnter, eventArgs.Trigger);
    }

    [Fact]
    public void MouseLeave_FromExpanded_EntersPendingCollapse()
    {
        var sm = new NotchStateMachine();
        sm.Fire(NotchTransitionTrigger.MouseEnter);

        bool? pendingEventValue = null;
        sm.PendingCollapseChanged += (_, isPending) => pendingEventValue = isPending;

        var result = sm.Fire(NotchTransitionTrigger.MouseLeave);

        Assert.True(result);
        Assert.Equal(NotchState.Expanded, sm.CurrentState);
        Assert.True(sm.IsPendingCollapse);
        Assert.False(sm.IsMouseOver);
        Assert.True(pendingEventValue);
    }

    [Fact]
    public void MouseEnter_WhilePendingCollapse_CancelsPendingCollapse()
    {
        var sm = new NotchStateMachine();
        sm.Fire(NotchTransitionTrigger.MouseEnter);
        sm.Fire(NotchTransitionTrigger.MouseLeave);
        Assert.True(sm.IsPendingCollapse);

        bool? pendingEventValue = null;
        sm.PendingCollapseChanged += (_, isPending) => pendingEventValue = isPending;

        var result = sm.Fire(NotchTransitionTrigger.MouseEnter);

        Assert.True(result);
        Assert.Equal(NotchState.Expanded, sm.CurrentState);
        Assert.False(sm.IsPendingCollapse);
        Assert.True(sm.IsMouseOver);
        Assert.False(pendingEventValue);
    }

    [Fact]
    public void CollapseDelayElapsed_WhenPending_TransitionsToCollapsed()
    {
        var sm = new NotchStateMachine();
        sm.Fire(NotchTransitionTrigger.MouseEnter);
        sm.Fire(NotchTransitionTrigger.MouseLeave);

        var result = sm.Fire(NotchTransitionTrigger.CollapseDelayElapsed);

        Assert.True(result);
        Assert.Equal(NotchState.Collapsed, sm.CurrentState);
        Assert.False(sm.IsPendingCollapse);
    }

    [Fact]
    public void CollapseDelayElapsed_WhenNotPending_TransitionsToCollapsedIfExpanded()
    {
        var sm = new NotchStateMachine();
        sm.Fire(NotchTransitionTrigger.MouseEnter);

        var result = sm.Fire(NotchTransitionTrigger.CollapseDelayElapsed);

        Assert.True(result);
        Assert.Equal(NotchState.Collapsed, sm.CurrentState);
    }

    [Fact]
    public void Pin_FromExpanded_TransitionsToPinned()
    {
        var sm = new NotchStateMachine();
        sm.Fire(NotchTransitionTrigger.MouseEnter);

        var result = sm.Fire(NotchTransitionTrigger.Pin);

        Assert.True(result);
        Assert.Equal(NotchState.Pinned, sm.CurrentState);
        Assert.False(sm.IsPendingCollapse);
    }

    [Fact]
    public void Pin_FromCollapsed_TransitionsToPinned()
    {
        var sm = new NotchStateMachine();

        var result = sm.Fire(NotchTransitionTrigger.Pin);

        Assert.True(result);
        Assert.Equal(NotchState.Pinned, sm.CurrentState);
    }

    [Fact]
    public void TogglePin_FromCollapsed_TransitionsToPinned()
    {
        var sm = new NotchStateMachine();

        var result = sm.Fire(NotchTransitionTrigger.TogglePin);

        Assert.True(result);
        Assert.Equal(NotchState.Pinned, sm.CurrentState);
    }

    [Fact]
    public void TogglePin_FromPinned_WhenMouseOver_TransitionsToExpanded()
    {
        var sm = new NotchStateMachine();
        sm.Fire(NotchTransitionTrigger.MouseEnter);
        sm.Fire(NotchTransitionTrigger.Pin);
        Assert.Equal(NotchState.Pinned, sm.CurrentState);

        var result = sm.Fire(NotchTransitionTrigger.TogglePin);

        Assert.True(result);
        Assert.Equal(NotchState.Expanded, sm.CurrentState);
    }

    [Fact]
    public void TogglePin_FromPinned_WhenMouseNotOver_TransitionsToCollapsed()
    {
        var sm = new NotchStateMachine();
        sm.Fire(NotchTransitionTrigger.MouseEnter);
        sm.Fire(NotchTransitionTrigger.Pin);
        sm.Fire(NotchTransitionTrigger.MouseLeave);
        Assert.Equal(NotchState.Pinned, sm.CurrentState);

        var result = sm.Fire(NotchTransitionTrigger.TogglePin);

        Assert.True(result);
        Assert.Equal(NotchState.Collapsed, sm.CurrentState);
    }

    [Fact]
    public void ForceCollapse_FromPinned_TransitionsToCollapsed()
    {
        var sm = new NotchStateMachine(NotchState.Pinned);

        var result = sm.Fire(NotchTransitionTrigger.ForceCollapse);

        Assert.True(result);
        Assert.Equal(NotchState.Collapsed, sm.CurrentState);
    }

    [Fact]
    public void ForceExpand_FromCollapsed_TransitionsToExpanded()
    {
        var sm = new NotchStateMachine();

        var result = sm.Fire(NotchTransitionTrigger.ForceExpand);

        Assert.True(result);
        Assert.Equal(NotchState.Expanded, sm.CurrentState);
    }

    [Fact]
    public void EditMode_TransitionsFlowCorrectly()
    {
        var sm = new NotchStateMachine();
        sm.Fire(NotchTransitionTrigger.MouseEnter); // In Expanded
        Assert.Equal(NotchState.Expanded, sm.CurrentState);

        var canEdit = sm.CanFire(NotchTransitionTrigger.EnterEditMode);
        Assert.True(canEdit);

        sm.Fire(NotchTransitionTrigger.EnterEditMode);
        Assert.Equal(NotchState.EditMode, sm.CurrentState);

        // Exit edit mode
        sm.Fire(NotchTransitionTrigger.ExitEditMode);
        Assert.Equal(NotchState.Collapsed, sm.CurrentState);
    }

    [Fact]
    public void RapidHoverOscillations_DoNotCauseInvalidTransitions()
    {
        var sm = new NotchStateMachine();

        for (int i = 0; i < 100; i++)
        {
            sm.Fire(NotchTransitionTrigger.MouseEnter);
            sm.Fire(NotchTransitionTrigger.MouseLeave);
        }

        Assert.Equal(NotchState.Expanded, sm.CurrentState);
        Assert.True(sm.IsPendingCollapse);
        Assert.False(sm.IsMouseOver);

        sm.Fire(NotchTransitionTrigger.CollapseDelayElapsed);
        Assert.Equal(NotchState.Collapsed, sm.CurrentState);
    }

    [Fact]
    public void InvalidTriggers_ReturnFalse_AndDoNotChangeState()
    {
        var sm = new NotchStateMachine(); // Collapsed

        // CollapseDelayElapsed is not valid in Collapsed state
        var canFire = sm.CanFire(NotchTransitionTrigger.CollapseDelayElapsed);
        Assert.False(canFire);

        var fired = sm.Fire(NotchTransitionTrigger.CollapseDelayElapsed);
        Assert.False(fired);
        Assert.Equal(NotchState.Collapsed, sm.CurrentState);
    }
}
