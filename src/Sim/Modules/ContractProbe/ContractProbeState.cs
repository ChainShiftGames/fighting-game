namespace Sim.Modules.ContractProbe;

/// <summary>The contract probe's slice of the match state.</summary>
public struct ContractProbeState : IModuleState
{
    public int FramesSeen;
    public int HitsSeen;
    public int LastDefender;
}
