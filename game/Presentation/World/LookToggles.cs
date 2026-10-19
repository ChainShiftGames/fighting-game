using System.Collections.Generic;
using FightingGame.Debug;
using Godot;

namespace FightingGame.Presentation.World;

/// <summary>On/off switches for whole-screen looks: developer settings (ARCHITECTURE.md §6, §9), both on.</summary>
/// <remarks>Debug panel only: every F key is taken.</remarks>
public sealed class LookToggles
{
    /// <summary>Linear tonemap and the anime colour grade, so painted colours show as painted (<see cref="AnimeGrade"/>).</summary>
    public DeveloperToggle Grade { get; } = new("Anime colour grade", Key.None, enabled: true);

    /// <summary>Softens the stage behind the fighters, like a painted backdrop (<see cref="BackdropBlur"/>).</summary>
    public DeveloperToggle BackdropBlur { get; } = new("Backdrop blur", Key.None, enabled: true);

    /// <summary>All of them, for the debug panel.</summary>
    public IReadOnlyList<DeveloperToggle> All => [Grade, BackdropBlur];
}
