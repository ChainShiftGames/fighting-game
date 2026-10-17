using System;
using System.Collections.Generic;
using Netcode.Desync;
using Netcode.Drivers;
using Sim.Input;
using Sim.State;

namespace Soak;

/// <summary>
/// Runs one peer's game logic and remembers the checksum of the state after every frame, so the soak can compare
/// the two peers frame by frame once frames are final, independently of the desync monitors.
/// </summary>
/// <remarks>
/// A rollback resimulates frames through here too, so each frame keeps the checksum of its latest run: once the
/// frame is final, that is its final state. The newest states and inputs are kept in a desync history, so a mismatch
/// gets the same dumps the monitors write (P2-09).
/// </remarks>
internal sealed class RecordingSimulation : IMatchSimulation<MatchState>
{
    private readonly IMatchSimulation<MatchState> _simulation;
    private readonly List<ulong> _checksums = [];
    private readonly List<bool> _isMatchOver = [];

    /// <param name="historyFrames">Newest frames kept whole for a dump (the desync history length).</param>
    public RecordingSimulation(IMatchSimulation<MatchState> simulation, int historyFrames)
    {
        _simulation = simulation;
        History = new FrameHistory<MatchState>(historyFrames);
    }

    /// <summary>The newest frame ever simulated (0 before the first).</summary>
    public int NewestFrame => _checksums.Count;

    /// <summary>The newest frames' states and inputs, for a dump.</summary>
    public FrameHistory<MatchState> History { get; }

    public MatchState CreateInitialState() => _simulation.CreateInitialState();

    public void Tick(ref MatchState state, InputWord player1, InputWord player2)
    {
        _simulation.Tick(ref state, player1, player2);
        Record(in state, player1, player2);
    }

    public ulong ChecksumAt(int frame) => _checksums[frame - 1];

    public bool IsMatchOverAt(int frame) => _isMatchOver[frame - 1];

    private void Record(in MatchState state, InputWord player1, InputWord player2)
    {
        int frame = state.Frame;
        if (frame < 1 || frame > NewestFrame + 1)
        {
            throw new InvalidOperationException(
                $"Frame {frame} ran after frame {NewestFrame}; the soak expects frames in order from 1.");
        }

        ulong checksum = StateChecksum.Compute(in state);
        bool isMatchOver = state.Round.Phase == RoundPhase.MatchOver;
        if (frame > NewestFrame)
        {
            _checksums.Add(checksum);
            _isMatchOver.Add(isMatchOver);
        }
        else
        {
            _checksums[frame - 1] = checksum;
            _isMatchOver[frame - 1] = isMatchOver;
        }

        History.Record(frame, in state, player1, player2);
    }
}
