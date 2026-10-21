using System.Collections.Generic;
using System.Text.Json.Nodes;
using FightingGame.Addons.CharacterEditor.Documents;

namespace FightingGame.Addons.CharacterEditor.Map;

/// <summary>
/// The move's projectiles on one of its frames, for the editor's preview: each from the move's first frame until its
/// lifetime ends, positioned as the match moves it (the fighter standing still at the origin).
/// </summary>
public static class ProjectilePreview
{
    /// <summary>Projectiles whose spawn frame, offset, speed or lifetime can't be read are left out (validation reports them).</summary>
    public static IReadOnlyList<ProjectileOnFrame> Of(JsonObject move, int frame)
    {
        var shown = new List<ProjectileOnFrame>();
        IReadOnlyList<JsonObject> projectiles = ProjectileFields.All(move);
        for (int index = 0; index < projectiles.Count; index++)
        {
            JsonObject projectile = projectiles[index];
            if (ProjectileFields.SpawnFrame(projectile) is not int spawn || ProjectileFields.OffsetOf(projectile) is not var (x, y)
                || ProjectileFields.SpeedForward(projectile) is not int speed || ProjectileFields.LifetimeOf(projectile) is not int lifetime)
            {
                continue;
            }

            int life = frame < spawn ? 0 : frame - spawn + 1;
            if (life > lifetime)
            {
                continue;
            }

            // Spawned at its offset with frame 1; each later frame moves it by its speed before anything else.
            int moved = life > 1 ? life - 1 : 0;
            int atX = x + (speed * moved);
            int atY = y + (ProjectileFields.SpeedUp(projectile) * moved);
            shown.Add(new ProjectileOnFrame(projectile, index, spawn, life, atX, atY, Boxes(projectile, life, atX, atY)));
        }

        return shown;
    }

    private static List<BoxArea> Boxes(JsonObject projectile, int life, int x, int y)
    {
        var boxes = new List<BoxArea>();
        foreach (JsonObject box in MoveBoxes.OnFrame(projectile, life < 1 ? 1 : life))
        {
            if (BoxArea.Of(box) is BoxArea area)
            {
                boxes.Add(area with { X = area.X + x, Y = area.Y + y });
            }
        }

        return boxes;
    }
}
