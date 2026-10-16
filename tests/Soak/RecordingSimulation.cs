using System;
using System.Collections.Generic;
using Netcode.Drivers;
using Sim.Input;
using Sim.State;

namespace Soak;

/// <summary>
/// Runs one peer's game logic and remembers the checksum of the state after every frame, so the soak can compare
/// the two peers frame by frame once frames are confirmed.
/// </summary>
/// <remarks>
/// A rollback resimulates frames through here too, so each frame keeps the checksum of its latest run: once the
/// frame is confirmed, that is its final state. The newest states are kept whole for a desync dump.
/// </remarks>
internal sealed class RecordingSimulation : IMatchSimulation<MatchState>
{
    // More than the frames between the newest one and the confirmed ones the soak compares.
    private const int KeptStates = 1024;

    private readonly IMatchSimulation<MatchState> _simulation;
    private readonly List<ulong> _checksums = [];
    private readonly List<bool> _isMatchOver = [];
    private readonly MatchState[] _keptStates = new MatchState[KeptStates];

    public RecordingSimulation(IMatchSimulation<MatchState> simulation)
    {
        _simulation = simulation;
    }

    /// <summary>The newest frame ever simulated (0 before the first).</summary>
    public int NewestFrame => _checksums.Count;

    public MatchState CreateInitialState() => _simulation.CreateInitialState();

    public void Tick(ref MatchState state, InputWord player1, InputWord player2)
    {
        _simulation.Tick(ref state, player1, player2);
        Record(in state);
    }

    public ulong ChecksumAt(int frame) => _checksums[frame - 1];

    public bool IsMatchOverAt(int frame) => _isMatchOver[frame - 1];

    /// <summary>The state after <paramref name="frame"/>, while it is among the newest kept ones.</summary>
    public bool TryGetState(int frame, out MatchState state)
    {
        state = _keptStates[frame % KeptStates];
        return frame > NewestFrame - KeptStates && state.Frame == frame;
    }

    private void Record(in MatchState state)
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

        _keptStates[frame % KeptStates] = state;
    }
}
