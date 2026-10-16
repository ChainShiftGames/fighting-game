using System;
using Sim.Characters;
using Sim.Events;
using Sim.Fighters.States;
using Sim.Input;
using Sim.Math;
using Sim.Rules;
using Sim.State;
using Sim.Tests.Modules;
using Xunit;
using static Sim.Tests.Combat.CombatCharacter;

namespace Sim.Tests.Combat;

public sealed class HitResolutionStageTests
{
    private const int Frame = 40;

    // P2 stands right of P1, so P2's back is to the right.
    private static readonly InputWord _back = new(InputWord.Right);
    private static readonly InputWord _downBack = new(InputWord.Right | InputWord.Down);

    private readonly CombatStages _stages = new();

    [Fact]
    public void AHitDamagesTheDefenderAndPutsItInHitstun()
    {
        MatchState state = P1Attacks(Mid, At(30_000, -1));

        _stages.Run(ref state);

        FighterState defender = state.Fighters[1];
        Assert.Equal(Health - MidHit.Damage, defender.Health);
        Assert.Equal(
            (FighterAction.Hitstun, MidHit.Hitstun, 1), (defender.Action, defender.StunFramesLeft, defender.MoveFrame));
        Assert.Equal(CancelOn.Hit, state.Fighters[0].MoveOutcome);
        Assert.Equal(Health, state.Fighters[0].Health);
    }

    [Fact]
    public void AHitstopFreezesBothFighters()
    {
        MatchState state = P1Attacks(Mid, At(30_000, -1));

        _stages.Run(ref state);

        Assert.Equal(MidHit.Hitstop, state.Fighters[0].Timing.HitstopFramesLeft);
        Assert.Equal(MidHit.Hitstop, state.Fighters[1].Timing.HitstopFramesLeft);
    }

    [Fact]
    public void ALongerHitstopAlreadyRunningIsKept()
    {
        MatchState state = P1Attacks(Mid, At(30_000, -1));
        state.Fighters[1].Timing.HitstopFramesLeft = 30;

        _stages.Run(ref state);

        Assert.Equal(30, state.Fighters[1].Timing.HitstopFramesLeft);
        Assert.Equal(MidHit.Hitstop, state.Fighters[0].Timing.HitstopFramesLeft);
    }

    [Fact]
    public void ACrouchingDefenderGoesToCrouchingHitstun()
    {
        MatchState state = P1Attacks(Low, At(30_000, -1, Crouch));

        _stages.Run(ref state);

        Assert.Equal((FighterAction.CrouchHitstun, Crouch), (state.Fighters[1].Action, state.Fighters[1].Move));
    }

    [Fact]
    public void AnAirborneDefenderGoesToAirHitstun()
    {
        MatchState state = P1Attacks(Mid, At(30_000, -1) with { Action = FighterAction.Jump, IsAirborne = 1 });

        _stages.Run(ref state);

        Assert.Equal(FighterAction.AirHitstun, state.Fighters[1].Action);
    }

    [Fact]
    public void AKnockdownHitKnocksAGroundedDefenderDown()
    {
        MatchState state = P1Attacks(Knockdown, At(30_000, -1));

        _stages.Run(ref state);

        Assert.Equal((FighterAction.Knockdown, 0), (state.Fighters[1].Action, state.Fighters[1].StunFramesLeft));
    }

    // Standard rule: a KO always knocks down, whatever the hit's reaction.
    [Fact]
    public void AHitThatKnocksOutAGroundedDefenderKnocksItDown()
    {
        MatchState state = P1Attacks(Mid, At(30_000, -1) with { Health = MidHit.Damage });

        _stages.Run(ref state);

        Assert.Equal((0, FighterAction.Knockdown), (state.Fighters[1].Health, state.Fighters[1].Action));
    }

    [Fact]
    public void AnAirborneDefenderKnockedOutStillFallsInAirHitstun()
    {
        MatchState state = P1Attacks(
            Mid, At(30_000, -1) with { Action = FighterAction.Jump, IsAirborne = 1, Health = MidHit.Damage });

        _stages.Run(ref state);

        Assert.Equal(FighterAction.AirHitstun, state.Fighters[1].Action);
    }

    [Fact]
    public void AChipKnockOutKnocksTheBlockingDefenderDown()
    {
        MatchState state = P1Attacks(Mid, At(30_000, -1) with { Health = MidHit.ChipDamage }, _back);

        _stages.Run(ref state);

        Assert.Equal((0, FighterAction.Knockdown), (state.Fighters[1].Health, state.Fighters[1].Action));
    }

    [Fact]
    public void ABlockCostsChipDamageAndPutsTheDefenderInBlockstun()
    {
        MatchState state = P1Attacks(Mid, At(30_000, -1), _back);

        _stages.Run(ref state);

        FighterState defender = state.Fighters[1];
        Assert.Equal(Health - MidHit.ChipDamage, defender.Health);
        Assert.Equal((FighterAction.Blockstun, MidHit.Blockstun), (defender.Action, defender.StunFramesLeft));
        Assert.Equal(CancelOn.Block, state.Fighters[0].MoveOutcome);
        Assert.Equal(MidHit.Hitstop, state.Fighters[0].Timing.HitstopFramesLeft);
        Assert.Equal(MidHit.Hitstop, defender.Timing.HitstopFramesLeft);
    }

    [Fact]
    public void ACrouchBlockGoesToCrouchingBlockstun()
    {
        MatchState state = P1Attacks(Low, At(30_000, -1, Crouch), _downBack);

        _stages.Run(ref state);

        Assert.Equal((FighterAction.CrouchBlockstun, Crouch), (state.Fighters[1].Action, state.Fighters[1].Move));
    }

    [Theory]
    [InlineData(Low, false)]
    [InlineData(Overhead, true)]
    [InlineData(Unblockable, false)]
    public void StandBlockStopsOnlyWhatItGuards(int move, bool isBlocked)
    {
        MatchState state = P1Attacks(move, At(30_000, -1), _back);

        _stages.Run(ref state);

        Assert.Equal(isBlocked ? FighterAction.Blockstun : FighterAction.Hitstun, state.Fighters[1].Action);
    }

    [Fact]
    public void AnOverheadHitsThroughACrouchBlock()
    {
        MatchState state = P1Attacks(Overhead, At(30_000, -1, Crouch), _downBack);

        _stages.Run(ref state);

        Assert.Equal(FighterAction.CrouchHitstun, state.Fighters[1].Action);
        Assert.Equal(Health - OverheadHit.Damage, state.Fighters[1].Health);
    }

    [Fact]
    public void HealthStopsAtZero()
    {
        MatchState hit = P1Attacks(HeavyMid, At(30_000, -1) with { Health = 100 });
        MatchState chip = P1Attacks(Mid, At(30_000, -1) with { Health = 10 }, _back);

        _stages.Run(ref hit);
        _stages.Run(ref chip);

        Assert.Equal(0, hit.Fighters[1].Health);
        Assert.Equal(0, chip.Fighters[1].Health);
    }

    [Fact]
    public void HitsAndBlocksEmitTheirEventWithTheAttackerContactPointAndMove()
    {
        MatchState hit = P1Attacks(Mid, At(30_000, -1));
        MatchState block = P1Attacks(Overhead, At(30_000, -1), _back);

        _stages.Run(ref hit);
        _stages.Run(ref block);

        FrameEvent hitEvent = Assert.Single(hit.Events.OfFrame(Frame).ToArray());
        Assert.Equal((FrameEventKind.Hit, 0, Mid), (hitEvent.Kind, hitEvent.Fighter, hitEvent.Detail));
        Assert.Equal(new IntVector2(17_500, 80_000), hitEvent.Position);
        FrameEvent blockEvent = Assert.Single(block.Events.OfFrame(Frame).ToArray());
        Assert.Equal((FrameEventKind.Block, 0, Overhead), (blockEvent.Kind, blockEvent.Fighter, blockEvent.Detail));
    }

    [Fact]
    public void HitAndBlockHooksSeeTheContact()
    {
        MatchState hit = P1Attacks(Mid, At(30_000, -1));
        MatchState block = P1Attacks(Mid, At(30_000, -1), _back);

        _stages.Run(ref hit);
        _stages.Run(ref block);

        CountingState onHit = CombatStages.Hooks(ref hit);
        Assert.Equal((1, 0, 0, 1), (onHit.OnHit, onHit.OnBlock, onHit.LastAttacker, onHit.LastDefender));
        CountingState onBlock = CombatStages.Hooks(ref block);
        Assert.Equal((0, 1), (onBlock.OnHit, onBlock.OnBlock));
    }

    [Fact]
    public void NoContactChangesNothing()
    {
        MatchState state = P1Attacks(Mid, At(90_000, -1));
        MatchState before = state;

        _stages.Run(ref state);

        Assert.Equal(StateChecksum.Compute(in before), StateChecksum.Compute(in state));
    }

    [Fact]
    public void AHitDuringTheDefendersStartupIsACounterHitWithTheBonusForBoth()
    {
        CombatStages stages = new(counterHit: new CounterBonus { ExtraHitstun = 2, ExtraHitstop = 3, ExtraDamagePercent = 20 });
        MatchState state = P1Attacks(Mid, At(30_000, -1, Low, moveFrame: 1));

        stages.Run(ref state);

        FighterState defender = state.Fighters[1];
        Assert.Equal((Health - 360, MidHit.Hitstun + 2), (defender.Health, defender.StunFramesLeft));
        Assert.Equal(MidHit.Hitstop + 3, state.Fighters[0].Timing.HitstopFramesLeft);
        Assert.Equal(MidHit.Hitstop + 3, defender.Timing.HitstopFramesLeft);
        FrameEvent[] events = state.Events.OfFrame(Frame).ToArray();
        Assert.Equal(
            [(FrameEventKind.Hit, 0, Mid), (FrameEventKind.CounterHit, 0, 0)],
            Array.ConvertAll(events, frameEvent => (frameEvent.Kind, frameEvent.Fighter, frameEvent.Detail)));
        Assert.Equal(events[0].Position, events[1].Position);
    }

    [Fact]
    public void AHitDuringTheDefendersRecoveryIsAPunishCounter()
    {
        CombatStages stages = new(punishCounter: new CounterBonus { ExtraHitstun = 4 });
        MatchState state = P1Attacks(Mid, At(30_000, -1, Low, moveFrame: 6));

        stages.Run(ref state);

        Assert.Equal(MidHit.Hitstun + 4, state.Fighters[1].StunFramesLeft);
        Assert.Equal(
            [FrameEventKind.Hit, FrameEventKind.PunishCounter],
            Array.ConvertAll(state.Events.OfFrame(Frame).ToArray(), frameEvent => frameEvent.Kind));
    }

    [Fact]
    public void ABlockIsNeverACounterHit()
    {
        CombatStages stages = new(counterHit: new CounterBonus { ExtraHitstun = 2 });
        MatchState state = P1Attacks(Mid, At(30_000, -1), _back);

        stages.Run(ref state);

        FrameEvent block = Assert.Single(state.Events.OfFrame(Frame).ToArray());
        Assert.Equal(FrameEventKind.Block, block.Kind);
    }

    [Fact]
    public void UnderTheTradeRuleBothSameFrameHitsLandAndTheLongerHitstopFreezesBoth()
    {
        MatchState state = BothAttack(Mid, HeavyMid);

        _stages.Run(ref state);

        Assert.Equal(Health - HeavyMidHit.Damage, state.Fighters[0].Health);
        Assert.Equal(Health - MidHit.Damage, state.Fighters[1].Health);
        Assert.Equal(FighterAction.Hitstun, state.Fighters[0].Action);
        Assert.Equal(FighterAction.Hitstun, state.Fighters[1].Action);
        Assert.Equal(HeavyMidHit.Hitstop, state.Fighters[0].Timing.HitstopFramesLeft);
        Assert.Equal(HeavyMidHit.Hitstop, state.Fighters[1].Timing.HitstopFramesLeft);
        // Each hit caught the other in its active frames: a hit and a counter hit event each.
        Assert.Equal(4, state.Events.OfFrame(Frame).Length);
        Assert.Equal(2, CombatStages.Hooks(ref state).OnHit);
    }

    [Fact]
    public void UnderTheClashRuleNeitherSameFrameHitLandsAndBothMovesAreSpent()
    {
        CombatStages stages = new(TradeRule.Clash);
        MatchState state = BothAttack(Mid, HeavyMid);

        stages.Run(ref state);

        for (int fighter = 0; fighter < MatchState.PlayerCount; fighter++)
        {
            FighterState after = state.Fighters[fighter];
            Assert.Equal(
                (Health, FighterAction.Attack, 0), (after.Health, after.Action, after.Timing.HitstopFramesLeft));
            Assert.Equal(CancelOn.Clash, after.MoveOutcome);
        }

        FrameEvent[] events = state.Events.OfFrame(Frame).ToArray();
        Assert.Equal(
            [(FrameEventKind.Clash, 0, Mid), (FrameEventKind.Clash, 1, HeavyMid)],
            Array.ConvertAll(events, frameEvent => (frameEvent.Kind, frameEvent.Fighter, frameEvent.Detail)));
        Assert.Equal(0, CombatStages.Hooks(ref state).OnHit);

        stages.Run(ref state);
        Assert.Equal(Health, state.Fighters[0].Health);
    }

    [Fact]
    public void UnderTheClashRuleASingleHitStillLands()
    {
        CombatStages stages = new(TradeRule.Clash);
        MatchState state = P1Attacks(Mid, At(30_000, -1));

        stages.Run(ref state);

        Assert.Equal(FighterAction.Hitstun, state.Fighters[1].Action);
    }

    [Theory]
    [InlineData(TradeRule.Trade)]
    [InlineData(TradeRule.Clash)]
    public void SwappingAndMirroringThePlayersMirrorsTheResult(TradeRule tradeRule)
    {
        CombatStages stages = new(tradeRule);
        MatchState original = BothAttack(Mid, HeavyMid);
        original.Fighters[1].Health = 4_000;
        MatchState swapped = default;
        swapped.Frame = Frame;
        swapped.Fighters[0] = Mirror(original.Fighters[1]);
        swapped.Fighters[1] = Mirror(original.Fighters[0]);

        stages.Run(ref original);
        stages.Run(ref swapped);

        Assert.Equal(Mirror(original.Fighters[0]), swapped.Fighters[1]);
        Assert.Equal(Mirror(original.Fighters[1]), swapped.Fighters[0]);
    }

    [Fact]
    public void ResolvingHitsBlocksAndTradesAllocatesNothing()
    {
        MatchState hit = P1Attacks(Mid, At(30_000, -1));
        MatchState block = P1Attacks(Mid, At(30_000, -1), _back);
        MatchState trade = BothAttack(Mid, Mid);
        RunAll(hit, block, trade);

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 1000; i++)
        {
            RunAll(hit, block, trade);
        }

        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
    }

    private void RunAll(MatchState hit, MatchState block, MatchState trade)
    {
        _stages.Run(ref hit);
        _stages.Run(ref block);
        _stages.Run(ref trade);
    }

    private static MatchState P1Attacks(int move, FighterState defender, InputWord defenderInput = default)
    {
        MatchState state = default;
        state.Frame = Frame;
        state.Fighters[0] = Attacking(-30_000, 1, move);
        state.Fighters[1] = defender;
        state.Inputs[1].Record(defenderInput);
        return state;
    }

    private static MatchState BothAttack(int player1Move, int player2Move)
    {
        MatchState state = default;
        state.Frame = Frame;
        state.Fighters[0] = Attacking(-30_000, 1, player1Move);
        state.Fighters[1] = Attacking(30_000, -1, player2Move);
        return state;
    }

    private static FighterState Mirror(FighterState fighter) => fighter with
    {
        Position = new IntVector2(-fighter.Position.X, fighter.Position.Y),
        Velocity = new IntVector2(-fighter.Velocity.X, fighter.Velocity.Y),
        Facing = -fighter.Facing,
    };
}
