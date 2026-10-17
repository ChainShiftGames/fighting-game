using Godot;
using Netcode.Drivers;
using Sim.State;

namespace FightingGame.Training;

/// <summary>The training menu's lines, in the order shown: the one place a training feature adds its entry.</summary>
public static class TrainingMenuEntries
{
    // Placeholder menu keys (developer settings until the menus, which will follow the player's bindings).
    private const Key DummyModeKey = Key.Key1;
    private const Key RestartKey = Key.Key2;

    public static ITrainingMenuEntry[] Create(CurrentTrainingSettings settings, ITrainingMatchDriver<MatchState> driver) =>
    [
        new DummyModeEntry(DummyModeKey, settings),
        new RestartEntry(RestartKey, driver.Restart),
    ];
}
