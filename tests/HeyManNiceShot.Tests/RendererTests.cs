using HeyManNiceShot.Editing;
using SkiaSharp;
using Xunit;

namespace HeyManNiceShot.Tests;

public class RendererTests
{
    [Fact]
    public void Flatten_EmptyDocument_MatchesBaseSize()
    {
        using var bitmap = new SKBitmap(100, 50);
        var doc = new Document(bitmap);
        using var flat = Renderer.Flatten(doc);
        Assert.Equal(100, flat.Width);
        Assert.Equal(50, flat.Height);
    }

    [Fact]
    public void Flatten_WithPadding_AddsPaddingToBothDimensions()
    {
        using var bitmap = new SKBitmap(100, 50);
        var doc = new Document(bitmap).WithPadding(20);
        using var flat = Renderer.Flatten(doc);
        Assert.Equal(140, flat.Width);
        Assert.Equal(90, flat.Height);
    }

    [Fact]
    public void Flatten_WithSolidBackground_FillsPaddingArea()
    {
        using var bitmap = new SKBitmap(10, 10);
        bitmap.Erase(SKColors.Transparent);
        var doc = new Document(bitmap)
            .WithPadding(5)
            .WithBackground(new DocumentBackground(BackgroundType.Solid, SKColors.Red));
        using var flat = Renderer.Flatten(doc);
        Assert.Equal(SKColors.Red, flat.GetPixel(0, 0));
    }

    [Fact]
    public void Flatten_WithAnnotation_Succeeds()
    {
        using var bitmap = new SKBitmap(50, 50);
        bitmap.Erase(SKColors.White);
        var doc = new Document(bitmap)
            .AddAnnotation(new RectangleLayer(new SKRect(10, 10, 40, 40), SKColors.Red, 2, null));
        using var flat = Renderer.Flatten(doc);
        Assert.NotNull(flat);
        Assert.Equal(50, flat.Width);
    }

    [Fact]
    public void Flatten_WithDropShadowAndPadding_ProducesExpectedSize()
    {
        using var bitmap = new SKBitmap(100, 100);
        var doc = new Document(bitmap).WithPadding(24).WithCornerRadius(12).WithDropShadow(true);
        using var flat = Renderer.Flatten(doc);
        Assert.Equal(148, flat.Width);
        Assert.Equal(148, flat.Height);
    }
}
