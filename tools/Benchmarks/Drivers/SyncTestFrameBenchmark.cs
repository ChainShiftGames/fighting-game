using Netcode.Drivers;
using Sim.Composition;
using Sim.Input;
using Sim.State;

namespace Benchmarks.Drivers;

/// <summary>
/// A Backdash SyncTest session: every frame saves and checksums the state, and every window-th frame rolls the
/// whole window back and resimulates it. With the 8-frame window one frame in eight is the rollback worst case, so
/// p99 measures that frame and the median a plain one.
/// </summary>
internal sealed class SyncTestFrameBenchmark : IDriverBenchmark
{
    private readonly RollbackMatchDriver<MatchState> _driver;
    private readonly InputSequence _inputs;
    private readonly InputWord[] _localInputs;

    public SyncTestFrameBenchmark(MatchSimulation simulation, InputSequence inputs, int rollbackFrames)
    {
        _driver = new RollbackMatchDriver<MatchState>(
            new ComposedMatchSimulation(simulation), SyncTestSession.Build<MatchState>(rollbackFrames));
        _inputs = inputs;
        _localInputs = new InputWord[_driver.LocalPlayerCount];
        Name = $"SyncTest frame ({rollbackFrames}-frame check)";
    }

    public string Name { get; }

    public int DriversInProcess => 1;

    public void BetweenFrames()
    {
        int frame = _driver.CurrentFrame;
        _localInputs[0] = _inputs.Player1At(frame);
        _localInputs[1] = _inputs.Player2At(frame);
    }

    public bool AdvanceFrame() => _driver.TryAdvanceFrame(_localInputs);

    public void Dispose() => _driver.Dispose();
}
