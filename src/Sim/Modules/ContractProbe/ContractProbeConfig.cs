namespace Sim.Modules.ContractProbe;

/// <summary>The contract probe's settings in its <c>MatchRules</c> slot (developer settings).</summary>
public struct ContractProbeConfig : IModuleConfig
{
    /// <summary>Hitstop the defender is set to when a hit lands.</summary>
    public int HitstopFrames;

    /// <summary>The frame it requests a spawn on (<see cref="ContractProbeState.FramesSeen"/>); 0: never.</summary>
    public int SpawnOnFrame;

    /// <summary>Who owns the spawned entity (0 = P1, 1 = P2).</summary>
    public int SpawnOwner;

    /// <summary>Which of the owner's character's projectiles it spawns (index in its projectile list).</summary>
    public int SpawnProjectile;
}
