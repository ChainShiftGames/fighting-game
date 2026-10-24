using System.Collections.Generic;

namespace FightingGame.Input.Profiles;

/// <summary>What the controls file holds (player settings, <c>user://controls.json</c>).</summary>
/// <param name="Keyboard">The one keyboard's layout; null for the default.</param>
/// <param name="JoypadDefault">What a joypad without its own layout uses; null for the default.</param>
/// <param name="Joypads">Each joypad that has its own layout.</param>
/// <param name="Presets">Layouts kept for the player to load, e.g. player 2's from the old dev bindings file.</param>
public sealed record ControlsFile(
    BindingProfile? Keyboard,
    BindingProfile? JoypadDefault,
    IReadOnlyList<StoredJoypadProfile> Joypads,
    IReadOnlyList<ControlPreset> Presets)
{
    /// <summary>Nothing saved: every device uses its kind's default.</summary>
    public static ControlsFile Empty { get; } = new(null, null, [], []);
}
