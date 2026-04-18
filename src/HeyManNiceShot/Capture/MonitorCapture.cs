using HeyManNiceShot.Geometry;
using SkiaSharp;
using System.Runtime.InteropServices;
using Vortice.Direct3D11;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;
using Windows.Graphics.DirectX.Direct3D11;
using WinRT;

namespace HeyManNiceShot.Capture;

internal sealed class MonitorCapture : IDisposable
{
    private readonly ID3D11Device _d3d;
    private readonly IDirect3DDevice _winRt;
    private readonly Direct3D11CaptureFramePool _pool;
    private readonly GraphicsCaptureSession _session;
    private TaskCompletionSource<SKBitmap>? _pending;

    private MonitorCapture(ID3D11Device d3d, IDirect3DDevice winRt, GraphicsCaptureItem item)
    {
        _d3d = d3d;
        _winRt = winRt;

        _pool = Direct3D11CaptureFramePool.Create(
            _winRt,
            DirectXPixelFormat.B8G8R8A8UIntNormalized,
            numberOfBuffers: 2,
            item.Size);
        _pool.FrameArrived += OnFrameArrived;

        _session = _pool.CreateCaptureSession(item);
        TrySet(s => s.IsCursorCaptureEnabled = false);
        TrySet(s => s.IsBorderRequired = false);
        _session.StartCapture();
    }

    public static MonitorCapture Create(MonitorInfo monitor)
    {
        var (d3d, winRt) = Direct3D11Helper.CreateDevice();
        var item = CreateForMonitor(monitor.Handle);
        return new MonitorCapture(d3d, winRt, item);
    }

    public Task<SKBitmap> NextFrameAsync()
    {
        _pending = new TaskCompletionSource<SKBitmap>(TaskCreationOptions.RunContinuationsAsynchronously);
        return _pending.Task;
    }

    private void OnFrameArrived(Direct3D11CaptureFramePool sender, object args)
    {
        using var frame = sender.TryGetNextFrame();
        if (frame is null || _pending is null) return;
        try
        {
            var bitmap = Direct3D11Helper.SurfaceToBitmap(frame.Surface, _d3d);
            var tcs = _pending; _pending = null;
            tcs.TrySetResult(bitmap);
        }
        catch (Exception ex)
        {
            var tcs = _pending; _pending = null;
            tcs?.TrySetException(ex);
        }
    }

    private void TrySet(Action<GraphicsCaptureSession> apply)
    {
        try { apply(_session); } catch { /* older Windows builds without the property */ }
    }

    public void Dispose()
    {
        _session.Dispose();
        _pool.Dispose();
        _d3d.Dispose();
    }

    // ---- IGraphicsCaptureItemInterop bridge ----

    private static GraphicsCaptureItem CreateForMonitor(IntPtr hmonitor)
    {
        var interopIid = typeof(IGraphicsCaptureItemInterop).GUID;
        RoGetActivationFactory(
            "Windows.Graphics.Capture.GraphicsCaptureItem",
            in interopIid,
            out var factoryPtr);
        try
        {
            var interop = (IGraphicsCaptureItemInterop)Marshal.GetObjectForIUnknown(factoryPtr);
            var itemIid = typeof(GraphicsCaptureItem).GUID;
            var rawItem = interop.CreateForMonitor(hmonitor, itemIid);
            try
            {
                return MarshalInterface<GraphicsCaptureItem>.FromAbi(rawItem);
            }
            finally
            {
                Marshal.Release(rawItem);
            }
        }
        finally
        {
            Marshal.Release(factoryPtr);
        }
    }

    [DllImport("combase.dll", PreserveSig = false)]
    private static extern void RoGetActivationFactory(
        [MarshalAs(UnmanagedType.HString)] string activatableClassId,
        [In] in Guid iid,
        out IntPtr factory);
}

[ComImport]
[Guid("3628E81B-3CAC-4C60-B7F4-23CE0E0C3356")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IGraphicsCaptureItemInterop
{
    IntPtr CreateForWindow([In] IntPtr window, [In] in Guid iid);
    IntPtr CreateForMonitor([In] IntPtr monitor, [In] in Guid iid);
}
