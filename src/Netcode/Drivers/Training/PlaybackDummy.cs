using System;
using Sim.Input;
using Sim.State;

namespace Netcode.Drivers.Training;

/// <summary>Playback mode: the dummy plays a recording, once, looped or from random slots (<see cref="PlaybackStyle"/>).</summary>
/// <remarks>
/// Recordings play relative to the dummy's facing at the time. The chosen slot and style are read when a playback
/// starts: on entering the mode, after <see cref="Reset"/> (training restart, loaded savestate) and on each loop.
/// With nothing recorded to play, the dummy stands.
/// </remarks>
public sealed class PlaybackDummy : IDummyBehaviour, ITrainingReset
{
    private const int NoSlot = -1;

    private readonly RecordingSlots _slots;
    private readonly Func<PlaybackChoice> _choice;
    private readonly Random _random;
    private int _slot = NoSlot;
    private int _frame;
    private bool _hasStarted;

    /// <param name="choice">The player's slot and style (player settings), read when a playback starts.</param>
    /// <param name="random">Picks the slot for <see cref="PlaybackStyle.Random"/>.</param>
    public PlaybackDummy(RecordingSlots slots, Func<PlaybackChoice> choice, Random random)
    {
        _slots = slots;
        _choice = choice;
        _random = random;
    }

    public InputWord Next(in MatchState state)
    {
        if (!_hasStarted)
        {
            _hasStarted = true;
            Begin();
        }

        if (_slot == NoSlot)
        {
            return InputWord.Neutral;
        }

        InputWord input = _slots.At(_slot, _frame++);
        if (_frame >= _slots.Length(_slot))
        {
            EndOfRecording();
        }

        return FacingInputs.Turn(input, state.Fighters[TrainingSides.Dummy].Facing);
    }

    /// <summary>Plays from the start again on the next frame, with the slot and style chosen then.</summary>
    public void Reset() => _hasStarted = false;

    private void Begin()
    {
        PlaybackChoice choice = _choice();
        _slot = choice.Style == PlaybackStyle.Random ? RandomRecordedSlot() : Recorded(choice.Slot);
        _frame = 0;
    }

    private void EndOfRecording()
    {
        if (_choice().Style == PlaybackStyle.Once)
        {
            _slot = NoSlot;
            return;
        }

        Begin();
    }

    private int Recorded(int slot) => _slots.Length(slot) > 0 ? slot : NoSlot;

    private int RandomRecordedSlot()
    {
        int recorded = 0;
        for (int slot = 0; slot < RecordingSlots.Count; slot++)
        {
            recorded += _slots.Length(slot) > 0 ? 1 : 0;
        }

        int pick = recorded > 0 ? _random.Next(recorded) : 0;
        for (int slot = 0; slot < RecordingSlots.Count; slot++)
        {
            if (_slots.Length(slot) > 0 && pick-- == 0)
            {
                return slot;
            }
        }

        return NoSlot;
    }
}
