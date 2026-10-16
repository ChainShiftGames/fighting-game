namespace Sim.State;

/// <summary>The match's projectiles and module objects, in a fixed number of slots per player (P1-21).</summary>
/// <remarks>
/// Each player owns a block of <see cref="MaxPerPlayer"/> slots, P1's first; the match rules decide how many of them
/// are used (<c>MatchRules.EntitiesPerPlayer</c>), so one player can never take the other's slots. The rules for
/// spawning, moving, hitting and clashing are in <c>Sim.Entities</c>. Round start empties the pool.
/// </remarks>
public struct EntityPool
{
    /// <summary>Slots reserved per player: the state layout's limit for the match rules' entities per player.</summary>
    public const int MaxPerPlayer = 8;

    public const int Capacity = MaxPerPlayer * MatchState.PlayerCount;

    public EntitySlots Slots;

    /// <summary>The first slot of <paramref name="owner"/>'s block.</summary>
    public static int FirstSlotOf(int owner) => owner * MaxPerPlayer;
}
