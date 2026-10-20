using System.Collections.Generic;
using Godot;
using Sim.Characters;

namespace FightingGame.Presentation.World;

/// <summary>
/// Makes each character's <see cref="IFighterArt"/> from its art file (developer setting, ADR-0028): the glTF model
/// when the file says so and the model loads, otherwise the placeholder, with the reason in the log.
/// </summary>
public static class FighterArtChoice
{
    /// <param name="color">The placeholder's colour.</param>
    /// <param name="art">The art to give poses to each frame; the returned node is the same object.</param>
    /// <param name="modelVariant">Which of the art file's models to draw (<see cref="FighterArtSettings.WithModel"/>).</param>
    /// <returns>The node to add to the 3D scene.</returns>
    public static Node3D Create(CharacterData character, Color color, int modelVariant, out IFighterArt art)
    {
        if (CreateModel(character.Id, character.MoveIds.ToArray(), modelVariant) is { } fighter)
        {
            art = fighter;
            return fighter;
        }

        var placeholder = new PlaceholderFighterModel();
        placeholder.Paint(color);
        art = placeholder;
        return placeholder;
    }

    /// <summary>
    /// The character's rigged model when its art file asks for one and it loads; null for a placeholder. Also used
    /// by the character editor, which has the move list but not the compiled character.
    /// </summary>
    public static RiggedFighterModel? CreateModel(string characterId, IReadOnlyList<string> moveIds, int modelVariant = 0)
    {
        if (ReadSettings(characterId) is not { Art: FighterArtKind.Model } fileSettings)
        {
            return null;
        }

        FighterArtSettings settings = fileSettings.WithModel(modelVariant);
        if (LoadModel(characterId, settings) is not { } model)
        {
            return null;
        }

        var fighter = new RiggedFighterModel();
        fighter.Build(model, settings, characterId, moveIds);
        return fighter;
    }

    /// <summary>The character's art file, or null without one or when it is invalid (the reason is logged).</summary>
    public static FighterArtSettings? ReadSettings(string characterId)
    {
        string path = FighterArtFile.PathFor(characterId);
        if (!FileAccess.FileExists(path))
        {
            return null;
        }

        FighterArtSettings? settings = FighterArtFile.Read(FileAccess.GetFileAsString(path), out string error);
        if (settings is null)
        {
            GD.PushError($"{path}: {error}; drawing {characterId} as a placeholder");
        }

        return settings;
    }

    private static PackedScene? LoadModel(string characterId, FighterArtSettings settings)
    {
        string path = FighterArtFile.Resolve(characterId, settings.Model);
        if (ResourceLoader.Exists(path) && ResourceLoader.Load(path) is PackedScene model)
        {
            return model;
        }

        GD.PushError($"{path}: no imported model there; drawing {characterId} as a placeholder");
        return null;
    }
}
