using System.Collections.Generic;
using Sim.Characters;

namespace FightingGame.Presentation.World.Projectiles;

/// <summary>The art for each of P1's and P2's projectiles, by owner and projectile index, read once at match start.</summary>
public static class ProjectileArtLookup
{
    /// <returns>Per owner, per index of its character's projectiles: the art, or null for none.</returns>
    public static ProjectileArtSettings?[][] Of(CharacterData[] characters)
    {
        var arts = new ProjectileArtSettings?[characters.Length][];
        for (int owner = 0; owner < characters.Length; owner++)
        {
            FighterArtSettings? settings = FighterArtChoice.ReadSettings(characters[owner].Id);
            arts[owner] = Match(ProjectileMoves.Of(characters[owner]), settings?.Projectiles);
        }

        return arts;
    }

    /// <summary>Each projectile's art: the entry for the move that spawns it.</summary>
    public static ProjectileArtSettings?[] Match(
        string[] spawningMoves, IReadOnlyDictionary<string, ProjectileArtSettings>? byMove)
    {
        var arts = new ProjectileArtSettings?[spawningMoves.Length];
        for (int projectile = 0; projectile < spawningMoves.Length; projectile++)
        {
            arts[projectile] = byMove is not null && byMove.TryGetValue(spawningMoves[projectile], out var art)
                ? art
                : null;
        }

        return arts;
    }
}
