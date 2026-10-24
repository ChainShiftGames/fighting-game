namespace FightingGame.Input.Profiles;

/// <summary>A joypad profile's analog options (player settings); keyboards ignore them.</summary>
/// <param name="LeftStick">Whether the left stick also gives directions, next to the bound d-pad buttons.</param>
/// <param name="StickDeadzone">Left stick travel (0–1) before it counts as a direction.</param>
/// <param name="TriggerThreshold">Trigger travel (0–1) before a trigger bound as a button counts as held.</param>
public readonly record struct StickOptions(bool LeftStick, float StickDeadzone, float TriggerThreshold)
{
    /// <summary>For keyboards, which have no sticks.</summary>
    public static StickOptions None { get; } = new(false, 0f, 0f);
}
