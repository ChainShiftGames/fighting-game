using System;
using Sim.Characters;
using Sim.Input;
using Sim.State;

namespace Netcode.Drivers.Training;

/// <summary>Plays the dummy as the training menu says right now: the inputs of the current <see cref="DummyMode"/>.</summary>
/// <remarks>
/// <see cref="Create"/> is the one place each mode gets its inputs; a new mode adds its value to
/// <see cref="DummyMode"/> and one entry there. The mode is read every frame, so a change applies on the next one: the
/// newly chosen mode starts over (<see cref="ITrainingReset"/>), and leaving record mode stops a recording.
/// </remarks>
public sealed class DummyModeInputs : ITrainingInputs<MatchState>, ITrainingReset
{
    private readonly ITrainingInputs<MatchState>[] _byMode;
    private readonly Func<DummyMode> _mode;
    private readonly RecordingSlots _recordings;
    private DummyMode _lastMode;

    /// <param name="byMode">The inputs for each mode, indexed by <see cref="DummyMode"/>.</param>
    /// <param name="mode">The mode the player chose (a player setting), read every frame.</param>
    /// <param name="recordings">The slots record mode records into.</param>
    public DummyModeInputs(ITrainingInputs<MatchState>[] byMode, Func<DummyMode> mode, RecordingSlots recordings)
    {
        if (byMode.Length != Enum.GetValues<DummyMode>().Length)
        {
            throw new ArgumentException("Every dummy mode needs its inputs.", nameof(byMode));
        }

        _byMode = byMode;
        _mode = mode;
        _recordings = recordings;
        _lastMode = mode();
    }

    /// <param name="player">The training player's character, which the dummy guards against.</param>
    /// <param name="playback">The player's playback slot and style, read when a playback starts.</param>
    public static DummyModeInputs Create(
        CharacterData player,
        TrainingTimings timings,
        Func<DummyMode> mode,
        RecordingSlots recordings,
        Func<PlaybackChoice> playback)
    {
        var guard = new GuardingDummy(player);
        ITrainingInputs<MatchState>[] byMode = new ITrainingInputs<MatchState>[Enum.GetValues<DummyMode>().Length];
        byMode[(int)DummyMode.Stand] = new PlayerAgainstDummy(new HeldInputDummy(InputWord.Neutral));
        byMode[(int)DummyMode.Crouch] = new PlayerAgainstDummy(new HeldInputDummy(new InputWord(InputWord.Down)));
        byMode[(int)DummyMode.BlockAll] = new PlayerAgainstDummy(guard);
        byMode[(int)DummyMode.BlockAfterFirstHit] =
            new PlayerAgainstDummy(new GuardAfterFirstHitDummy(guard, timings.GuardResetFrames));
        byMode[(int)DummyMode.Jump] = new PlayerAgainstDummy(new HeldInputDummy(new InputWord(InputWord.Up)));
        byMode[(int)DummyMode.Record] = new RecordingDummy(recordings);
        byMode[(int)DummyMode.Playback] =
            new PlayerAgainstDummy(new PlaybackDummy(recordings, playback, new Random()));
        return new DummyModeInputs(byMode, mode, recordings);
    }

    public FrameInputs Next(in MatchState state, InputWord player)
    {
        DummyMode mode = _mode();
        if (mode != _lastMode)
        {
            ResetMode(mode);
            _lastMode = mode;
        }

        if (mode != DummyMode.Record)
        {
            _recordings.Stop();
        }

        return _byMode[(int)mode].Next(in state, player);
    }

    public void Reset()
    {
        foreach (DummyMode mode in Enum.GetValues<DummyMode>())
        {
            ResetMode(mode);
        }
    }

    private void ResetMode(DummyMode mode) => (_byMode[(int)mode] as ITrainingReset)?.Reset();
}
