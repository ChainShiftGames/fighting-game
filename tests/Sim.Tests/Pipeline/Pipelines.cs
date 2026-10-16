using Sim.Modules;
using Sim.Pipeline;
using Sim.Rules;

namespace Sim.Tests.Pipeline;

/// <summary>Builds a tick pipeline with the given modules enabled in slots 0, 1, 2… (their hook order).</summary>
internal static class Pipelines
{
    public static TickPipeline Create(TickStages stages, params IMechanicModule[] modulesInSlotOrder)
    {
        MatchRules rules = default;
        for (int slot = 0; slot < modulesInSlotOrder.Length; slot++)
        {
            rules.Modules[slot].Module = modulesInSlotOrder[slot].Id;
        }

        var requests = new CoreRequests();
        var lineup = new ModuleLineup(new ModuleCatalog(modulesInSlotOrder), in rules.Modules, requests);
        return new TickPipeline(stages, lineup, requests);
    }

    public static TickPipeline Create(params IMechanicModule[] modulesInSlotOrder) =>
        Create(new TickStages(), modulesInSlotOrder);
}
