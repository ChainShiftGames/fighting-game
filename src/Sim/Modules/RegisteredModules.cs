using Sim.Modules.AirBlock;
using Sim.Modules.Burst;
using Sim.Modules.ComboLimits;
using Sim.Modules.ContractProbe;
using Sim.Modules.Gatlings;
using Sim.Modules.InstantBlock;
using Sim.Modules.Meter;
using Sim.Modules.NegativePenalty;
using Sim.Modules.RomanCancel;

namespace Sim.Modules;

/// <summary>The one place mechanics modules are registered (ARCHITECTURE.md §5): one line per module.</summary>
/// <remarks>
/// The only type outside a module's folder that may name it (architecture test), so removing a module means deleting
/// its folder and its line here (plus its data file, which data loading would otherwise reject). Registering doesn't
/// enable a module: <c>data/rules/modules.json</c> and the module's own data file do, through
/// <see cref="Rules.MatchRules"/>. Order here doesn't matter (hook order is in data); keep it alphabetical so parallel
/// module tasks add their lines in different places. How to add one: README.md in this folder.
/// </remarks>
public static class RegisteredModules
{
    /// <summary>Creates every registered module; called once at match start and when data is loaded.</summary>
    public static ModuleCatalog Create() => new(
    [
        new AirBlockModule(),
        new BurstModule(),
        new ComboLimitsModule(),
        new ContractProbeModule(),
        new GatlingsModule(),
        new InstantBlockModule(),
        new MeterModule(),
        new NegativePenaltyModule(),
        new RomanCancelModule(),
    ]);
}
