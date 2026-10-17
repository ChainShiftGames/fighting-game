using System;
using Godot;
using Netcode.Drivers.Training;

namespace FightingGame.Training;

/// <summary>The training menu's lines, in the order shown: the one place a training feature adds its entry.</summary>
public static class TrainingMenuEntries
{
    // Placeholder menu keys (developer settings until the menus, which will follow the player's bindings).
    private const Key DummyModeKey = Key.Key1;
    private const Key RestartKey = Key.Key2;
    private const Key FrameMeterKey = Key.Key3;
    private const Key SlotKey = Key.Key4;
    private const Key RecordKey = Key.Key5;
    private const Key PlaybackKey = Key.Key6;

    /// <param name="restart">Puts training back at the start: the match and everything shown about it.</param>
    /// <param name="recordings">The dummy's recording slots, kept while the game runs.</param>
    public static ITrainingMenuEntry[] Create(
        CurrentTrainingSettings settings, Action restart, RecordingSlots recordings) =>
    [
        new DummyModeEntry(DummyModeKey, settings),
        new RestartEntry(RestartKey, restart),
        new FrameMeterEntry(FrameMeterKey, settings),
        new RecordingSlotEntry(SlotKey, settings, recordings),
        new RecordEntry(RecordKey, settings, recordings),
        new PlaybackStyleEntry(PlaybackKey, settings),
    ];
}
