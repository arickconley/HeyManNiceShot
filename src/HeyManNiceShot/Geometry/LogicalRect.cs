namespace HeyManNiceShot.Geometry;

public readonly record struct LogicalRect(double X, double Y, double Width, double Height)
{
    public double Right => X + Width;
    public double Bottom => Y + Height;

    public PhysicalRect ToPhysical(double dpiScale)
        => new(
            (int)Math.Round(X * dpiScale),
            (int)Math.Round(Y * dpiScale),
            (int)Math.Round(Width * dpiScale),
            (int)Math.Round(Height * dpiScale));
}
