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

    [Fact]
    public void RecordEntrySwitchesToRecordModeAndRecordsIntoTheChosenSlot()
    {
        var settings = new CurrentTrainingSettings(new TrainingSettings { Slot = 3 }, _ => { });
        var slots = new RecordingSlots();
        var entry = new RecordEntry(Key.Key5, settings, slots);
        Assert.Equal("Record slot 3", entry.Describe());

        entry.Press();
        slots.Add(default);

        Assert.Equal(DummyMode.Record, settings.Value.Dummy);
        Assert.Equal(2, slots.RecordingSlot);
        Assert.Equal("Stop recording slot 3 (0.0 s)", entry.Describe());

        entry.Press();

        Assert.Null(slots.RecordingSlot);
        Assert.Equal(1, slots.Length(2));
    }

    [Fact]
    public void SlotEntryCyclesThroughTheSlotsAndShowsWhatEachHolds()
    {
        var settings = new CurrentTrainingSettings(new TrainingSettings { Slot = RecordingSlots.Count }, _ => { });
        var slots = new RecordingSlots();
        slots.Start(0);
        for (int frame = 0; frame < 90; frame++)
        {
            slots.Add(default);
        }

        var entry = new RecordingSlotEntry(Key.Key4, settings, slots);
        Assert.Equal($"Slot: {RecordingSlots.Count} (empty)", entry.Describe());

        entry.Press();

        Assert.Equal(1, settings.Value.Slot);
        Assert.Equal("Slot: 1 (1.5 s)", entry.Describe());
    }

    [Fact]
    public void PlaybackEntryCyclesThroughTheStyles()
    {
        var settings = new CurrentTrainingSettings(new TrainingSettings(), _ => { });
        var entry = new PlaybackStyleEntry(Key.Key6, settings);
        Assert.Equal("Playback: Loop", entry.Describe());

        entry.Press();
        Assert.Equal(PlaybackStyle.Random, settings.Value.Playback);
        entry.Press();
        Assert.Equal(PlaybackStyle.Once, settings.Value.Playback);
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(5, 4)]
    [InlineData(0, 0)]
    [InlineData(99, 4)]
    public void APlaybackSlotOutOfRangeIsClamped(int slot, int expectedIndex) =>
        Assert.Equal(expectedIndex, new TrainingSettings { Slot = slot }.ToPlaybackChoice().Slot);
}
