using Avalonia.Input;
using HeyManNiceShot.Settings;
using Xunit;

namespace HeyManNiceShot.Tests;

public class SettingsTests
{
    [Fact]
    public void Defaults_AreSensible()
    {
        var s = AppSettings.CreateDefault();
        Assert.Equal(KeyModifiers.Control | KeyModifiers.Shift, s.CaptureHotkey.Modifiers);
        Assert.Equal(Key.D4, s.CaptureHotkey.Key);
        Assert.Equal(ImageFormat.Png, s.DefaultFormat);
    }

    [Fact]
    public void SettingsStore_RoundTrips()
    {
        var temp = Path.Combine(Path.GetTempPath(), $"HeyManNiceShot.test.{Guid.NewGuid():N}.json");
        try
        {
            var store = new SettingsStore(temp);
            var original = new AppSettings(
                new HotkeyChord(KeyModifiers.Alt, Key.F1),
                @"C:\Foo",
                ImageFormat.Jpeg);

            store.Save(original);
            var loaded = store.Load();

            Assert.Equal(original.CaptureHotkey.Modifiers, loaded.CaptureHotkey.Modifiers);
            Assert.Equal(original.CaptureHotkey.Key, loaded.CaptureHotkey.Key);
            Assert.Equal(original.SaveFolder, loaded.SaveFolder);
            Assert.Equal(original.DefaultFormat, loaded.DefaultFormat);
        }
        finally
        {
            if (File.Exists(temp)) File.Delete(temp);
        }
    }

    [Fact]
    public void SettingsStore_Load_MissingFile_ReturnsDefaults()
    {
        var temp = Path.Combine(Path.GetTempPath(), $"HeyManNiceShot.missing.{Guid.NewGuid():N}.json");
        var store = new SettingsStore(temp);
        var loaded = store.Load();
        Assert.NotNull(loaded);
        Assert.Equal(HotkeyChord.Default, loaded.CaptureHotkey);
    }

    [Theory]
    [InlineData("Ctrl+Shift+4",   KeyModifiers.Control | KeyModifiers.Shift, Key.D4)]
    [InlineData("ctrl+shift+4",   KeyModifiers.Control | KeyModifiers.Shift, Key.D4)]
    [InlineData("Alt+F1",         KeyModifiers.Alt,                          Key.F1)]
    [InlineData("Win+Space",      KeyModifiers.Meta,                         Key.Space)]
    [InlineData("Ctrl+Shift+A",   KeyModifiers.Control | KeyModifiers.Shift, Key.A)]
    public void HotkeyChord_TryParse_ParsesValid(string input, KeyModifiers mods, Key key)
    {
        Assert.True(HotkeyChord.TryParse(input, out var chord));
        Assert.Equal(mods, chord.Modifiers);
        Assert.Equal(key, chord.Key);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Ctrl+Shift")]
    [InlineData("Just+Text")]
    public void HotkeyChord_TryParse_RejectsInvalid(string input)
    {
        Assert.False(HotkeyChord.TryParse(input, out _));
    }

    [Fact]
    public void HotkeyChord_ToString_RoundTripsViaTryParse()
    {
        var chord = new HotkeyChord(KeyModifiers.Control | KeyModifiers.Shift, Key.D4);
        var s = chord.ToString();
        Assert.True(HotkeyChord.TryParse(s, out var roundTripped));
        Assert.Equal(chord, roundTripped);
    }
}
