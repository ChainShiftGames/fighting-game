using System.IO;
using Godot;
using Sim.Data.Input;
using Sim.Data.Sources;
using Sim.Data.Validation;
using Sim.Input;

namespace FightingGame.Input;

/// <summary>
/// Loads the button set (ADR-0010) the bindings refer to: from the repository's <c>data/</c> folder when there is
/// one, else from the data embedded in Sim.Data, as exported builds do (ADR-0015).
/// </summary>
public static class ButtonSetFile
{
    /// <summary>The button set; invalid button data fails loudly.</summary>
    /// <exception cref="DataValidationException">The button set file is invalid.</exception>
    public static ButtonSet? Load()
    {
        string? root = FindDataFolder();
        IDataSource source = root is null ? new EmbeddedDataSource() : new DirectoryDataSource(root);
        var errors = new DataErrors();
        ButtonSet? buttons = ButtonSetLoader.Load(source, errors);
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
