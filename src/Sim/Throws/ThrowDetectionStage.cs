using Sim.Characters;
using Sim.Combat;
using Sim.Math;
using Sim.Pipeline;
using Sim.State;

namespace Sim.Throws;

/// <summary>The throw part of the hit detection step: whose throw boxes touch a throwable opponent.</summary>
/// <remarks>
/// Like <see cref="HitDetectionStage"/>: on the frame's final positions, for every fighter, against the state as the
/// step started, only recording contacts. A throw touches when its throw boxes on the current move frame overlap the
/// opponent's hurtboxes, the move hasn't connected yet and the opponent can be thrown (<see cref="Throwability"/>).
/// What contacts do is up to <see cref="ThrowResolutionStage"/>.
/// </remarks>
public sealed class ThrowDetectionStage : ITickStage
{
    private const CancelOn Connected = CancelOn.Hit | CancelOn.Block | CancelOn.Clash;

    private readonly CharacterData _player1;
    private readonly CharacterData _player2;
    private readonly ThrowContacts _contacts;

    public ThrowDetectionStage(CharacterData player1, CharacterData player2, ThrowContacts contacts)
    {
        _player1 = player1;
        _player2 = player2;
        _contacts = contacts;
    }

    public void Run(ref MatchState state, in FrameActivity activity)
    {
        _contacts.Clear();
        Detect(0, _player1, in state.Fighters[0], _player2, in state.Fighters[1]);
        Detect(1, _player2, in state.Fighters[1], _player1, in state.Fighters[0]);
    }

    private void Detect(
        int thrower,
        CharacterData throwerCharacter,
        in FighterState throwerState,
        CharacterData defenderCharacter,
        in FighterState defender)
    {
        bool canGrab = (throwerState.MoveOutcome & Connected) == 0 &&
            Throwability.CanBeThrown(in defender, defenderCharacter);
        if (canGrab && HitboxOverlap.TryFind(
            BoxKind.Throw, throwerCharacter, in throwerState, defenderCharacter, in defender, out IntVector2 point))
        {
            _contacts.Record(thrower, point);
        }
    }
}
