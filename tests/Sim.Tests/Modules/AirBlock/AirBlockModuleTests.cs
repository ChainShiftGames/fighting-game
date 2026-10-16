using System;
using System.Runtime.InteropServices;
using Sim.Characters;
using Sim.Modules;
using Sim.Modules.AirBlock;
using Sim.Rules;
using Sim.State;
using Xunit;

namespace Sim.Tests.Modules.AirBlock;

/// <summary>The air block module through the real registration: its config decides which attacks it lets through.</summary>
/// <remarks>Deleted together with src/Sim/Modules/AirBlock/ in the removal test.</remarks>
public sealed class AirBlockModuleTests
{
    private const int Slot = 1;

    private static readonly AirGuardCheck _groundNormal = Attack(MoveCategory.Normal, MoveStance.Ground);
    private static readonly AirGuardCheck _airNormal = Attack(MoveCategory.Normal, MoveStance.Air);
    private static readonly AirGuardCheck _special = Attack(MoveCategory.Special, MoveStance.Ground);
    private static readonly AirGuardCheck _super = Attack(MoveCategory.Super, MoveStance.Ground);
    private static readonly AirGuardCheck _projectile =
        new(attacker: 0, defender: 1, MoveCategory.None, MoveStance.Ground, isProjectile: true);

    [Fact]
    public void WithoutConfigEveryAttackCanBeAirBlocked()
    {
        ModuleLineup lineup = LineupWith(unblockableAttacks: 0);
        MatchState state = default;

        foreach (AirGuardCheck check in new[] { _groundNormal, _airNormal, _special, _super, _projectile })
        {
            Assert.True(lineup.AllowsAirGuard(ref state, in check));
        }
    }

    [Fact]
    public void TheConfigRulesOutEachMarkedKind()
    {
        ModuleLineup lineup = LineupWith(AirBlockAttacks.GroundNormals | AirBlockAttacks.Projectiles);
        MatchState state = default;

        Assert.False(lineup.AllowsAirGuard(ref state, in _groundNormal));
        Assert.False(lineup.AllowsAirGuard(ref state, in _projectile));
        Assert.True(lineup.AllowsAirGuard(ref state, in _airNormal));
        Assert.True(lineup.AllowsAirGuard(ref state, in _special));
        Assert.True(lineup.AllowsAirGuard(ref state, in _super));
    }

    [Fact]
    public void IsFoundByItsDataName()
    {
        Assert.True(RegisteredModules.Create().TryFindId("air_block", out ModuleId id));
        Assert.Equal(new AirBlockModule().Id, id);
    }

    private static ModuleLineup LineupWith(int unblockableAttacks)
    {
        MatchRules rules = default;
        rules.Modules[Slot].Module = new AirBlockModule().Id;
        ConfigOf(ref rules.Modules[Slot].Config).UnblockableAttacks = unblockableAttacks;
        return new ModuleLineup(RegisteredModules.Create(), in rules.Modules, new CoreRequests());
    }

    private static AirGuardCheck Attack(MoveCategory category, MoveStance stance) =>
        new(attacker: 0, defender: 1, category, stance, isProjectile: false);

    private static ref AirBlockConfig ConfigOf(ref ModuleConfig config) =>
        ref MemoryMarshal.AsRef<AirBlockConfig>(MemoryMarshal.AsBytes((Span<int>)config));
}
