using FightingGame.Presentation.World.Projectiles;
using Xunit;

namespace FightingGame.Tests.Presentation.World.Projectiles;

public sealed class ProjectileMovesTests
{
    [Fact]
    public void NamesTheMoveThatSpawnsEachProjectile()
    {
        string[] moves = ProjectileMoves.Of(TestGameData.Testman);

        // Testman's only projectile is 236C's fireball.
        Assert.Equal(["236c"], moves);
    }
}
