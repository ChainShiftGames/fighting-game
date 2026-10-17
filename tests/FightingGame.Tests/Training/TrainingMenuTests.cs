using System;
using System.Collections.Generic;
using FightingGame.Training;
using Godot;
using Netcode.Drivers.Training;
using Xunit;

namespace FightingGame.Tests.Training;

public sealed class TrainingMenuTests
{
    [Fact]
    public void DummyEntryCyclesThroughEveryModeAndSavesEachChange()
    {
        var saved = new List<TrainingSettings>();
        var settings = new CurrentTrainingSettings(new TrainingSettings(), saved.Add);
        var entry = new DummyModeEntry(Key.Key1, settings);
        DummyMode[] modes = Enum.GetValues<DummyMode>();

        for (int press = 1; press <= modes.Length; press++)
        {
            entry.Press();
            Assert.Equal(modes[press % modes.Length], settings.Value.Dummy);
        }

        Assert.Equal(modes.Length, saved.Count);
    }

    [Fact]
    public void DescribesEveryEntryWithItsKey()
    {
        var settings = new CurrentTrainingSettings(new TrainingSettings { Dummy = DummyMode.BlockAll }, _ => { });
        ITrainingMenuEntry[] entries = [new DummyModeEntry(Key.Key1, settings)];

        string text = TrainingMenuText.Describe(entries, Key.Tab);

        Assert.Contains("1   Dummy: Block all", text, StringComparison.Ordinal);
        Assert.Contains("Tab or Esc   close", text, StringComparison.Ordinal);
    }

    [Fact]
    public void SummaryShowsTheFirstEntryAndTheMenuKey()
    {
        var settings = new CurrentTrainingSettings(new TrainingSettings { Dummy = DummyMode.Crouch }, _ => { });
        ITrainingMenuEntry[] entries = [new DummyModeEntry(Key.Key1, settings)];

        Assert.Equal("Training — Dummy: Crouch — Tab menu", TrainingMenuText.Summary(entries, Key.Tab));
    }

    [Fact]
    public void FrameMeterEntryShowsAndHidesTheMeter()
    {
        var settings = new CurrentTrainingSettings(new TrainingSettings(), _ => { });
        var entry = new FrameMeterEntry(Key.Key3, settings);
        Assert.Equal("Frame meter: On", entry.Describe());

        entry.Press();

        Assert.False(settings.Value.ShowFrameMeter);
        Assert.Equal("Frame meter: Off", entry.Describe());
    }
}
