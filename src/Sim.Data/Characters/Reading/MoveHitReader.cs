using System.Collections.Generic;
using Sim.Characters;
using Sim.Data.Validation;
using Sim.Math;

namespace Sim.Data.Characters.Reading;

/// <summary>
/// Reads a move's <c>hits</c> list (ADR-0022): each hit's frames, damage, guard, stun, hitstop, pushback, counter hit
/// bonuses and optional launch, bounce, knockdown and OTG data.
/// </summary>
/// <remarks>
/// One hit without frames covers the move's whole active range. With several, each names its frames: in order, not
/// overlapping, inside the active frames; every hit has an attack box on its frames and every attack box frame
/// belongs to a hit, so each box knows which hit it deals.
/// </remarks>
public static class MoveHitReader
{
    /// <summary>Hits a move can have; each takes a bit of <c>FighterState.HitsConnected</c>.</summary>
    public const int MaxHits = 32;

    private const string Field = "hits";

    public static IReadOnlyList<HitData> Read(FieldReader move, IReadOnlyList<BoxData> boxes, MoveFrames frames)
    {
        move.Forbid("hit", "schema 2 lists hits in \"hits\" (run tools/DataMigrate)");
        IReadOnlyList<FieldReader?> entries = move.ObjectArray(Field, required: false);
        bool hasAttackBox = HasAttackBox(boxes);
        if (entries.Count == 0)
        {
            if (hasAttackBox)
            {
                move.Error(Field, "required because the move has hit or throw boxes");
            }

            return [];
        }

        if (!hasAttackBox)
        {
            move.Error(Field, "the move has no hit or throw box for these hits to come from");
        }

        if (entries.Count > MaxHits)
        {
            move.Error(Field, $"a move has at most {MaxHits} hits, got {entries.Count}");
        }

        List<HitData> hits = [];
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i] is { } entry)
            {
                hits.Add(ReadMoveHit(move, entry, i, entries.Count, frames));
            }
        }

        CheckOrder(move, hits);
        CheckBoxes(move, hits, boxes, frames);
        return hits;
    }

    private static HitData ReadMoveHit(FieldReader move, FieldReader entry, int index, int count, MoveFrames frames)
    {
        int first = frames.FirstActive;
        int last = frames.LastActive;
        if (count > 1 || entry.Has("firstFrame") || entry.Has("lastFrame"))
        {
            int low = frames.HasActiveFrames ? frames.FirstActive : 1;
            int high = frames.HasActiveFrames ? frames.LastActive : frames.Total;
            first = entry.RequiredInt("firstFrame", low, high);
            last = entry.RequiredInt("lastFrame", first, high);
        }

        return ReadHit(entry) with { FirstFrame = first, LastFrame = last };
    }

    private static void CheckOrder(FieldReader move, List<HitData> hits)
    {
        for (int i = 1; i < hits.Count; i++)
        {
            if (hits[i].FirstFrame <= hits[i - 1].LastFrame)
            {
                move.Error($"{Field}[{i}]", "hits must be in frame order and must not overlap; this one starts on " +
                    $"frame {hits[i].FirstFrame}, the one before ends on {hits[i - 1].LastFrame}");
            }
        }
    }

    // Every attack box frame needs a hit to deal, and every hit needs a box on its frames to come from.
    private static void CheckBoxes(FieldReader move, List<HitData> hits, IReadOnlyList<BoxData> boxes, MoveFrames frames)
    {
        for (int i = 0; i < hits.Count; i++)
        {
            if (!HasAttackBoxWithin(boxes, hits[i].FirstFrame, hits[i].LastFrame))
            {
                move.Error($"{Field}[{i}]", $"no hit or throw box is active on its frames {hits[i].FirstFrame}-" +
                    $"{hits[i].LastFrame}");
            }
        }

        foreach (BoxData box in boxes)
        {
            if (box.Kind is not (BoxKind.Hit or BoxKind.Throw))
            {
                continue;
            }

            // Frames outside the active ones are reported by the box reader already.
            int first = box.FirstFrame > frames.FirstActive ? box.FirstFrame : frames.FirstActive;
            int last = box.LastFrame < frames.LastActive ? box.LastFrame : frames.LastActive;
            for (int frame = first; frame <= last; frame++)
            {
                if (!AnyCovers(hits, frame))
                {
                    move.Error(Field, $"no hit covers frame {frame}, where a {JsonNameOf(box.Kind)} box is active");
                    break;
                }
            }
        }
    }

    private static bool AnyCovers(List<HitData> hits, int frame)
    {
        foreach (HitData hit in hits)
        {
            if (hit.Covers(frame))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasAttackBoxWithin(IReadOnlyList<BoxData> boxes, int first, int last)
    {
        foreach (BoxData box in boxes)
        {
            if (box.Kind is BoxKind.Hit or BoxKind.Throw && box.FirstFrame <= last && box.LastFrame >= first)
            {
                return true;
            }
        }

        return false;
    }

    private static string JsonNameOf(BoxKind kind) => kind == BoxKind.Throw ? "throw" : "hit";

    /// <summary>Reads one <c>hit</c> object's fields; projectiles use the same format (P1-21).</summary>
    public static HitData ReadHit(FieldReader hit)
    {
        const int MaxFrames = CharacterSchema.MaxMoveFrames;
        int damage = hit.RequiredInt("damage", 0, CharacterSchema.MaxDamage);
        HitData data = new()
        {
            Damage = damage,
            ChipDamage = hit.OptionalInt("chipDamage", 0, 0, damage),
            Guard = hit.RequiredChoice("guard", CharacterJsonNames.Guards),
            Hitstun = hit.RequiredInt("hitstun", 0, MaxFrames),
            Blockstun = hit.RequiredInt("blockstun", 0, MaxFrames),
            Hitstop = hit.RequiredInt("hitstop", 0, MaxFrames),
            Pushback = hit.RequiredInt("pushback", 0, CharacterSchema.MaxDistance),
            Reaction = hit.OptionalChoice("reaction", CharacterJsonNames.Reactions, HitReaction.Hitstun),
            CounterHit = CounterBonusReader.ReadOwn(hit, "counterHit"),
            PunishCounter = CounterBonusReader.ReadOwn(hit, "punishCounter"),
            KnockdownFrames = hit.OptionalInt("knockdownFrames", 0, 1, MaxFrames),
            Otg = hit.OptionalBool("otg", false) ? 1 : 0,
            GroundBounce = ReadGroundBounce(hit),
            WallBounce = ReadVector(hit.OptionalObject("wallBounce"), xMin: 1),
        };
        if (hit.OptionalObject("launch") is { } launch)
        {
            data = data with { HasLaunch = 1, Launch = ReadVector(launch, xMin: -CharacterSchema.MaxDistance) };
        }

        if (hit.OptionalObject("airLaunch") is { } airLaunch)
        {
            data = data with { HasAirLaunch = 1, AirLaunch = ReadVector(airLaunch, xMin: -CharacterSchema.MaxDistance) };
        }

        hit.Finish();
        return data;
    }

    private static int ReadGroundBounce(FieldReader hit)
    {
        if (hit.OptionalObject("groundBounce") is not { } bounce)
        {
            return 0;
        }

        int y = bounce.RequiredInt("y", 1, CharacterSchema.MaxDistance);
        bounce.Finish();
        return y;
    }

    private static IntVector2 ReadVector(FieldReader? vector, int xMin)
    {
        if (vector is null)
        {
            return IntVector2.Zero;
        }

        const int MaxDistance = CharacterSchema.MaxDistance;
        IntVector2 value = new(vector.RequiredInt("x", xMin, MaxDistance), vector.RequiredInt("y", -MaxDistance, MaxDistance));
        vector.Finish();
        return value;
    }

    private static bool HasAttackBox(IReadOnlyList<BoxData> boxes)
    {
        foreach (BoxData box in boxes)
        {
            if (box.Kind is BoxKind.Hit or BoxKind.Throw)
            {
                return true;
            }
        }

        return false;
    }
}
