namespace GrumphySwarm.Core.State;

/// <summary>
/// Represents the distinct interaction states of the Grumphy Swarm notch.
/// </summary>
public enum NotchState
{
    /// <summary>
    /// Compact pill attached flush to the top edge of the screen.
    /// </summary>
    Collapsed,

    /// <summary>
    /// Expanded panel displaying interactive widgets and status.
    /// </summary>
    Expanded,

    /// <summary>
    /// Pinned state where the panel remains expanded regardless of mouse hover.
    /// </summary>
    Pinned,

    /// <summary>
    /// Reserved for future milestone: interactive editing/rearranging of notch widgets.
    /// </summary>
    EditMode
}
