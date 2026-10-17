using System;
using Netcode.Drivers;
using Sim.Input;
using Sim.State;

namespace Netcode.Online;

/// <summary>Runs an online match through another driver and tells the input delay when a round is over.</summary>
/// <remarks>It owns the wrapped driver and disposes it.</remarks>
public sealed class InputDelayMatchDriver : IMatchDriver<MatchState>, IConnectionReadout
{
    private readonly IMatchDriver<MatchState> _driver;
    private readonly IConnectionReadout _connection;
    private readonly InputDelayController _delay;

    /// <param name="driver">Runs the online match.</param>
    /// <param name="connection">The same match's connection readings, passed through for the overlay.</param>
    /// <param name="delay">Keeps the match's input delay.</param>
    public InputDelayMatchDriver(IMatchDriver<MatchState> driver, IConnectionReadout connection, InputDelayController delay)
    {
        _driver = driver;
        _connection = connection;
        _delay = delay;
    }

    public int LocalPlayerCount => _driver.LocalPlayerCount;

    public int CurrentFrame => _driver.CurrentFrame;

    public ref readonly MatchState State => ref _driver.State;

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

    public void Dispose() => _driver.Dispose();
}
