using System;
using System.Collections.Generic;
using Sim.Characters;
using Sim.Modules;
using Sim.Rules;
using Sim.State;
using Xunit;

namespace Sim.Tests.Modules;

/// <summary>ADR-0023: modules read the characters, and find moves by role instead of by index.</summary>
public sealed class ModuleCharactersTests
{
    private static readonly CharacterData _player1 =
        Character("first", new MoveRole("dodge", 1), new MoveRole("guard", 0));

    private static readonly CharacterData _player2 =
        Character("second", new MoveRole("guard", 2), new MoveRole("dodge", 0));

    [Fact]
    public void EachFighterGetsItsOwnCharactersMoveForEachRole()
    {
        var module = new RoleReadingModule(1, ["guard", "dodge"]);

        Run(module, _player1, _player2);

        Assert.Equal([0, 1, 2, 0], module.Moves);
    }

    [Fact]
    public void ModulesSeeEachFightersCharacter()
    {
        var module = new RoleReadingModule(1, []);

        Run(module, _player1, _player2);

        Assert.Equal(["first", "second"], module.CharacterIds);
    }

    [Fact]
    public void ACharacterWithoutARoleTheLineupNeedsFailsAtMatchStart()
    {
        var module = new RoleReadingModule(1, ["burst"]);

        ArgumentException error = Assert.Throws<ArgumentException>(() => Run(module, _player1, _player2));
        Assert.Contains("burst", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AskingForARoleTheModuleDoesNotHaveFailsLoudly()
    {
        var module = new RoleReadingModule(1, ["dodge"]) { ExtraRole = 1 };

        Assert.Throws<ArgumentOutOfRangeException>(() => Run(module, _player1, _player2));
    }

    [Fact]
    public void ALineupWithoutCharactersFailsWhenAModuleAsks()
    {
        var module = new RoleReadingModule(1, []);

        Assert.Throws<InvalidOperationException>(() => Run(module, null, null));
    }

    private static void Run(RoleReadingModule module, CharacterData? player1, CharacterData? player2)
    {
        MatchRules rules = default;
        rules.Modules[0].Module = module.Id;
        var lineup = new ModuleLineup(new ModuleCatalog(module), in rules.Modules, new CoreRequests(), player1, player2);
        MatchState state = default;
        lineup.RunAfterInput(ref state);
    }

    private static CharacterData Character(string id, params MoveRole[] roles) =>
        new(id, id, default, ["a", "b", "c"], new MoveData[3], [], [], [], [], [], [], null, roles);

    // Records each fighter's character id and role moves, fighter by fighter.
    private sealed class RoleReadingModule(int id, IReadOnlyList<string> roles) : IMechanicModule, IAfterInputHook
    {
        public ModuleId Id { get; } = new(id);

        public string Name => "role_reading";

        public IReadOnlyList<string> MoveRoles { get; } = roles;

        public int ExtraRole { get; init; } = -1;

        public List<int> Moves { get; } = [];

        public List<string> CharacterIds { get; } = [];

        public void AfterInput(ModuleContext context)
        {
            for (int fighter = 0; fighter < MatchState.PlayerCount; fighter++)
            {
                CharacterIds.Add(context.CharacterOf(fighter).Id);
                for (int role = 0; role < MoveRoles.Count; role++)
                {
                    Moves.Add(context.RoleMove(fighter, role));
                }

                if (ExtraRole >= 0)
                {
                    context.RoleMove(fighter, ExtraRole);
                }
            }
        }
    }
}
