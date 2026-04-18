using SkiaSharp;

namespace HeyManNiceShot.Editing;

public abstract record Layer;

public sealed record ArrowLayer(SKPoint Start, SKPoint End, SKColor Color, float StrokeWidth) : Layer;

public sealed record RectangleLayer(SKRect Rect, SKColor Color, float StrokeWidth, SKColor? Fill = null) : Layer;

public sealed record EllipseLayer(SKRect Rect, SKColor Color, float StrokeWidth, SKColor? Fill = null) : Layer;

public sealed record FreehandLayer(IReadOnlyList<SKPoint> Points, SKColor Color, float StrokeWidth) : Layer;

public sealed record TextLayer(SKPoint Position, string Text, SKColor Color, float FontSize) : Layer;

public sealed record BlurLayer(SKRect Rect, float Sigma) : Layer;
