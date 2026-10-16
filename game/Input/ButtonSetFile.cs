using System.IO;
using Godot;
using Sim.Data.Input;
using Sim.Data.Sources;
using Sim.Data.Validation;
using Sim.Input;

namespace FightingGame.Input;

/// <summary>
/// Loads the button set (ADR-0010) the bindings refer to, from the repository's <c>data/</c> folder, until match
/// startup loads all game data.
/// </summary>
public static class ButtonSetFile
{
    /// <summary>
    /// The button set, or null when the game has no <c>data/</c> folder (exported builds don't ship it yet): then
    /// only directions are bound. Invalid button data still fails loudly.
    /// </summary>
    /// <exception cref="DataValidationException">The button set file is invalid.</exception>
    public static ButtonSet? Load()
    {
        string? root = FindDataFolder();
        if (root is null)
        {
            GD.PushError("No data/ folder found: buttons are unbound, only directions work.");
            return null;
        }

        var errors = new DataErrors();
        ButtonSet? buttons = ButtonSetLoader.Load(new DirectoryDataSource(root), errors);
        errors.ThrowIfAny();
        return buttons;
    }

    // Run from the editor or `godot --path game`: next to game/. Exported: next to the executable.
    private static string? FindDataFolder()
    {
        string[] candidates =
        [
            Path.Combine(ProjectSettings.GlobalizePath("res://"), "..", "data"),
            Path.Combine(Path.GetDirectoryName(OS.GetExecutablePath()) ?? string.Empty, "data"),
        ];
        foreach (string candidate in candidates)
        {
            if (Directory.Exists(candidate))
            {
                return Path.GetFullPath(candidate);
            }
        }

        return null;
    }
}
