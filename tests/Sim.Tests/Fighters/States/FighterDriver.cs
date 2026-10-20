using Sim.Characters;
using Sim.Fighters.States;
using Sim.Input;
using Sim.State;

namespace Sim.Tests.Fighters.States;

/// <summary>Runs one fighter's state machine frame by frame with given inputs, the way the tick does.</summary>
internal sealed class FighterDriver
{
    private readonly FighterStateMachine _machine;
    private readonly CharacterData _character;
    private InputHistory _history;

    public FighterDriver(CharacterData character, IMoveInput? input = null, int facing = 1)
    {
        _character = character;
        _machine = new FighterStateMachine(input ?? new ButtonPressMoveInput());
        Fighter.Facing = facing;
        Fighter.Health = character.Stats.Health;
        MoveEntry.EnterBasic(ref Fighter, FighterAction.Idle, character);
    }

    public FighterState Fighter;

    /// <summary>The facing toward the opponent the machine is given each frame; null gives none (no turning).</summary>
    public int? OpponentFacing { get; set; }

    public FighterAction Action => Fighter.Action;

    /// <summary>One frame: records the input, then advances the state machine.</summary>
    public FighterDriver Frame(ushort input = 0)
    {
        _history.Record(new InputWord(input));
        if (OpponentFacing is int opponentFacing)
        {
            _machine.Advance(
                ref Fighter, in _history, _character, CancelPermission.EveryWindow, AvailableMeter.Unlimited,
                opponentFacing);
        }
        else
        {
            _machine.Advance(ref Fighter, in _history, _character);
        }
        return this;
    }

    public FighterDriver Frames(int count, ushort input = 0)
    {
        for (int frame = 0; frame < count; frame++)
        {
            Frame(input);
        }

        return this;
    }

    public MovePhase Phase => MovePhases.Of(in _character.Moves[Fighter.Move], Fighter.MoveFrame);
}
