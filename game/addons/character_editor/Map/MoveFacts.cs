using System.Collections.Generic;
using System.Text.Json.Nodes;
using FightingGame.Addons.CharacterEditor.Documents;

namespace FightingGame.Addons.CharacterEditor.Map;

/// <summary>
/// What a move does, in the terms the map's drawer explains it: when it comes out, its damage and stun, and who
/// moves first afterwards. Worked out from the file, so it follows every edit; null where the file can't say.
/// </summary>
/// <remarks>
/// Advantage is standard frame data, as the frame advantage scenarios check it: stun minus the attacker's frames left
/// after the hit, for the last hit landing on its first frame. Projectile and knockdown advantage depend on distance
/// or the opponent's wake-up, so they are null.
/// </remarks>
public sealed record MoveFacts(
    FramePhases? Phases,
    int? ComesOutOn,
    int? Damage,
    int? Hitstun,
    int? Blockstun,
    int? OnHit,
    int? OnBlock,
    bool HitsFromProjectile,
    IReadOnlySet<int> HitFrames)
{
    private const string HitBoxKind = "hit";
    private const string ThrowBoxKind = "throw";
    private const string HitField = "hit";
    private const string DamageField = "damage";
    private const string HitstunField = "hitstun";
    private const string BlockstunField = "blockstun";

    // A hit with any of these ends in a knockdown or a juggle, not plain hitstun.
    private static readonly string[] _notPlainHitstun = ["reaction", "launch", "airLaunch", "groundBounce", "wallBounce"];

    public static MoveFacts Of(JsonObject move)
    {
        FramePhases? phases = FramePhases.Of(move);
        IReadOnlyList<JsonObject> hits = Objects(move[MoveFields.Hits]);
        HashSet<int> hitFrames = HitBoxFrames(move);
        if (hits.Count > 0 && phases is { IsBasic: false })
        {
            return Melee(phases, hits, hitFrames, JsonNodeValue.Text(move[MoveFields.Category]) == "throw");
        }

        IReadOnlyList<JsonObject> projectiles = Objects(move[MoveFields.Projectiles]);
        if (projectiles.Count > 0)
        {
            int? spawn = null;
            int damage = 0;
            foreach (JsonObject projectile in projectiles)
            {
                int? frame = JsonNodeValue.Int(projectile[MoveFields.SpawnFrame]);
                spawn = spawn is int earlier && frame is int later ? System.Math.Min(earlier, later) : spawn ?? frame;
                damage += JsonNodeValue.Int(projectile[HitField]?[DamageField]) ?? 0;
            }

            JsonNode? first = projectiles[0][HitField];
            return new MoveFacts(phases, spawn, damage, JsonNodeValue.Int(first?[HitstunField]),
                JsonNodeValue.Int(first?[BlockstunField]), null, null, HitsFromProjectile: true, hitFrames);
        }

        return new MoveFacts(phases, null, null, null, null, null, null, HitsFromProjectile: false, hitFrames);
    }

    private static MoveFacts Melee(FramePhases phases, IReadOnlyList<JsonObject> hits, HashSet<int> hitFrames, bool isThrow)
    {
        int damage = 0;
        foreach (JsonObject hit in hits)
        {
            damage += JsonNodeValue.Int(hit[DamageField]) ?? 0;
        }

        JsonObject last = hits[^1];
        int lastHitFrame = JsonNodeValue.Int(last[MoveFields.FirstFrame]) ?? phases.FirstActiveFrame;
        int framesLeft = phases.TotalFrames - lastHitFrame;
        int? hitstun = JsonNodeValue.Int(last[HitstunField]);
        int? blockstun = isThrow ? null : JsonNodeValue.Int(last[BlockstunField]);
        bool plain = true;
        foreach (string field in _notPlainHitstun)
        {
            plain &= !last.ContainsKey(field);
        }

        return new MoveFacts(
            phases,
            JsonNodeValue.Int(hits[0][MoveFields.FirstFrame]) ?? phases.FirstActiveFrame,
            damage,
            hitstun,
            blockstun,
            plain && !isThrow ? hitstun - framesLeft : null,
            blockstun - framesLeft,
            HitsFromProjectile: false,
            hitFrames);
    }

    private static HashSet<int> HitBoxFrames(JsonObject move)
    {
        HashSet<int> frames = [];
        foreach (JsonObject box in MoveBoxes.All(move))
        {
            string? kind = JsonNodeValue.Text(box[MoveFields.Kind]);
            if ((kind == HitBoxKind || kind == ThrowBoxKind) && MoveBoxes.Range(box) is (int first, int last))
            {
                for (int frame = first; frame <= last; frame++)
                {
                    frames.Add(frame);
                }
            }
        }

        return frames;
    }

    private static List<JsonObject> Objects(JsonNode? list)
    {
        List<JsonObject> objects = [];
        foreach (JsonNode? item in list as JsonArray ?? [])
        {
            if (item is JsonObject obj)
            {
                objects.Add(obj);
            }
        }

        return objects;
    }
}
