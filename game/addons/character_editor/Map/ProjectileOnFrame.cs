using System.Collections.Generic;
using System.Text.Json.Nodes;
using FightingGame.Addons.CharacterEditor.Documents;

namespace FightingGame.Addons.CharacterEditor.Map;

/// <summary>
/// Where one of a move's projectiles is on a frame of the move, facing right from the fighter's origin, as the match
/// moves it: at its offset on the spawn frame, then on by its speed each frame.
/// </summary>
/// <param name="Index">Its place in the move's <c>projectiles</c> list.</param>
/// <param name="LifeFrame">Frame of its life (1 on the spawn frame); 0 before it spawns, when it is shown where it will appear.</param>
/// <param name="X">Its position, in subpixels.</param>
/// <param name="Boxes">Its hitboxes on that frame (its first frame's before it spawns), moved to its position.</param>
public sealed record ProjectileOnFrame(
    JsonObject Projectile, int Index, int SpawnFrame, int LifeFrame, int X, int Y, IReadOnlyList<BoxArea> Boxes)
{
    public bool HasSpawned => LifeFrame > 0;
}
