using System.Text.Json;
using System.Text.Json.Serialization;

namespace HeyManNiceShot.Settings;

public sealed class SettingsStore
{
    private readonly string _path;
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Converters =
        {
            new JsonStringEnumConverter(),
            new HotkeyChordJsonConverter(),
        },
    };

    public SettingsStore()
        : this(DefaultPath()) { }

    public SettingsStore(string path)
    {
        _path = path;
        var dir = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
    }

    public string Path => _path;

    public AppSettings Load()
    {
        if (!File.Exists(_path)) return AppSettings.CreateDefault();
        try
        {
            var json = File.ReadAllText(_path);
            return JsonSerializer.Deserialize<AppSettings>(json, Options) ?? AppSettings.CreateDefault();
        }
        catch
        {
            return AppSettings.CreateDefault();
        }
    }

    public void Save(AppSettings settings)
    {
        var json = JsonSerializer.Serialize(settings, Options);
        File.WriteAllText(_path, json);
    }

    public static string DefaultPath()
    {
        var dir = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "HeyManNiceShot");
        return System.IO.Path.Combine(dir, "settings.json");
    }
}

internal sealed class HotkeyChordJsonConverter : JsonConverter<HotkeyChord>
{
    public override HotkeyChord Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var s = reader.GetString();
        return HotkeyChord.TryParse(s, out var chord) ? chord : HotkeyChord.Default;
    }

    public override void Write(Utf8JsonWriter writer, HotkeyChord value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString());
}
