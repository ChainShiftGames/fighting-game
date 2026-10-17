using Sim.Characters;
using Sim.Collision;
using Sim.Fighters.States;
using Sim.Input;
using Sim.State;

namespace Netcode.Drivers.Training;

/// <summary>A dummy that blocks everything blockable: it holds back while under attack, low or high as the attack needs.</summary>
/// <remarks>
/// Unlike a CPU opponent it may read the attacker's move data: the point is a perfect guard, not fair play. It
/// crouch-blocks (stopping mids and lows) unless the attacker's move has an overhead hit, and stands otherwise, so it
/// doesn't walk away between attacks. Projectiles are crouch-blocked.
/// </remarks>
public sealed class GuardingDummy : IDummyBehaviour
{
    private readonly CharacterData _attacker;

    /// <param name="attacker">The training player's character, whose moves the dummy blocks.</param>
    public GuardingDummy(CharacterData attacker)
    {
        _attacker = attacker;
    }

    public InputWord Next(in MatchState state)
    {
        FighterState dummy = state.Fighters[TrainingSides.Dummy];
        FighterState attacker = state.Fighters[TrainingSides.Player];
        if (!IsUnderAttack(in state, in dummy, in attacker))
        {
            return InputWord.Neutral;
        }

        ushort height = IsOverhead(in attacker) ? (ushort)0 : InputWord.Down;
        return new InputWord((ushort)(Back(in dummy, in attacker) | height));
    }

    private static bool IsUnderAttack(in MatchState state, in FighterState dummy, in FighterState attacker) =>
        attacker.Action == FighterAction.Attack || HasProjectileOut(in state) || dummy.Action is
            FighterAction.Blockstun or FighterAction.CrouchBlockstun or FighterAction.AirBlockstun;

    private static bool HasProjectileOut(in MatchState state)
    {
        int first = EntityPool.FirstSlotOf(TrainingSides.Player);
        for (int slot = first; slot < first + EntityPool.MaxPerPlayer; slot++)
        {
            if (state.Entities.Slots[slot].IsActive != 0)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsOverhead(in FighterState attacker)
    {
        if (attacker.Action != FighterAction.Attack)
        {
            return false;
        }

        foreach (HitData hit in _attacker.HitsOf(in _attacker.Moves[attacker.Move]))
        {
            if (hit.Guard == Guard.Overhead)
            {
                return true;
            }
        }

        return false;
    }

    // Away from the attacker; at the same x, away from where the dummy faces, as blocking reads it.
    private static ushort Back(in FighterState dummy, in FighterState attacker)
    {
        int side = FighterSides.Of(in dummy, in attacker);
        int towardAttacker = side == 0 ? dummy.Facing : -side;
        return towardAttacker > 0 ? InputWord.Left : InputWord.Right;
    }
}
