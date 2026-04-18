using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using HeyManNiceShot.Geometry;

namespace HeyManNiceShot.Overlay;

public partial class OverlayWindow : Window
{
    private readonly VirtualScreen? _vs;
    private readonly TaskCompletionSource<OverlayResult?> _tcs = new();

    private Point? _dragStart;
    private Rect? _lockedSelection;

    public OverlayWindow() : this(null) { }

    public OverlayWindow(VirtualScreen? vs)
    {
        _vs = vs;
        InitializeComponent();

        if (_vs is not null)
        {
            Layer.PointerPressed  += OnPointerPressed;
            Layer.PointerMoved    += OnPointerMoved;
            Layer.PointerReleased += OnPointerReleased;
            KeyDown += OnKeyDown;
            Opened += OnOpenedHandler;
        }
    }

    public Task<OverlayResult?> PickAsync()
    {
        Show();
        Activate();
        Focus();
        return _tcs.Task;
    }

    private void OnOpenedHandler(object? sender, EventArgs e)
    {
        if (_vs is null) return;
        var scale = _vs.Primary.DpiScale;
        Position = new PixelPoint(_vs.PhysicalBounds.X, _vs.PhysicalBounds.Y);
        Width  = _vs.PhysicalBounds.Width  / scale;
        Height = _vs.PhysicalBounds.Height / scale;
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_lockedSelection is not null) return;
        var p = e.GetPosition(Layer);
        _dragStart = p;
        Layer.Selection = new Rect(p, p);
        e.Pointer.Capture(Layer);
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_dragStart is null || _lockedSelection is not null) return;
        var p = e.GetPosition(Layer);
        var rect = Normalize(_dragStart.Value, p);
        Layer.Selection = rect;
        UpdateDimensionBadge(p, rect);
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        e.Pointer.Capture(null);

        if (_dragStart is null)
        {
            Complete(null);
            return;
        }

        var p = e.GetPosition(Layer);
        var rect = Normalize(_dragStart.Value, p);
        _dragStart = null;

        if (rect.Width < 4 || rect.Height < 4)
        {
            Complete(null);
            return;
        }

        _lockedSelection = rect;
        Layer.Selection = rect;
        DimensionBadge.IsVisible = false;
        ShowSelectionToolbar(rect);
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
            Complete(null);
    }

    private void UpdateDimensionBadge(Point cursor, Rect selection)
    {
        if (_vs is null) return;
        var scale = _vs.Primary.DpiScale;
        var pixelW = (int)Math.Round(selection.Width * scale);
        var pixelH = (int)Math.Round(selection.Height * scale);
        DimensionText.Text = $"{pixelW} × {pixelH}";

        DimensionBadge.Measure(Size.Infinity);
        var badgeWidth = DimensionBadge.DesiredSize.Width;
        var badgeHeight = DimensionBadge.DesiredSize.Height;

        var x = cursor.X + 16;
        var y = cursor.Y + 16;
        if (x + badgeWidth + 8 > Bounds.Width) x = cursor.X - badgeWidth - 16;
        if (y + badgeHeight + 8 > Bounds.Height) y = cursor.Y - badgeHeight - 16;

        Canvas.SetLeft(DimensionBadge, Math.Max(8, x));
        Canvas.SetTop(DimensionBadge, Math.Max(8, y));
        DimensionBadge.IsVisible = true;
    }

    private void ShowSelectionToolbar(Rect sel)
    {
        SelectionToolbar.Measure(Size.Infinity);
        var tbWidth = SelectionToolbar.DesiredSize.Width;
        var tbHeight = SelectionToolbar.DesiredSize.Height;

        var x = sel.X + (sel.Width - tbWidth) / 2;
        x = Math.Max(8, Math.Min(Bounds.Width - tbWidth - 8, x));

        var y = sel.Bottom + 10;
        if (y + tbHeight + 8 > Bounds.Height) y = sel.Y - tbHeight - 10;
        if (y < 8) y = 8;

        Canvas.SetLeft(SelectionToolbar, x);
        Canvas.SetTop(SelectionToolbar, y);
        SelectionToolbar.IsVisible = true;
    }

    private void OnCopyClick(object? sender, RoutedEventArgs e)   => CompleteWith(OverlayAction.Copy);
    private void OnSaveClick(object? sender, RoutedEventArgs e)   => CompleteWith(OverlayAction.Save);
    private void OnEditClick(object? sender, RoutedEventArgs e)   => CompleteWith(OverlayAction.Edit);
    private void OnCancelClick(object? sender, RoutedEventArgs e) => Complete(null);

    private void CompleteWith(OverlayAction action)
    {
        if (_lockedSelection is not Rect sel || _vs is null) { Complete(null); return; }
        var scale = _vs.Primary.DpiScale;
        var pixelX = Position.X + (int)Math.Round(sel.X * scale);
        var pixelY = Position.Y + (int)Math.Round(sel.Y * scale);
        var pixelW = (int)Math.Round(sel.Width * scale);
        var pixelH = (int)Math.Round(sel.Height * scale);
        Complete(new OverlayResult(new PhysicalRect(pixelX, pixelY, pixelW, pixelH), action));
    }

    private void Complete(OverlayResult? result)
    {
        Close();
        _tcs.TrySetResult(result);
    }

    private static Rect Normalize(Point a, Point b)
    {
        var x = Math.Min(a.X, b.X);
        var y = Math.Min(a.Y, b.Y);
        var w = Math.Abs(b.X - a.X);
        var h = Math.Abs(b.Y - a.Y);
        return new Rect(x, y, w, h);
    }
}
