using System.Linq;
using Sim.Characters;
using Sim.Events;
using Sim.Fighters.States;
using Sim.Input;
using Sim.Math;
using Sim.Rules;
using Sim.State;
using Sim.Tests.Modules;
using Xunit;
using static Sim.Tests.Entities.ProjectileCharacter;

namespace Sim.Tests.Entities;

/// <summary>Projectile hits, blocks and clashes, from detection through resolution.</summary>
/// <remarks>P2 stands at 100000 (hurtbox 75000 to 125000); a projectile at x = 80000 touches it.</remarks>
public sealed class EntityHitResolutionStageTests
{
    private const int Frame = 40;
    private const int TouchingP2 = 80_000;
    private const int TouchingP1 = -80_000;
    private static readonly int _player2Slot = EntityPool.FirstSlotOf(1);

    private readonly EntityStages _stages = new();

    [Fact]
    public void AProjectileHitDamagesTheDefenderAndDisappears()
    {
        MatchState state = Fighters();
        state.Entities.Slots[0] = Entity(0, TouchingP2);

        _stages.Hit(ref state);

        FighterState defender = state.Fighters[1];
        Assert.Equal(Health - FireballHit.Damage, defender.Health);
        Assert.Equal((FighterAction.Hitstun, FireballHit.Hitstun), (defender.Action, defender.StunFramesLeft));
        Assert.Equal(default, state.Entities.Slots[0]);
        FrameEvent hit = Assert.Single(state.Events.OfFrame(Frame).ToArray());
        Assert.Equal(
            (FrameEventKind.ProjectileHit, 0, new IntVector2(82_500, 50_000), FireballProjectile),
            (hit.Kind, hit.Fighter, hit.Position, hit.Detail));
    }

    // Decided rule (ADR-0017): projectiles counter hit like attacks; P2 is on its fireball's startup frame.
    [Fact]
    public void AProjectileHitDuringTheDefendersStartupIsACounterHit()
    {
        EntityStages stages = new(counterHit: new CounterBonus { ExtraHitstun = 2, ExtraHitstop = 3, ExtraDamagePercent = 20 });
        MatchState state = Fighters();
        state.Fighters[1] = At(100_000, -1, Fireball, 1);
        state.Entities.Slots[0] = Entity(0, TouchingP2);

        stages.Hit(ref state);

        FighterState defender = state.Fighters[1];
        Assert.Equal(Health - (FireballHit.Damage * 6 / 5), defender.Health);
        Assert.Equal(
            (FireballHit.Hitstun + 2, FireballHit.Hitstop + 3),
            (defender.StunFramesLeft, defender.Timing.HitstopFramesLeft));
        Assert.Equal(0, state.Fighters[0].Timing.HitstopFramesLeft);
        Assert.Equal(
            [(FrameEventKind.ProjectileHit, 0), (FrameEventKind.CounterHit, 0)],
            state.Events.OfFrame(Frame).ToArray().Select(frameEvent => (frameEvent.Kind, frameEvent.Fighter)));
    }

    // Decided rule: the owner isn't in contact, so only the defender gets hitstop and the owner's move records nothing.
    [Fact]
    public void OnlyTheDefenderGetsTheProjectilesHitstop()
    {
        MatchState state = Fighters();
        state.Fighters[0] = At(-100_000, 1, Fireball, 20);
        state.Entities.Slots[0] = Entity(0, TouchingP2);

        _stages.Hit(ref state);

        Assert.Equal(FireballHit.Hitstop, state.Fighters[1].Timing.HitstopFramesLeft);
        Assert.Equal(0, state.Fighters[0].Timing.HitstopFramesLeft);
        Assert.Equal(CancelOn.None, state.Fighters[0].MoveOutcome);
    }

    [Fact]
    public void AProjectileHitRunsTheModulesHitHookWithTheOwnerAsAttacker()
    {
        MatchState state = Fighters();
        state.Entities.Slots[_player2Slot] = Entity(1, TouchingP1);

        _stages.Hit(ref state);

        CountingState hooks = EntityStages.Hooks(ref state);
        Assert.Equal((1, 0, 1, 0), (hooks.OnHit, hooks.OnBlock, hooks.LastAttacker, hooks.LastDefender));
    }

    [Fact]
    public void HoldingBackBlocksAProjectileWithChipAndBlockstun()
    {
        MatchState state = Fighters();
        state.Inputs[1].Record(new InputWord(InputWord.Right));
        state.Entities.Slots[0] = Entity(0, TouchingP2);

        _stages.Hit(ref state);

        FighterState defender = state.Fighters[1];
        Assert.Equal(Health - FireballHit.ChipDamage, defender.Health);
        Assert.Equal((FighterAction.Blockstun, FireballHit.Blockstun), (defender.Action, defender.StunFramesLeft));
        Assert.Equal(FrameEventKind.ProjectileBlock, state.Events.OfFrame(Frame)[0].Kind);
        Assert.Equal(1, EntityStages.Hooks(ref state).OnBlock);
        Assert.Equal(0, EntityStages.Count(in state));
    }

    // Standard rule: "back" is away from the owner, not from the projectile: a projectile from behind (here at
    // x = 120000, past P2) is still blocked by holding away from P1.
    [Theory]
    [InlineData(InputWord.Right, true)]
    [InlineData(InputWord.Left, false)]
    public void AProjectileFromBehindIsBlockedByHoldingAwayFromTheOwner(int direction, bool isBlocked)
    {
        MatchState state = Fighters();
        state.Inputs[1].Record(new InputWord((ushort)direction));
        state.Entities.Slots[0] = Entity(0, 120_000);

        _stages.Hit(ref state);

        Assert.Equal(isBlocked ? FighterAction.Blockstun : FighterAction.Hitstun, state.Fighters[1].Action);
    }

    [Fact]
    public void AnInvulnerableDefenderIsNotTouched()
    {
        MatchState state = Fighters();
        state.Fighters[1] = At(100_000, -1, Invulnerable);
        state.Entities.Slots[0] = Entity(0, TouchingP2);

        _stages.Hit(ref state);

        Assert.Equal(Health, state.Fighters[1].Health);
        Assert.Equal(1, state.Entities.Slots[0].IsActive);
    }

    [Fact]
    public void AProjectileNeverTouchesItsOwner()
    {
        MatchState state = Fighters();
        state.Entities.Slots[0] = Entity(0, TouchingP1);

        _stages.Hit(ref state);

        Assert.Equal(Health, state.Fighters[0].Health);
        Assert.Equal(1, EntityStages.Count(in state));
    }

    [Fact]
    public void OverlappingProjectilesEachLoseOneDurabilityAndDisappearAtZero()
    {
        MatchState state = Fighters();
        state.Entities.Slots[0] = Entity(0, 0, SturdyProjectile);
        state.Entities.Slots[_player2Slot] = Entity(1, 5_000);

        _stages.Hit(ref state);

        Assert.Equal(SturdyDurability - 1, state.Entities.Slots[0].Durability);
        Assert.Equal(default, state.Entities.Slots[_player2Slot]);
        FrameEvent[] clashes = state.Events.OfFrame(Frame).ToArray();
        Assert.Equal([FrameEventKind.ProjectileClash, FrameEventKind.ProjectileClash], clashes.Select(e => e.Kind));
        Assert.Equal([0, 1], clashes.Select(e => e.Fighter));
        Assert.Equal(new IntVector2(2_500, 50_000), clashes[0].Position);
    }

    [Fact]
    public void ProjectilesClashAgainOnEveryFrameTheyStillOverlap()
    {
        MatchState state = Fighters();
        state.Entities.Slots[0] = Entity(0, 0, SturdyProjectile);
        state.Entities.Slots[_player2Slot] = Entity(1, 5_000, SturdyProjectile);

        _stages.Hit(ref state);
        state.Frame++;
        _stages.Hit(ref state);

        Assert.Equal((1, 1), (state.Entities.Slots[0].Durability, state.Entities.Slots[_player2Slot].Durability));
    }

    [Fact]
    public void OneProjectileOverlappingTwoLosesTwoDurability()
    {
        MatchState state = Fighters();
        state.Entities.Slots[0] = Entity(0, 0, SturdyProjectile);
        state.Entities.Slots[_player2Slot] = Entity(1, 5_000);
        state.Entities.Slots[_player2Slot + 1] = Entity(1, -5_000);

        _stages.Hit(ref state);

        Assert.Equal(SturdyDurability - 2, state.Entities.Slots[0].Durability);
        Assert.Equal(1, EntityStages.Count(in state));
    }

    [Fact]
    public void AnOwnersProjectilesDoNotClashWithEachOther()
    {
        MatchState state = Fighters();
        state.Entities.Slots[0] = Entity(0, 0);
        state.Entities.Slots[1] = Entity(0, 5_000);

        _stages.Hit(ref state);

        Assert.Equal(2, EntityStages.Count(in state));
    }

    [Fact]
    public void AProjectileDestroyedInAClashHitsNothing()
    {
        MatchState state = Fighters();
        state.Entities.Slots[0] = Entity(0, TouchingP2);
        state.Entities.Slots[_player2Slot] = Entity(1, TouchingP2);

        _stages.Hit(ref state);

        Assert.Equal(Health, state.Fighters[1].Health);
        Assert.Equal(0, EntityStages.Count(in state));
    }

    [Fact]
    public void AProjectileThatSurvivesAClashStillHitsThatFrame()
    {
        MatchState state = Fighters();
        state.Entities.Slots[0] = Entity(0, TouchingP2, SturdyProjectile);
        state.Entities.Slots[_player2Slot] = Entity(1, TouchingP2);

        _stages.Hit(ref state);

        Assert.Equal(Health - SturdyHit.Damage, state.Fighters[1].Health);
        Assert.Equal(0, EntityStages.Count(in state));
    }

    [Fact]
    public void UnderTheTradeRuleProjectileExchangesBothLand()
    {
        MatchState state = Fighters();
        state.Entities.Slots[0] = Entity(0, TouchingP2);
        state.Entities.Slots[_player2Slot] = Entity(1, TouchingP1, SturdyProjectile);

        _stages.Hit(ref state);

        Assert.Equal(
            (Health - SturdyHit.Damage, Health - FireballHit.Damage),
            (state.Fighters[0].Health, state.Fighters[1].Health));
        Assert.Equal(
            (SturdyHit.Hitstop, FireballHit.Hitstop),
            (state.Fighters[0].Timing.HitstopFramesLeft, state.Fighters[1].Timing.HitstopFramesLeft));
    }

    [Fact]
    public void UnderTheClashRuleAnExchangeCancelsTheProjectiles()
    {
        EntityStages stages = new(tradeRule: TradeRule.Clash);
        MatchState state = Fighters();
        state.Entities.Slots[0] = Entity(0, TouchingP2);
        state.Entities.Slots[_player2Slot] = Entity(1, TouchingP1);

        stages.Hit(ref state);

        Assert.Equal((Health, Health), (state.Fighters[0].Health, state.Fighters[1].Health));
        Assert.Equal(0, EntityStages.Count(in state));
        Assert.All(state.Events.OfFrame(Frame).ToArray(), e => Assert.Equal(FrameEventKind.ProjectileClash, e.Kind));
        Assert.Equal(0, EntityStages.Hooks(ref state).OnHit);
    }

    [Fact]
    public void UnderTheClashRuleAOneSidedHitStillLands()
    {
        EntityStages stages = new(tradeRule: TradeRule.Clash);
        MatchState state = Fighters();
        state.Entities.Slots[0] = Entity(0, TouchingP2);

        stages.Hit(ref state);

        Assert.Equal(Health - FireballHit.Damage, state.Fighters[1].Health);
    }

    [Fact]
    public void TwoProjectilesTouchingOneDefenderBothLandInSlotOrder()
    {
        MatchState state = Fighters();
        state.Entities.Slots[0] = Entity(0, TouchingP2);
        state.Entities.Slots[1] = Entity(0, TouchingP2, SturdyProjectile);

        _stages.Hit(ref state);

        Assert.Equal(Health - FireballHit.Damage - SturdyHit.Damage, state.Fighters[1].Health);
        Assert.Equal(SturdyHit.Hitstun, state.Fighters[1].StunFramesLeft);
        Assert.Equal(SturdyHit.Hitstop, state.Fighters[1].Timing.HitstopFramesLeft);
    }

    private static MatchState Fighters()
    {
        MatchState state = default;
        state.Frame = Frame;
        state.Fighters[0] = At(-100_000, 1);
        state.Fighters[1] = At(100_000, -1);
        return state;
    }
}
