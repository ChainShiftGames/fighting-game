using System.IO;
using Godot;

namespace FightingGame.Training;

/// <summary>Loads and saves <see cref="TrainingSettings"/> as JSON under <c>user://</c>.</summary>
public sealed class TrainingSettingsStore
{
    private readonly string _path;

    public TrainingSettingsStore(string userPath)
    {
        _path = ProjectSettings.GlobalizePath(userPath);
    }

    /// <summary>The saved settings, or the defaults when nothing is saved yet or the file is unreadable (with a warning).</summary>
    public TrainingSettings Load()
    {
        if (!File.Exists(_path))
        {
            return new TrainingSettings();
        }

        TrainingSettings settings = TrainingSettingsJson.Read(File.ReadAllText(_path), out string warning);
        if (warning.Length > 0)
        {
            GD.PushWarning($"Training settings at {_path}: {warning}.");
        }

        return settings;
    }

    public void Save(TrainingSettings settings) => File.WriteAllText(_path, TrainingSettingsJson.Write(settings));
}
