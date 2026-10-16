using System.IO;
using Godot;
using Sim.Data;
using Sim.Data.Sources;
using Sim.Data.Validation;

namespace FightingGame.Online;

/// <summary>
/// The repository's <c>data/</c> folder, which local matches load all game data from. Exported builds don't ship it
/// yet (ADR-0015 embeds the data), so for now this works in editor runs and with the folder next to the executable.
/// </summary>
public sealed class GameDataFolder
{
    // Developer setting: where characters come from while data/ has none (relative to the repository root). The
    // Sim.Data test fixture is a small valid character; remove this once Testman (P1-17) is in data/.
    private const string PlaceholderCharacterFolder = "tests/Sim.Data.Tests/Fixtures/data";

    private GameDataFolder(string root)
    {
        Root = root;
    }

    /// <summary>The data folder's full path; the debug panel writes developer settings into its files.</summary>
    public string Root { get; }

    /// <summary>The data folder, or null when the game has none (exported builds, until ADR-0015).</summary>
    public static GameDataFolder? Find()
    {
        // Run from the editor or `godot --path game`: next to game/. Exported: next to the executable.
        string[] candidates =
        [
            Path.Combine(ProjectSettings.GlobalizePath("res://"), "..", "data"),
            Path.Combine(Path.GetDirectoryName(OS.GetExecutablePath()) ?? string.Empty, "data"),
        ];
        foreach (string candidate in candidates)
        {
            if (Directory.Exists(candidate))
            {
                return new GameDataFolder(Path.GetFullPath(candidate));
            }
        }

        return null;
    }

    /// <summary>Loads and validates everything, reading the files again so data edits show up in the next match.</summary>
    /// <exception cref="DataValidationException">Any file is invalid.</exception>
    /// <exception cref="InvalidDataException">There is no character to play, not even a placeholder.</exception>
    public GameData Load()
    {
        IDataSource source = new DirectoryDataSource(Root);
        string placeholder = Path.GetFullPath(Path.Combine(Root, "..", PlaceholderCharacterFolder));
        if (Directory.Exists(placeholder))
        {
            var withPlaceholder = new PlaceholderCharacterSource(source, new DirectoryDataSource(placeholder));
            if (withPlaceholder.UsesPlaceholder)
            {
                GD.PushWarning($"{Root} has no characters yet: using the placeholder characters in {placeholder}.");
            }

            source = withPlaceholder;
        }

        GameData data = new GameDataLoader(source).Load();
        if (data.Characters.Count == 0)
        {
            throw new InvalidDataException($"No characters in {Root}: add one under characters/ to play.");
        }

        return data;
    }
}
