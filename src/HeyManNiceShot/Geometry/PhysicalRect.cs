namespace HeyManNiceShot.Geometry;

public readonly record struct PhysicalRect(int X, int Y, int Width, int Height)
{
    public int Right => X + Width;
    public int Bottom => Y + Height;
    public bool IsEmpty => Width <= 0 || Height <= 0;

    public bool Contains(int px, int py)
        => px >= X && px < Right && py >= Y && py < Bottom;

    public PhysicalRect Intersect(PhysicalRect other)
    {
        var x = Math.Max(X, other.X);
        var y = Math.Max(Y, other.Y);
        var r = Math.Min(Right, other.Right);
        var b = Math.Min(Bottom, other.Bottom);
        return r <= x || b <= y
            ? new PhysicalRect(0, 0, 0, 0)
            : new PhysicalRect(x, y, r - x, b - y);
    }

    public LogicalRect ToLogical(double dpiScale)
        => new(X / dpiScale, Y / dpiScale, Width / dpiScale, Height / dpiScale);

    public static PhysicalRect FromCorners(int x1, int y1, int x2, int y2)
    {
        var x = Math.Min(x1, x2);
        var y = Math.Min(y1, y2);
        return new PhysicalRect(x, y, Math.Abs(x2 - x1), Math.Abs(y2 - y1));
    }
}
