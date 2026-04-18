using HeyManNiceShot.Editing;
using HeyManNiceShot.Editor;
using HeyManNiceShot.Export;
using HeyManNiceShot.Overlay;

namespace HeyManNiceShot;

public static class CaptureOrchestrator
{
    private static bool _active;

    public static async void Run(App app)
    {
        if (_active) return;
        _active = true;
        try
        {
            var vs = await app.Capture.GetVirtualScreenAsync().ConfigureAwait(true);
            var overlay = new OverlayWindow(vs);
            var result = await overlay.PickAsync().ConfigureAwait(true);
            if (result is null) return;

            // Small yield so DXGI doesn't latch the overlay's last frame.
            await Task.Yield();

            var bitmap = await app.Capture.CaptureRegionAsync(result.Selection).ConfigureAwait(true);
            var doc = new Document(bitmap);

            switch (result.Action)
            {
                case OverlayAction.Copy:
                    Exporter.Copy(doc);
                    break;
                case OverlayAction.Save:
                    await Exporter.SaveAsync(doc, app.Settings).ConfigureAwait(true);
                    break;
                case OverlayAction.Edit:
                    new EditorWindow(doc, app).Show();
                    break;
            }
        }
        catch (Exception ex)
        {
            CrashLog.Write("CaptureOrchestrator", ex);
        }
        finally
        {
            _active = false;
        }
    }
}
