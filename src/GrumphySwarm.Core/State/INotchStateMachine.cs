namespace GrumphySwarm.Core.State;

/// <summary>
/// Interface for the deterministic notch state machine.
/// Pure logic contract suitable for automated unit testing without UI dependencies.
/// </summary>
public interface INotchStateMachine
{
    /// <summary>
    /// Gets the current state of the notch.
    /// </summary>
    NotchState CurrentState { get; }

    /// <summary>
    /// Indicates whether the cursor has left the expanded notch and a collapse timer is currently running.
    /// </summary>
    bool IsPendingCollapse { get; }

    /// <summary>
    /// Indicates whether the cursor is currently inside the notch interaction bounds.
    /// </summary>
    bool IsMouseOver { get; }

    /// <summary>
    /// Determines whether a trigger can be processed from the current state.
    /// </summary>
    bool CanFire(NotchTransitionTrigger trigger);

    /// <summary>
    /// Fires a trigger to attempt a state transition.
    /// Returns true if a transition occurred, or false if ignored/invalid.
    /// </summary>
    bool Fire(NotchTransitionTrigger trigger);

    /// <summary>
    /// Event raised whenever the state transitions.
    /// </summary>
    event EventHandler<NotchStateChangedEventArgs>? StateChanged;

    /// <summary>
    /// Event raised when a pending collapse starts or cancels.
    /// </summary>
    event EventHandler<bool>? PendingCollapseChanged;
}
