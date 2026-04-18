namespace HeyManNiceShot.Geometry;

public sealed record MonitorInfo(
    IntPtr Handle,
    PhysicalRect PhysicalBounds,
    double DpiScale,
    bool IsPrimary);
