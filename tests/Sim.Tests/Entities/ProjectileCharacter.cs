using System;
using Sim.Characters;
using Sim.Fighters.States;
using Sim.Math;
using Sim.State;

namespace Sim.Tests.Entities;

/// <summary>A character built in code with projectile moves, for entity tests (Sim.Tests can't load JSON).</summary>
/// <remarks>
/// <see cref="Idle"/> is every basic state. <see cref="Fireball"/> spawns <see cref="FireballProjectile"/> and
/// <see cref="Sturdy"/> spawns <see cref="SturdyProjectile"/>, both on move frame <see cref="SpawnFrame"/> at
/// (40000, 50000) in front, with a 20000-square hitbox around their position. <see cref="Invulnerable"/> can't be hit.
/// </remarks>
internal static class ProjectileCharacter
{
    public const int Idle = 0;
    public const int Fireball = 1;
    public const int Sturdy = 2;
    public const int Invulnerable = 3;

    public const int FireballProjectile = 0;
    public const int SturdyProjectile = 1;

    public const int Health = 10_000;
    public const int SpawnFrame = 2;
    public const int FireballSpeed = 8_000;
    public const int FireballLifetime = 30;
    public const int SturdySpeed = 2_000;
    public const int SturdyDurability = 3;

    public static readonly IntVector2 Offset = new(40_000, 50_000);

    public static readonly HitData FireballHit = new()
    {
        Damage = 600,
        ChipDamage = 60,
        Guard = Guard.Mid,
        Hitstun = 16,
        Blockstun = 12,
        Hitstop = 10,
        Pushback = 10_000,
    };

    public static readonly HitData SturdyHit = FireballHit with { Damage = 900, Hitstop = 12 };

    private static readonly AxisAlignedBox _hurtbox = new(-25_000, 0, 25_000, 120_000);
    private static readonly AxisAlignedBox _projectileHitbox = new(-10_000, -10_000, 10_000, 10_000);

    public static CharacterData Create()
    {
        BoxData[] boxes =
        [
            Box(BoxKind.Hurt, 1, 60, _hurtbox),
            Box(BoxKind.Hit, 1, 200, _projectileHitbox),
            Box(BoxKind.Hit, 1, 200, _projectileHitbox),
        ];
        MoveData[] moves =
        [
            new() { Category = MoveCategory.Basic, TotalFrames = 60, BoxStart = 0, BoxCount = 1 },
            Special(FireballProjectile),
            Special(SturdyProjectile),
            new()
            {
                Category = MoveCategory.Normal, TotalFrames = 60, BoxStart = 0, BoxCount = 1,
                InvulnerableFirstFrame = 1, InvulnerableLastFrame = 60,
            },
        ];
        ProjectileData[] projectiles =
        [
            Projectile(FireballSpeed, FireballLifetime, durability: 1, boxStart: 1, FireballHit),
            Projectile(SturdySpeed, 200, SturdyDurability, boxStart: 2, SturdyHit),
        ];
        return new CharacterData(
            "projectile", "Projectile", new CharacterStats { Health = Health },
            ["idle", "fireball", "sturdy", "invulnerable"],
            moves, boxes, [], [], [], [], new int[Enum.GetValues<BasicState>().Length], projectiles);
    }

    /// <summary>A grounded fighter at <paramref name="x"/> on a frame of <paramref name="move"/>.</summary>
    public static FighterState At(int x, int facing, int move = Idle, int moveFrame = 1) => new()
    {
        Position = new IntVector2(x, 0),
        Facing = facing,
        Health = Health,
        Action = move == Idle ? FighterAction.Idle : FighterAction.Attack,
        Move = move,
        MoveFrame = moveFrame,
    };

    /// <summary>A live projectile of <paramref name="owner"/> at <paramref name="x"/>, 50000 up.</summary>
    /// <remarks>P1's flies right and P2's left, as if each fighter faced the other.</remarks>
    public static EntityState Entity(int owner, int x, int projectile = FireballProjectile, int frame = 1)
    {
        bool isSturdy = projectile == SturdyProjectile;
        int facing = owner == 0 ? 1 : -1;
        return new EntityState
        {
            IsActive = 1,
            Owner = owner,
            Projectile = projectile,
            Frame = frame,
            Position = new IntVector2(x, Offset.Y),
            Velocity = new IntVector2(facing * (isSturdy ? SturdySpeed : FireballSpeed), 0),
            Facing = facing,
            Durability = isSturdy ? SturdyDurability : 1,
        };
    }

    // Active on its spawn frame, as data moves with a projectile are, so a hit during it can be a counter hit.
    private static MoveData Special(int projectile) => new()
    {
        Category = MoveCategory.Special,
        TotalFrames = 30,
        FirstActiveFrame = SpawnFrame,
        LastActiveFrame = SpawnFrame,
        BoxStart = 0,
        BoxCount = 1,
        ProjectileStart = projectile,
        ProjectileCount = 1,
    };

    private static ProjectileData Projectile(int speed, int lifetime, int durability, int boxStart, HitData hit)
    {
        return new ProjectileData
        {
            SpawnFrame = SpawnFrame,
            Offset = Offset,
            Velocity = new IntVector2(speed, 0),
            Lifetime = lifetime,
            Durability = durability,
            BoxStart = boxStart,
            BoxCount = 1,
            Hit = hit,
        };
    }

    private static BoxData Box(BoxKind kind, int firstFrame, int lastFrame, AxisAlignedBox area) =>
        new() { Kind = kind, FirstFrame = firstFrame, LastFrame = lastFrame, Area = area };
}
