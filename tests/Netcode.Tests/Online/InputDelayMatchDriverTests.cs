using Netcode.Online;
using Sim.Input;
using Sim.State;
using Xunit;

namespace Netcode.Tests.Online;

public sealed class InputDelayMatchDriverTests
{
    private const int RollbackFrames = 8;
    private const int SampleInterval = 1;
    private const int Samples = 3;
    private const int LowPing = 20;

    private readonly ScriptedRoundDriver _inner = new();
    private readonly ScriptedConnection _connection = new() { PingMilliseconds = LowPing };
    private readonly RecordingInputDelay _control = new();
    private readonly InputDelayMatchDriver _driver;

    public InputDelayMatchDriverTests()
    {
        var settings = new InputDelaySettings
        {
            Automatic = new InputDelayTuning
            {
                MinimumFrames = 1,
                MaximumFrames = 2,
                PingStepMilliseconds = 50,
                PingSampleIntervalFrames = SampleInterval,
                PingSamples = Samples,
            },
        };
        _driver = new InputDelayMatchDriver(
            _inner, _inner, _connection, _connection, new InputDelayController(settings, _connection, _control, RollbackFrames));
    }

    [Fact]
    public void TheDelayChangesInTheRoundOverPause()
    {
        Advance(Samples);
        Assert.Equal(2, _driver.InputDelayFrames);

        _inner.Phase = RoundPhase.RoundOver;
        Advance(RollbackFrames + 1);

        Assert.Equal(1, _driver.InputDelayFrames);
        Assert.Equal([1], _control.Changes);
    }

    [Fact]
    public void TheMatchEndingChangesNothing()
    {
        Advance(Samples);

        _inner.Phase = RoundPhase.MatchOver;
        Advance(RollbackFrames + 1);

        Assert.Empty(_control.Changes);
    }

    [Fact]
    public void FramesThatDidNotRunDontCountTowardsTheBreak()
    {
        Advance(Samples);
        _inner.Phase = RoundPhase.RoundOver;
        _inner.CanAdvance = false;

        Assert.False(_driver.TryAdvanceFrame([InputWord.Neutral]));
        Advance(RollbackFrames + 1);

        Assert.Empty(_control.Changes);
    }

    [Fact]
    public void PassesTheMatchAndConnectionThrough()
    {
        Advance(2);

        Assert.Equal(2, _driver.CurrentFrame);
        Assert.Equal(_inner.LocalPlayerCount, _driver.LocalPlayerCount);
        Assert.True(_driver.TryReadConnection(out ConnectionStats stats));
        Assert.Equal(LowPing, stats.PingMilliseconds);

        _driver.Dispose();
        Assert.True(_inner.Disposed);
    }

    private void Advance(int frames)
    {
        for (int frame = 0; frame < frames; frame++)
        {
            _driver.TryAdvanceFrame([InputWord.Neutral]);
        }
    }
}
