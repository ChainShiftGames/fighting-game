using System.IO;
using Godot;
using Sim.Input;

namespace FightingGame.Input.Profiles;

/// <summary>
/// Loads and saves the controls file (<c>user://controls.json</c>, player settings). With no controls file yet it
/// migrates the old dev bindings file once and saves the result; the old file is left as it was and never read again.
/// </summary>
public sealed class ControlsStore
{
    private readonly string _path;
    private readonly string _devBindingsPath;
    private readonly ButtonSet? _buttons;
    private readonly BindingTargets _targets;
    private readonly BuiltInPresets _builtIn;

    /// <param name="devBindingsUserPath">The old per-player bindings file, migrated when there is no controls file.</param>
    public ControlsStore(string userPath, string devBindingsUserPath, ButtonSet? buttons)
    {
        _path = ProjectSettings.GlobalizePath(userPath);
        _devBindingsPath = ProjectSettings.GlobalizePath(devBindingsUserPath);
        _buttons = buttons;
        _targets = new BindingTargets(buttons);
        _builtIn = BuiltInPresets.Load(_targets);
    }

    public BindingTargets Targets => _targets;

    public ControlProfiles Load()
    {
        GD.Print($"Controls: {_path}");
        if (File.Exists(_path))
        {
            return new ControlProfiles(_builtIn, ReadFile());
        }

        if (!File.Exists(_devBindingsPath))
        {
            return new ControlProfiles(_builtIn, ControlsFile.Empty);
        }

        var profiles = new ControlProfiles(_builtIn, Migrate());
        Save(profiles);
        GD.Print($"Controls: moved the bindings of {_devBindingsPath} into {_path}; the old file is no longer read.");
        return profiles;
    }

    public void Save(ControlProfiles profiles) => File.WriteAllText(_path, ControlsFileJson.Write(profiles.ToFile()));

    // An unreadable file is copied aside before the defaults replace it on the next save, so nothing is lost.
    private ControlsFile ReadFile()
    {
        ControlsFile file = ControlsFileJson.Read(
            File.ReadAllText(_path), _targets, _builtIn, out string warning, out bool isUnreadable);
        if (isUnreadable)
        {
            File.Copy(_path, _path + ".unreadable", overwrite: true);
            GD.PushError($"Controls at {_path}: {warning}; using the defaults, the file is kept as {_path}.unreadable.");
        }
        else if (warning.Length > 0)
        {
            GD.PushWarning($"Controls at {_path}: {warning}.");
        }

        return file;
    }

    private ControlsFile Migrate()
    {
        var parser = new BindingsParser(_buttons, BindingsParser.PlayerCount);
        if (parser.Parse(File.ReadAllText(_devBindingsPath)) is { } players)
        {
            return DevBindingsMigration.Migrate(players, _targets, _builtIn);
        }

        // The old game played with the defaults while its file had problems, so the defaults lose nothing.
        foreach (string error in parser.Errors)
        {
            GD.PushWarning($"{_devBindingsPath}: {error}");
        }

        GD.PushWarning($"{_devBindingsPath} has problems (listed above); not migrated, every device starts from the defaults.");
        return ControlsFile.Empty;
    }
}
