using Sim.Characters;
using Sim.Fighters.States;
using Sim.Math;
using Sim.Rules;
using Sim.State;

namespace Sim.Match;

/// <summary>Sets up the state for a round: fighters at their start positions, full health, idle; a fresh timer.</summary>
/// <remarks>
/// Everything round-scoped is cleared: both fighters' whole state (moves, stun, hitstop, time scale, pending
/// cancels), every entity (projectiles disappear without events), the global freeze, the camera (back to the stage
/// centre) and the latest round's result. Round wins,
/// the frame counter, the PRNG, input histories and module state carry over: module state belongs to the module, and
/// resetting it per round needs a round-start hook in the module contract (ADR-0011), which doesn't exist yet.
/// </remarks>
public sealed class RoundStart
{
    private const int FacingRight = 1;
    private const int FacingLeft = -1;

    private readonly CharacterData _player1;
    private readonly CharacterData _player2;
    private readonly int _startDistance;
    private readonly int _timerFrames;

    public RoundStart(CharacterData player1, CharacterData player2, in MatchRules rules)
    {
        _player1 = player1;
        _player2 = player2;
        _startDistance = rules.RoundStartDistance;
        _timerFrames = rules.RoundTimerFrames;
    }

    /// <summary>Starts round <paramref name="round"/>: P1 left of the centre facing right, P2 mirrored.</summary>
    public void Begin(ref MatchState state, int round)
    {
        ref RoundState rounds = ref state.Round;
        rounds.Round = round;
        rounds.TimerFramesLeft = _timerFrames;
        rounds.Phase = RoundPhase.Fighting;
        rounds.RoundOverFramesLeft = 0;
        rounds.Ending = RoundEnding.None;
        rounds.RoundWinners = Winners.None;
        state.Freeze = default;
        state.Camera = default;
        state.Entities = default;
        Place(ref state.Fighters[0], -_startDistance, FacingRight, _player1);
        Place(ref state.Fighters[1], _startDistance, FacingLeft, _player2);
    }

    private static void Place(ref FighterState fighter, int x, int facing, CharacterData character)
    {
        fighter = default;
        fighter.Position = new IntVector2(x, 0);
        fighter.Facing = facing;
        fighter.Health = character.Stats.Health;
        MoveEntry.EnterBasic(ref fighter, FighterAction.Idle, character);
    }
}
