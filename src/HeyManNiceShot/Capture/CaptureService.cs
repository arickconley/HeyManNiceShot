using HeyManNiceShot.Geometry;
using SkiaSharp;
using System.Runtime.InteropServices;

namespace HeyManNiceShot.Capture;

public sealed class CaptureService
{
    public Task<VirtualScreen> GetVirtualScreenAsync()
    {
        var monitors = EnumerateMonitors();
        var bounds = VirtualScreen.ComputeBounds(monitors);
        return Task.FromResult(new VirtualScreen(monitors, bounds));
    }

    public async Task<SKBitmap> CaptureRegionAsync(PhysicalRect region)
    {
        var vs = await GetVirtualScreenAsync().ConfigureAwait(false);
        var output = new SKBitmap(new SKImageInfo(
            region.Width, region.Height, SKColorType.Bgra8888, SKAlphaType.Premul));
        using var canvas = new SKCanvas(output);
        canvas.Clear(SKColors.Transparent);

        foreach (var monitor in vs.Monitors)
        {
            var inter = monitor.PhysicalBounds.Intersect(region);
            if (inter.IsEmpty) continue;

            using var capture = MonitorCapture.Create(monitor);
            using var monitorBitmap = await capture.NextFrameAsync().ConfigureAwait(false);

            var srcX = inter.X - monitor.PhysicalBounds.X;
            var srcY = inter.Y - monitor.PhysicalBounds.Y;
            var dstX = inter.X - region.X;
            var dstY = inter.Y - region.Y;

            canvas.DrawBitmap(
                monitorBitmap,
                source: new SKRect(srcX, srcY, srcX + inter.Width, srcY + inter.Height),
                dest:   new SKRect(dstX, dstY, dstX + inter.Width, dstY + inter.Height));
        }

        return output;
    }

    private static IReadOnlyList<MonitorInfo> EnumerateMonitors()
    {
        var list = new List<MonitorInfo>();
        bool Callback(IntPtr hMonitor, IntPtr hdc, IntPtr lprc, IntPtr data)
        {
            var info = new MONITORINFOEX { cbSize = Marshal.SizeOf<MONITORINFOEX>() };
            if (!GetMonitorInfo(hMonitor, ref info)) return true;

            uint dpiX = 96, dpiY = 96;
            try { GetDpiForMonitor(hMonitor, MonitorDpiType.Effective, out dpiX, out dpiY); }
            catch { /* fall back to 96 */ }

            list.Add(new MonitorInfo(
                Handle: hMonitor,
                PhysicalBounds: new PhysicalRect(
                    info.rcMonitor.Left,
                    info.rcMonitor.Top,
                    info.rcMonitor.Right - info.rcMonitor.Left,
                    info.rcMonitor.Bottom - info.rcMonitor.Top),
                DpiScale: dpiX / 96.0,
                IsPrimary: (info.dwFlags & MONITORINFOF_PRIMARY) != 0));
            return true;
        }

        EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, Callback, IntPtr.Zero);
        return list;
    }

    // ---- p/invoke ----

    private const int MONITORINFOF_PRIMARY = 1;

    private delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdc, IntPtr lprcMonitor, IntPtr dwData);

    [DllImport("user32.dll")]
    private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFOEX lpmi);

    [DllImport("Shcore.dll")]
    private static extern int GetDpiForMonitor(IntPtr hMonitor, MonitorDpiType dpiType, out uint dpiX, out uint dpiY);

    private enum MonitorDpiType { Effective = 0, Angular = 1, Raw = 2 }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT { public int Left, Top, Right, Bottom; }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct MONITORINFOEX
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string szDevice;
    }
}
