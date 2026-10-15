using System.IO;
using System.Text.Json;
using Godot;

namespace FightingGame.Settings;

/// <summary>Loads and saves <see cref="PlayerSettings"/> as JSON under <c>user://</c>.</summary>
public sealed class PlayerSettingsStore
{
    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    private readonly string _path;

    public PlayerSettingsStore(string userPath)
    {
        _path = ProjectSettings.GlobalizePath(userPath);
    }

    /// <summary>The saved settings, or the defaults when nothing is saved yet or the file can't be read.</summary>
    public PlayerSettings Load()
    {
        if (!File.Exists(_path))
        {
            return new PlayerSettings();
        }

        try
        {
            return JsonSerializer.Deserialize<PlayerSettings>(File.ReadAllText(_path), _json) ?? new PlayerSettings();
        }
        catch (JsonException exception)
        {
            GD.PushWarning($"Ignoring unreadable player settings at {_path}: {exception.Message}");
            return new PlayerSettings();
        }
    }

    public void Save(PlayerSettings settings) => File.WriteAllText(_path, JsonSerializer.Serialize(settings, _json));
}
