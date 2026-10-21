namespace GrumphySwarm.Core.State;

/// <summary>
/// Event arguments delivered when the notch transitions between states.
/// </summary>
public sealed class NotchStateChangedEventArgs : EventArgs
{
    public NotchState PreviousState { get; }
    public NotchState NewState { get; }
    public NotchTransitionTrigger Trigger { get; }
    public DateTimeOffset Timestamp { get; }

    public NotchStateChangedEventArgs(NotchState previousState, NotchState newState, NotchTransitionTrigger trigger)
    {
        PreviousState = previousState;
        NewState = newState;
        Trigger = trigger;
        Timestamp = DateTimeOffset.UtcNow;
    }
}
