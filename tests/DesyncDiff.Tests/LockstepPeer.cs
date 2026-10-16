using System;
using Netcode.Desync;
using Netcode.Drivers;
using Sim.Input;
using Sim.State;

namespace DesyncDiff.Tests;

/// <summary>
/// One PC of an in-process match without prediction: both PCs get the same inputs every frame, so each frame is
/// final as soon as it runs. Its desync monitor compares checksums with the other peer's.
/// </summary>
internal sealed class LockstepPeer : IDisposable
{
    private readonly LocalMatchDriver<MatchState> _driver;
    private readonly DesyncMonitor<MatchState> _monitor;

    public LockstepPeer(IMatchSimulation<MatchState> simulation, DesyncMonitor<MatchState> monitor)
    {
        _driver = new LocalMatchDriver<MatchState>(simulation);
        _monitor = monitor;
    }

    /// <summary>The desync this peer found; null while it agrees with the other one.</summary>
    public DesyncDetectedException? Desync { get; private set; }

    public void Step(InputWord player1, InputWord player2)
    {
        if (Desync is not null)
        {
            return;
        }

        try
        {
            _driver.TryAdvanceFrame([player1, player2]);
            _monitor.RecordFrame(_driver.CurrentFrame, in _driver.State, player1, player2);
            _monitor.ConfirmThrough(_driver.CurrentFrame);
        }
        catch (DesyncDetectedException desync)
        {
            Desync = desync;
        }
    }

    public void Dispose() => _driver.Dispose();
}
