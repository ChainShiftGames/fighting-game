using System;
using Netcode.Bots;
using Netcode.Tests.Matches;
using Sim.Characters;
using Sim.Fighters.States;
using Sim.Math;
using Sim.State;
using Xunit;

namespace Netcode.Tests.Bots;

public sealed class MatchObserverTests
{
    private readonly CharacterData _testman = BotMatch.Character;
    private MatchState _state = TestmanMirror.Create().StartState();

    [Fact]
    public void EachPlayerSeesItselfAsSelf()
    {
        _state.Fighters[0].Health = 111;
        _state.Fighters[1].Health = 222;

        BotObservation player1 = Observe(0);
        BotObservation player2 = Observe(1);

        Assert.Equal(111, player1.Self.Health);
        Assert.Equal(222, player1.Opponent.Health);
        Assert.Equal(222, player2.Self.Health);
        Assert.Equal(_state.Fighters[0].Position.X, player2.Opponent.X);
        Assert.True(player1.IsFighting);
    }

    [Theory]
    [InlineData(4, MovePhase.Startup)]
    [InlineData(5, MovePhase.Active)]
    [InlineData(9, MovePhase.Recovery)]
    public void AnAttackShowsItsPhase(int moveFrame, MovePhase phase)
    {
        Attack(1, "5a", moveFrame);

        FighterView opponent = Observe(0).Opponent;

        Assert.Equal(FighterPose.Attacking, opponent.Pose);
        Assert.Equal(phase, opponent.AttackPhase);
    }

    [Fact]
    public void ALowShowsAsLow()
    {
        Attack(1, "2b", 1);

        Assert.Equal(Guard.Low, Observe(0).Opponent.AttackGuard);
    }

    [Theory]
    [InlineData(FighterAction.CrouchHitstun, FighterPose.Hitstun)]
    [InlineData(FighterAction.CrouchBlockstun, FighterPose.Blockstun)]
    [InlineData(FighterAction.WalkBack, FighterPose.Walking)]
    [InlineData(FighterAction.WakeUp, FighterPose.KnockedDown)]
    [InlineData(FighterAction.Prejump, FighterPose.Jumping)]
    public void ActionsShowAsTheirPose(FighterAction action, FighterPose pose)
    {
        _state.Fighters[1].Action = action;

        FighterView opponent = Observe(0).Opponent;

        Assert.Equal(pose, opponent.Pose);
        Assert.Equal(MovePhase.None, opponent.AttackPhase);
    }

    [Fact]
    public void EveryActionHasAPose()
    {
        foreach (FighterAction action in Enum.GetValues<FighterAction>())
        {
            _state.Fighters[1].Action = action;
            _state.Fighters[1].Move = _testman.BasicStateMove(BasicState.Idle);
            Observe(0);
        }
    }

    [Fact]
    public void OnlyTheOpponentsProjectilesFlyingTowardsTheBotCount()
    {
        int botX = _state.Fighters[0].Position.X;
        int towardBot = Math.Sign(botX - _state.Fighters[1].Position.X);
        Spawn(EntityPool.FirstSlotOf(1), botX - (towardBot * 90_000), towardBot);
        Spawn(EntityPool.FirstSlotOf(1) + 1, botX - (towardBot * 30_000), -towardBot);
        Spawn(EntityPool.FirstSlotOf(0), botX - (towardBot * 10_000), towardBot);

        Assert.Equal(90_000, Observe(0).IncomingProjectileDistance);
    }

    [Fact]
    public void NoProjectileIsNoThreat()
    {
        Assert.Equal(BotObservation.NoProjectile, Observe(0).IncomingProjectileDistance);
    }

    [Fact]
    public void ARoundOverIsNotFighting()
    {
        _state.Round.Phase = RoundPhase.RoundOver;

        Assert.False(Observe(0).IsFighting);
    }

    private BotObservation Observe(int player) => MatchObserver.Observe(in _state, player, _testman, _testman);

    private void Attack(int player, string move, int moveFrame)
    {
        _state.Fighters[player].Action = FighterAction.Attack;
        _state.Fighters[player].Move = _testman.MoveIds.IndexOf(move);
        _state.Fighters[player].MoveFrame = moveFrame;
    }

    private void Spawn(int slot, int x, int direction)
    {
        _state.Entities.Slots[slot] = new EntityState
        {
            IsActive = 1,
            Owner = slot / EntityPool.MaxPerPlayer,
            Position = new IntVector2(x, 0),
            Velocity = new IntVector2(direction * 6000, 0),
        };
    }
}
