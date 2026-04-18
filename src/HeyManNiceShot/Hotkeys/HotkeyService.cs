using Avalonia.Input;
using HeyManNiceShot.Settings;
using System.Runtime.InteropServices;

namespace HeyManNiceShot.Hotkeys;

public sealed class HotkeyService : IDisposable
{
    private const int HotkeyId = 1;
    private const int WM_HOTKEY = 0x0312;
    private const uint MOD_ALT = 0x0001;
    private const uint MOD_CONTROL = 0x0002;
    private const uint MOD_SHIFT = 0x0004;
    private const uint MOD_WIN = 0x0008;
    private const uint MOD_NOREPEAT = 0x4000;

    private readonly MessageWindow _window;
    private bool _registered;

    public event Action? Pressed;

    public HotkeyService()
    {
        _window = new MessageWindow(OnMessage);
    }

    public bool Register(HotkeyChord chord)
    {
        if (_registered)
        {
            UnregisterHotKey(_window.Handle, HotkeyId);
            _registered = false;
        }

        var mods = ToWin32Mods(chord.Modifiers) | MOD_NOREPEAT;
        var vk = ToWin32Vk(chord.Key);
        if (vk == 0) return false;

        _registered = RegisterHotKey(_window.Handle, HotkeyId, mods, vk);
        return _registered;
    }

    public void Unregister()
    {
        if (_registered)
        {
            UnregisterHotKey(_window.Handle, HotkeyId);
            _registered = false;
        }
    }

    private void OnMessage(uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg == WM_HOTKEY && wParam.ToInt32() == HotkeyId)
            Pressed?.Invoke();
    }

    public void Dispose()
    {
        Unregister();
        _window.Dispose();
    }

    private static uint ToWin32Mods(KeyModifiers modifiers)
    {
        uint result = 0;
        if (modifiers.HasFlag(KeyModifiers.Control)) result |= MOD_CONTROL;
        if (modifiers.HasFlag(KeyModifiers.Shift))   result |= MOD_SHIFT;
        if (modifiers.HasFlag(KeyModifiers.Alt))     result |= MOD_ALT;
        if (modifiers.HasFlag(KeyModifiers.Meta))    result |= MOD_WIN;
        return result;
    }

    private static uint ToWin32Vk(Key key)
    {
        if (key is >= Key.D0 and <= Key.D9) return (uint)(0x30 + (key - Key.D0));
        if (key is >= Key.A and <= Key.Z)   return (uint)(0x41 + (key - Key.A));
        if (key is >= Key.F1 and <= Key.F12) return (uint)(0x70 + (key - Key.F1));
        return key switch
        {
            Key.Space     => 0x20,
            Key.Enter     => 0x0D,
            Key.Escape    => 0x1B,
            Key.Tab       => 0x09,
            Key.Back      => 0x08,
            Key.Insert    => 0x2D,
            Key.Delete    => 0x2E,
            Key.Home      => 0x24,
            Key.End       => 0x23,
            Key.PageUp    => 0x21,
            Key.PageDown  => 0x22,
            Key.Left      => 0x25,
            Key.Up        => 0x26,
            Key.Right     => 0x27,
            Key.Down      => 0x28,
            _             => 0,
        };
    }

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}
