namespace Sim.Combat;

/// <summary>What an attack that touched the opponent does this frame.</summary>
public enum HitOutcome
{
    /// <summary>It didn't touch, or a clash cancelled it.</summary>
    None,
    Hit,
    Block,
}
