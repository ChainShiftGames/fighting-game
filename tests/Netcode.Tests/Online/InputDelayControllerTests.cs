using System;
using Netcode.Online;
using Xunit;

namespace Netcode.Tests.Online;

public sealed class InputDelayControllerTests
{
    // Developer settings as in data/netcode/online.json, with shorter sampling so the tests stay small.
    private const int PingStep = 50;
    private const int SampleInterval = 10;
    private const int Samples = 5;
    private const int RollbackFrames = 8;
    private const int LowPing = 20;
    private const int HighPing = 90;

    private readonly ScriptedConnection _connection = new();
    private readonly RecordingInputDelay _control = new();

    [Fact]
    public void TheFirstRoundUsesTheHighestAutomaticDelay()
    {
        InputDelayController delay = Create(playerOverride: null);

        Assert.Equal(2, delay.CurrentFrames);
        Assert.Empty(_control.Changes);
    }

    [Fact]
    public void ALowPingLowersTheDelayBetweenRounds()
    {
        InputDelayController delay = Create(playerOverride: null);

        Readings(delay, LowPing, LowPing, LowPing, LowPing, LowPing);
        Pause(delay, RollbackFrames + 1);

        Assert.Equal(1, delay.CurrentFrames);
        Assert.Equal([1], _control.Changes);
    }

    [Fact]
    public void TheDelayNeverChangesWhileARoundIsFought()
    {
        InputDelayController delay = Create(playerOverride: null);

        Readings(delay, LowPing, LowPing, LowPing, LowPing, LowPing);
        Fight(delay, 3600);

        Assert.Equal(2, delay.CurrentFrames);
        Assert.Empty(_control.Changes);
    }

    [Fact]
    public void ThePauseMustOutlastTheRollbackWindowFirst()
    {
        InputDelayController delay = Create(playerOverride: null);
        Readings(delay, LowPing, LowPing, LowPing, LowPing, LowPing);

        Pause(delay, RollbackFrames);
        Assert.Empty(_control.Changes);

        Pause(delay, 1);
        Assert.Equal([1], _control.Changes);
    }

    [Fact]
    public void ARoundEndThatARollbackUndoesChangesNothing()
    {
        InputDelayController delay = Create(playerOverride: null);
        Readings(delay, LowPing, LowPing, LowPing, LowPing, LowPing);

        Pause(delay, RollbackFrames);
        Fight(delay, 1);
        Pause(delay, RollbackFrames);

        Assert.Empty(_control.Changes);
    }

    [Fact]
    public void OneSpikeDoesNotRaiseTheDelay()
    {
        InputDelayController delay = Create(playerOverride: null);
        Readings(delay, LowPing, LowPing, LowPing, LowPing, LowPing);
        Pause(delay, RollbackFrames + 1);

        Readings(delay, LowPing, LowPing, 900, LowPing, LowPing);
        Pause(delay, RollbackFrames + 1);

        Assert.Equal(1, delay.CurrentFrames);
        Assert.Equal([1], _control.Changes);
    }

    [Fact]
    public void OneTooLowReadingDoesNotLowerTheDelay()
    {
        InputDelayController delay = Create(playerOverride: null);

        Readings(delay, HighPing, HighPing, 4, HighPing, HighPing);
        Pause(delay, RollbackFrames + 1);

        Assert.Equal(2, delay.CurrentFrames);
        Assert.Empty(_control.Changes);
    }

    [Fact]
    public void ALastingRiseKeepsTheLoweredDelayForTheRestOfTheMatch()
    {
        // Backdash can't raise the delay of a running session; the next match starts at the highest delay again.
        InputDelayController delay = Create(playerOverride: null);
        Readings(delay, LowPing, LowPing, LowPing, LowPing, LowPing);
        Pause(delay, RollbackFrames + 1);

        Readings(delay, HighPing, HighPing, HighPing, HighPing, HighPing);
        Pause(delay, RollbackFrames + 1);

        Assert.Equal(1, delay.CurrentFrames);
        Assert.Equal([1], _control.Changes);
    }

    [Fact]
    public void TheDelayChangesOncePerBreak()
    {
        InputDelayController delay = Create(playerOverride: null);
        Readings(delay, HighPing, HighPing, HighPing, HighPing, HighPing);
        Pause(delay, RollbackFrames + 1);

        // The ping drops during the same break: it waits for the next one.
        for (int reading = 0; reading < Samples; reading++)
        {
            _connection.PingMilliseconds = LowPing;
            Pause(delay, SampleInterval);
        }

        Assert.Empty(_control.Changes);

        Fight(delay, 1);
        Pause(delay, RollbackFrames + 1);
        Assert.Equal([1], _control.Changes);
    }

    [Fact]
    public void WithoutAnyReadingTheDelayStays()
    {
        InputDelayController delay = Create(playerOverride: null);

        _connection.PingMilliseconds = null;
        Fight(delay, SampleInterval * Samples);
        _connection.PingMilliseconds = 0;
        Fight(delay, SampleInterval * Samples);
        Pause(delay, RollbackFrames + 1);

        Assert.Equal(2, delay.CurrentFrames);
        Assert.Empty(_control.Changes);
    }

    [Fact]
    public void ThePingIsReadOncePerSampleInterval()
    {
        InputDelayController delay = Create(playerOverride: null);
        _connection.PingMilliseconds = LowPing;

        Fight(delay, SampleInterval * 3);

        Assert.Equal(3, _connection.Reads);
    }

    [Theory]
    [InlineData(InputDelaySettings.MinOverrideFrames)]
    [InlineData(3)]
    [InlineData(InputDelaySettings.MaxOverrideFrames)]
    public void ThePlayersOverrideWinsOverThePing(int frames)
    {
        InputDelayController delay = Create(playerOverride: frames);

        Assert.Equal(frames, delay.CurrentFrames);
        Readings(delay, HighPing, HighPing, HighPing, HighPing, HighPing);
        Pause(delay, RollbackFrames + 1);
        Readings(delay, LowPing, LowPing, LowPing, LowPing, LowPing);
        Pause(delay, RollbackFrames + 1);

        Assert.Equal(frames, delay.CurrentFrames);
        Assert.Empty(_control.Changes);
    }

    [Theory]
    [InlineData(InputDelaySettings.MinOverrideFrames - 1)]
    [InlineData(InputDelaySettings.MaxOverrideFrames + 1)]
    public void AnOverrideOutsideThePlayersRangeIsRejected(int frames)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Settings(frames));
    }

    [Fact]
    public void TheStartingDelayIsTheOverrideOrTheHighestAutomaticDelay()
    {
        Assert.Equal(0, Settings(0).StartingFrames);
        Assert.Equal(2, Settings(null).StartingFrames);
    }

    private InputDelayController Create(int? playerOverride) =>
        new(Settings(playerOverride), _connection, _control, RollbackFrames);

    private static InputDelaySettings Settings(int? playerOverride) => new()
    {
        Automatic = new InputDelayTuning
        {
            MinimumFrames = 1,
            MaximumFrames = 2,
            PingStepMilliseconds = PingStep,
            PingSampleIntervalFrames = SampleInterval,
            PingSamples = Samples,
        },
        PlayerOverrideFrames = playerOverride,
    };

    // One reading per ping, taken during the fight.
    private void Readings(InputDelayController delay, params int[] pings)
    {
        foreach (int ping in pings)
        {
            _connection.PingMilliseconds = ping;
            Fight(delay, SampleInterval);
        }
    }

    private static void Fight(InputDelayController delay, int frames)
    {
        for (int frame = 0; frame < frames; frame++)
        {
            delay.AfterFrame(betweenRounds: false);
        }
    }

    private static void Pause(InputDelayController delay, int frames)
    {
        for (int frame = 0; frame < frames; frame++)
        {
            delay.AfterFrame(betweenRounds: true);
        }
    }
}
