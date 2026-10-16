using System.IO;
using Godot;
using Sim.Input;

namespace FightingGame.Input;

/// <summary>
/// The dev bindings file, <c>user://dev_bindings.jsonc</c> (player settings). A stand-in until Phase 5 saves
/// bindings with the other player settings and the options menu edits them.
/// </summary>
/// <remarks>
/// On first run it is created from the defaults embedded in the game, so it works the same from the editor and in
/// exported builds. It lives in <c>user://</c>, not the repository, because bindings belong to the player.
/// </remarks>
public sealed class DevBindingsFile
{
    /// <summary>Players one PC can seat: two for local matches.</summary>
    public const int PlayerCount = 2;

    private readonly string _path;

    public DevBindingsFile(string userPath)
    {
        _path = ProjectSettings.GlobalizePath(userPath);
    }

    /// <summary>The bindings in the dev file; the defaults, with every problem logged, when it has any.</summary>
    public PlayerBindings[] Load(ButtonSet? buttons)
    {
        string defaults = DefaultBindings.ReadJson();
        if (!File.Exists(_path))
        {
            File.WriteAllText(_path, defaults);
        }

        GD.Print($"Bindings: {_path} (delete it to get the defaults back)");
        var parser = new BindingsParser(buttons, PlayerCount);
        if (parser.Parse(File.ReadAllText(_path)) is { } bindings)
        {
            return bindings;
        }

        foreach (string error in parser.Errors)
        {
            GD.PushError($"{_path}: {error}");
        }

        GD.PushError($"{_path} has problems (listed above); using the default bindings until they are fixed.");

        // The embedded defaults are checked by the tests, so they always parse.
        return new BindingsParser(buttons, PlayerCount).Parse(defaults)!;
    }
}
