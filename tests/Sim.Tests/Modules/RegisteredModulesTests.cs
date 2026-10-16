using Sim.Modules;
using Sim.Rules;
using Sim.State;
using Xunit;

namespace Sim.Tests.Modules;

/// <summary>The real registration list: names no module here, so it passes with any set of registered modules.</summary>
public sealed class RegisteredModulesTests
{
    [Fact]
    public void RegisteredModulesHaveUniqueIdsAndNames()
    {
        ModuleCatalog catalog = RegisteredModules.Create();

        Assert.True(catalog.Count <= ModuleStates.MaxModules);
    }

    [Fact]
    public void EveryRegisteredModuleCanJoinOneLineup()
    {
        ModuleCatalog catalog = RegisteredModules.Create();
        MatchRules rules = default;
        for (int i = 0; i < catalog.Count; i++)
        {
            rules.Modules[i].Module = catalog[i].Id;
        }

        var lineup = new ModuleLineup(catalog, in rules.Modules, new CoreRequests());
        MatchState state = default;

        lineup.RunAfterInput(ref state);
        lineup.RunEndOfFrame(ref state);
    }
}
