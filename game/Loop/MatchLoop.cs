using System;
using System.Diagnostics;
using FightingGame.Diagnostics;
using FightingGame.Input;
using Netcode.Drivers;
using Sim.Input;

namespace FightingGame.Loop;

/// <summary>
/// Our own fixed 60 Hz loop (ADR-0004): fed wall-clock time from Godot's frame callback, it runs each due logic
/// frame with input read immediately before it.
/// </summary>
public sealed class MatchLoop<TState>
    where TState : unmanaged
{
    private readonly FixedStepClock _clock;
    private readonly IMatchDriver<TState> _driver;
    private readonly PlayerInputReader[] _players;
    private readonly InputLatencyCounter _latency;
    private readonly InputWord[] _inputs;

    public MatchLoop(
        FixedStepClock clock,
        IMatchDriver<TState> driver,
        PlayerInputReader[] players,
        InputLatencyCounter latency)
    {
        if (players.Length != driver.LocalPlayerCount)
        {
            throw new ArgumentException(
                $"The driver expects {driver.LocalPlayerCount} local players, got {players.Length}.",
                nameof(players));
        }

        _clock = clock;
        _driver = driver;
        _players = players;
        _latency = latency;
        _inputs = new InputWord[players.Length];
    }

    /// <summary>The raw words read for the last logic frame, one per local player (for the developer overlay).</summary>
    public ReadOnlySpan<InputWord> LastInputs => _inputs;

    /// <summary>Runs every logic frame due at <paramref name="timestamp"/>; returns how many ran.</summary>
    public int Advance(long timestamp)
    {
        int dueFrames = _clock.Advance(timestamp);
        for (int frame = 0; frame < dueFrames; frame++)
        {
            if (!RunFrame())
            {
                return frame;
            }
        }

        return dueFrames;
    }

    private bool RunFrame()
    {
        PollDevices();
        for (int player = 0; player < _players.Length; player++)
        {
            _inputs[player] = _players[player].Read();
        }

        _latency.InputRead(Stopwatch.GetTimestamp());
        return _driver.TryAdvanceFrame(_inputs);
    }

    // Godot pumps OS input events once per displayed frame, before the frame callback. Pumping again here picks up
    // presses that arrived since, instead of leaving them for the next displayed frame (ADR-0004 §2).
    private static void PollDevices()
    {
        Godot.DisplayServer.ProcessEvents();
        Godot.Input.FlushBufferedEvents();
    }
}
