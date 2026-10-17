using Sim.Rules;

namespace Sim.Modules.Gatlings;

/// <summary>
/// Gatlings and cancels (P3-10): fighters cancel attacks through the cancel windows in their character data, by
/// chain routes into normals, normal → special → super, into throws and into jumps, except the kinds its config
/// closes. Off (not in the lineup), attacks never cancel through data windows: only links.
/// </summary>
/// <remarks>
/// The routes, frames and outcomes (hit, block, whiff) are per character, in each move's <c>cancels</c>; this
/// answers the core's <see cref="ICancelWindowHook"/> question and the state machine does the rest. Stateless: it
/// only reads its config.
/// </remarks>
public sealed class GatlingsModule : IMechanicModule, ICancelWindowHook
{
    public ModuleId Id => new(10);

    public string Name => "gatlings";

    public ModuleConfigLayout ConfigLayout { get; } = ModuleConfigLayout.For<GatlingsConfig>(
        new ModuleConfigField("closedCancels", 0, GatlingCancels.All));

    public bool CanCancel(ModuleContext context, in CancelCheck check)
    {
        ref readonly GatlingsConfig config = ref context.GetConfig<GatlingsConfig>();
        return (config.ClosedCancels & GatlingCancels.Of(in check)) == 0;
    }
}
