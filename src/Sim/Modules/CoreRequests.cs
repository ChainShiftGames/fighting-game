using System;
using Sim.State;

namespace Sim.Modules;

/// <summary>The core requests modules made this frame; the only way a module changes core state (ADR-0011).</summary>
/// <remarks>
/// Modules write through <see cref="ModuleContext"/>; the tick pipeline validates and applies the tables in a fixed
/// order, then clears them. Each module slot gets one entry per fighter per kind, so the size is fixed.
/// </remarks>
public sealed class CoreRequests
{
    private const int FighterKeyCount = ModuleStates.MaxModules * MatchState.PlayerCount;

    public RequestTable<CancelRequest> Cancels { get; } = new(FighterKeyCount);
    public RequestTable<TimeScaleRequest> TimeScales { get; } = new(FighterKeyCount);
    public RequestTable<HitstopOverrideRequest> HitstopOverrides { get; } = new(FighterKeyCount);

    /// <summary>Keyed by module slot only: a freeze affects the whole match.</summary>
    public RequestTable<FreezeRequest> Freezes { get; } = new(ModuleStates.MaxModules);

    /// <summary>Key of a per-fighter request: slot-major, so ascending keys follow the module order.</summary>
    public static int FighterKey(int slot, int fighter)
    {
        // Checked because an out-of-range fighter would silently land in the next slot's entry.
        ArgumentOutOfRangeException.ThrowIfNegative(fighter);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(fighter, MatchState.PlayerCount);
        return (slot * MatchState.PlayerCount) + fighter;
    }

    public void Clear()
    {
        Cancels.Clear();
        TimeScales.Clear();
        HitstopOverrides.Clear();
        Freezes.Clear();
    }
}
