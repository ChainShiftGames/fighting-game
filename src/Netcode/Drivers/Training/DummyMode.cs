namespace Netcode.Drivers.Training;

/// <summary>What the training dummy does: a player setting chosen in the training menu (ARCHITECTURE.md §6).</summary>
/// <remarks>Saved by name, so the values may be reordered; new modes (P3-32 playback) are added at the end.</remarks>
public enum DummyMode
{
    /// <summary>Stands still and never blocks.</summary>
    Stand,

    /// <summary>Holds down and never blocks.</summary>
    Crouch,

    /// <summary>Blocks every blockable attack: low or high as the attack needs.</summary>
    BlockAll,

    /// <summary>Stands until it is hit, then blocks until the player's pressure is over.</summary>
    BlockAfterFirstHit,

    /// <summary>Holds up: jumps again on every landing.</summary>
    Jump,

    /// <summary>The player controls the dummy while their own fighter stands still; P3-32 records these inputs.</summary>
    Record,
}
