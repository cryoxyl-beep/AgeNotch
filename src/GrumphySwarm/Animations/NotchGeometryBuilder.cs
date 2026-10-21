using System.Windows;
using System.Windows.Media;

namespace GrumphySwarm.Animations;

/// <summary>
/// Constructs the custom flared notch silhouette geometry for Windows 11.
/// Features concave outward flares at the top display edge and smooth convex rounded corners at the bottom.
/// </summary>
public static class NotchGeometryBuilder
{
    /// <summary>
    /// Builds a frozen PathGeometry representing the seamless native notch silhouette.
    /// </summary>
    /// <param name="width">Total width of the notch in DIPs (including flares).</param>
    /// <param name="height">Total height of the notch downward from top edge.</param>
    /// <param name="flareRadius">Radius of the upper concave flares blending into screen top.</param>
    /// <param name="bottomRadius">Radius of the lower convex rounded corners.</param>
    public static PathGeometry BuildGeometry(double width, double height, double flareRadius, double bottomRadius)
    {
        // Guard against degenerate dimensions during rapid transitions
        width = Math.Max(40.0, width);
        height = Math.Max(10.0, height);
        flareRadius = Math.Max(2.0, Math.Min(flareRadius, height / 2.0));
        
        double maxBottomRadius = (width - (2.0 * flareRadius)) / 2.0;
        bottomRadius = Math.Max(2.0, Math.Min(bottomRadius, Math.Min(height - flareRadius, maxBottomRadius)));

        var figure = new PathFigure
        {
            StartPoint = new Point(0, 0),
            IsClosed = true,
            IsFilled = true
        };

        // 1. Top-Left flare: concave curve from (0, 0) downward and right to (flareRadius, flareRadius)
        // Center is at (0, flareRadius). Clockwise sweep from 12 o'clock to 3 o'clock.
        figure.Segments.Add(new ArcSegment
        {
            Point = new Point(flareRadius, flareRadius),
            Size = new Size(flareRadius, flareRadius),
            SweepDirection = SweepDirection.Clockwise,
            IsLargeArc = false
        });

        // 2. Left vertical wall down to bottom corner start
        figure.Segments.Add(new LineSegment
        {
            Point = new Point(flareRadius, height - bottomRadius)
        });

        // 3. Bottom-Left corner: convex curve to (flareRadius + bottomRadius, height)
        // Center is at (flareRadius + bottomRadius, height - bottomRadius). Counterclockwise sweep from 9 o'clock to 6 o'clock.
        figure.Segments.Add(new ArcSegment
        {
            Point = new Point(flareRadius + bottomRadius, height),
            Size = new Size(bottomRadius, bottomRadius),
            SweepDirection = SweepDirection.Counterclockwise,
            IsLargeArc = false
        });

        // 4. Bottom horizontal edge
        figure.Segments.Add(new LineSegment
        {
            Point = new Point(width - flareRadius - bottomRadius, height)
        });

        // 5. Bottom-Right corner: convex curve to (width - flareRadius, height - bottomRadius)
        // Center is at (width - flareRadius - bottomRadius, height - bottomRadius). Counterclockwise sweep from 6 o'clock to 3 o'clock.
        figure.Segments.Add(new ArcSegment
        {
            Point = new Point(width - flareRadius, height - bottomRadius),
            Size = new Size(bottomRadius, bottomRadius),
            SweepDirection = SweepDirection.Counterclockwise,
            IsLargeArc = false
        });

        // 6. Right vertical wall up to top flare start
        figure.Segments.Add(new LineSegment
        {
            Point = new Point(width - flareRadius, flareRadius)
        });

        // 7. Top-Right flare: concave curve upward and right to (width, 0)
        // Center is at (width, flareRadius). Clockwise sweep from 9 o'clock to 12 o'clock.
        figure.Segments.Add(new ArcSegment
        {
            Point = new Point(width, 0),
            Size = new Size(flareRadius, flareRadius),
            SweepDirection = SweepDirection.Clockwise,
            IsLargeArc = false
        });

        // 8. Top horizontal edge flush with screen top back to (0, 0)
        figure.Segments.Add(new LineSegment
        {
            Point = new Point(0, 0)
        });

        var geometry = new PathGeometry();
        geometry.Figures.Add(figure);
        geometry.Freeze(); // Freezing optimizes rendering and thread safety

        return geometry;
    }
}
