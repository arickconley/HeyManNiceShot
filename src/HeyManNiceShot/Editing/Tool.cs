using SkiaSharp;

namespace HeyManNiceShot.Editing;

public abstract class Tool
{
    public abstract string Name { get; }
    public SKColor Color { get; set; } = new SKColor(0xFF, 0x4D, 0x4F);
    public float StrokeWidth { get; set; } = 4f;

    public abstract void OnPointerDown(SKPoint p);
    public abstract void OnPointerMove(SKPoint p);
    public abstract Layer? OnPointerUp(SKPoint p);

    public virtual Layer? Preview() => null;
    public virtual void Cancel() { }
}

public abstract class TwoPointTool : Tool
{
    protected SKPoint? Start;
    protected SKPoint? Current;
    protected const float MinDrag = 4f;

    public override void OnPointerDown(SKPoint p) { Start = p; Current = p; }
    public override void OnPointerMove(SKPoint p) { Current = p; }

    public override Layer? OnPointerUp(SKPoint p)
    {
        var s = Start; Start = null; Current = null;
        if (s is null) return null;
        return Distance(s.Value, p) < MinDrag ? null : CreateLayer(s.Value, p);
    }

    public override Layer? Preview()
        => Start is { } s && Current is { } c && Distance(s, c) >= MinDrag
            ? CreateLayer(s, c)
            : null;

    public override void Cancel() { Start = null; Current = null; }

    protected abstract Layer CreateLayer(SKPoint start, SKPoint end);

    protected static float Distance(SKPoint a, SKPoint b)
    {
        var dx = b.X - a.X;
        var dy = b.Y - a.Y;
        return MathF.Sqrt(dx * dx + dy * dy);
    }

    protected static SKRect Normalize(SKPoint a, SKPoint b)
        => new(MathF.Min(a.X, b.X), MathF.Min(a.Y, b.Y), MathF.Max(a.X, b.X), MathF.Max(a.Y, b.Y));
}

public sealed class SelectTool : Tool
{
    public override string Name => "Select";
    public override void OnPointerDown(SKPoint p) { }
    public override void OnPointerMove(SKPoint p) { }
    public override Layer? OnPointerUp(SKPoint p) => null;
}

public sealed class ArrowTool : TwoPointTool
{
    public override string Name => "Arrow";
    protected override Layer CreateLayer(SKPoint start, SKPoint end)
        => new ArrowLayer(start, end, Color, StrokeWidth);
}

public sealed class RectangleTool : TwoPointTool
{
    public override string Name => "Rectangle";
    public SKColor? FillColor { get; set; }
    protected override Layer CreateLayer(SKPoint start, SKPoint end)
        => new RectangleLayer(Normalize(start, end), Color, StrokeWidth, FillColor);
}

public sealed class EllipseTool : TwoPointTool
{
    public override string Name => "Ellipse";
    public SKColor? FillColor { get; set; }
    protected override Layer CreateLayer(SKPoint start, SKPoint end)
        => new EllipseLayer(Normalize(start, end), Color, StrokeWidth, FillColor);
}

public sealed class BlurTool : TwoPointTool
{
    public override string Name => "Blur";
    public float Sigma { get; set; } = 12f;
    protected override Layer CreateLayer(SKPoint start, SKPoint end)
        => new BlurLayer(Normalize(start, end), Sigma);
}

public sealed class FreehandTool : Tool
{
    public override string Name => "Pen";
    private List<SKPoint>? _points;

    public override void OnPointerDown(SKPoint p) => _points = new List<SKPoint> { p };
    public override void OnPointerMove(SKPoint p) => _points?.Add(p);

    public override Layer? OnPointerUp(SKPoint p)
    {
        var pts = _points; _points = null;
        if (pts is null || pts.Count < 2) return null;
        return new FreehandLayer(pts, Color, StrokeWidth);
    }

    public override Layer? Preview()
        => _points is { Count: >= 2 } pts
            ? new FreehandLayer(pts.ToArray(), Color, StrokeWidth)
            : null;

    public override void Cancel() => _points = null;
}

public sealed class TextTool : Tool
{
    public override string Name => "Text";
    public float FontSize { get; set; } = 24f;
    public string PendingText { get; set; } = "Text";

    private SKPoint? _start;

    public override void OnPointerDown(SKPoint p) => _start = p;
    public override void OnPointerMove(SKPoint p) { }

    public override Layer? OnPointerUp(SKPoint p)
    {
        var s = _start; _start = null;
        if (s is null) return null;
        if (string.IsNullOrEmpty(PendingText)) return null;
        return new TextLayer(s.Value, PendingText, Color, FontSize);
    }

    public override void Cancel() => _start = null;
}
