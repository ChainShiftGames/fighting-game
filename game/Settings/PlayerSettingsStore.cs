using System.IO;
using Godot;

namespace FightingGame.Settings;

/// <summary>Loads and saves <see cref="PlayerSettings"/> as JSON under <c>user://</c>.</summary>
public sealed class PlayerSettingsStore
{
    private readonly string _path;

    public PlayerSettingsStore(string userPath)
    {
        _path = ProjectSettings.GlobalizePath(userPath);
    }

    /// <summary>The saved settings, or the defaults when nothing is saved yet; invalid values fall back with a warning.</summary>
    public PlayerSettings Load()
    {
        if (!File.Exists(_path))
        {
            return new PlayerSettings();
        }

        PlayerSettings settings = PlayerSettingsJson.Read(File.ReadAllText(_path), out string warning);
        if (warning.Length > 0)
        {
            GD.PushWarning($"Player settings at {_path}: {warning}.");
        }

        return settings;
    }

    public void Save(PlayerSettings settings) => File.WriteAllText(_path, PlayerSettingsJson.Write(settings));
}
