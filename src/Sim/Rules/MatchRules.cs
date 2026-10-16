using Sim.State;

namespace Sim.Rules;

/// <summary>Every gameplay-affecting setting of a match, as one blittable struct (developer settings, ADR-0011).</summary>
/// <remarks>
/// The logic reads gameplay settings only from here. Players never change these: they are fixed in shipped builds and
/// compared by <see cref="ComputeHash"/> before an online match (ARCHITECTURE.md §6). Fields are 4-byte integers or
/// structs of them (booleans as 0/1, enums backed by int), so the struct has no padding and equal rules hash equally;
/// a test checks that and changes every field in turn. Later tasks add their settings as fields here.
/// </remarks>
public struct MatchRules
{
    /// <summary>The enabled mechanics modules in hook order, with each one's config.</summary>
    public ModuleSlots Modules;

    /// <summary>Frames a motion (236, 623…) may take, first direction to button press (ADR-0002; 12 by default).</summary>
    /// <remarks>Developer setting. A motion in data may set its own window instead.</remarks>
    public int MotionWindow;

    /// <summary>Frames a button press stays valid after it happened (ADR-0002; 2 by default).</summary>
    /// <remarks>Developer setting. A press 2 frames ago still counts with a buffer of 2; 3 frames ago doesn't.</remarks>
    public int ButtonBuffer;

    /// <summary>Each fighter's distance from the stage centre when a round starts, in subpixels.</summary>
    /// <remarks>Developer setting, from data/rules/match.json.</remarks>
    public int RoundStartDistance;

    /// <summary>Each stage wall's distance from the stage centre, in subpixels; fighters' pushboxes stay inside.</summary>
    /// <remarks>Developer setting, from data/rules/match.json.</remarks>
    public int WallDistance;

    /// <summary>Hash over the whole struct, so no gameplay setting can be left out of the online check.</summary>
    public readonly ulong ComputeHash() => StateChecksum.Compute(in this);
}
