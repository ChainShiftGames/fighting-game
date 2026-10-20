using Sim.Fighters;
using Sim.Math;
using Sim.State;
using Xunit;

namespace Sim.Tests.Fighters;

public sealed class FaceOpponentTests
{
    [Theory]
    [InlineData(0, 1, 50_000, 1)]
    [InlineData(0, 1, -50_000, -1)]
    [InlineData(0, -1, 50_000, 1)]
    [InlineData(0, -1, -50_000, -1)]
    [InlineData(0, 1, 0, 1)]    // same x: keeps its facing
    [InlineData(0, -1, 0, -1)]
    public void AFighterTurnsTowardItsOpponentByPosition(int x, int facing, int opponentX, int expected)
    {
        FighterState fighter = new() { Position = new IntVector2(x, 0), Facing = facing };
        FighterState opponent = new() { Position = new IntVector2(opponentX, 0), Facing = 1 };

        FaceOpponent.Turn(ref fighter, in opponent);

        Assert.Equal(expected, fighter.Facing);
        Assert.Equal(expected, FaceOpponent.FacingFor(in fighter, in opponent));
    }
}
