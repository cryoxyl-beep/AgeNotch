namespace GrumphySwarm.Core.State;

/// <summary>
/// Triggers that cause the notch state machine to transition between states.
/// </summary>
public enum NotchTransitionTrigger
{
    /// <summary>
    /// Mouse pointer entered the notch interaction bounds.
    /// </summary>
    MouseEnter,

    /// <summary>
    /// Mouse pointer left the notch interaction bounds.
    /// </summary>
    MouseLeave,

    /// <summary>
    /// Hover collapse delay timer expired without the mouse returning.
    /// </summary>
    CollapseDelayElapsed,

    /// <summary>
    /// User clicked the pin control to toggle pinning.
    /// </summary>
    TogglePin,

    /// <summary>
    /// User explicitly clicked pin button to lock expanded state.
    /// </summary>
    Pin,

    /// <summary>
    /// User unpinned the notch.
    /// </summary>
    Unpin,

    /// <summary>
    /// User triggered expand via tray icon or shortcut.
    /// </summary>
    ForceExpand,

    /// <summary>
    /// User or system triggered immediate collapse.
    /// </summary>
    ForceCollapse,

    /// <summary>
    /// User entered edit mode (reserved for future milestone).
    /// </summary>
    EnterEditMode,

    /// <summary>
    /// User exited edit mode (reserved for future milestone).
    /// </summary>
    ExitEditMode
}
