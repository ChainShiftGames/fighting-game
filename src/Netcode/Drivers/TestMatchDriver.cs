using System;
using Sim.Input;

namespace Netcode.Drivers;

/// <summary>
/// A match no one on this PC plays: each local player's inputs come from an <see cref="IInputSource"/> (script,
/// seeded inputs, bot), fed through any other driver.
/// </summary>
/// <remarks>
/// It wraps the driver that runs the match (local, SyncTest, online), so automated tests, soak runs and bot matches
/// go through the same drivers players use. It owns the wrapped driver and disposes it.
/// </remarks>
public sealed class TestMatchDriver<TState> : IMatchDriver<TState>
    where TState : unmanaged
{
    private readonly IMatchDriver<TState> _driver;
    private readonly IInputSource[] _players;
    private readonly InputWord[] _inputs;
    private bool _hasUnusedInputs;

    /// <param name="driver">Runs the match; this driver feeds it.</param>
    /// <param name="players">One source per local player of <paramref name="driver"/>, in port order.</param>
    public TestMatchDriver(IMatchDriver<TState> driver, IInputSource[] players)
    {
        if (players.Length != driver.LocalPlayerCount)
        {
            throw new ArgumentException(
                $"The driver expects {driver.LocalPlayerCount} local players, got {players.Length} input sources.",
                nameof(players));
        }

        _driver = driver;
        _players = players;
        _inputs = new InputWord[players.Length];
    }

    public int LocalPlayerCount => 0;

    public int CurrentFrame => _driver.CurrentFrame;

    public ref readonly TState State => ref _driver.State;

    public bool TryAdvanceFrame(ReadOnlySpan<InputWord> localInputs)
    {
        MatchPlayers.EnsureInputCount(localInputs, LocalPlayerCount);

        // A frame that can't run yet (online, waiting for the other PC) gets the same inputs on the next try, so
        // each source's sequence lines up with the frames that actually run.
        if (!_hasUnusedInputs)
        {
            for (int player = 0; player < _players.Length; player++)
            {
                _inputs[player] = _players[player].Next();
            }

            _hasUnusedInputs = true;
        }

        if (!_driver.TryAdvanceFrame(_inputs))
        {
            return false;
        }

        _hasUnusedInputs = false;
        return true;
    }

    public void Dispose() => _driver.Dispose();
}
