using Netcode.Bots;
using Sim.Characters;
using Sim.State;
using Xunit;

namespace Netcode.Tests.Bots;

public sealed class MoveRepertoireTests
{
    // Testman's standing hurtbox reaches 25,000 in front of its origin; 5a's hitbox ends 60,000 in front of Testman's.
    private const int FiveAReach = 85_000;

    private readonly MoveRepertoire _moves = MoveRepertoire.Build(BotMatch.Character, BotMatch.Character);
    private RandomState _random = RandomState.FromSeed(1);

    [Fact]
    public void OnlyAttacksAreInIt()
    {
        foreach (BotMove move in _moves.Moves)
        {
            Assert.NotEqual(0, move.ButtonMask);
            Assert.NotEqual(MoveCategory.Basic, move.Category);
            Assert.True(move.StartupFrames > 0);
        }

        Assert.False(_moves.Moves.IsEmpty);
    }

    [Fact]
    public void ReachCountsTheHitboxAndTheOpponentsBody()
    {
        Assert.Contains(_moves.Moves.ToArray(), move => move.Motion == 5 && move.Reach == FiveAReach);
    }

    [Fact]
    public void AProjectileReachesFullScreen()
    {
        Assert.True(_moves.TryPickProjectile(ref _random, out BotMove projectile));
        Assert.Equal(236, projectile.Motion);
        Assert.Equal(BotMove.FullScreen, projectile.Reach);
    }

    [Fact]
    public void FarAwayOnlyAProjectileReaches()
    {
        for (int pick = 0; pick < 20; pick++)
        {
            Assert.True(_moves.TryPickInReach(1_000_000, MoveStance.Ground, ref _random, out BotMove move));
            Assert.True(move.IsProjectile);
        }

        Assert.False(_moves.TryPickFastest(1_000_000, out _));
    }

    [Fact]
    public void ThePunishIsTheFastestHitNotAThrow()
    {
        Assert.True(_moves.TryPickFastest(50_000, out BotMove punish));

        Assert.NotEqual(MoveCategory.Throw, punish.Category);
        Assert.Equal(5, punish.StartupFrames);
    }

    [Fact]
    public void CloseAndThreatRangesAreTheLongestGroundReach()
    {
        Assert.True(_moves.CloseRange >= FiveAReach);
        Assert.Equal(_moves.CloseRange, _moves.ThreatRange);
    }

    [Fact]
    public void AirAttacksArePickedOnlyInTheAir()
    {
        Assert.True(_moves.TryPickInReach(0, MoveStance.Air, ref _random, out BotMove air));
        Assert.Equal(MoveStance.Air, air.Stance);
    }
}
