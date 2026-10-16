using Sim.Characters;
using Sim.Data.Hashing;

namespace Sim.Data.Characters;

/// <summary>Adds everything the game logic reads from a character to the data hash.</summary>
public static class CharacterDataHash
{
    public static void Append(DataHashBuilder hash, CharacterData character)
    {
        hash.Add(character.Id);
        hash.Add(character.Stats);
        hash.Add(character.Moves);
        hash.Add(character.Boxes);
        hash.Add(character.Cancels);
        hash.Add(character.CancelTargets);
        hash.Add(character.Hits);
        hash.Add(character.Velocities);
        hash.Add(character.BasicStateMoves);
        hash.Add(character.Projectiles);
    }
}
