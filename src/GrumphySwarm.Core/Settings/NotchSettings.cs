namespace GrumphySwarm.Core.Settings;

/// <summary>
/// User-configurable and operational settings for the Grumphy Swarm notch.
/// </summary>
public sealed class NotchSettings
{
    public const double MinCollapsedWidth = 180.0;
    public const double MaxCollapsedWidth = 450.0;
    public const double MinCollapsedHeight = 28.0;
    public const double MaxCollapsedHeight = 56.0;

    public const double MinExpandedWidth = 520.0;
    public const double MaxExpandedWidth = 900.0;
    public const double MinExpandedHeight = 150.0;
    public const double MaxExpandedHeight = 280.0;

    public const double MinHorizontalOffset = -600.0;
    public const double MaxHorizontalOffset = 600.0;

    public double CollapsedWidth { get; set; } = 250.0;
    public double CollapsedHeight { get; set; } = 36.0;

    public double ExpandedWidth { get; set; } = 680.0;
    public double ExpandedHeight { get; set; } = 185.0;

    public NotchHorizontalPosition HorizontalPosition { get; set; } = NotchHorizontalPosition.Center;
    public double HorizontalOffset { get; set; } = 0.0;

    public double CornerRadiusCollapsed { get; set; } = 16.0;
    public double CornerRadiusExpanded { get; set; } = 24.0;

    public double FlareRadiusCollapsed { get; set; } = 10.0;
    public double FlareRadiusExpanded { get; set; } = 12.0;

    public int HoverExpandDelayMs { get; set; } = 80;
    public int HoverCollapseDelayMs { get; set; } = 350;

    public int AnimationDurationMs { get; set; } = 260;

    public bool StartPinned { get; set; } = false;
    public bool EnableReducedMotion { get; set; } = false;

    public bool ShowLiveClock { get; set; } = true;
    public bool ShowPlaceholderStatus { get; set; } = true;

    public string AccentColorHex { get; set; } = "#10B981";

    public void ClampBounds()
    {
        CollapsedWidth = Math.Clamp(CollapsedWidth, MinCollapsedWidth, MaxCollapsedWidth);
        CollapsedHeight = Math.Clamp(CollapsedHeight, MinCollapsedHeight, MaxCollapsedHeight);
        ExpandedWidth = Math.Clamp(ExpandedWidth, MinExpandedWidth, MaxExpandedWidth);
        ExpandedHeight = Math.Clamp(ExpandedHeight, MinExpandedHeight, MaxExpandedHeight);
        HorizontalOffset = Math.Clamp(HorizontalOffset, MinHorizontalOffset, MaxHorizontalOffset);
    }

    public NotchSettings Clone()
    {
        return (NotchSettings)MemberwiseClone();
    }
}
