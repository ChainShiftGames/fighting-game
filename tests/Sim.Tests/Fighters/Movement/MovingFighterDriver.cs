using Sim.Characters;
using Sim.Fighters.Movement;
using Sim.Fighters.States;
using Sim.Input;
using Sim.State;
using Sim.Tests.Fighters.States;

namespace Sim.Tests.Fighters.Movement;

/// <summary>Runs one fighter's state machine and then its movement each frame, in tick order.</summary>
internal sealed class MovingFighterDriver
{
    /// <summary>A spare button bit the stand-in input treats as the dash button.</summary>
    public const ushort DashButton = 1 << 9;

    private readonly FighterStateMachine _machine = new(new ButtonPressMoveInput(DashButton));
    private readonly FighterMovement _movement = new();
    private readonly CharacterData _character;
    private InputHistory _history;

    public MovingFighterDriver(CharacterData character, int facing = 1)
    {
        _character = character;
        Fighter.Facing = facing;
        MoveEntry.EnterBasic(ref Fighter, FighterAction.Idle, character);
    }

    public FighterState Fighter;

    public FighterAction Action => Fighter.Action;

    public int X => Fighter.Position.X;

    public int Y => Fighter.Position.Y;

    public MovingFighterDriver Frame(ushort input = 0)
    {
        _history.Record(new InputWord(input));
        _machine.Advance(ref Fighter, in _history, _character);
        _movement.Advance(ref Fighter, _character);
        return this;
    }

    public MovingFighterDriver Frames(int count, ushort input = 0)
    {
        for (int frame = 0; frame < count; frame++)
        {
            Frame(input);
        }

        return this;
    }

    /// <summary>Runs frames until the fighter is on the ground again; returns how many it took.</summary>
    public int FramesUntilGrounded(ushort input = 0)
    {
        int frames = 0;
        while (Fighter.IsAirborne != 0)
        {
            Frame(input);
            frames++;
        }

        return frames;
    }

    /// <summary>Holds up through prejump; returns on the takeoff frame.</summary>
    public MovingFighterDriver JumpTakeoff(ushort direction = 0) =>
        Frames(TestCharacter.PrejumpFrames + 1, (ushort)(InputWord.Up | direction));
}
