namespace HeyManNiceShot.Geometry;

public sealed record VirtualScreen(
    IReadOnlyList<MonitorInfo> Monitors,
    PhysicalRect PhysicalBounds)
{
    public MonitorInfo? MonitorAt(int x, int y)
    {
        foreach (var m in Monitors)
            if (m.PhysicalBounds.Contains(x, y))
                return m;
        return null;
    }

    public MonitorInfo Primary
        => Monitors.FirstOrDefault(m => m.IsPrimary)
        ?? Monitors.FirstOrDefault()
        ?? throw new InvalidOperationException("VirtualScreen has no monitors.");

    public static PhysicalRect ComputeBounds(IEnumerable<MonitorInfo> monitors)
    {
        var arr = monitors as MonitorInfo[] ?? monitors.ToArray();
        if (arr.Length == 0) return new PhysicalRect(0, 0, 0, 0);
        var x = arr.Min(m => m.PhysicalBounds.X);
        var y = arr.Min(m => m.PhysicalBounds.Y);
        var r = arr.Max(m => m.PhysicalBounds.Right);
        var b = arr.Max(m => m.PhysicalBounds.Bottom);
        return new PhysicalRect(x, y, r - x, b - y);
    }
}
