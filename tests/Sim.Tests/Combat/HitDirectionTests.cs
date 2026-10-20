using Sim.Combat;
using Sim.Math;
using Sim.State;
using Xunit;

namespace Sim.Tests.Combat;

public sealed class HitDirectionTests
{
    [Theory]
    [InlineData(10_000, 0, 1, 1)]
    [InlineData(-10_000, 0, 1, -1)]  // crossup: behind an attacker still facing right
    [InlineData(-10_000, 0, -1, -1)]
    [InlineData(10_000, 0, -1, 1)]   // crossup: behind an attacker still facing left
    [InlineData(0, 0, 1, 1)]         // same x: the attacker's facing
    [InlineData(0, 0, -1, -1)]
    public void AFightersHitSendsTheDefenderAwayFromWhereTheAttackerStands(
        int defenderX, int attackerX, int attackerFacing, int expected)
    {
        FighterState defender = new() { Position = new IntVector2(defenderX, 0), Facing = -attackerFacing };
        FighterState attacker = new() { Position = new IntVector2(attackerX, 0), Facing = attackerFacing };

        Assert.Equal(expected, HitDirection.AwayFromAttacker(in defender, in attacker));
    }

    [Theory]
    [InlineData(8_000, 1, 1)]
    [InlineData(-8_000, -1, -1)]
    [InlineData(-8_000, 1, -1)]  // flying back against its facing (a returning projectile)
    [InlineData(0, 1, 1)]        // not moving sideways: its facing
    [InlineData(0, -1, -1)]
    public void AProjectileSendsTheDefenderAlongItsFlight(int speedX, int facing, int expected)
    {
        EntityState projectile = new() { Velocity = new IntVector2(speedX, 0), Facing = facing };

        Assert.Equal(expected, HitDirection.AlongFlight(in projectile));
    }
}
