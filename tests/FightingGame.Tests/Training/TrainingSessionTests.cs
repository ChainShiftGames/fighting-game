using System;
using FightingGame.Online;
using FightingGame.Presentation.Events;
using FightingGame.Training;
using Godot;
using Netcode.Drivers;
using Netcode.Drivers.Training;
using Sim.Diagnostics;
using Sim.Input;
using Sim.State;
using Xunit;

namespace FightingGame.Tests.Training;

/// <summary>Savestates and restarts on a real training match of Testman (P3-33).</summary>
public sealed class TrainingSessionTests
{
    private static readonly InputWord _forward = new(InputWord.Right);
    private static readonly InputWord _back = new(InputWord.Left);

    private readonly FrameMeter _meter = new();
    private readonly CountingEffect _effect = new();
    private readonly FrameEventReconciler _reconciler;
    private readonly TrainingMatchDriver<MatchState> _driver;
    private readonly TrainingSession _session;

    public TrainingSessionTests()
    {
        MatchLineup lineup = MatchLineup.From(TestGameData.Data);
        var timings = new TrainingTimings(GuardResetFrames: 20, HealthRefillDelayFrames: 60);
        _reconciler = new FrameEventReconciler(_effect, MatchEffects.RememberedFrames);
        _driver = new TrainingMatchDriver<MatchState>(
            new EventObservingSimulation(new ComposedMatchSimulation(lineup.Simulation), _reconciler),
            DummyModeInputs.Create(
                lineup.Characters[TrainingSides.Player],
                timings,
                () => DummyMode.Stand,
                new RecordingSlots(),
                () => default),
            TrainingAdjustments.Create(lineup.Simulation, timings));
        _session = new TrainingSession(_driver, _meter, new ReconcilerReset(_reconciler));
    }

    [Fact]
    public void LoadingGoesBackToTheSavedStateAndClearsTheFrameMeter()
    {
        Run(_forward, 20);
        _session.SaveState();
        MatchState saved = _driver.State;
        Run(_back, 40);
        _meter.Record(new FighterFrameState(FrameKind.Startup, false), default);

        _session.LoadState();

        Assert.Equal(StateChecksum.Compute(in saved), StateChecksum.Compute(in _driver.State));
        Assert.Equal(0, _meter.Count);
    }

    [Fact]
    public void ASavestateCanBeLoadedAgainAndAgain()
    {
        Run(_forward, 10);
        _session.SaveState();
        int savedX = _driver.State.Fighters[TrainingSides.Player].Position.X;

        for (int load = 0; load < 3; load++)
        {
            Run(_forward, 15);
            _session.LoadState();
            Assert.Equal(savedX, _driver.State.Fighters[TrainingSides.Player].Position.X);
        }
    }

    [Fact]
    public void LoadingBeforeSavingChangesNothing()
    {
        Run(_forward, 10);
        MatchState now = _driver.State;

        _session.LoadState();

        Assert.False(_session.HasSavestate);
        Assert.Equal(StateChecksum.Compute(in now), StateChecksum.Compute(in _driver.State));
    }

    [Fact]
    public void RestartingKeepsTheSavestate()
    {
        Run(_forward, 10);
        _session.SaveState();
        int savedX = _driver.State.Fighters[TrainingSides.Player].Position.X;

        _session.Restart();
        Assert.NotEqual(savedX, _driver.State.Fighters[TrainingSides.Player].Position.X);
        _session.LoadState();

        Assert.Equal(savedX, _driver.State.Fighters[TrainingSides.Player].Position.X);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EffectsKeepWorkingAfterJumpingBackFurtherThanTheirWindow(bool loadState)
    {
        _session.SaveState();
        Run(_forward, 3 * MatchEffects.RememberedFrames);

        if (loadState)
        {
            _session.LoadState();
        }
        else
        {
            _session.Restart();
        }

        // Frames numbered lower than the remembered ones run again; without the reset this throws.
        Exception? thrown = Record.Exception(() => Run(_forward, 3 * MatchEffects.RememberedFrames));
        Assert.Null(thrown);
    }

    [Fact]
    public void TheMenuLinesSayWhetherAStateIsSaved()
    {
        var save = new SaveStateEntry(Key.Key7, _session);
        var load = new LoadStateEntry(Key.Key8, _session);
        Assert.Equal("Load state (nothing saved)", load.Describe());

        save.Press();

        Assert.Equal("Load state", load.Describe());
        Assert.Equal("Save state (replaces the saved one)", save.Describe());
    }

    private sealed class ReconcilerReset(FrameEventReconciler reconciler) : IEffectReset
    {
        public void ResetEffects() => reconciler.Reset();
    }

    private sealed class CountingEffect : IFrameEventEffect
    {
        public void Play(in Sim.Events.FrameEvent frameEvent)
        {
        }

        public void Cancel(in Sim.Events.FrameEvent frameEvent)
        {
        }
    }

    private void Run(InputWord input, int frames)
    {
        for (int frame = 0; frame < frames; frame++)
        {
            _driver.TryAdvanceFrame([input]);
        }
    }
}
