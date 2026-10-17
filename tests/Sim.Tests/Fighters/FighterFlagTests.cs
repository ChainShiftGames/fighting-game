using System;
using System.Collections.Generic;
using Sim.Characters;
using Sim.Combat;
using Sim.Fighters;
using Sim.Fighters.States;
using Sim.Input;
using Sim.Modules;
using Sim.Pipeline;
using Sim.Rules;
using Sim.State;
using Sim.Tests.Modules;
using Sim.Tests.Pipeline;
using Xunit;

namespace Sim.Tests.Fighters;

/// <summary>ADR-0023: fighter flags set and cleared by moves, hits and module requests, and named once for modules.</summary>
public sealed class FighterFlagTests
{
    private const int Player1 = 0;
    private const int Stance = 1;
    private const int Install = 2;

    [Fact]
    public void AnAttackClearsThenSetsItsFlagsAsItStarts()
    {
        CharacterData character = Character(new MoveData { Category = MoveCategory.Special, ClearsFlags = Stance | Install, SetsFlags = Stance });
        FighterState fighter = new() { Flags = Install | 4 };

        MoveEntry.EnterAttack(ref fighter, 0, character);

        Assert.Equal(Stance | 4, fighter.Flags);
    }

    [Fact]
    public void AHitTakesAwayOnlyTheFlagsMarkedClearedWhenHit()
    {
        CharacterData character = Character(new MoveData { Category = MoveCategory.Basic, TotalFrames = 10 });
        FighterState fighter = new() { Flags = Stance | Install, Health = 100 };

        HitReactions.TakeHit(ref fighter, new HitData { Hitstun = 5 }, character, 1, flagsClearedWhenHit: Stance);

        Assert.Equal(Install, fighter.Flags);
    }

    [Fact]
    public void FlagRequestsClearThenSetSlotBySlot()
    {
        var clearing = new ScriptedModule(1)
        {
            OnAfterInput = context => context.RequestFlags(Player1, new FlagsRequest { Clear = Stance | Install }),
        };
        var setting = new ScriptedModule(2)
        {
            OnAfterInput = context => context.RequestFlags(Player1, new FlagsRequest { Set = Install }),
        };
        TickPipeline pipeline = Pipelines.Create(clearing, setting);
        MatchState state = default;
        state.Fighters[Player1].Flags = Stance;

        pipeline.Tick(ref state, InputWord.Neutral, InputWord.Neutral);

        Assert.Equal(Install, state.Fighters[Player1].Flags);
    }

    [Fact]
    public void ModulesGetTheBitsOfTheFlagsTheyName()
    {
        var module = new FlagNamingModule(["install", "stance"]);

        Assert.Equal([Install, Stance], Run(module, new FighterFlagNames(["stance", "install"])));
    }

    [Fact]
    public void AModuleNamingAnUnknownFlagFailsAtMatchStart()
    {
        var module = new FlagNamingModule(["dodge"]);

        Assert.Throws<ArgumentException>(() => Run(module, new FighterFlagNames(["stance"])));
    }

    [Fact]
    public void ALineupWithoutFlagNamesFailsWhenAModuleAsks()
    {
        var module = new FlagNamingModule(["stance"]);

        Assert.Throws<InvalidOperationException>(() => Run(module, null));
    }

    [Fact]
    public void AFlagNamedTwiceFailsLoudly()
    {
        Assert.Throws<ArgumentException>(() => new FighterFlagNames(["a", "a"]));
    }

    [Fact]
    public void MoreFlagsThanFitOneIntFailLoudly()
    {
        var names = new List<string>();
        for (int i = 0; i <= FighterFlagNames.MaxFlags; i++)
        {
            names.Add($"flag_{i}");
        }

        Assert.Throws<ArgumentException>(() => new FighterFlagNames(names));
    }

    private static List<int> Run(FlagNamingModule module, FighterFlagNames? flags)
    {
        MatchRules rules = default;
        rules.Modules[0].Module = module.Id;
        var lineup = new ModuleLineup(new ModuleCatalog(module), in rules.Modules, new CoreRequests(), null, null, flags);
        MatchState state = default;
        lineup.RunAfterInput(ref state);
        return module.Masks;
    }

    private static CharacterData Character(MoveData move) =>
        new("c", "C", default, ["move"], [move], [], [], [], [], [], new int[21]);

    // Records the mask of each flag it names.
    private sealed class FlagNamingModule(IReadOnlyList<string> flags) : IMechanicModule, IAfterInputHook
    {
        public ModuleId Id { get; } = new(1);

        public string Name => "flag_naming";

        public IReadOnlyList<string> Flags { get; } = flags;

        public List<int> Masks { get; } = [];

        public void AfterInput(ModuleContext context)
        {
            for (int flag = 0; flag < Flags.Count; flag++)
            {
                Masks.Add(context.FlagMask(flag));
            }
        }
    }
}
