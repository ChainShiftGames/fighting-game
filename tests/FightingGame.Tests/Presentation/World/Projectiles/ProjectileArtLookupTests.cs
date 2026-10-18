using System.Collections.Generic;
using FightingGame.Presentation.World.Projectiles;
using Xunit;

namespace FightingGame.Tests.Presentation.World.Projectiles;

public sealed class ProjectileArtLookupTests
{
    [Fact]
    public void GivesEachProjectileTheArtOfTheMoveThatSpawnsIt()
    {
        var fireball = new ProjectileArtSettings("dark_fireball", "dark_fireball_charge", "dark_fireball_impact");
        var byMove = new Dictionary<string, ProjectileArtSettings> { ["236c"] = fireball };

        ProjectileArtSettings?[] arts = ProjectileArtLookup.Match(["236c", "214c", "236c"], byMove);

        Assert.Equal([fireball, null, fireball], arts);
    }

    [Fact]
    public void GivesNoArtWithoutAnArtFile()
    {
        Assert.Equal([null], ProjectileArtLookup.Match(["236c"], null));
    }
}
