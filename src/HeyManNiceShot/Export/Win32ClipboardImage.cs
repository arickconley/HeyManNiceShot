using SkiaSharp;
using System.Runtime.InteropServices;

namespace HeyManNiceShot.Export;

internal static unsafe class Win32ClipboardImage
{
    private const uint CF_DIB = 8;
    private const uint GMEM_MOVEABLE = 0x0002;

    public static void SetPngAndBitmap(byte[] pngBytes)
    {
        var cfPng = RegisterClipboardFormat("PNG");
        if (cfPng == 0)
            throw new InvalidOperationException("RegisterClipboardFormat(PNG) failed.");

        if (!OpenClipboard(IntPtr.Zero))
            throw new InvalidOperationException("OpenClipboard failed.");
        try
        {
            EmptyClipboard();
            SetGlobalData(cfPng, pngBytes);

            var dib = EncodeDib(pngBytes);
            if (dib is not null) SetGlobalData(CF_DIB, dib);
        }
        finally
        {
            CloseClipboard();
        }
    }

    private static void SetGlobalData(uint format, byte[] data)
    {
        var hMem = GlobalAlloc(GMEM_MOVEABLE, (UIntPtr)data.Length);
        if (hMem == IntPtr.Zero) throw new OutOfMemoryException("GlobalAlloc failed.");
        var locked = GlobalLock(hMem);
        if (locked == IntPtr.Zero)
        {
            GlobalFree(hMem);
            throw new InvalidOperationException("GlobalLock failed.");
        }
        try
        {
            Marshal.Copy(data, 0, locked, data.Length);
        }
        finally
        {
            GlobalUnlock(hMem);
        }
        if (SetClipboardData(format, hMem) == IntPtr.Zero)
        {
            GlobalFree(hMem);
            throw new InvalidOperationException("SetClipboardData failed.");
        }
        // hMem is now owned by the clipboard.
    }

    private static byte[]? EncodeDib(byte[] pngBytes)
    {
        try
        {
            using var ms = new MemoryStream(pngBytes);
            using var decoded = SKBitmap.Decode(ms);
            if (decoded is null) return null;

            using var source = decoded.ColorType == SKColorType.Bgra8888
                ? decoded.Copy()
                : decoded.Copy(SKColorType.Bgra8888);
            if (source is null) return null;

            var width = source.Width;
            var height = source.Height;
            var stride = width * 4;
            var pixelBytes = stride * height;
            var headerSize = Marshal.SizeOf<BITMAPINFOHEADER>();
            var buffer = new byte[headerSize + pixelBytes];

            var header = new BITMAPINFOHEADER
            {
                biSize = (uint)headerSize,
                biWidth = width,
                biHeight = -height,
                biPlanes = 1,
                biBitCount = 32,
                biCompression = 0,
                biSizeImage = (uint)pixelBytes,
            };

            fixed (byte* dst = buffer)
            {
                Marshal.StructureToPtr(header, (IntPtr)dst, false);
                var src = (byte*)source.GetPixels().ToPointer();
                var srcStride = source.RowBytes;
                for (var y = 0; y < height; y++)
                {
                    Buffer.MemoryCopy(
                        source: src + (long)y * srcStride,
                        destination: dst + headerSize + (long)y * stride,
                        destinationSizeInBytes: stride,
                        sourceBytesToCopy: stride);
                }
            }

            return buffer;
        }
        catch
        {
            return null;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BITMAPINFOHEADER
    {
        public uint biSize;
        public int biWidth;
        public int biHeight;
        public ushort biPlanes;
        public ushort biBitCount;
        public uint biCompression;
        public uint biSizeImage;
        public int biXPelsPerMeter;
        public int biYPelsPerMeter;
        public uint biClrUsed;
        public uint biClrImportant;
    }

    [DllImport("user32.dll")] private static extern bool OpenClipboard(IntPtr hWndNewOwner);
    [DllImport("user32.dll")] private static extern bool CloseClipboard();
    [DllImport("user32.dll")] private static extern bool EmptyClipboard();
    [DllImport("user32.dll")] private static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern uint RegisterClipboardFormat(string lpszFormat);

    [DllImport("kernel32.dll")] private static extern IntPtr GlobalAlloc(uint uFlags, UIntPtr dwBytes);
    [DllImport("kernel32.dll")] private static extern IntPtr GlobalLock(IntPtr hMem);
    [DllImport("kernel32.dll")] private static extern bool GlobalUnlock(IntPtr hMem);
    [DllImport("kernel32.dll")] private static extern IntPtr GlobalFree(IntPtr hMem);
}
