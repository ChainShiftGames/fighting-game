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
        MoveEntry.EnterBasic(ref Fighter, FighterAction.Idle, character);
    }

    public FighterState Fighter;

    public FighterAction Action => Fighter.Action;

    /// <summary>One frame: records the input, then advances the state machine.</summary>
    public FighterDriver Frame(ushort input = 0)
    {
        _history.Record(new InputWord(input));
        _machine.Advance(ref Fighter, in _history, _character);
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
