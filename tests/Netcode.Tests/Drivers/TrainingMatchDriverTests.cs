using System;
using Netcode.Drivers;
using Netcode.Drivers.Training;
using Netcode.Tests.Matches;
using Sim.Composition;
using Sim.Fighters.States;
using Sim.Input;
using Sim.State;
using Xunit;

namespace Netcode.Tests.Drivers;

public sealed class TrainingMatchDriverTests
{
    private static readonly TrainingTimings _timings = new(GuardResetFrames: 20, HealthRefillDelayFrames: 60);

    private readonly MatchSimulation _match = TestmanMirror.Create();

    [Fact]
    public void TakesOnlyTheTrainingPlayersInput()
    {
        using TrainingMatchDriver<MatchState> driver = Create(DummyMode.Stand);

        Assert.Equal(1, driver.LocalPlayerCount);
        Assert.True(driver.TryAdvanceFrame([InputWord.Neutral]));
        Assert.Throws<ArgumentException>(() => driver.TryAdvanceFrame([InputWord.Neutral, InputWord.Neutral]));
    }

    [Fact]
    public void CrouchingDummyHoldsDown()
    {
        using TrainingMatchDriver<MatchState> driver = Create(DummyMode.Crouch);
        Run(driver, InputWord.Neutral, 10);

        Assert.Equal(FighterAction.Crouch, driver.State.Fighters[TrainingSides.Dummy].Action);
    }

    [Fact]
    public void RecordModeLetsThePlayerMoveTheDummy()
    {
        using TrainingMatchDriver<MatchState> driver = Create(DummyMode.Record);
        int dummyStart = driver.State.Fighters[TrainingSides.Dummy].Position.X;
        int playerStart = driver.State.Fighters[TrainingSides.Player].Position.X;
        Run(driver, new InputWord(InputWord.Right), 30);

        Assert.True(driver.State.Fighters[TrainingSides.Dummy].Position.X > dummyStart);
        Assert.Equal(playerStart, driver.State.Fighters[TrainingSides.Player].Position.X);
    }

    [Fact]
    public void TimerStaysFullAndRestartGoesBackToTheStart()
    {
        using TrainingMatchDriver<MatchState> driver = Create(DummyMode.Stand);
        Run(driver, new InputWord(InputWord.Right), 120);

        Assert.Equal(_match.Rules.RoundTimerFrames, driver.State.Round.TimerFramesLeft);
        driver.Restart();
        MatchState start = _match.StartState();
        Assert.Equal(StateChecksum.Compute(in start), StateChecksum.Compute(in driver.State));
        Assert.Equal(120, driver.CurrentFrame);
    }

    [Fact]
    public void RestoreContinuesFromTheSavedState()
    {
        using TrainingMatchDriver<MatchState> driver = Create(DummyMode.Stand);
        Run(driver, new InputWord(InputWord.Right), 30);
        MatchState saved = driver.State;
        Run(driver, new InputWord(InputWord.Left), 30);

        driver.Restore(in saved);

        Assert.Equal(StateChecksum.Compute(in saved), StateChecksum.Compute(in driver.State));
    }

    private static void Run(TrainingMatchDriver<MatchState> driver, InputWord input, int frames)
    {
        for (int frame = 0; frame < frames; frame++)
        {
            driver.TryAdvanceFrame([input]);
        }
    }

    private TrainingMatchDriver<MatchState> Create(DummyMode mode) => new(
        new ComposedMatchSimulation(_match),
        DummyModeInputs.Create(_match.Player1, _timings, () => mode),
        TrainingAdjustments.Create(_match, _timings));
}
