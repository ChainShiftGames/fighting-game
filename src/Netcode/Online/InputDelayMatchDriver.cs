using System;
using Netcode.Drivers;
using Sim.Input;
using Sim.State;

namespace Netcode.Online;

/// <summary>Runs an online match through another driver and tells the input delay when a round is over.</summary>
/// <remarks>It owns the wrapped driver and disposes it.</remarks>
public sealed class InputDelayMatchDriver :
    IMatchDriver<MatchState>, IConfirmedFrameReadout, IConnectionReadout, IConnectionStatusReadout
{
    private readonly IMatchDriver<MatchState> _driver;
    private readonly IConfirmedFrameReadout _confirmed;
    private readonly IConnectionReadout _connection;
    private readonly IConnectionStatusReadout _status;
    private readonly InputDelayController _delay;

    /// <param name="driver">Runs the online match.</param>
    /// <param name="confirmed">The same match's final frames, passed through for its replay.</param>
    /// <param name="connection">The same match's connection readings, passed through for the overlay.</param>
    /// <param name="status">The same match's connection status, passed through for the player's messages.</param>
    /// <param name="delay">Keeps the match's input delay.</param>
    public InputDelayMatchDriver(
        IMatchDriver<MatchState> driver,
        IConfirmedFrameReadout confirmed,
        IConnectionReadout connection,
        IConnectionStatusReadout status,
        InputDelayController delay)
    {
        _driver = driver;
        _confirmed = confirmed;
        _connection = connection;
        _status = status;
        _delay = delay;
    }

    public int LocalPlayerCount => _driver.LocalPlayerCount;

    public int CurrentFrame => _driver.CurrentFrame;

    public ref readonly MatchState State => ref _driver.State;

    public int ConfirmedFrame => _confirmed.ConfirmedFrame;

    /// <summary>The delay this PC's inputs have now.</summary>
    public int InputDelayFrames => _delay.CurrentFrames;

    public bool TryAdvanceFrame(ReadOnlySpan<InputWord> localInputs)
    {
        if (!_driver.TryAdvanceFrame(localInputs))
        {
            return false;
        }

        _delay.AfterFrame(_driver.State.Round.Phase == RoundPhase.RoundOver);
        return true;
    }

    public bool TryReadConnection(out ConnectionStats stats) => _connection.TryReadConnection(out stats);

    public ConnectionStatus ReadConnectionStatus() => _status.ReadConnectionStatus();

    public void Dispose() => _driver.Dispose();
}
