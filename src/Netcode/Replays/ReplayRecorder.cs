using System;
using System.Collections.Generic;
using Netcode.Drivers;
using Sim.Input;

namespace Netcode.Replays;

/// <summary>Collects every frame's inputs of one match as it runs, for a <see cref="Replay"/>.</summary>
/// <remarks>
/// A rolled-back frame is recorded again when it is resimulated: its new inputs replace the old ones and the frames
/// after it are dropped, as they are resimulated too. So the recording always follows the match the driver shows.
/// </remarks>
public sealed class ReplayRecorder
{
    // Ten minutes at 60 Hz, reserved up front so a normal match never grows the list mid-match.
    private const int ReservedFrames = 10 * 60 * 60;

    private readonly ReplayHeader _header;
    private readonly List<FrameInputs> _inputs = new(ReservedFrames);

    public ReplayRecorder(ReplayHeader header)
    {
        _header = header;
    }

    /// <summary>Frames recorded so far.</summary>
    public int FrameCount => _inputs.Count;

    /// <summary>Records the inputs of the frame that runs from <paramref name="frame"/> (0 for the first frame).</summary>
    /// <exception cref="ArgumentOutOfRangeException">Frames before <paramref name="frame"/> weren't recorded.</exception>
    public void Record(int frame, InputWord player1, InputWord player2)
    {
        if (frame < 0 || frame > _inputs.Count)
        {
            throw new ArgumentOutOfRangeException(
                nameof(frame), frame, $"Only frames 0 to {_inputs.Count} can be recorded next.");
        }

        if (frame < _inputs.Count)
        {
            // A rollback: this frame and everything after it run again.
            _inputs.RemoveRange(frame, _inputs.Count - frame);
        }

        _inputs.Add(new FrameInputs(player1, player2));
    }

    /// <summary>The match so far as a replay; recording can go on afterwards.</summary>
    public Replay ToReplay() => new(_header, _inputs.ToArray());
}
