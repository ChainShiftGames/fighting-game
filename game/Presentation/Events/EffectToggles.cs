using System.Collections.Generic;
using FightingGame.Debug;
using Godot;

namespace FightingGame.Presentation.Events;

/// <summary>On/off switches for each kind of event effect: developer settings (ARCHITECTURE.md §6, §9), all on.</summary>
/// <remarks>
/// Flipped with F7 to F10 or in the debug panel (F1 to F6 are taken by the host). Turning one off doesn't stop what
/// is playing.
/// </remarks>
public sealed class EffectToggles
{
    public DeveloperToggle Sparks { get; } = new("Hit sparks", Key.F7, enabled: true);

    public DeveloperToggle Shake { get; } = new("Screen shake", Key.F8, enabled: true);

    public DeveloperToggle Sounds { get; } = new("Sound effects", Key.F9, enabled: true);

    public DeveloperToggle Rumble { get; } = new("Controller rumble", Key.F10, enabled: true);

    /// <summary>All of them, for the debug panel.</summary>
    public IReadOnlyList<DeveloperToggle> All => [Sparks, Shake, Sounds, Rumble];
}
