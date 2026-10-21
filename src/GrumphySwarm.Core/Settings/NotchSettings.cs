namespace GrumphySwarm.Core.Settings;

/// <summary>
/// User-configurable and operational settings for the Grumphy Swarm notch.
/// </summary>
public sealed class NotchSettings
{
    public double CollapsedWidth { get; set; } = 230.0;
    public double CollapsedHeight { get; set; } = 34.0;

    public double ExpandedWidth { get; set; } = 660.0;
    public double ExpandedHeight { get; set; } = 175.0;

    public double CornerRadiusCollapsed { get; set; } = 16.0;
    public double CornerRadiusExpanded { get; set; } = 24.0;

    public double FlareRadiusCollapsed { get; set; } = 10.0;
    public double FlareRadiusExpanded { get; set; } = 12.0;

    public int HoverExpandDelayMs { get; set; } = 80;
    public int HoverCollapseDelayMs { get; set; } = 350;

    public int AnimationDurationMs { get; set; } = 280;

    public bool StartPinned { get; set; } = false;
    public bool EnableReducedMotion { get; set; } = false;

    public bool ShowLiveClock { get; set; } = true;
    public bool ShowPlaceholderStatus { get; set; } = true;

    public string AccentColorHex { get; set; } = "#10B981";

    public NotchSettings Clone()
    {
        return (NotchSettings)MemberwiseClone();
    }
}
