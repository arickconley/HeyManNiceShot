namespace HeyManNiceShot;

public static class CrashLog
{
    private static readonly object _lock = new();
    private const long MaxBytes = 1_000_000;
    private const int Keep = 3;

    public static string LogPath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "HeyManNiceShot", "logs", "crash.log");

    public static void Write(string source, Exception? ex)
    {
        if (ex is null) return;
        try
        {
            lock (_lock)
            {
                var dir = Path.GetDirectoryName(LogPath);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

                if (File.Exists(LogPath) && new FileInfo(LogPath).Length > MaxBytes)
                    Rotate();

                var line = $"[{DateTimeOffset.Now:O}] [{source}] {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}\n\n";
                File.AppendAllText(LogPath, line);
            }
        }
        catch
        {
            // Crash logging must never throw.
        }
    }

    private static void Rotate()
    {
        try
        {
            var dir = Path.GetDirectoryName(LogPath)!;
            var name = Path.GetFileNameWithoutExtension(LogPath);
            var ext = Path.GetExtension(LogPath);

            for (var i = Keep; i >= 1; i--)
            {
                var src = i == 1 ? LogPath : Path.Combine(dir, $"{name}.{i - 1}{ext}");
                var dst = Path.Combine(dir, $"{name}.{i}{ext}");
                if (!File.Exists(src)) continue;
                if (File.Exists(dst)) File.Delete(dst);
                File.Move(src, dst);
            }
        }
        catch { }
    }
}
