using HeyManNiceShot.Editing;
using HeyManNiceShot.Settings;
using SkiaSharp;

namespace HeyManNiceShot.Export;

public static class Exporter
{
    public static void Copy(Document doc)
    {
        var bytes = Encode(doc, ImageFormat.Png);
        Win32ClipboardImage.SetPngAndBitmap(bytes);
    }

    public static async Task<string> SaveAsync(Document doc, AppSettings settings)
    {
        Directory.CreateDirectory(settings.SaveFolder);
        var bytes = Encode(doc, settings.DefaultFormat);
        var ext = settings.DefaultFormat == ImageFormat.Jpeg ? ".jpg" : ".png";
        var name = $"HeyManNiceShot_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}{ext}";
        var path = Path.Combine(settings.SaveFolder, name);
        await File.WriteAllBytesAsync(path, bytes).ConfigureAwait(false);
        return path;
    }

    private static byte[] Encode(Document doc, ImageFormat format)
    {
        using var flat = Renderer.Flatten(doc);
        using var image = SKImage.FromBitmap(flat);
        var skFormat = format == ImageFormat.Jpeg ? SKEncodedImageFormat.Jpeg : SKEncodedImageFormat.Png;
        using var data = image.Encode(skFormat, quality: format == ImageFormat.Jpeg ? 92 : 100);
        return data.ToArray();
    }
}
