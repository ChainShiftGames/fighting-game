using System;
using Sim.Characters;
using Sim.Input;
using Sim.State;

namespace Netcode.Drivers.Training;

/// <summary>Plays the dummy as the training menu says right now: the inputs of the current <see cref="DummyMode"/>.</summary>
/// <remarks>
/// <see cref="Create"/> is the one place each mode gets its inputs; a new mode (P3-32 playback) adds its value to
/// <see cref="DummyMode"/> and one entry there. The mode is read every frame, so a change applies on the next one.
/// </remarks>
public sealed class DummyModeInputs : ITrainingInputs<MatchState>
{
    private readonly ITrainingInputs<MatchState>[] _byMode;
    private readonly Func<DummyMode> _mode;

    /// <param name="byMode">The inputs for each mode, indexed by <see cref="DummyMode"/>.</param>
    /// <param name="mode">The mode the player chose (a player setting), read every frame.</param>
    public DummyModeInputs(ITrainingInputs<MatchState>[] byMode, Func<DummyMode> mode)
    {
        if (byMode.Length != Enum.GetValues<DummyMode>().Length)
        {
            throw new ArgumentException("Every dummy mode needs its inputs.", nameof(byMode));
        }

        _byMode = byMode;
        _mode = mode;
    }

    /// <param name="player">The training player's character, which the dummy guards against.</param>
    public static DummyModeInputs Create(CharacterData player, TrainingTimings timings, Func<DummyMode> mode)
    {
        var guard = new GuardingDummy(player);
        ITrainingInputs<MatchState>[] byMode = new ITrainingInputs<MatchState>[Enum.GetValues<DummyMode>().Length];
        byMode[(int)DummyMode.Stand] = new PlayerAgainstDummy(new HeldInputDummy(InputWord.Neutral));
        byMode[(int)DummyMode.Crouch] = new PlayerAgainstDummy(new HeldInputDummy(new InputWord(InputWord.Down)));
        byMode[(int)DummyMode.BlockAll] = new PlayerAgainstDummy(guard);
        byMode[(int)DummyMode.BlockAfterFirstHit] =
            new PlayerAgainstDummy(new GuardAfterFirstHitDummy(guard, timings.GuardResetFrames));
        byMode[(int)DummyMode.Jump] = new PlayerAgainstDummy(new HeldInputDummy(new InputWord(InputWord.Up)));
        byMode[(int)DummyMode.Record] = new PlayerControlsDummy();
        return new DummyModeInputs(byMode, mode);
    }

    public FrameInputs Next(in MatchState state, InputWord player) => _byMode[(int)_mode()].Next(in state, player);
}
