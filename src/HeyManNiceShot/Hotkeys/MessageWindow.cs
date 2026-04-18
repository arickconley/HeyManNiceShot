using System.Runtime.InteropServices;

namespace HeyManNiceShot.Hotkeys;

internal sealed class MessageWindow : IDisposable
{
    private static readonly string ClassName = $"HeyManNiceShotMsgWindow_{Environment.ProcessId}";
    private static readonly IntPtr HWND_MESSAGE = new(-3);

    private readonly WndProcDelegate _wndProc;
    private readonly Action<uint, IntPtr, IntPtr> _callback;

    public IntPtr Handle { get; }

    public MessageWindow(Action<uint, IntPtr, IntPtr> callback)
    {
        _callback = callback;
        _wndProc = WndProc;

        var hInstance = GetModuleHandle(null);

        var classEx = new WNDCLASSEX
        {
            cbSize = Marshal.SizeOf<WNDCLASSEX>(),
            lpfnWndProc = Marshal.GetFunctionPointerForDelegate(_wndProc),
            hInstance = hInstance,
            lpszClassName = ClassName,
        };
        // Atom of 0 means already registered (acceptable).
        RegisterClassEx(ref classEx);

        Handle = CreateWindowEx(
            dwExStyle: 0,
            lpClassName: ClassName,
            lpWindowName: "HeyManNiceShot.Hotkeys",
            dwStyle: 0,
            x: 0, y: 0, width: 0, height: 0,
            hWndParent: HWND_MESSAGE,
            hMenu: IntPtr.Zero,
            hInstance: hInstance,
            lpParam: IntPtr.Zero);

        if (Handle == IntPtr.Zero)
            throw new InvalidOperationException(
                $"CreateWindowEx failed: 0x{Marshal.GetLastWin32Error():X8}");
    }

    private IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        try { _callback(msg, wParam, lParam); }
        catch { /* swallow — never let a callback escape into the pump */ }
        return DefWindowProc(hWnd, msg, wParam, lParam);
    }

    public void Dispose()
    {
        if (Handle != IntPtr.Zero) DestroyWindow(Handle);
        UnregisterClass(ClassName, GetModuleHandle(null));
    }

    private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WNDCLASSEX
    {
        public int cbSize;
        public uint style;
        public IntPtr lpfnWndProc;
        public int cbClsExtra;
        public int cbWndExtra;
        public IntPtr hInstance;
        public IntPtr hIcon;
        public IntPtr hCursor;
        public IntPtr hbrBackground;
        public string? lpszMenuName;
        public string lpszClassName;
        public IntPtr hIconSm;
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern ushort RegisterClassEx(ref WNDCLASSEX wcx);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr CreateWindowEx(
        uint dwExStyle,
        string lpClassName, string lpWindowName,
        uint dwStyle,
        int x, int y, int width, int height,
        IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

    [DllImport("user32.dll")]
    private static extern bool DestroyWindow(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr DefWindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool UnregisterClass(string lpClassName, IntPtr hInstance);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);
}
