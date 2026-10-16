using FightingGame.Diagnostics;
using FightingGame.Input;
using Netcode.Drivers;

namespace FightingGame.Loop;

/// <summary>Builds each match's loop with the game's loop timing and its one input latency counter.</summary>
public sealed class MatchLoopFactory
{
    private readonly LoopSettings _settings;
    private readonly long _ticksPerSecond;
    private readonly InputLatencyCounter _latency;

    public MatchLoopFactory(LoopSettings settings, long ticksPerSecond, InputLatencyCounter latency)
    {
        _settings = settings;
        _ticksPerSecond = ticksPerSecond;
        _latency = latency;
    }

    /// <param name="stepping">Pauses and steps the match; null for matches that can't pause (online).</param>
    public MatchLoop<TState> Create<TState>(
        IMatchDriver<TState> driver, PlayerInputReader[] players, FrameStepping? stepping)
        where TState : unmanaged =>
        new(new FixedStepClock(_settings, _ticksPerSecond), driver, players, _latency, stepping);
}
