using System.Collections.Generic;
using Sim.Characters;
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
        int slot = 0;
        for (int i = 0; i < catalog.Count; i++)
        {
            if (!catalog[i].IsCharacterMechanic)
            {
                rules.Modules[slot++].Module = catalog[i].Id;
            }
        }

        // Each character mechanic joins as P1's, in its own match, with P2 playing a character without one. Its largest
        // config values put a gauge on the last channel, clear of the lineup's main meter.
        for (int i = 0; i < catalog.Count; i++)
        {
            ModuleSlot mechanic = catalog[i].IsCharacterMechanic ? LargestConfig(catalog[i]) : default;
            var lineup = new ModuleLineup(
                catalog, in rules.Modules, new CoreRequests(), Character(catalog, mechanic), Character(catalog, default));
            MatchState state = default;

            lineup.RunAfterInput(ref state);
            lineup.RunEndOfFrame(ref state);
        }
    }

    private static ModuleSlot LargestConfig(IMechanicModule module)
    {
        ModuleSlot slot = new() { Module = module.Id };
        for (int i = 0; i < module.ConfigLayout.Count; i++)
        {
            slot.Config[i] = module.ConfigLayout[i].Max;
        }

        return slot;
    }

    // Names every role any module asks for, all as its one move.
    private static CharacterData Character(ModuleCatalog catalog, ModuleSlot mechanic)
    {
        List<MoveRole> roles = [];
        for (int i = 0; i < catalog.Count; i++)
        {
            foreach (string role in catalog[i].MoveRoles)
            {
                roles.Add(new MoveRole(role, 0));
            }
        }

        return new CharacterData("any", "Any", default, ["move"], new MoveData[1], [], [], [], [], [], [], null, [.. roles], mechanic);
    }
}
