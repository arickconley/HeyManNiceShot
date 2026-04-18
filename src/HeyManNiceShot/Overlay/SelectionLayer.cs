using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using SkiaSharp;

namespace HeyManNiceShot.Overlay;

public sealed class SelectionLayer : Control
{
    public static readonly StyledProperty<Rect?> SelectionProperty =
        AvaloniaProperty.Register<SelectionLayer, Rect?>(nameof(Selection));

    public static readonly StyledProperty<Color> DimColorProperty =
        AvaloniaProperty.Register<SelectionLayer, Color>(nameof(DimColor),
            defaultValue: Color.FromArgb(140, 0, 0, 0));

    public static readonly StyledProperty<Color> BorderColorProperty =
        AvaloniaProperty.Register<SelectionLayer, Color>(nameof(BorderColor),
            defaultValue: Color.FromArgb(255, 0x4C, 0xC2, 0xFF));

    public static readonly StyledProperty<double> BorderWidthProperty =
        AvaloniaProperty.Register<SelectionLayer, double>(nameof(BorderWidth),
            defaultValue: 2.0);

    static SelectionLayer()
    {
        AffectsRender<SelectionLayer>(SelectionProperty, DimColorProperty, BorderColorProperty, BorderWidthProperty);
    }

    public Rect? Selection
    {
        get => GetValue(SelectionProperty);
        set => SetValue(SelectionProperty, value);
    }

    public Color DimColor
    {
        get => GetValue(DimColorProperty);
        set => SetValue(DimColorProperty, value);
    }

    public Color BorderColor
    {
        get => GetValue(BorderColorProperty);
        set => SetValue(BorderColorProperty, value);
    }

    public double BorderWidth
    {
        get => GetValue(BorderWidthProperty);
        set => SetValue(BorderWidthProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        context.Custom(new SelectionDrawOp(Bounds, Selection, DimColor, BorderColor, BorderWidth));
    }
}

internal sealed class SelectionDrawOp : ICustomDrawOperation
{
    private readonly Rect _bounds;
    private readonly Rect? _selection;
    private readonly Color _dim;
    private readonly Color _border;
    private readonly double _borderWidth;

    public SelectionDrawOp(Rect bounds, Rect? selection, Color dim, Color border, double borderWidth)
    {
        _bounds = bounds;
        _selection = selection;
        _dim = dim;
        _border = border;
        _borderWidth = borderWidth;
    }

    public Rect Bounds => _bounds;
    public bool HitTest(Point p) => false;
    public bool Equals(ICustomDrawOperation? other) => false;
    public void Dispose() { }

    public void Render(ImmediateDrawingContext context)
    {
        var lease = context.TryGetFeature<ISkiaSharpApiLeaseFeature>()?.Lease();
        if (lease is null) return;
        try
        {
            var canvas = lease.SkCanvas;
            var fullRect = SKRect.Create(0, 0, (float)_bounds.Width, (float)_bounds.Height);

            using var dim = new SKPaint
            {
                Color = new SKColor(_dim.R, _dim.G, _dim.B, _dim.A),
            };

            if (_selection is { } sel && sel.Width > 0 && sel.Height > 0)
            {
                var skSel = new SKRect(
                    (float)sel.X, (float)sel.Y,
                    (float)(sel.X + sel.Width), (float)(sel.Y + sel.Height));

                canvas.Save();
                canvas.ClipRect(skSel, SKClipOperation.Difference, antialias: false);
                canvas.DrawRect(fullRect, dim);
                canvas.Restore();

                using var border = new SKPaint
                {
                    Color = new SKColor(_border.R, _border.G, _border.B, _border.A),
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = (float)_borderWidth,
                    IsAntialias = true,
                };
                canvas.DrawRect(skSel, border);
            }
            else
            {
                canvas.DrawRect(fullRect, dim);
            }
        }
        finally
        {
            lease.Dispose();
        }
    }
}
