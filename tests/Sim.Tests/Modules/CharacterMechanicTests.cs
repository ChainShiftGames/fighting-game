using System;
using System.Collections.Generic;
using Sim.Characters;
using Sim.Modules;
using Sim.Rules;
using Sim.State;
using Sim.Tests.Pipeline;
using Xunit;

namespace Sim.Tests.Modules;

/// <summary>ADR-0023: a character names its own mechanic, which runs in that fighter's slot for that fighter only.</summary>
public sealed class CharacterMechanicTests
{
    private const int Player1 = 0;
    private const int Player2 = 1;

    [Fact]
    public void AMechanicRunsInItsOwnersSlotWithItsCharactersConfig()
    {
        var mechanic = new OwnerRecordingMechanic(5);

        MatchState state = Run([mechanic], Character(mechanic, config: 7), Character(null));

        Assert.Equal([(ModuleStates.MechanicSlot(Player1), Player1, 7)], mechanic.Calls);
        Assert.Equal(1, state.Modules[ModuleStates.MechanicSlot(Player1)][0]);
        Assert.Equal(0, state.Modules[ModuleStates.MechanicSlot(Player2)][0]);
    }

    [Fact]
    public void InAMirrorTheSameMechanicRunsOncePerFighterWithEachOnesConfig()
    {
        var mechanic = new OwnerRecordingMechanic(5);

        Run([mechanic], Character(mechanic, config: 1), Character(mechanic, config: 2));

        Assert.Equal(
            [(ModuleStates.MechanicSlot(Player1), Player1, 1), (ModuleStates.MechanicSlot(Player2), Player2, 2)],
            mechanic.Calls);
    }

    [Fact]
    public void LineupModulesHaveNoOwner()
    {
        int owner = 0;
        var lineupModule = new ScriptedModule(1) { OnAfterInput = context => owner = context.Owner };

        Run([lineupModule], Character(null), Character(null), lineupModule);

        Assert.Equal(ModuleContext.NoOwner, owner);
    }

    [Fact]
    public void AMechanicsGaugeServesItsOwnerOnly()
    {
        var gauge = new MechanicGauge(5);
        var meters = new List<int>();
        var reader = new ScriptedModule(1)
        {
            OnAfterInput = context => meters.AddRange([context.MeterOf(Player1, 3), context.MeterOf(Player2, 3)]),
        };

        Run([gauge, reader], Character(gauge, config: 3), Character(null), reader);

        Assert.Equal([MechanicGauge.Meter, 0], meters);
    }

    [Fact]
    public void MirrorGaugesOnOneChannelDontCollide()
    {
        var gauge = new MechanicGauge(5);

        Run([gauge], Character(gauge, config: 3), Character(gauge, config: 3));
    }

    [Fact]
    public void AMechanicGaugeOnALineupGaugesChannelFailsAtMatchStart()
    {
        var gauge = new MechanicGauge(5);
        var lineupGauge = new SpentMeterGauge(1, channel: 3);

        Assert.Throws<ArgumentException>(() =>
            Run([gauge, lineupGauge], Character(gauge, config: 3), Character(null), lineupGauge));
    }

    [Fact]
    public void TheLineupCantHoldAMechanic()
    {
        var mechanic = new OwnerRecordingMechanic(5);

        Assert.Throws<ArgumentException>(() => Run([mechanic], Character(null), Character(null), mechanic));
    }

    [Fact]
    public void ACharacterCantNameALineupModuleAsItsMechanic()
    {
        var lineupModule = new ScriptedModule(1);

        Assert.Throws<ArgumentException>(() => Run([lineupModule], Character(lineupModule, config: 0), Character(null)));
    }

    [Fact]
    public void AMechanicNeedsItsRolesFromItsOwnersCharacterOnly()
    {
        var mechanic = new OwnerRecordingMechanic(5, ["install"]);
        CharacterData withRole = Character(mechanic, config: 0, new MoveRole("install", 0));

        Run([mechanic], withRole, Character(null));
        Assert.Throws<ArgumentException>(() => Run([mechanic], Character(mechanic, config: 0), Character(null)));
    }

    private static MatchState Run(
        IMechanicModule[] registered, CharacterData player1, CharacterData player2, params IMechanicModule[] lineup)
    {
        MatchRules rules = default;
        for (int slot = 0; slot < lineup.Length; slot++)
        {
            rules.Modules[slot].Module = lineup[slot].Id;
        }

        var modules = new ModuleLineup(new ModuleCatalog(registered), in rules.Modules, new CoreRequests(), player1, player2);
        MatchState state = default;
        modules.RunAfterInput(ref state);
        return state;
    }

    private static CharacterData Character(IMechanicModule? mechanic, int config = 0, params MoveRole[] roles)
    {
        ModuleSlot slot = default;
        if (mechanic is not null)
        {
            slot.Module = mechanic.Id;
            slot.Config[0] = config;
        }

        return new CharacterData("c", "C", default, ["move"], new MoveData[1], [], [], [], [], [], [], null, roles, slot);
    }

    // Records (slot, owner, config[0]) per call and counts its calls in its slice.
    private sealed class OwnerRecordingMechanic(int id, IReadOnlyList<string>? roles = null) : IMechanicModule, IAfterInputHook
    {
        public ModuleId Id { get; } = new(id);

        public string Name => "owner_recording";

        public bool IsCharacterMechanic => true;

        public IReadOnlyList<string> MoveRoles { get; } = roles ?? [];

        public List<(int Slot, int Owner, int Config)> Calls { get; } = [];

        public void AfterInput(ModuleContext context)
        {
            Calls.Add((ModuleStates.MechanicSlot(context.Owner), context.Owner, context.GetConfig<TestConfig>().Value));
            context.GetState<CountingState>().AfterInput++;
        }
    }

    // A gauge on the channel in config[0] holding a fixed meter.
    private sealed class MechanicGauge(int id) : IMechanicModule, IMeterGauge
    {
        public const int Meter = 42;

        public ModuleId Id { get; } = new(id);

        public string Name => "mechanic_gauge";

        public bool IsCharacterMechanic => true;

        public int ChannelOf(ref readonly ModuleConfig config) => config[0];

        public int MeterOf(ModuleContext context, int fighter) => Meter;

        public bool TrySpend(ModuleContext context, int fighter, int amount) => false;
    }
}
