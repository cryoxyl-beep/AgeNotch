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

    public const double MinExpandedWidth = 480.0;
    public const double MaxExpandedWidth = 900.0;
    public const double MinExpandedHeight = 90.0;
    public const double MaxExpandedHeight = 260.0;

    public const double MinHorizontalOffset = -600.0;
    public const double MaxHorizontalOffset = 600.0;

    public double CollapsedWidth { get; set; } = 240.0;
    public double CollapsedHeight { get; set; } = 34.0;

    public double ExpandedWidth { get; set; } = 660.0;
    public double ExpandedHeight { get; set; } = 140.0;

    public NotchHorizontalPosition HorizontalPosition { get; set; } = NotchHorizontalPosition.Center;
    public double HorizontalOffset { get; set; } = 0.0;

    public double CornerRadiusCollapsed { get; set; } = 16.0;
    public double CornerRadiusExpanded { get; set; } = 30.0;

    public double FlareRadiusCollapsed { get; set; } = 10.0;
    public double FlareRadiusExpanded { get; set; } = 26.0;

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

        // Migrate legacy oversized/square settings to sleek reference silhouette
        if (ExpandedHeight > 160.0) ExpandedHeight = 140.0;
        if (FlareRadiusExpanded < 20.0) FlareRadiusExpanded = 26.0;
        if (CornerRadiusExpanded < 26.0) CornerRadiusExpanded = 30.0;
    }

    public NotchSettings Clone()
    {
        return (NotchSettings)MemberwiseClone();
    }
}
