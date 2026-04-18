using SkiaSharp;

namespace HeyManNiceShot.Editing;

public enum BackgroundType { None, Solid }

public sealed record DocumentBackground(BackgroundType Type, SKColor Color)
{
    public static DocumentBackground None { get; } = new(BackgroundType.None, SKColors.Transparent);
    public static DocumentBackground SolidDark { get; } = new(BackgroundType.Solid, new SKColor(0x1E, 0x1E, 0x1E));
}

public sealed record Document(
    SKBitmap Bitmap,
    IReadOnlyList<Layer> Annotations,
    DocumentBackground Background,
    int Padding,
    int CornerRadius,
    bool DropShadow)
{
    public Document(SKBitmap bitmap)
        : this(bitmap, Array.Empty<Layer>(), DocumentBackground.None, Padding: 0, CornerRadius: 0, DropShadow: false) { }

    public Document AddAnnotation(Layer layer)
    {
        var next = new List<Layer>(Annotations.Count + 1);
        next.AddRange(Annotations);
        next.Add(layer);
        return this with { Annotations = next };
    }

    public Document RemoveLastAnnotation()
        => Annotations.Count == 0
            ? this
            : this with { Annotations = Annotations.Take(Annotations.Count - 1).ToArray() };

    public Document WithAnnotations(IReadOnlyList<Layer> annotations) => this with { Annotations = annotations };
    public Document WithBackground(DocumentBackground bg) => this with { Background = bg };
    public Document WithPadding(int padding) => this with { Padding = Math.Max(0, padding) };
    public Document WithCornerRadius(int radius) => this with { CornerRadius = Math.Max(0, radius) };
    public Document WithDropShadow(bool on) => this with { DropShadow = on };
}
