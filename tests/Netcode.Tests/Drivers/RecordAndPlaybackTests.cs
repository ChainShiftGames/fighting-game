using System;
using Netcode.Drivers;
using Netcode.Drivers.Training;
using Netcode.Tests.Matches;
using Sim.Composition;
using Sim.Input;
using Sim.State;
using Xunit;

namespace Netcode.Tests.Drivers;

/// <summary>Recording the dummy into slots and playing it back (P3-32).</summary>
public sealed class RecordAndPlaybackTests
{
    private const ushort ButtonA = 1 << InputWord.FirstButtonBit;

    private static readonly TrainingTimings _timings = new(GuardResetFrames: 20, HealthRefillDelayFrames: 60);
    private static readonly InputWord _left = new(InputWord.Left);
    private static readonly InputWord _right = new(InputWord.Right);

    private readonly MatchSimulation _match = TestmanMirror.Create();

    [Theory]
    [InlineData(InputWord.Left, 1, InputWord.Left)]
    [InlineData(InputWord.Left, -1, InputWord.Right)]
    [InlineData(InputWord.Right | InputWord.Down, -1, InputWord.Left | InputWord.Down)]
    [InlineData(InputWord.Left | InputWord.Right, -1, InputWord.Left | InputWord.Right)]
    [InlineData(InputWord.Up | ButtonA, -1, InputWord.Up | ButtonA)]
    public void TurningSwapsOnlyLeftAndRightWhenFacingLeft(ushort input, int facing, ushort expected)
    {
        Assert.Equal(new InputWord(expected), FacingInputs.Turn(new InputWord(input), facing));
        Assert.Equal(new InputWord(input), FacingInputs.Turn(FacingInputs.Turn(new InputWord(input), facing), facing));
    }

    [Fact]
    public void ASlotRecordsUntilStoppedOrFull()
    {
        var slots = new RecordingSlots();
        slots.Add(_left);
        Assert.Equal(0, slots.Length(0));

        slots.Start(2);
        slots.Add(_left);
        slots.Add(_right);
        slots.Stop();
        slots.Add(_left);

        Assert.Equal(2, slots.Length(2));
        Assert.Equal(_right, slots.At(2, 1));
        Assert.Null(slots.RecordingSlot);

        slots.Start(2);
        for (int frame = 0; frame < RecordingSlots.MaxFrames + 5; frame++)
        {
            slots.Add(_right);
        }

        Assert.Equal(RecordingSlots.MaxFrames, slots.Length(2));
        Assert.Null(slots.RecordingSlot);
        Assert.Throws<ArgumentOutOfRangeException>(() => slots.Start(RecordingSlots.Count));
    }

    [Fact]
    public void RecordModeGivesThePlayersInputToTheDummyAndRecordsItRelativeToItsFacing()
    {
        var slots = new RecordingSlots();
        slots.Start(0);
        MatchState state = _match.StartState();

        FrameInputs inputs = new RecordingDummy(slots).Next(in state, _left);

        // The dummy starts on the right, facing left: left is forward for it.
        Assert.Equal(new FrameInputs(InputWord.Neutral, _left), inputs);
        Assert.Equal(-1, state.Fighters[TrainingSides.Dummy].Facing);
        Assert.Equal(_right, slots.At(0, 0));
    }

    [Fact]
    public void OncePlaysTheSlotThenStandsUntilReset()
    {
        RecordingSlots slots = Recorded((0, [_right, _left]));
        var playback = new PlaybackDummy(slots, () => new PlaybackChoice(0, PlaybackStyle.Once), new Random(1));
        MatchState facingRight = _match.StartState();
        facingRight.Fighters[TrainingSides.Dummy].Facing = 1;

        Assert.Equal([_right, _left, InputWord.Neutral, InputWord.Neutral], Play(playback, in facingRight, 4));
        playback.Reset();
        Assert.Equal([_right, _left], Play(playback, in facingRight, 2));
    }

    [Fact]
    public void LoopRepeatsTheSlotTurnedToTheDummysFacing()
    {
        RecordingSlots slots = Recorded((1, [_right, _left]));
        var playback = new PlaybackDummy(slots, () => new PlaybackChoice(1, PlaybackStyle.Loop), new Random(1));
        MatchState facingLeft = _match.StartState();

        Assert.Equal([_left, _right, _left, _right], Play(playback, in facingLeft, 4));
    }

    [Fact]
    public void RandomPlaysOnlyRecordedSlots()
    {
        RecordingSlots slots = Recorded((1, [_right]), (3, [_left]));
        var playback = new PlaybackDummy(slots, () => new PlaybackChoice(0, PlaybackStyle.Random), new Random(7));
        MatchState facingRight = _match.StartState();
        facingRight.Fighters[TrainingSides.Dummy].Facing = 1;

        InputWord[] played = Play(playback, in facingRight, 50);

        Assert.Contains(_right, played);
        Assert.Contains(_left, played);
        Assert.DoesNotContain(InputWord.Neutral, played);
    }

    [Fact]
    public void AnEmptySlotLeavesTheDummyStanding()
    {
        var playback = new PlaybackDummy(
            new RecordingSlots(), () => new PlaybackChoice(0, PlaybackStyle.Random), new Random(1));
        MatchState state = _match.StartState();

        Assert.Equal([InputWord.Neutral, InputWord.Neutral], Play(playback, in state, 2));
    }

    [Fact]
    public void LeavingRecordModeStopsTheRecording()
    {
        var mode = DummyMode.Record;
        var slots = new RecordingSlots();
        var inputs = DummyModeInputs.Create(_match.Player1, _timings, () => mode, slots, () => default);
        MatchState state = _match.StartState();
        slots.Start(0);
        inputs.Next(in state, _left);

        mode = DummyMode.Stand;
        inputs.Next(in state, _left);

        Assert.Null(slots.RecordingSlot);
        Assert.Equal(1, slots.Length(0));
    }

    [Fact]
    public void ARecordedWalkPlaysBackTheSameAfterARestart()
    {
        var mode = DummyMode.Record;
        var slots = new RecordingSlots();
        using var driver = new TrainingMatchDriver<MatchState>(
            new ComposedMatchSimulation(_match),
            DummyModeInputs.Create(
                _match.Player1, _timings, () => mode, slots, () => new PlaybackChoice(0, PlaybackStyle.Once)),
            TrainingAdjustments.Create(_match, _timings));
        int dummyStart = driver.State.Fighters[TrainingSides.Dummy].Position.X;

        slots.Start(0);
        Run(driver, _left, 30);
        slots.Stop();
        int recordedX = driver.State.Fighters[TrainingSides.Dummy].Position.X;
        driver.Restart();
        mode = DummyMode.Playback;
        Run(driver, InputWord.Neutral, 30);

        Assert.True(recordedX < dummyStart, "The dummy walked forward while recorded.");
        Assert.Equal(recordedX, driver.State.Fighters[TrainingSides.Dummy].Position.X);
    }

    private static RecordingSlots Recorded(params (int Slot, InputWord[] Inputs)[] recordings)
    {
        var slots = new RecordingSlots();
        foreach ((int slot, InputWord[] inputs) in recordings)
        {
            slots.Start(slot);
            foreach (InputWord input in inputs)
            {
                slots.Add(input);
            }

            slots.Stop();
        }

        return slots;
    }

    private static InputWord[] Play(PlaybackDummy playback, in MatchState state, int frames)
    {
        var played = new InputWord[frames];
        for (int frame = 0; frame < frames; frame++)
        {
            played[frame] = playback.Next(in state);
        }

        return played;
    }

    private static void Run(TrainingMatchDriver<MatchState> driver, InputWord input, int frames)
    {
        for (int frame = 0; frame < frames; frame++)
        {
            driver.TryAdvanceFrame([input]);
        }
    }
}
