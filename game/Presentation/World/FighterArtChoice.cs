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
    /// <returns>The node to add to the 3D scene.</returns>
    public static Node3D Create(CharacterData character, Color color, out IFighterArt art)
    {
        if (ReadSettings(character.Id) is { Art: FighterArtKind.Model } settings &&
            LoadModel(character.Id, settings) is { } model)
        {
            var fighter = new RiggedFighterModel();
            fighter.Build(model, settings, character);
            art = fighter;
            return fighter;
        }

        var placeholder = new PlaceholderFighterModel();
        placeholder.Paint(color);
        art = placeholder;
        return placeholder;
    }

    private static FighterArtSettings? ReadSettings(string characterId)
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
