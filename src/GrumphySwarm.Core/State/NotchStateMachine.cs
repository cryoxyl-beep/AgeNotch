using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace GrumphySwarm.Core.State;

/// <summary>
/// Deterministic, thread-safe state machine for the notch component.
/// Encapsulates all interaction logic and transition rules without any UI framework dependencies.
/// </summary>
public sealed class NotchStateMachine : INotchStateMachine
{
    private readonly object _syncRoot = new();
    private readonly ILogger<NotchStateMachine> _logger;
    private NotchState _currentState;
    private bool _isPendingCollapse;
    private bool _isMouseOver;

    public NotchStateMachine(
        NotchState initialState = NotchState.Collapsed,
        ILogger<NotchStateMachine>? logger = null)
    {
        _currentState = initialState;
        _logger = logger ?? NullLogger<NotchStateMachine>.Instance;
    }

    public NotchState CurrentState
    {
        get
        {
            lock (_syncRoot)
            {
                return _currentState;
            }
        }
    }

    public bool IsPendingCollapse
    {
        get
        {
            lock (_syncRoot)
            {
                return _isPendingCollapse;
            }
        }
    }

    public bool IsMouseOver
    {
        get
        {
            lock (_syncRoot)
            {
                return _isMouseOver;
            }
        }
    }

    public event EventHandler<NotchStateChangedEventArgs>? StateChanged;
    public event EventHandler<bool>? PendingCollapseChanged;

    public bool CanFire(NotchTransitionTrigger trigger)
    {
        lock (_syncRoot)
        {
            return DetermineNextState(trigger, out _);
        }
    }

    public bool Fire(NotchTransitionTrigger trigger)
    {
        NotchState previousState;
        NotchState nextState;
        bool stateTransitioned = false;
        bool pendingCollapseChanged = false;
        bool newPendingCollapse = false;

        lock (_syncRoot)
        {
            previousState = _currentState;

            // Track mouse presence
            if (trigger == NotchTransitionTrigger.MouseEnter)
            {
                _isMouseOver = true;
            }
            else if (trigger == NotchTransitionTrigger.MouseLeave)
            {
                _isMouseOver = false;
            }

            if (!DetermineNextState(trigger, out nextState))
            {
                // In some states (e.g. Expanded), MouseLeave triggers pending collapse rather than immediate state change
                if (_currentState == NotchState.Expanded)
                {
                    if (trigger == NotchTransitionTrigger.MouseLeave && !_isPendingCollapse)
                    {
                        _isPendingCollapse = true;
                        pendingCollapseChanged = true;
                        newPendingCollapse = true;
                        _logger.LogDebug("Mouse left expanded notch. Collapse pending.");
                    }
                    else if (trigger == NotchTransitionTrigger.MouseEnter && _isPendingCollapse)
                    {
                        _isPendingCollapse = false;
                        pendingCollapseChanged = true;
                        newPendingCollapse = false;
                        _logger.LogDebug("Mouse re-entered expanded notch. Collapse cancelled.");
                    }
                }

                if (pendingCollapseChanged)
                {
                    // Notify pending collapse change outside lock
                }
                else
                {
                    _logger.LogTrace("Trigger {Trigger} ignored in state {CurrentState}.", trigger, _currentState);
                    return false;
                }
            }
            else
            {
                // Valid transition
                _currentState = nextState;
                stateTransitioned = (previousState != nextState);

                // Reset pending collapse on state change
                if (_isPendingCollapse)
                {
                    _isPendingCollapse = false;
                    pendingCollapseChanged = true;
                    newPendingCollapse = false;
                }

                _logger.LogInformation("Transition: {PreviousState} -> {NewState} via {Trigger}",
                    previousState, nextState, trigger);
            }
        }

        if (pendingCollapseChanged)
        {
            PendingCollapseChanged?.Invoke(this, newPendingCollapse);
        }

        if (stateTransitioned)
        {
            StateChanged?.Invoke(this, new NotchStateChangedEventArgs(previousState, nextState, trigger));
        }

        return true;
    }

    private bool DetermineNextState(NotchTransitionTrigger trigger, out NotchState nextState)
    {
        nextState = _currentState;

        switch (_currentState)
        {
            case NotchState.Collapsed:
                switch (trigger)
                {
                    case NotchTransitionTrigger.MouseEnter:
                    case NotchTransitionTrigger.ForceExpand:
                        nextState = NotchState.Expanded;
                        return true;

                    case NotchTransitionTrigger.TogglePin:
                    case NotchTransitionTrigger.Pin:
                        nextState = NotchState.Pinned;
                        return true;

                    default:
                        return false;
                }

            case NotchState.Expanded:
                switch (trigger)
                {
                    case NotchTransitionTrigger.CollapseDelayElapsed:
                    case NotchTransitionTrigger.ForceCollapse:
                        nextState = NotchState.Collapsed;
                        return true;

                    case NotchTransitionTrigger.TogglePin:
                    case NotchTransitionTrigger.Pin:
                        nextState = NotchState.Pinned;
                        return true;

                    case NotchTransitionTrigger.EnterEditMode:
                        nextState = NotchState.EditMode;
                        return true;

                    default:
                        return false;
                }

            case NotchState.Pinned:
                switch (trigger)
                {
                    case NotchTransitionTrigger.TogglePin:
                    case NotchTransitionTrigger.Unpin:
                        // If mouse is currently hovering, return to normal Expanded; otherwise collapse back to pill
                        nextState = _isMouseOver ? NotchState.Expanded : NotchState.Collapsed;
                        return true;

                    case NotchTransitionTrigger.ForceCollapse:
                        nextState = NotchState.Collapsed;
                        return true;

                    case NotchTransitionTrigger.EnterEditMode:
                        nextState = NotchState.EditMode;
                        return true;

                    default:
                        return false;
                }

            case NotchState.EditMode:
                switch (trigger)
                {
                    case NotchTransitionTrigger.ExitEditMode:
                    case NotchTransitionTrigger.ForceCollapse:
                        nextState = NotchState.Collapsed;
                        return true;

                    default:
                        return false;
                }

            default:
                return false;
        }
    }
}
