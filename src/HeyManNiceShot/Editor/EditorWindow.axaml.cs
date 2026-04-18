using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using HeyManNiceShot.Editing;
using HeyManNiceShot.Export;
using SkiaSharp;

namespace HeyManNiceShot.Editor;

public partial class EditorWindow : Window
{
    private Document _document = null!;
    private readonly UndoStack<Document> _history = new();
    private Tool _tool = new ArrowTool();
    private readonly App? _app;

    private static readonly Dictionary<string, Func<Tool>> ToolFactories = new()
    {
        ["Select"]    = () => new SelectTool(),
        ["Arrow"]     = () => new ArrowTool(),
        ["Rectangle"] = () => new RectangleTool(),
        ["Ellipse"]   = () => new EllipseTool(),
        ["Pen"]       = () => new FreehandTool(),
        ["Text"]      = () => new TextTool(),
        ["Blur"]      = () => new BlurTool(),
    };

    public EditorWindow() : this(null!, null) { }

    public EditorWindow(Document document, App? app)
    {
        _app = app;
        InitializeComponent();

        if (document is not null)
        {
            _document = document;
            _history.Push(_document);
            Surface.Document = _document;

            Surface.PointerPressed  += OnSurfacePointerPressed;
            Surface.PointerMoved    += OnSurfacePointerMoved;
            Surface.PointerReleased += OnSurfacePointerReleased;
            KeyDown += OnKeyDown;

            ApplyToolStyle();
            UpdateUndoRedoState();
        }
    }

    // ---- pointer routing ----

    private void OnSurfacePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var p = Surface.ViewportToImage(e.GetPosition(Surface));
        if (p is null) return;
        _tool.OnPointerDown(p.Value);
        Surface.PreviewLayer = _tool.Preview();
        e.Pointer.Capture(Surface);
    }

    private void OnSurfacePointerMoved(object? sender, PointerEventArgs e)
    {
        if (e.Pointer.Captured != Surface) return;
        var p = Surface.ViewportToImage(e.GetPosition(Surface));
        if (p is null) return;
        _tool.OnPointerMove(p.Value);
        Surface.PreviewLayer = _tool.Preview();
    }

    private void OnSurfacePointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (e.Pointer.Captured == Surface) e.Pointer.Capture(null);
        var p = Surface.ViewportToImage(e.GetPosition(Surface));
        if (p is null) { _tool.Cancel(); Surface.PreviewLayer = null; return; }

        var layer = _tool.OnPointerUp(p.Value);
        Surface.PreviewLayer = null;

        if (layer is not null)
        {
            _document = _document.AddAnnotation(layer);
            _history.Push(_document);
            Surface.Document = _document;
            UpdateUndoRedoState();
        }
    }

    // ---- keyboard shortcuts ----

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            switch (e.Key)
            {
                case Key.Z: OnUndoClick(this, new RoutedEventArgs()); e.Handled = true; break;
                case Key.Y: OnRedoClick(this, new RoutedEventArgs()); e.Handled = true; break;
                case Key.C: OnCopyClick(this, new RoutedEventArgs()); e.Handled = true; break;
                case Key.S: OnSaveClick(this, new RoutedEventArgs()); e.Handled = true; break;
            }
        }
    }

    // ---- toolbar handlers ----

    private void OnUndoClick(object? sender, RoutedEventArgs e)
    {
        if (!_history.CanUndo) return;
        _document = _history.Undo();
        Surface.Document = _document;
        UpdateUndoRedoState();
    }

    private void OnRedoClick(object? sender, RoutedEventArgs e)
    {
        if (!_history.CanRedo) return;
        _document = _history.Redo();
        Surface.Document = _document;
        UpdateUndoRedoState();
    }

    private void OnCopyClick(object? sender, RoutedEventArgs e)
    {
        try { Exporter.Copy(_document); }
        catch (Exception ex) { CrashLog.Write("EditorCopy", ex); }
    }

    private async void OnSaveClick(object? sender, RoutedEventArgs e)
    {
        if (_app is null) return;
        try { await Exporter.SaveAsync(_document, _app.Settings); }
        catch (Exception ex) { CrashLog.Write("EditorSave", ex); }
    }

    private void OnToolClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not ToggleButton btn) return;
        var key = btn.Tag?.ToString();
        if (key is null || !ToolFactories.TryGetValue(key, out var factory)) return;

        _tool = factory();
        ApplyToolStyle();

        // Make this the only checked tool
        foreach (var other in new[] { ToolSelect, ToolArrow, ToolRectangle, ToolEllipse, ToolPen, ToolText, ToolBlur })
            other.IsChecked = ReferenceEquals(other, btn);
    }

    private void OnToolColorChanged(object? sender, ColorChangedEventArgs e)
    {
        var c = e.NewColor;
        _tool.Color = new SKColor(c.R, c.G, c.B, c.A);
    }

    private void OnToolStrokeChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        _tool.StrokeWidth = (float)e.NewValue;
    }

    // ---- footer / document controls ----

    private void OnBackgroundChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (BackgroundCombo.SelectedIndex == 0)
        {
            BackgroundColorPicker.IsEnabled = false;
            ApplyDocChange(_document.WithBackground(DocumentBackground.None));
        }
        else
        {
            BackgroundColorPicker.IsEnabled = true;
            var c = BackgroundColorPicker.Color;
            ApplyDocChange(_document.WithBackground(new DocumentBackground(BackgroundType.Solid, new SKColor(c.R, c.G, c.B, c.A))));
        }
    }

    private void OnBackgroundColorChanged(object? sender, ColorChangedEventArgs e)
    {
        if (_document.Background.Type != BackgroundType.Solid) return;
        var c = e.NewColor;
        ApplyDocChange(_document.WithBackground(new DocumentBackground(BackgroundType.Solid, new SKColor(c.R, c.G, c.B, c.A))));
    }

    private void OnPaddingChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        var v = (int)(e.NewValue ?? 0);
        if (v == _document.Padding) return;
        ApplyDocChange(_document.WithPadding(v));
    }

    private void OnCornerChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        var v = (int)e.NewValue;
        if (v == _document.CornerRadius) return;
        ApplyDocChange(_document.WithCornerRadius(v));
    }

    private void OnShadowToggled(object? sender, RoutedEventArgs e)
    {
        var on = ShadowToggle.IsChecked == true;
        if (on == _document.DropShadow) return;
        ApplyDocChange(_document.WithDropShadow(on));
    }

    private void ApplyDocChange(Document next)
    {
        _document = next;
        _history.Push(next);
        Surface.Document = _document;
        UpdateUndoRedoState();
    }

    // ---- helpers ----

    private void ApplyToolStyle()
    {
        var c = ToolColorPicker.Color;
        _tool.Color = new SKColor(c.R, c.G, c.B, c.A);
        _tool.StrokeWidth = (float)ToolStrokeSlider.Value;
    }

    private void UpdateUndoRedoState()
    {
        Dispatcher.UIThread.Post(() =>
        {
            UndoBtn.IsEnabled = _history.CanUndo;
            RedoBtn.IsEnabled = _history.CanRedo;
        });
    }
}
