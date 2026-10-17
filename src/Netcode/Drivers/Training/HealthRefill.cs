using System;
using Sim.Characters;
using Sim.State;

namespace Netcode.Drivers.Training;

/// <summary>Refills a fighter's health once the combo or blockstring on it is over and it has been free for a moment.</summary>
/// <remarks>
/// The delay leaves the damage on the health bar long enough to read. Only while the round is on: a combo that KOs
/// still ends the round, and the next one starts with full health anyway.
/// </remarks>
public sealed class HealthRefill : ITrainingAdjustment<MatchState>, ITrainingReset
{
    private readonly int[] _fullHealth;
    private readonly int[] _freeFrames = new int[MatchState.PlayerCount];
    private readonly int _delayFrames;

    /// <param name="characters">P1's and P2's character, for their full health.</param>
    /// <param name="delayFrames">Frames a fighter must be free before its health refills.</param>
    public HealthRefill(CharacterData[] characters, int delayFrames)
    {
        _fullHealth = [characters[0].Stats.Health, characters[1].Stats.Health];
        _delayFrames = delayFrames;
    }

    public void Apply(ref MatchState state)
    {
        for (int fighter = 0; fighter < MatchState.PlayerCount; fighter++)
        {
            ref FighterState current = ref state.Fighters[fighter];
            _freeFrames[fighter] = FighterCondition.IsFree(in current) ? _freeFrames[fighter] + 1 : 0;
            if (state.Round.Phase == RoundPhase.Fighting && _freeFrames[fighter] >= _delayFrames)
            {
                current.Health = _fullHealth[fighter];
            }
        }
    }

    /// <summary>Waits the whole delay again before the next refill.</summary>
    public void Reset() => Array.Clear(_freeFrames);
}
