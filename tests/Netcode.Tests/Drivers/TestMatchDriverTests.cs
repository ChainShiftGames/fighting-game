using System;
using System.Collections.Generic;
using Netcode.Drivers;
using Netcode.Tests.Matches;
using Sim.Composition;
using Sim.Input;
using Sim.State;
using Xunit;

namespace Netcode.Tests.Drivers;

public sealed class TestMatchDriverTests
{
    private const int Frames = 600;
    private const int Player1Seed = 1357;
    private const int Player2Seed = 2468;

    private readonly MatchSimulation _match = TestmanMirror.Create();

    [Fact]
    public void FeedsTheWrappedDriverFromTheInputSources()
    {
        using var driver = new TestMatchDriver<MatchState>(
            new LocalMatchDriver<MatchState>(new ComposedMatchSimulation(_match)),
            [new SeededInputs(Player1Seed), new SeededInputs(Player2Seed)]);
        MatchState expected = _match.StartState();
        var player1 = new SeededInputs(Player1Seed);
        var player2 = new SeededInputs(Player2Seed);

        for (int frame = 0; frame < Frames; frame++)
        {
            Assert.True(driver.TryAdvanceFrame([]));
            _match.Tick(ref expected, player1.Next(), player2.Next());
        }

        Assert.Equal(0, driver.LocalPlayerCount);
        Assert.Equal(Frames, driver.CurrentFrame);
        Assert.Equal(StateChecksum.Compute(in expected), StateChecksum.Compute(in driver.State));
    }

    [Fact]
    public void AFrameThatCantRunYetGetsTheSameInputsAgain()
    {
        var waiting = new WaitingDriver();
        using var driver = new TestMatchDriver<MatchState>(waiting, [new CountingSource()]);

        Assert.False(driver.TryAdvanceFrame([]));
        Assert.True(driver.TryAdvanceFrame([]));
        Assert.False(driver.TryAdvanceFrame([]));
        Assert.True(driver.TryAdvanceFrame([]));

        Assert.Equal([1, 1, 2, 2], waiting.Offered);
    }

    [Fact]
    public void NeedsOneSourcePerLocalPlayer()
    {
        using var local = new LocalMatchDriver<MatchState>(new ComposedMatchSimulation(_match));

        Assert.Throws<ArgumentException>(() => new TestMatchDriver<MatchState>(local, [new SeededInputs(Player1Seed)]));
    }

    [Fact]
    public void DisposesTheWrappedDriver()
    {
        var waiting = new WaitingDriver();

        new TestMatchDriver<MatchState>(waiting, [new CountingSource()]).Dispose();

        Assert.True(waiting.IsDisposed);
    }

    // Numbers its frames' inputs 1, 2, 3, … so a test can see which frame an input was made for.
    private sealed class CountingSource : IInputSource
    {
        private ushort _next;

        public InputWord Next() => new(++_next);
    }

    // One local player; every other frame can't run yet, like an online match waiting for the other PC.
    private sealed class WaitingDriver : IMatchDriver<MatchState>
    {
        private MatchState _state;
        private bool _isReady = true;

        public List<int> Offered { get; } = [];

        public bool IsDisposed { get; private set; }

        public int LocalPlayerCount => 1;

        public int CurrentFrame { get; private set; }

        public ref readonly MatchState State => ref _state;

        public bool TryAdvanceFrame(ReadOnlySpan<InputWord> localInputs)
        {
            Offered.Add(localInputs[0].Bits);
            _isReady = !_isReady;
            if (!_isReady)
            {
                return false;
            }

            CurrentFrame++;
            return true;
        }

        public void Dispose() => IsDisposed = true;
    }
}
