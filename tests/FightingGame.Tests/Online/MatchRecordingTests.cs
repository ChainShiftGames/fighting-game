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
        var recording = new MatchRecording(_recorder, _driver, predictionFrames: 0, "local");
        RunFrames(30);

        Replay replay = Assert.IsType<Replay>(recording.TakeOnEnd());

        Assert.Equal(30, replay.Inputs.Count);
    }

    [Fact]
    public void LeavingAnOnlineMatchLeavesOutTheFramesARollbackCouldStillChange()
    {
        var recording = new MatchRecording(_recorder, _driver, predictionFrames: 8, "online-p1");
        RunFrames(30);

        Replay replay = Assert.IsType<Replay>(recording.TakeOnEnd());

        Assert.Equal(22, replay.Inputs.Count);
        Assert.Equal(InputsOf(21), replay.Inputs[21]);
    }

    [Fact]
    public void AMatchWithoutConfirmedFramesHasNoReplay()
    {
        var recording = new MatchRecording(_recorder, _driver, predictionFrames: 8, "online-p1");
        RunFrames(5);

        Assert.Null(recording.TakeOnEnd());
    }

    [Fact]
    public void HasNoFinishedReplayBeforeTheMatchIsOver()
    {
        var recording = new MatchRecording(_recorder, _driver, predictionFrames: 0, "local");
        RunFrames(30);

        Assert.Null(recording.TakeFinishedReplay());
    }

    [Fact]
    public void HandsTheFinishedMatchOverOnceItIsOver()
    {
        var recording = new MatchRecording(_recorder, _driver, predictionFrames: 0, "local");
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
        var recording = new MatchRecording(_recorder, _driver, predictionFrames: 8, "online-p1");
        RunFrames(30);
        _driver.Phase = RoundPhase.MatchOver;

        Assert.Null(recording.TakeFinishedReplay());
        RunFrames(7);
        Assert.Null(recording.TakeFinishedReplay());
        RunFrames(1);

        Replay replay = Assert.IsType<Replay>(recording.TakeFinishedReplay());
        Assert.Equal(30, replay.Inputs.Count);
    }

    [Fact]
    public void ARollbackThatTakesTheResultBackStartsTheWaitAgain()
    {
        var recording = new MatchRecording(_recorder, _driver, predictionFrames: 8, "online-p1");
        RunFrames(30);
        _driver.Phase = RoundPhase.MatchOver;
        Assert.Null(recording.TakeFinishedReplay());

        _driver.Phase = RoundPhase.Fighting;
        RunFrames(10);
        Assert.Null(recording.TakeFinishedReplay());
        _driver.Phase = RoundPhase.MatchOver;
        RunFrames(5);

        Assert.Null(recording.TakeFinishedReplay());
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

    /// <summary>A driver whose frame and round phase the test sets.</summary>
    private sealed class ScriptedDriver : IMatchDriver<MatchState>
    {
        private MatchState _state;

        public int LocalPlayerCount => 0;

        public int CurrentFrame { get; set; }

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
