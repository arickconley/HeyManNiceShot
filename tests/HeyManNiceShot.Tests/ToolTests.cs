using HeyManNiceShot.Editing;
using SkiaSharp;
using Xunit;

namespace HeyManNiceShot.Tests;

public class ToolTests
{
    [Fact]
    public void ArrowTool_CompleteDrag_ReturnsArrowLayer()
    {
        var tool = new ArrowTool();
        tool.OnPointerDown(new SKPoint(0, 0));
        tool.OnPointerMove(new SKPoint(10, 10));
        var layer = tool.OnPointerUp(new SKPoint(10, 10));
        Assert.IsType<ArrowLayer>(layer);
    }

    [Fact]
    public void ArrowTool_DegenerateDrag_ReturnsNull()
    {
        var tool = new ArrowTool();
        tool.OnPointerDown(new SKPoint(0, 0));
        var layer = tool.OnPointerUp(new SKPoint(2, 2));
        Assert.Null(layer);
    }

    [Fact]
    public void RectangleTool_NormalizesReversedDrag()
    {
        var tool = new RectangleTool();
        tool.OnPointerDown(new SKPoint(50, 50));
        var layer = tool.OnPointerUp(new SKPoint(10, 10)) as RectangleLayer;
        Assert.NotNull(layer);
        Assert.Equal(10, layer!.Rect.Left);
        Assert.Equal(10, layer.Rect.Top);
        Assert.Equal(50, layer.Rect.Right);
        Assert.Equal(50, layer.Rect.Bottom);
    }

    [Fact]
    public void EllipseTool_CarriesColorAndStrokeFromTool()
    {
        var tool = new EllipseTool { Color = SKColors.Lime, StrokeWidth = 7 };
        tool.OnPointerDown(new SKPoint(0, 0));
        var layer = tool.OnPointerUp(new SKPoint(20, 20)) as EllipseLayer;
        Assert.NotNull(layer);
        Assert.Equal(SKColors.Lime, layer!.Color);
        Assert.Equal(7, layer.StrokeWidth);
    }

    [Fact]
    public void FreehandTool_AccumulatesPoints()
    {
        var tool = new FreehandTool();
        tool.OnPointerDown(new SKPoint(0, 0));
        tool.OnPointerMove(new SKPoint(5, 5));
        tool.OnPointerMove(new SKPoint(10, 10));
        var layer = tool.OnPointerUp(new SKPoint(10, 10)) as FreehandLayer;
        Assert.NotNull(layer);
        Assert.Equal(3, layer!.Points.Count);
    }

    [Fact]
    public void FreehandTool_SinglePoint_ReturnsNull()
    {
        var tool = new FreehandTool();
        tool.OnPointerDown(new SKPoint(0, 0));
        var layer = tool.OnPointerUp(new SKPoint(0, 0));
        Assert.Null(layer);
    }

    [Fact]
    public void TextTool_OnRelease_CreatesTextLayer()
    {
        var tool = new TextTool { PendingText = "hello" };
        tool.OnPointerDown(new SKPoint(20, 30));
        var layer = tool.OnPointerUp(new SKPoint(20, 30)) as TextLayer;
        Assert.NotNull(layer);
        Assert.Equal("hello", layer!.Text);
        Assert.Equal(20, layer.Position.X);
    }

    [Fact]
    public void BlurTool_NormalizesDragRect()
    {
        var tool = new BlurTool { Sigma = 8 };
        tool.OnPointerDown(new SKPoint(40, 60));
        var layer = tool.OnPointerUp(new SKPoint(10, 30)) as BlurLayer;
        Assert.NotNull(layer);
        Assert.Equal(10, layer!.Rect.Left);
        Assert.Equal(30, layer.Rect.Top);
        Assert.Equal(8, layer.Sigma);
    }

    [Fact]
    public void SelectTool_DoesNothing()
    {
        var tool = new SelectTool();
        tool.OnPointerDown(new SKPoint(0, 0));
        tool.OnPointerMove(new SKPoint(50, 50));
        var layer = tool.OnPointerUp(new SKPoint(50, 50));
        Assert.Null(layer);
    }
}
