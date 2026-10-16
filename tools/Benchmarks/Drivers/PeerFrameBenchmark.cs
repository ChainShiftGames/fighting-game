using System.Net;
using System.Threading;
using Netcode.Drivers;
using Netcode.Online;
using Sim.Composition;
using Sim.Input;
using Sim.State;

namespace Benchmarks.Drivers;

/// <summary>
/// Two Backdash peer sessions in this process over loopback UDP: input exchange, prediction and the network jobs.
/// Only P1's frames are timed; P2 runs between them.
/// </summary>
/// <remarks>
/// Paced at about a frame per millisecond, not 60 Hz, so a run stays short. With the default input delay the
/// other player's input has usually arrived by then, so this measures the no-rollback case; the SyncTest
/// benchmark covers rollbacks.
/// </remarks>
internal sealed class PeerFrameBenchmark : IDriverBenchmark
{
    // Above the ports PeerSessionTests uses, so a benchmark and the tests can run at the same time.
    private const int Player1Port = 47111;
    private const int Player2Port = 47112;

    private const int PacingMilliseconds = 1;

    private readonly RollbackMatchDriver<MatchState> _player1;
    private readonly RollbackMatchDriver<MatchState> _player2;
    private readonly InputSequence _inputs;
    private readonly InputWord[] _player1Input = new InputWord[1];
    private readonly InputWord[] _player2Input = new InputWord[1];

    public PeerFrameBenchmark(MatchSimulation simulation, InputSequence inputs, int rollbackFrames)
    {
        _player1 = CreateDriver(simulation, Player1Port, Player2Port, localIsPlayer1: true, rollbackFrames);
        _player2 = CreateDriver(simulation, Player2Port, Player1Port, localIsPlayer1: false, rollbackFrames);
        _inputs = inputs;
    }

    public string Name => "Peer frame (loopback UDP)";

    public int DriversInProcess => 2;

    public void BetweenFrames()
    {
        _player2Input[0] = _inputs.Player2At(_player2.CurrentFrame);
        _player2.TryAdvanceFrame(_player2Input);
        _player1Input[0] = _inputs.Player1At(_player1.CurrentFrame);

        // Lets the packets cross and Backdash's network jobs run, as real time between frames would.
        Thread.Sleep(PacingMilliseconds);
    }

    public bool AdvanceFrame() => _player1.TryAdvanceFrame(_player1Input);

    public void Dispose()
    {
        _player1.Dispose();
        _player2.Dispose();
    }

    private static RollbackMatchDriver<MatchState> CreateDriver(
        MatchSimulation simulation, int localPort, int remotePort, bool localIsPlayer1, int rollbackFrames) =>
        new(new ComposedMatchSimulation(simulation), PeerSession.Build<MatchState>(new PeerMatchSettings
        {
            LocalPort = localPort,
            RemoteEndPoint = new IPEndPoint(IPAddress.Loopback, remotePort),
            LocalIsPlayer1 = localIsPlayer1,
            RollbackFrames = rollbackFrames,
        }, PeerTransport.DirectUdp));
}
