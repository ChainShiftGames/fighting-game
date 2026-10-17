using System;
using System.Collections.Generic;
using Netcode.Bots;
using Netcode.Drivers;
using Netcode.Tests.Matches;
using Sim.Characters;
using Sim.Composition;
using Sim.Input;
using Sim.State;

namespace Netcode.Tests.Bots;

/// <summary>A Testman mirror with a CPU bot on P1 and a bot or other inputs on P2, through a real driver.</summary>
internal sealed class BotMatch : IDisposable
{
    // The rollback window (developer setting, ADR-0003).
    private const int RollbackFrames = 8;

    private readonly TestMatchDriver<MatchState> _driver;

    /// <param name="syncTest">Run through SyncTest (every frame rolled back and resimulated), not locally.</param>
    /// <param name="player2">P2's inputs; null for a second bot with <paramref name="player2Settings"/>.</param>
    public BotMatch(
        BotSettings player1Settings,
        uint player1Seed,
        BotSettings? player2Settings = null,
        uint player2Seed = 0,
        bool syncTest = false,
        IInputSource? player2 = null)
    {
        MatchSimulation match = TestmanMirror.Create();
        IMatchDriver<MatchState> inner = syncTest
            ? new RollbackMatchDriver<MatchState>(
                new ComposedMatchSimulation(match), SyncTestSession.Build<MatchState>(RollbackFrames))
            : new LocalMatchDriver<MatchState>(new ComposedMatchSimulation(match));
        Player1 = CreateBot(inner, 0, player1Settings, player1Seed);
        Player2 = player2 is null ? CreateBot(inner, 1, player2Settings ?? player1Settings, player2Seed) : null;
        _driver = new TestMatchDriver<MatchState>(
            inner, [new Recording(Player1, Player1Inputs), new Recording(player2 ?? Player2!, Player2Inputs)]);
    }

    public static CharacterData Character => TestmanMirror.Data.Character(TestmanMirror.Character);

    public CpuBot Player1 { get; }

    /// <summary>P2's bot; null when P2 plays other inputs.</summary>
    public CpuBot? Player2 { get; }

    public List<InputWord> Player1Inputs { get; } = [];

    public List<InputWord> Player2Inputs { get; } = [];

    public ref readonly MatchState State => ref _driver.State;

    public void Advance()
    {
        if (!_driver.TryAdvanceFrame([]))
        {
            throw new InvalidOperationException($"Frame {_driver.CurrentFrame + 1} did not run.");
        }
    }

    public void Dispose() => _driver.Dispose();

    private static CpuBot CreateBot(IMatchDriver<MatchState> driver, int player, BotSettings settings, uint seed) =>
        new(
            new DriverObservationSource(driver, player, Character, Character),
            MoveRepertoire.Build(Character, Character),
            TestmanMirror.Data.Motions.Movement,
            settings,
            seed);

    private sealed class Recording : IInputSource
    {
        private readonly IInputSource _source;
        private readonly List<InputWord> _inputs;

        public Recording(IInputSource source, List<InputWord> inputs)
        {
            _source = source;
            _inputs = inputs;
        }

        public InputWord Next()
        {
            InputWord input = _source.Next();
            _inputs.Add(input);
            return input;
        }
    }
}
