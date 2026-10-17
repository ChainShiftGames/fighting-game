using Sim.Rules;

namespace Sim.Modules.AirBlock;

/// <summary>
/// Air block (P1-24): an airborne fighter holding back blocks attacks, except the kinds its config rules out
/// (standard: ground normals). Off (not in the lineup), airborne fighters can't block.
/// </summary>
/// <remarks>
/// Answers the core's <see cref="IAirGuardHook"/> question; the core does the rest (air blockstun, chip, landing).
/// Stateless: it only reads its config.
/// </remarks>
public sealed class AirBlockModule : IMechanicModule, IAirGuardHook
{
    public ModuleId Id => new(2);

    public string Name => "air_block";

    public ModuleConfigLayout ConfigLayout { get; } = ModuleConfigLayout.For<AirBlockConfig>(
        new ModuleConfigField("unblockableAttacks", 0, AirBlockAttacks.All));

    public bool CanAirGuard(ModuleContext context, in AirGuardCheck check)
    {
        ref readonly AirBlockConfig config = ref context.GetConfig<AirBlockConfig>();
        return (config.UnblockableAttacks & AirBlockAttacks.Of(in check)) == 0;
    }
}
