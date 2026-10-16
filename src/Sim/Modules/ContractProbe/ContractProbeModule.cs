using Sim.Rules;

namespace Sim.Modules.ContractProbe;

/// <summary>
/// Test-only module proving the module contract end to end (P1-20): counts frames and hits in its own slice and asks
/// the core to override the defender's hitstop on hit. Registered but never enabled in shipped match rules.
/// </summary>
public sealed class ContractProbeModule : IMechanicModule, IAfterInputHook, IOnHitHook
{
    public ModuleId Id => new(1);

    public string Name => "contract_probe";

    public void AfterInput(ModuleContext context)
    {
        context.GetState<ContractProbeState>().FramesSeen++;
    }

    public void OnHit(ModuleContext context, in FighterContact contact)
    {
        ref ContractProbeState state = ref context.GetState<ContractProbeState>();
        state.HitsSeen++;
        state.LastDefender = contact.Defender;

        ref readonly ContractProbeConfig config = ref context.GetConfig<ContractProbeConfig>();
        context.RequestHitstopOverride(contact.Defender, new HitstopOverrideRequest { Frames = config.HitstopFrames });
    }
}
