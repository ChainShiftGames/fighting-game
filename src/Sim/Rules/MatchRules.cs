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

    /// <summary>Hash over the whole struct, so no gameplay setting can be left out of the online check.</summary>
    public readonly ulong ComputeHash() => StateChecksum.Compute(in this);
}
