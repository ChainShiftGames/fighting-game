using Netcode.Drivers;
using Netcode.Tests.Matches;
using Sim.Input;
using Sim.State;

namespace Netcode.Tests.Online;

/// <summary>The Testman mirror, remembering the inputs each frame last ran with on this PC.</summary>
/// <remarks>
/// A resimulated frame overwrites its predicted inputs, so once a frame's inputs are confirmed this holds the inputs
/// this PC settled on for it.
/// </remarks>
internal sealed class InputRecordingSimulation : IMatchSimulation<MatchState>
{
    private readonly ComposedMatchSimulation _simulation = new(TestmanMirror.Create());
    private readonly InputWord[] _player1;

    public InputRecordingSimulation(int frames)
    {
        _player1 = new InputWord[frames];
    }

    public MatchState CreateInitialState() => _simulation.CreateInitialState();

    public void Tick(ref MatchState state, InputWord player1, InputWord player2)
    {
        if (state.Frame < _player1.Length)
        {
            _player1[state.Frame] = player1;
        }

        _simulation.Tick(ref state, player1, player2);
    }

    /// <summary>Player 1's input that <paramref name="frame"/> last ran with.</summary>
    public InputWord Player1At(int frame) => _player1[frame];
}
