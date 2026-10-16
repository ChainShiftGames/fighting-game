using Sim.Characters;
using Sim.Combat;
using Sim.Fighters.States;
using Sim.Input;
using Sim.Math;
using Sim.Modules;
using Sim.Rules;
using Sim.State;
using Xunit;
using static Sim.Tests.Combat.CombatCharacter;

namespace Sim.Tests.Combat;

public sealed class GuardingTests
{
    // The attacker stands left of the defender, so the defender's back is to the right.
    private static readonly FighterState _attacker = At(-30_000, facing: 1);
    private static readonly InputWord _back = new(InputWord.Right);
    private static readonly AirGuardCheck _check = new(0, 1, MoveCategory.Normal, MoveStance.Ground, false);

    [Fact]
    public void AnAirborneFighterHoldingBackAirBlocksWhenAModuleAllowsIt()
    {
        (Guarding guarding, _) = With(answer: true);
        MatchState state = default;

        Assert.Equal(BlockStance.Air, guarding.StanceOf(ref state, Jumping(), in _attacker, _back, in _check));
    }

    [Fact]
    public void WithoutAModuleAirborneFightersCannotBlock()
    {
        Guarding guarding = new(Lineup());
        MatchState state = default;

        Assert.Equal(BlockStance.None, guarding.StanceOf(ref state, Jumping(), in _attacker, _back, in _check));
    }

    [Fact]
    public void AModuleThatSaysNoStopsTheAirBlock()
    {
        (Guarding guarding, _) = With(answer: false);
        MatchState state = default;

        Assert.Equal(BlockStance.None, guarding.StanceOf(ref state, Jumping(), in _attacker, _back, in _check));
    }

    [Theory]
    [InlineData(FighterAction.Jump, 0, false)]
    [InlineData(FighterAction.AirDash, InputWord.Right, false)]
    [InlineData(FighterAction.AirHitstun, InputWord.Right, false)]
    [InlineData(FighterAction.AirBlockstun, InputWord.Right, true)]
    [InlineData(FighterAction.Jump, InputWord.Right | InputWord.Up, true)]
    public void OnlyAFreeOrAlreadyBlockingAirborneFighterHoldingBackIsAsked(
        FighterAction action, ushort input, bool isAsked)
    {
        (Guarding guarding, AirGuardAnswer module) = With(answer: true);
        MatchState state = default;
        FighterState defender = Jumping() with { Action = action };

        guarding.StanceOf(ref state, in defender, in _attacker, new InputWord(input), in _check);

        Assert.Equal(isAsked ? 1 : 0, module.Questions);
    }

    [Fact]
    public void OnTheGroundModulesAreNotAsked()
    {
        (Guarding guarding, AirGuardAnswer module) = With(answer: false);
        MatchState state = default;

        Assert.Equal(
            BlockStance.Standing, guarding.StanceOf(ref state, At(30_000, -1), in _attacker, _back, in _check));
        Assert.Equal(0, module.Questions);
    }

    [Theory]
    [InlineData(Guard.Mid, true)]
    [InlineData(Guard.Low, true)]
    [InlineData(Guard.Overhead, true)]
    [InlineData(Guard.Unblockable, false)]
    public void AnAirBlockStopsEverythingBlockable(Guard guard, bool isBlocked)
    {
        Assert.Equal(isBlocked, Blocking.Stops(BlockStance.Air, guard));
    }

    private static FighterState Jumping() => At(30_000, -1) with
    {
        Action = FighterAction.Jump,
        IsAirborne = 1,
        Position = new IntVector2(30_000, 40_000),
    };

    private static (Guarding Guarding, AirGuardAnswer Module) With(bool answer)
    {
        AirGuardAnswer module = new(1, answer);
        return (new Guarding(Lineup(module)), module);
    }

    private static ModuleLineup Lineup(params IMechanicModule[] modules)
    {
        MatchRules rules = default;
        for (int slot = 0; slot < modules.Length; slot++)
        {
            rules.Modules[slot].Module = modules[slot].Id;
        }

        return new ModuleLineup(new ModuleCatalog(modules), in rules.Modules, new CoreRequests());
    }
}
