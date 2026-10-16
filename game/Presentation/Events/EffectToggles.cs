using System.Collections.Generic;
using FightingGame.Debug;
using Godot;

namespace FightingGame.Presentation.Events;

/// <summary>On/off switches for each kind of event effect: developer settings (ARCHITECTURE.md §6, §9), all on.</summary>
/// <remarks>No keys of their own; they are flipped in the debug panel. Turning one off doesn't stop what is playing.</remarks>
public sealed class EffectToggles
{
    public DeveloperToggle Sparks { get; } = new("Hit sparks", Key.None, enabled: true);

    public DeveloperToggle Shake { get; } = new("Screen shake", Key.None, enabled: true);

    public DeveloperToggle Sounds { get; } = new("Sound effects", Key.None, enabled: true);

    public DeveloperToggle Rumble { get; } = new("Controller rumble", Key.None, enabled: true);

    /// <summary>All of them, for the debug panel.</summary>
    public IReadOnlyList<DeveloperToggle> All => [Sparks, Shake, Sounds, Rumble];
}
