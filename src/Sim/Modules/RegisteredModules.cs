using Sim.Modules.ContractProbe;

namespace Sim.Modules;

/// <summary>The one place mechanics modules are registered (ARCHITECTURE.md §5): one line per module.</summary>
/// <remarks>
/// The only type outside a module's folder that may name it (architecture test), so removing a module means deleting
/// its folder and its line here. Registering doesn't enable a module; <see cref="Rules.MatchRules"/> does.
/// </remarks>
public static class RegisteredModules
{
    /// <summary>Creates every registered module; called once at match start.</summary>
    public static ModuleCatalog Create() => new(
    [
        new ContractProbeModule(),
    ]);
}
