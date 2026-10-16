namespace Sim.Modules.ContractProbe;

/// <summary>The contract probe's settings in its <c>MatchRules</c> slot (developer settings).</summary>
public struct ContractProbeConfig : IModuleConfig
{
    /// <summary>Hitstop the defender is set to when a hit lands.</summary>
    public int HitstopFrames;
}
