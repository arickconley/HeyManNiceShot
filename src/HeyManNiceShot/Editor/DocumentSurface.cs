using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using HeyManNiceShot.Editing;
using SkiaSharp;

namespace HeyManNiceShot.Editor;

public sealed class DocumentSurface : Control
{
    public static readonly DirectProperty<DocumentSurface, Document?> DocumentProperty =
        AvaloniaProperty.RegisterDirect<DocumentSurface, Document?>(
            nameof(Document),
            o => o.Document,
            (o, v) => o.Document = v);

    public static readonly DirectProperty<DocumentSurface, Layer?> PreviewLayerProperty =
        AvaloniaProperty.RegisterDirect<DocumentSurface, Layer?>(
            nameof(PreviewLayer),
            o => o.PreviewLayer,
            (o, v) => o.PreviewLayer = v);

    private Document? _document;
    private Layer? _previewLayer;

    public Document? Document
    {
        get => _document;
        set
        {
            if (SetAndRaise(DocumentProperty, ref _document, value))
                InvalidateVisual();
        }
    }

    public Layer? PreviewLayer
    {
        get => _previewLayer;
        set
        {
            if (SetAndRaise(PreviewLayerProperty, ref _previewLayer, value))
                InvalidateVisual();
        }
    }

    /// Maps a viewport point (DIPs within this control) to image-local coordinates
    /// inside the flattened document (excluding padding — i.e. the same coord space
    /// annotation Layers live in).
    public SKPoint? ViewportToImage(Point viewport)
    {
        if (_document is null) return null;
        var docW = _document.Bitmap.Width + _document.Padding * 2;
        var docH = _document.Bitmap.Height + _document.Padding * 2;
        if (docW <= 0 || docH <= 0) return null;

        var scale = Math.Min(Bounds.Width / docW, Bounds.Height / docH);
        if (scale <= 0) return null;

        var renderW = docW * scale;
        var renderH = docH * scale;
        var offsetX = (Bounds.Width - renderW) / 2;
        var offsetY = (Bounds.Height - renderH) / 2;

        var docX = (viewport.X - offsetX) / scale - _document.Padding;
        var docY = (viewport.Y - offsetY) / scale - _document.Padding;
        return new SKPoint((float)docX, (float)docY);
    }

    public override void Render(DrawingContext context)
    {
        if (_document is null) return;

        var renderDoc = _previewLayer is null
            ? _document
            : _document.AddAnnotation(_previewLayer);

        context.Custom(new DocumentDrawOp(Bounds, renderDoc));
    }
}

internal sealed class DocumentDrawOp : ICustomDrawOperation
{
    private readonly Rect _bounds;
    private readonly Document _document;

    public DocumentDrawOp(Rect bounds, Document document)
    {
        _bounds = bounds;
        _document = document;
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
            using var flat = Renderer.Flatten(_document);

            DrawCheckerboard(canvas);

            var docW = flat.Width;
            var docH = flat.Height;
            var scale = (float)Math.Min(_bounds.Width / docW, _bounds.Height / docH);
            if (scale <= 0) return;

            var renderW = docW * scale;
            var renderH = docH * scale;
            var offsetX = (float)((_bounds.Width - renderW) / 2);
            var offsetY = (float)((_bounds.Height - renderH) / 2);

            var dest = new SKRect(offsetX, offsetY, offsetX + renderW, offsetY + renderH);
            using var paint = new SKPaint { FilterQuality = SKFilterQuality.High };
            canvas.DrawBitmap(flat, dest, paint);
        }
        finally
        {
            lease.Dispose();
        }
    }

    private void DrawCheckerboard(SKCanvas canvas)
    {
        const int squareSize = 12;
        var rect = new SKRect(0, 0, (float)_bounds.Width, (float)_bounds.Height);
        using var dark  = new SKPaint { Color = new SKColor(0x18, 0x18, 0x18) };
        using var light = new SKPaint { Color = new SKColor(0x22, 0x22, 0x22) };
        canvas.DrawRect(rect, dark);
        for (var y = 0; y < _bounds.Height; y += squareSize)
        for (var x = 0; x < _bounds.Width;  x += squareSize)
        {
            if (((x / squareSize) + (y / squareSize)) % 2 == 0) continue;
            canvas.DrawRect(SKRect.Create(x, y, squareSize, squareSize), light);
        }
    }
}
