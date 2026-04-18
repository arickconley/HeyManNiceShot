using SkiaSharp;
using System.Runtime.InteropServices;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Windows.Graphics.DirectX.Direct3D11;
using WinRT;

namespace HeyManNiceShot.Capture;

internal static unsafe class Direct3D11Helper
{
    public static (ID3D11Device D3D, IDirect3DDevice WinRt) CreateDevice()
    {
        var flags = DeviceCreationFlags.BgraSupport;
        var levels = new[] { FeatureLevel.Level_11_1, FeatureLevel.Level_11_0 };

        ID3D11Device? device;
        var hr = D3D11.D3D11CreateDevice(
            adapter: null, DriverType.Hardware, flags, levels,
            out device, out _, out _);

        if (hr.Failure || device is null)
        {
            D3D11.D3D11CreateDevice(
                adapter: null, DriverType.Warp, flags, levels,
                out device, out _, out _).CheckError();
        }

        if (device is null)
            throw new InvalidOperationException("Failed to create a D3D11 device (hardware and WARP both failed).");

        using var dxgi = device.QueryInterface<IDXGIDevice>();
        var hr2 = CreateDirect3D11DeviceFromDXGIDevice(dxgi.NativePointer, out var inspectable);
        Marshal.ThrowExceptionForHR(hr2);
        try
        {
            var winRt = MarshalInspectable<IDirect3DDevice>.FromAbi(inspectable);
            return (device, winRt);
        }
        finally
        {
            Marshal.Release(inspectable);
        }
    }

    public static SKBitmap SurfaceToBitmap(IDirect3DSurface surface, ID3D11Device device)
    {
        var access = surface.As<IDirect3DDxgiInterfaceAccess>();
        var iid = typeof(ID3D11Texture2D).GUID;
        var texPtr = access.GetInterface(iid);

        using var texture = new ID3D11Texture2D(texPtr);
        var desc = texture.Description;

        var stagingDesc = new Texture2DDescription
        {
            Width = desc.Width,
            Height = desc.Height,
            MipLevels = 1,
            ArraySize = 1,
            Format = desc.Format,
            SampleDescription = new SampleDescription(1, 0),
            Usage = ResourceUsage.Staging,
            CPUAccessFlags = CpuAccessFlags.Read,
            BindFlags = BindFlags.None,
            MiscFlags = ResourceOptionFlags.None,
        };

        using var staging = device.CreateTexture2D(stagingDesc);
        device.ImmediateContext.CopyResource(staging, texture);

        var mapped = device.ImmediateContext.Map(staging, 0, MapMode.Read);
        var bitmap = new SKBitmap(new SKImageInfo(
            (int)desc.Width, (int)desc.Height,
            SKColorType.Bgra8888, SKAlphaType.Premul));
        try
        {
            var rowBytes = (int)desc.Width * 4;
            var dst = (byte*)bitmap.GetPixels().ToPointer();
            var src = (byte*)mapped.DataPointer;
            for (var y = 0; y < desc.Height; y++)
            {
                Buffer.MemoryCopy(
                    source: src + (long)y * mapped.RowPitch,
                    destination: dst + (long)y * rowBytes,
                    destinationSizeInBytes: rowBytes,
                    sourceBytesToCopy: rowBytes);
            }
        }
        finally
        {
            device.ImmediateContext.Unmap(staging, 0);
        }

        return bitmap;
    }

    [DllImport("d3d11.dll")]
    private static extern int CreateDirect3D11DeviceFromDXGIDevice(IntPtr dxgiDevice, out IntPtr graphicsDevice);
}

[ComImport]
[Guid("A9B3D012-3DF2-4EE3-B8D1-8695F457D3C1")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IDirect3DDxgiInterfaceAccess
{
    IntPtr GetInterface([In] in Guid iid);
}
