using HeyManNiceShot.Geometry;
using Xunit;

namespace HeyManNiceShot.Tests;

public class VirtualScreenTests
{
    [Fact]
    public void ComputeBounds_SingleMonitor_MatchesItsBounds()
    {
        var m = new MonitorInfo(IntPtr.Zero, new PhysicalRect(0, 0, 1920, 1080), 1.0, true);
        var bounds = VirtualScreen.ComputeBounds(new[] { m });
        Assert.Equal(new PhysicalRect(0, 0, 1920, 1080), bounds);
    }

    [Fact]
    public void ComputeBounds_TwoSideBySideMonitors_ReturnsUnion()
    {
        var m1 = new MonitorInfo(IntPtr.Zero, new PhysicalRect(0, 0, 1920, 1080), 1.0, true);
        var m2 = new MonitorInfo(IntPtr.Zero, new PhysicalRect(1920, 0, 2560, 1440), 1.0, false);
        var bounds = VirtualScreen.ComputeBounds(new[] { m1, m2 });
        Assert.Equal(new PhysicalRect(0, 0, 4480, 1440), bounds);
    }

    [Fact]
    public void ComputeBounds_WithLeftMonitor_HandlesNegativeOrigin()
    {
        var primary = new MonitorInfo(IntPtr.Zero, new PhysicalRect(0, 0, 1920, 1080), 1.0, true);
        var left = new MonitorInfo(IntPtr.Zero, new PhysicalRect(-1920, 0, 1920, 1080), 1.0, false);
        var bounds = VirtualScreen.ComputeBounds(new[] { primary, left });
        Assert.Equal(new PhysicalRect(-1920, 0, 3840, 1080), bounds);
    }

    [Fact]
    public void MonitorAt_FindsContainingMonitor()
    {
        var m1 = new MonitorInfo(IntPtr.Zero, new PhysicalRect(0, 0, 1920, 1080), 1.0, true);
        var m2 = new MonitorInfo(IntPtr.Zero, new PhysicalRect(1920, 0, 2560, 1440), 1.0, false);
        var vs = new VirtualScreen(new[] { m1, m2 }, VirtualScreen.ComputeBounds(new[] { m1, m2 }));
        var found = vs.MonitorAt(2000, 500);
        Assert.NotNull(found);
        Assert.Equal(2560, found!.PhysicalBounds.Width);
    }

    [Fact]
    public void Intersect_OverlappingRects_ReturnsOverlap()
    {
        var a = new PhysicalRect(0, 0, 100, 100);
        var b = new PhysicalRect(50, 50, 100, 100);
        Assert.Equal(new PhysicalRect(50, 50, 50, 50), a.Intersect(b));
    }

    [Fact]
    public void Intersect_DisjointRects_ReturnsEmpty()
    {
        var a = new PhysicalRect(0, 0, 100, 100);
        var b = new PhysicalRect(200, 200, 100, 100);
        Assert.True(a.Intersect(b).IsEmpty);
    }

    [Fact]
    public void FromCorners_HandlesReversedDrag()
    {
        var rect = PhysicalRect.FromCorners(50, 80, 10, 20);
        Assert.Equal(new PhysicalRect(10, 20, 40, 60), rect);
    }
}
