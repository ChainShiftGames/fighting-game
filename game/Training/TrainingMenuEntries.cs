using System;
using Godot;

namespace FightingGame.Training;

/// <summary>The training menu's lines, in the order shown: the one place a training feature adds its entry.</summary>
public static class TrainingMenuEntries
{
    // Placeholder menu keys (developer settings until the menus, which will follow the player's bindings).
    private const Key DummyModeKey = Key.Key1;
    private const Key RestartKey = Key.Key2;
    private const Key FrameMeterKey = Key.Key3;

    /// <param name="restart">Puts training back at the start: the match and everything shown about it.</param>
    public static ITrainingMenuEntry[] Create(CurrentTrainingSettings settings, Action restart) =>
    [
        new DummyModeEntry(DummyModeKey, settings),
        new RestartEntry(RestartKey, restart),
        new FrameMeterEntry(FrameMeterKey, settings),
    ];
}
