using System.Windows;
using GrumphySwarm.Animations;
using Xunit;

namespace GrumphySwarm.Tests;

public class NotchGeometryBuilderTests
{
    [Theory]
    [InlineData(230, 34, 10, 16)]
    [InlineData(660, 175, 12, 24)]
    public void BuildGeometry_ReturnsValidFrozenGeometryWithExpectedBounds(
        double width, double height, double flareRadius, double bottomRadius)
    {
        // Act
        var geometry = NotchGeometryBuilder.BuildGeometry(width, height, flareRadius, bottomRadius);

        // Assert
        Assert.NotNull(geometry);
        Assert.True(geometry.IsFrozen);

        var bounds = geometry.Bounds;
        Assert.True(bounds.Width <= width + 0.5 && bounds.Width >= width - 0.5,
            $"Expected width ~{width}, got {bounds.Width}");
        Assert.True(bounds.Height <= height + 0.5 && bounds.Height >= height - 0.5,
            $"Expected height ~{height}, got {bounds.Height}");

        // Notch interior center should be inside
        Assert.True(geometry.FillContains(new Point(width / 2.0, height / 2.0)));

        // Cutout outside top-left flare should be OUTSIDE
        // (x < flareRadius and y > 0 but above the flare curve)
        Assert.False(geometry.FillContains(new Point(flareRadius * 0.2, flareRadius * 0.8)));

        // Cutout outside top-right flare should be OUTSIDE
        Assert.False(geometry.FillContains(new Point(width - (flareRadius * 0.2), flareRadius * 0.8)));

        // Cutout outside bottom-left convex corner should be OUTSIDE
        Assert.False(geometry.FillContains(new Point(flareRadius * 0.5, height)));

        // Cutout outside bottom-right convex corner should be OUTSIDE
        Assert.False(geometry.FillContains(new Point(width - (flareRadius * 0.5), height)));
    }
}
