using Avalonia;
using Avalonia.Controls;
using SkiaSharp;

namespace HeyManNiceShot.Tray;

public sealed class TrayHost : IDisposable
{
    private readonly TrayIcon _icon;

    public event Action? CaptureRequested;
    public event Action? SettingsRequested;
    public event Action? QuitRequested;

    public TrayHost()
    {
        _icon = new TrayIcon
        {
            Icon = CreateIcon(),
            ToolTipText = "HeyManNiceShot",
            IsVisible = true,
            Menu = BuildMenu(),
        };
        _icon.Clicked += (_, _) => CaptureRequested?.Invoke();
    }

    public void Show()
    {
        TrayIcon.SetIcons(Application.Current!, new TrayIcons { _icon });
    }

    public void Dispose()
    {
        _icon.IsVisible = false;
        _icon.Dispose();
    }

    private NativeMenu BuildMenu()
    {
        var menu = new NativeMenu();

        var capture = new NativeMenuItem("Capture");
        capture.Click += (_, _) => CaptureRequested?.Invoke();
        menu.Add(capture);

        menu.Add(new NativeMenuItemSeparator());

        var settings = new NativeMenuItem("Settings…");
        settings.Click += (_, _) => SettingsRequested?.Invoke();
        menu.Add(settings);

        menu.Add(new NativeMenuItemSeparator());

        var quit = new NativeMenuItem("Quit");
        quit.Click += (_, _) => QuitRequested?.Invoke();
        menu.Add(quit);

        return menu;
    }

    private static WindowIcon CreateIcon()
    {
        using var bitmap = new SKBitmap(32, 32);
        using (var canvas = new SKCanvas(bitmap))
        {
            canvas.Clear(SKColors.Transparent);

            using var fill = new SKPaint
            {
                Color = new SKColor(0x4C, 0xC2, 0xFF),
                Style = SKPaintStyle.Fill,
                IsAntialias = true,
            };
            canvas.DrawRoundRect(new SKRect(2, 2, 30, 30), 7, 7, fill);

            using var inner = new SKPaint
            {
                Color = SKColors.White,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 2,
                IsAntialias = true,
            };
            canvas.DrawRoundRect(new SKRect(8, 10, 24, 22), 2, 2, inner);
        }

        using var img = SKImage.FromBitmap(bitmap);
        using var data = img.Encode(SKEncodedImageFormat.Png, 100);
        var ms = new MemoryStream(data.ToArray()) { Position = 0 };
        return new WindowIcon(ms);
    }
}
