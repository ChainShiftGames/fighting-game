using System.Collections.Generic;
using Netcode.Drivers;
using Netcode.Tests.Matches;
using Sim.Input;
using Sim.State;

namespace Netcode.Tests.Online;

/// <summary>The Testman mirror, remembering the checksum of the state after each frame's latest run on this PC.</summary>
/// <remarks>A resimulated frame overwrites its checksum, so a frame that is final keeps the one it has now.</remarks>
internal sealed class ChecksumRecordingSimulation : IMatchSimulation<MatchState>
{
    private readonly ComposedMatchSimulation _simulation = new(TestmanMirror.Create());
    private readonly List<ulong> _checksums = [];

    public MatchState CreateInitialState() => _simulation.CreateInitialState();

    public void Tick(ref MatchState state, InputWord player1, InputWord player2)
    {
        _simulation.Tick(ref state, player1, player2);
        ulong checksum = StateChecksum.Compute(in state);
        if (state.Frame > _checksums.Count)
        {
            _checksums.Add(checksum);
        }
        else
        {
            _checksums[state.Frame - 1] = checksum;
        }
    }

    /// <summary>The checksum of the state after <paramref name="frame"/> (from 1), as it last ran.</summary>
    public ulong ChecksumAt(int frame) => _checksums[frame - 1];
}
