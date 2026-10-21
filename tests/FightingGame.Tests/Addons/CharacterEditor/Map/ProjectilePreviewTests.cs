using System.Text.Json.Nodes;
using FightingGame.Addons.CharacterEditor.Documents;
using FightingGame.Addons.CharacterEditor.Map;
using Xunit;

namespace FightingGame.Tests.Addons.CharacterEditor.Map;

/// <summary>The editor's preview shows a move's projectile where the match moves it.</summary>
public sealed class ProjectilePreviewTests
{
    [Fact]
    public void ShowsWhereItWillAppearBeforeItSpawns()
    {
        ProjectileOnFrame shown = Assert.Single(ProjectilePreview.Of(Fireball(), 10));

        Assert.False(shown.HasSpawned);
        Assert.Equal((45000, 55000), (shown.X, shown.Y));
        Assert.Equal([new BoxArea(30000, 55000, 30000, 30000)], shown.Boxes);
    }

    [Theory]
    [InlineData(33, 1, 45000)]
    [InlineData(34, 2, 58000)]
    [InlineData(43, 11, 175000)]
    public void AppearsOnItsOffsetThenFliesOnByItsSpeed(int frame, int life, int x)
    {
        // Like the match: a projectile doesn't move on its spawn frame.
        ProjectileOnFrame shown = Assert.Single(ProjectilePreview.Of(Fireball(), frame));

        Assert.Equal(life, shown.LifeFrame);
        Assert.Equal(x, shown.X);
        Assert.Equal(x - 15000, shown.Boxes[0].X);
    }

    [Fact]
    public void IsGoneAfterItsLifetime()
    {
        Assert.Single(ProjectilePreview.Of(Fireball(lifetime: 5), 37));
        Assert.Empty(ProjectilePreview.Of(Fireball(lifetime: 5), 38));
    }

    [Fact]
    public void MovesItsSpawnPoint()
    {
        JsonObject move = Fireball();
        JsonObject projectile = ProjectileFields.All(move)[0];

        ProjectileFields.SetOffset(projectile, 50000, 60000);

        Assert.Equal((50000, 60000), ProjectileFields.OffsetOf(projectile));
        Assert.Equal("""{"spawnFrame":33,"offset":{"x":50000,"y":60000},"velocity":{"x":13000},"lifetime":120,"boxes":[{"firstFrame":1,"lastFrame":120,"x":-15000,"y":0,"width":30000,"height":30000}]}""",
            projectile.ToJsonString());
    }

    // 236c: spawns on frame 33 at (45000, 55000), 13 px a frame forward, a 30-pixel box centred on it.
    private static JsonObject Fireball(int lifetime = 120) => (JsonObject)JsonNode.Parse($$"""
        {
          "id": "236c",
          "frameData": { "startup": 33, "active": 1, "recovery": 32 },
          "projectiles": [
            {
              "spawnFrame": 33, "offset": { "x": 45000, "y": 55000 }, "velocity": { "x": 13000 }, "lifetime": {{lifetime}},
              "boxes": [{ "firstFrame": 1, "lastFrame": 120, "x": -15000, "y": 0, "width": 30000, "height": 30000 }]
            }
          ]
        }
        """)!;
}
