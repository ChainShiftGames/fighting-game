using System.Collections.Generic;
using System.Text.Json.Nodes;
using FightingGame.Addons.CharacterEditor.Documents;

namespace FightingGame.Addons.CharacterEditor.Map;

/// <summary>
/// The projectile settings the drawer edits directly: the frame it appears on, its speed forward and up (subpixels
/// per frame), how long it lasts and how many clashes it survives. Written in place; validation checks the values.
/// </summary>
public static class ProjectileFields
{
    private const string Offset = "offset";
    private const string X = "x";
    private const string Y = "y";
    private const string Lifetime = "lifetime";
    private const string Durability = "durability";

    /// <summary>The schema's durability when the field is left out.</summary>
    public const int DefaultDurability = 1;

    /// <summary>The move's projectiles; entries that aren't objects are skipped (validation reports them).</summary>
    public static IReadOnlyList<JsonObject> All(JsonObject move)
    {
        List<JsonObject> projectiles = [];
        foreach (JsonNode? item in move[MoveFields.Projectiles] as JsonArray ?? [])
        {
            if (item is JsonObject projectile)
            {
                projectiles.Add(projectile);
            }
        }

        return projectiles;
    }

    public static int? SpawnFrame(JsonObject projectile) => JsonNodeValue.Int(projectile[MoveFields.SpawnFrame]);

    public static void SetSpawnFrame(JsonObject projectile, int frame) => projectile[MoveFields.SpawnFrame] = frame;

    public static int? SpeedForward(JsonObject projectile) => JsonNodeValue.Int(projectile[MoveFields.Velocity]?[X]);

    /// <summary>Upward speed; the schema's default 0 when left out.</summary>
    public static int SpeedUp(JsonObject projectile) => JsonNodeValue.Int(projectile[MoveFields.Velocity]?[Y]) ?? 0;

    public static void SetSpeedForward(JsonObject projectile, int subpixelsPerFrame) =>
        Velocity(projectile)[X] = subpixelsPerFrame;

    /// <summary>Writes the upward speed; 0 removes the field, the way hand-written files leave it out.</summary>
    public static void SetSpeedUp(JsonObject projectile, int subpixelsPerFrame)
    {
        JsonObject velocity = Velocity(projectile);
        if (subpixelsPerFrame == 0)
        {
            velocity.Remove(Y);
        }
        else
        {
            JsonFieldOrder.Set(velocity, Y, subpixelsPerFrame, [X, Y]);
        }
    }

    public static int? LifetimeOf(JsonObject projectile) => JsonNodeValue.Int(projectile[Lifetime]);

    public static void SetLifetime(JsonObject projectile, int frames) => projectile[Lifetime] = frames;

    public static int DurabilityOf(JsonObject projectile) =>
        JsonNodeValue.Int(projectile[Durability]) ?? DefaultDurability;

    public static void SetDurability(JsonObject projectile, int durability) => projectile[Durability] = durability;

    /// <summary>Where it appears, forward and up from the fighter's origin; null when the file can't say.</summary>
    public static (int X, int Y)? OffsetOf(JsonObject projectile) =>
        JsonNodeValue.Int(projectile[Offset]?[X]) is int x && JsonNodeValue.Int(projectile[Offset]?[Y]) is int y
            ? (x, y)
            : null;

    /// <summary>Moves where it appears, keeping the offset's place in the file.</summary>
    public static void SetOffset(JsonObject projectile, int x, int y)
    {
        if (projectile[Offset] is not JsonObject offset)
        {
            offset = [];
            projectile[Offset] = offset;
        }

        offset[X] = x;
        offset[Y] = y;
    }

    private static JsonObject Velocity(JsonObject projectile)
    {
        if (projectile[MoveFields.Velocity] is not JsonObject velocity)
        {
            velocity = new JsonObject { [X] = 0 };
            projectile[MoveFields.Velocity] = velocity;
        }

        return velocity;
    }
}
