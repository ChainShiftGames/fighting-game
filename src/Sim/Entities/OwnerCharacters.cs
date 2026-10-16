using Sim.Characters;
using Sim.State;

namespace Sim.Entities;

/// <summary>Both players' characters, to find an entity's data through its owner.</summary>
/// <remarks>Read-only data, not state: entities store only indexes into their owner's character.</remarks>
public sealed class OwnerCharacters
{
    private readonly CharacterData _player1;
    private readonly CharacterData _player2;

    public OwnerCharacters(CharacterData player1, CharacterData player2)
    {
        _player1 = player1;
        _player2 = player2;
    }

    public CharacterData Of(int owner) => owner == 0 ? _player1 : _player2;

    public ref readonly ProjectileData ProjectileOf(in EntityState entity) =>
        ref Of(entity.Owner).Projectiles[entity.Projectile];
}
