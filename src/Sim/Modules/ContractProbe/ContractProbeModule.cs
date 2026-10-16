using Sim.Rules;

namespace Sim.Modules.ContractProbe;

/// <summary>
/// Test-only module proving the module contract end to end (P1-20): counts frames and hits in its own slice, asks
/// the core to override the defender's hitstop on hit, and on one configured frame asks it to spawn an entity
/// (P1-21). Registered but never enabled in shipped match rules.
/// </summary>
public sealed class ContractProbeModule : IMechanicModule, IAfterInputHook, IOnHitHook
{
    public ModuleId Id => new(1);

    public string Name => "contract_probe";

    public void AfterInput(ModuleContext context)
    {
        int frame = ++context.GetState<ContractProbeState>().FramesSeen;
        ref readonly ContractProbeConfig config = ref context.GetConfig<ContractProbeConfig>();
        if (frame == config.SpawnOnFrame)
        {
            context.RequestSpawn(config.SpawnOwner, new SpawnRequest { Projectile = config.SpawnProjectile });
        }
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
