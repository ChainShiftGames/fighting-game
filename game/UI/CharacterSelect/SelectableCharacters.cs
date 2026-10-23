using System.Collections.Generic;
using FightingGame.Presentation.World;
using Godot;
using Sim.Characters;
using Sim.Data;

namespace FightingGame.UI.CharacterSelect;

/// <summary>
/// The characters on the grid: every character in the game data, in its order, each with the look from its select
/// file (<see cref="CharacterSelectFile"/>) beside its art file. Without a file, or with an invalid one (the reason is
/// logged), a character shows as a placeholder in one colour.
/// </summary>
public static class SelectableCharacters
{
    public static IReadOnlyList<SelectableCharacter> From(GameData data)
    {
        var characters = new List<SelectableCharacter>(data.Characters.Count);
        foreach (CharacterData character in data.Characters)
        {
            characters.Add(Read(character.Id, character.DisplayName));
        }

        return characters;
    }

    private static SelectableCharacter Read(string id, string name)
    {
        string path = $"{FighterArtFile.FolderFor(id)}/{CharacterSelectFile.FileNameFor(id)}";
        if (!FileAccess.FileExists(path))
        {
            return CharacterSelectFile.Placeholder(id, name);
        }

        if (CharacterSelectFile.Read(id, name, FileAccess.GetFileAsString(path), out string error) is { } character)
        {
            return character;
        }

        GD.PushError($"{path}: {error}; showing {id} as a placeholder");
        return CharacterSelectFile.Placeholder(id, name);
    }
}
