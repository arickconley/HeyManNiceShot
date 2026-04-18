using SkiaSharp;

namespace HeyManNiceShot.Editing;

public static class Renderer
{
    public static SKBitmap Flatten(Document doc)
    {
        var pad = doc.Padding;
        var imageW = doc.Bitmap.Width;
        var imageH = doc.Bitmap.Height;
        var totalW = imageW + pad * 2;
        var totalH = imageH + pad * 2;

        var output = new SKBitmap(new SKImageInfo(totalW, totalH, SKColorType.Bgra8888, SKAlphaType.Premul));
        using var canvas = new SKCanvas(output);
        canvas.Clear(SKColors.Transparent);

        if (doc.Background.Type == BackgroundType.Solid)
            canvas.Clear(doc.Background.Color);

        var imageRect = SKRect.Create(pad, pad, imageW, imageH);

        if (doc.DropShadow && pad > 0)
            DrawShadow(canvas, imageRect, doc.CornerRadius);

        canvas.Save();
        if (doc.CornerRadius > 0)
        {
            using var clip = new SKPath();
            clip.AddRoundRect(imageRect, doc.CornerRadius, doc.CornerRadius);
            canvas.ClipPath(clip, antialias: true);
        }

        canvas.DrawBitmap(doc.Bitmap, imageRect);

        canvas.Save();
        canvas.Translate(imageRect.Left, imageRect.Top);
        foreach (var layer in doc.Annotations)
            DrawLayer(canvas, layer, doc.Bitmap);
        canvas.Restore();

        canvas.Restore();
        return output;
    }

    private static void DrawShadow(SKCanvas canvas, SKRect imageRect, int cornerRadius)
    {
        using var shadowPaint = new SKPaint
        {
            Color = SKColors.Black.WithAlpha(140),
            MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 12),
            IsAntialias = true,
        };
        var shadowRect = new SKRect(imageRect.Left, imageRect.Top + 6, imageRect.Right, imageRect.Bottom + 8);
        if (cornerRadius > 0)
            canvas.DrawRoundRect(shadowRect, cornerRadius, cornerRadius, shadowPaint);
        else
            canvas.DrawRect(shadowRect, shadowPaint);
    }

    public static void DrawLayer(SKCanvas canvas, Layer layer, SKBitmap baseBitmap)
    {
        switch (layer)
        {
            case ArrowLayer a:     DrawArrow(canvas, a); break;
            case RectangleLayer r: DrawRectangle(canvas, r); break;
            case EllipseLayer e:   DrawEllipse(canvas, e); break;
            case FreehandLayer f:  DrawFreehand(canvas, f); break;
            case TextLayer t:      DrawText(canvas, t); break;
            case BlurLayer b:      DrawBlur(canvas, b, baseBitmap); break;
        }
    }

    private static void DrawArrow(SKCanvas canvas, ArrowLayer a)
    {
        using var paint = new SKPaint
        {
            Color = a.Color,
            StrokeWidth = a.StrokeWidth,
            Style = SKPaintStyle.Stroke,
            StrokeCap = SKStrokeCap.Round,
            IsAntialias = true,
        };
        canvas.DrawLine(a.Start, a.End, paint);

        var head = a.StrokeWidth * 4;
        var dx = a.End.X - a.Start.X;
        var dy = a.End.Y - a.Start.Y;
        var len = MathF.Sqrt(dx * dx + dy * dy);
        if (len < 0.5f) return;
        var ux = dx / len;
        var uy = dy / len;
        var px = -uy;
        var py = ux;
        var baseX = a.End.X - ux * head;
        var baseY = a.End.Y - uy * head;
        var p1 = new SKPoint(baseX + px * head * 0.5f, baseY + py * head * 0.5f);
        var p2 = new SKPoint(baseX - px * head * 0.5f, baseY - py * head * 0.5f);

        using var fill = new SKPaint { Color = a.Color, IsAntialias = true, Style = SKPaintStyle.Fill };
        using var path = new SKPath();
        path.MoveTo(a.End);
        path.LineTo(p1);
        path.LineTo(p2);
        path.Close();
        canvas.DrawPath(path, fill);
    }

    private static void DrawRectangle(SKCanvas canvas, RectangleLayer r)
    {
        if (r.Fill is { } fillColor)
        {
            using var fill = new SKPaint { Color = fillColor, IsAntialias = true, Style = SKPaintStyle.Fill };
            canvas.DrawRect(r.Rect, fill);
        }
        using var stroke = new SKPaint
        {
            Color = r.Color,
            StrokeWidth = r.StrokeWidth,
            Style = SKPaintStyle.Stroke,
            IsAntialias = true,
        };
        canvas.DrawRect(r.Rect, stroke);
    }

    private static void DrawEllipse(SKCanvas canvas, EllipseLayer e)
    {
        if (e.Fill is { } fillColor)
        {
            using var fill = new SKPaint { Color = fillColor, IsAntialias = true, Style = SKPaintStyle.Fill };
            canvas.DrawOval(e.Rect, fill);
        }
        using var stroke = new SKPaint
        {
            Color = e.Color,
            StrokeWidth = e.StrokeWidth,
            Style = SKPaintStyle.Stroke,
            IsAntialias = true,
        };
        canvas.DrawOval(e.Rect, stroke);
    }

    private static void DrawFreehand(SKCanvas canvas, FreehandLayer f)
    {
        if (f.Points.Count < 2) return;
        using var paint = new SKPaint
        {
            Color = f.Color,
            StrokeWidth = f.StrokeWidth,
            Style = SKPaintStyle.Stroke,
            StrokeCap = SKStrokeCap.Round,
            StrokeJoin = SKStrokeJoin.Round,
            IsAntialias = true,
        };
        using var path = new SKPath();
        path.MoveTo(f.Points[0]);
        for (var i = 1; i < f.Points.Count; i++)
            path.LineTo(f.Points[i]);
        canvas.DrawPath(path, paint);
    }

    private static void DrawText(SKCanvas canvas, TextLayer t)
    {
        using var paint = new SKPaint
        {
            Color = t.Color,
            TextSize = t.FontSize,
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName("Segoe UI Variable", SKFontStyle.Normal)
                ?? SKTypeface.Default,
        };
        canvas.DrawText(t.Text, t.Position.X, t.Position.Y + t.FontSize, paint);
    }

    private static void DrawBlur(SKCanvas canvas, BlurLayer b, SKBitmap baseBitmap)
    {
        canvas.Save();
        canvas.ClipRect(b.Rect, antialias: true);
        using var paint = new SKPaint
        {
            ImageFilter = SKImageFilter.CreateBlur(b.Sigma, b.Sigma),
        };
        var srcRect = SKRect.Create(0, 0, baseBitmap.Width, baseBitmap.Height);
        canvas.DrawBitmap(baseBitmap, srcRect, paint);
        canvas.Restore();
    }
}
