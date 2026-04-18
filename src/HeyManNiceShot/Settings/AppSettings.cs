namespace HeyManNiceShot.Settings;

public enum ImageFormat { Png, Jpeg }

public sealed record AppSettings(
    HotkeyChord CaptureHotkey,
    string SaveFolder,
    ImageFormat DefaultFormat)
{
    public static AppSettings CreateDefault()
        => new(
            HotkeyChord.Default,
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
                "Screenshots"),
            ImageFormat.Png);
}
