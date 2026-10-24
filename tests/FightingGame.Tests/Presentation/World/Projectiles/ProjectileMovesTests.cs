using FightingGame.Presentation.World.Projectiles;
using Xunit;

namespace FightingGame.Tests.Presentation.World.Projectiles;

public sealed class ProjectileMovesTests
{
    [Fact]
    public void NamesTheMoveThatSpawnsEachProjectile()
    {
        string[] moves = ProjectileMoves.Of(TestGameData.Testman);

        // Testman's projectiles: 236C's fireball and 214A/B/C's floor fires, in the character's move order.
        Assert.Equal(["236c", "214a", "214b", "214c"], moves);
    }
}
