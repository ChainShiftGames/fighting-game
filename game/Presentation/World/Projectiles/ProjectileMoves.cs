using Sim.Characters;

namespace FightingGame.Presentation.World.Projectiles;

/// <summary>Which move spawns each of a character's projectiles, so art can be chosen by move ID.</summary>
public static class ProjectileMoves
{
    /// <returns>The spawning move's ID per index of <c>CharacterData.Projectiles</c>.</returns>
    public static string[] Of(CharacterData character)
    {
        var moveIds = new string[character.Projectiles.Length];
        for (int move = 0; move < character.Moves.Length; move++)
        {
            MoveData data = character.Moves[move];
            for (int i = 0; i < data.ProjectileCount; i++)
            {
                moveIds[data.ProjectileStart + i] = character.MoveIds[move];
            }
        }

        return moveIds;
    }
}
