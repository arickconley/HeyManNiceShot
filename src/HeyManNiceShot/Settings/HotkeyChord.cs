using Avalonia.Input;

namespace HeyManNiceShot.Settings;

public sealed record HotkeyChord(KeyModifiers Modifiers, Key Key)
{
    public static HotkeyChord Default { get; } =
        new(KeyModifiers.Control | KeyModifiers.Shift, Key.D4);

    public override string ToString()
    {
        var parts = new List<string>(4);
        if (Modifiers.HasFlag(KeyModifiers.Control)) parts.Add("Ctrl");
        if (Modifiers.HasFlag(KeyModifiers.Shift))   parts.Add("Shift");
        if (Modifiers.HasFlag(KeyModifiers.Alt))     parts.Add("Alt");
        if (Modifiers.HasFlag(KeyModifiers.Meta))    parts.Add("Win");
        parts.Add(KeyDisplay(Key));
        return string.Join("+", parts);
    }

    public static bool TryParse(string? s, out HotkeyChord chord)
    {
        chord = Default;
        if (string.IsNullOrWhiteSpace(s)) return false;

        var parts = s.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return false;

        var mods = KeyModifiers.None;
        Key? key = null;
        foreach (var part in parts)
        {
            switch (part.ToLowerInvariant())
            {
                case "ctrl" or "control": mods |= KeyModifiers.Control; break;
                case "shift":             mods |= KeyModifiers.Shift; break;
                case "alt":               mods |= KeyModifiers.Alt; break;
                case "win" or "meta":     mods |= KeyModifiers.Meta; break;
                default:
                    if (TryParseKey(part, out var k)) key = k;
                    break;
            }
        }
        if (key is null) return false;
        chord = new HotkeyChord(mods, key.Value);
        return true;
    }

    private static bool TryParseKey(string s, out Key key)
    {
        // Single digits must be remapped to D0..D9 first — otherwise Enum.TryParse
        // interprets "4" as the underlying integer value (which is Key.LineFeed).
        if (s.Length == 1 && char.IsDigit(s[0]) && Enum.TryParse($"D{s}", ignoreCase: true, out key))
            return true;
        return Enum.TryParse(s, ignoreCase: true, out key) && !char.IsDigit(s[0]);
    }

    private static string KeyDisplay(Key key)
        => key switch
        {
            >= Key.D0 and <= Key.D9 => ((int)(key - Key.D0)).ToString(),
            _ => key.ToString(),
        };
}
