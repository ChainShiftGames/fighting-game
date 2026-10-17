using System;
using FightingGame.Online;
using Netcode.Drivers;
using Netcode.Replays;
using Sim.Input;
using Sim.State;
using Xunit;

namespace FightingGame.Tests.Online;

public sealed class MatchRecordingTests
{
    private readonly ReplayRecorder _recorder = new(MatchLineup.From(TestGameData.Data).CreateReplayHeader(TestGameData.Data));
    private readonly ScriptedDriver _driver = new();

    [Fact]
    public void LeavingALocalMatchHandsOverEveryFrame()
    {
        var recording = new MatchRecording(_recorder, _driver, confirmed: null, "local");
        RunFrames(30);

        Replay replay = Assert.IsType<Replay>(recording.TakeOnEnd());

        Assert.Equal(30, replay.Inputs.Count);
    }

    [Fact]
    public void LeavingAnOnlineMatchHandsOverExactlyTheConfirmedFrames()
    {
        var recording = new MatchRecording(_recorder, _driver, _driver, "online-p1");
        RunFrames(30);
        _driver.ConfirmedFrame = 25;

        Replay replay = Assert.IsType<Replay>(recording.TakeOnEnd());

        Assert.Equal(25, replay.Inputs.Count);
        Assert.Equal(InputsOf(24), replay.Inputs[24]);
    }

    [Fact]
    public void AMatchWithoutConfirmedFramesHasNoReplay()
    {
        var recording = new MatchRecording(_recorder, _driver, _driver, "online-p1");
        RunFrames(5);

        Assert.Null(recording.TakeOnEnd());
    }

    [Fact]
    public void HasNoFinishedReplayBeforeTheMatchIsOver()
    {
        var recording = new MatchRecording(_recorder, _driver, confirmed: null, "local");
        RunFrames(30);

        Assert.Null(recording.TakeFinishedReplay());
    }

    [Fact]
    public void HandsTheFinishedMatchOverOnceItIsOver()
    {
        var recording = new MatchRecording(_recorder, _driver, confirmed: null, "local");
        RunFrames(30);
        _driver.Phase = RoundPhase.MatchOver;

        Replay replay = Assert.IsType<Replay>(recording.TakeFinishedReplay());

        Assert.Equal(30, replay.Inputs.Count);
        Assert.Null(recording.TakeFinishedReplay());
        Assert.Null(recording.TakeOnEnd());
    }

    [Fact]
    public void OnlineWaitsUntilTheResultIsConfirmed()
    {
        var recording = new MatchRecording(_recorder, _driver, _driver, "online-p1");
        RunFrames(30);
        _driver.Phase = RoundPhase.MatchOver;
        _driver.ConfirmedFrame = 22;

        Assert.Null(recording.TakeFinishedReplay());
        RunFrames(7);
        _driver.ConfirmedFrame = 29;
        Assert.Null(recording.TakeFinishedReplay());
        _driver.ConfirmedFrame = 30;

        Replay replay = Assert.IsType<Replay>(recording.TakeFinishedReplay());
        Assert.Equal(30, replay.Inputs.Count);
    }

    [Fact]
    public void ARollbackThatTakesTheResultBackStartsTheWaitAgain()
    {
        var recording = new MatchRecording(_recorder, _driver, _driver, "online-p1");
        RunFrames(30);
        _driver.Phase = RoundPhase.MatchOver;
        Assert.Null(recording.TakeFinishedReplay());

        _driver.Phase = RoundPhase.Fighting;
        RunFrames(10);
        _driver.ConfirmedFrame = 32;
        Assert.Null(recording.TakeFinishedReplay());
        _driver.Phase = RoundPhase.MatchOver;
        RunFrames(5);

        Assert.Null(recording.TakeFinishedReplay());
    }

    [Fact]
    public void NeverHandsOverMoreFramesThanWereRecorded()
    {
        var recording = new MatchRecording(_recorder, _driver, _driver, "online-p1");
        RunFrames(10);
        _driver.ConfirmedFrame = 12;

        Assert.Equal(10, recording.ConfirmedFrameCount);
    }

    private static FrameInputs InputsOf(int frame) =>
        new(new InputWord((ushort)(frame % 16)), new InputWord((ushort)(frame % 7)));

    private void RunFrames(int count)
    {
        for (int i = 0; i < count; i++)
        {
            FrameInputs inputs = InputsOf(_driver.CurrentFrame);
            _recorder.Record(_driver.CurrentFrame, inputs.Player1, inputs.Player2);
            _driver.CurrentFrame++;
        }
    }

    /// <summary>A driver whose frame, final frames and round phase the test sets.</summary>
    private sealed class ScriptedDriver : IMatchDriver<MatchState>, IConfirmedFrameReadout
    {
        private MatchState _state;

        public int LocalPlayerCount => 0;

        public int CurrentFrame { get; set; }

        public int ConfirmedFrame { get; set; }

        public RoundPhase Phase
        {
            set => _state.Round.Phase = value;
        }

        public ref readonly MatchState State => ref _state;

        public bool TryAdvanceFrame(ReadOnlySpan<InputWord> localInputs) => false;

        public void Dispose()
        {
        }
    }
}
