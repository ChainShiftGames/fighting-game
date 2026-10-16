using System.Collections.Generic;
using Sim.Characters;
using Sim.Data.Validation;
using Sim.Math;

namespace Sim.Data.Characters.Reading;

/// <summary>Reads a move's optional <c>projectiles</c>: spawn frame, offset, velocity, lifetime, boxes, hit.</summary>
/// <remarks>
/// Projectile boxes are all hit boxes, with frames counted from the projectile's spawn (1) to its lifetime. Basic
/// moves can't spawn projectiles: the state machine enters them on its own (hitstun, walking…).
/// </remarks>
public static class MoveProjectileReader
{
    public static IReadOnlyList<ProjectileRecord> Read(FieldReader move, MoveCategory category, MoveFrames frames)
    {
        IReadOnlyList<FieldReader?> entries = move.ObjectArray("projectiles", required: false);
        if (category == MoveCategory.Basic && entries.Count > 0)
        {
            move.Error("projectiles", "basic moves can't spawn projectiles");
        }

        List<ProjectileRecord> projectiles = [];
        foreach (FieldReader? entry in entries)
        {
            if (entry is not null)
            {
                projectiles.Add(ReadProjectile(entry, frames));
            }
        }

        return projectiles;
    }

    private static ProjectileRecord ReadProjectile(FieldReader entry, MoveFrames frames)
    {
        int spawnFrame = entry.RequiredInt("spawnFrame", 1, frames.Total);
        IntVector2 offset = ReadVector(entry.RequiredObject("offset"), requireY: true);
        IntVector2 velocity = ReadVector(entry.RequiredObject("velocity"), requireY: false);
        int lifetime = entry.RequiredInt("lifetime", 1, CharacterSchema.MaxMoveFrames);
        int durability = entry.OptionalInt("durability", 1, 1, CharacterSchema.MaxDurability);
        IReadOnlyList<BoxData> boxes = ReadBoxes(entry, lifetime);
        FieldReader? hit = entry.RequiredObject("hit");
        HitData hitData = hit is null ? default : MoveHitReader.ReadHit(hit);
        entry.Finish();

        ProjectileData projectile = new()
        {
            SpawnFrame = spawnFrame,
            Offset = offset,
            Velocity = velocity,
            Lifetime = lifetime,
            Durability = durability,
            Hit = hitData,
        };
        return new ProjectileRecord(projectile, boxes);
    }

    private static IntVector2 ReadVector(FieldReader? vector, bool requireY)
    {
        if (vector is null)
        {
            return IntVector2.Zero;
        }

        const int MaxDistance = CharacterSchema.MaxDistance;
        int x = vector.RequiredInt("x", -MaxDistance, MaxDistance);
        int y = requireY
            ? vector.RequiredInt("y", -MaxDistance, MaxDistance)
            : vector.OptionalInt("y", 0, -MaxDistance, MaxDistance);
        vector.Finish();
        return new IntVector2(x, y);
    }

    private static IReadOnlyList<BoxData> ReadBoxes(FieldReader entry, int lifetime)
    {
        IReadOnlyList<FieldReader?> entries = entry.ObjectArray("boxes", required: true);
        if (entries.Count == 0)
        {
            entry.Error("boxes", "a projectile needs at least one box to hit with");
        }

        List<BoxData> boxes = [];
        foreach (FieldReader? box in entries)
        {
            if (box is null)
            {
                continue;
            }

            int firstFrame = box.RequiredInt("firstFrame", 1, lifetime);
            int lastFrame = box.RequiredInt("lastFrame", firstFrame, lifetime);
            AxisAlignedBox area = MoveBoxReader.ReadArea(box);
            box.Finish();
            boxes.Add(new BoxData { Kind = BoxKind.Hit, FirstFrame = firstFrame, LastFrame = lastFrame, Area = area });
        }

        return boxes;
    }
}
