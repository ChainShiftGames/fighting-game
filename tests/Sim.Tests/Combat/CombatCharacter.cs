using System;
using System.Collections.Generic;
using Sim.Characters;
using Sim.Fighters.States;
using Sim.Math;
using Sim.State;

namespace Sim.Tests.Combat;

/// <summary>A character built in code with hit data, for combat tests (Sim.Tests can't load JSON).</summary>
/// <remarks>
/// Basic moves: <see cref="Idle"/> (every basic state) and <see cref="Crouch"/> (the crouching states), with a lower
/// hurtbox. Attacks are 10 frames, active on frames 2–3, with a hitbox from 20000 to 60000 in front; the low one hits
/// near the floor and the overhead from above a crouching hurtbox. <see cref="Invulnerable"/> has no hitbox and
/// can't be hit on any of its frames.
/// </remarks>
internal static class CombatCharacter
{
    public const int Idle = 0;
    public const int Crouch = 1;
    public const int Mid = 2;
    public const int Low = 3;
    public const int Overhead = 4;
    public const int Unblockable = 5;
    public const int Knockdown = 6;
    public const int HeavyMid = 7;
    public const int Invulnerable = 8;

    public const int Health = 10_000;
    public const int ActiveFrame = 2;

    public static readonly HitData MidHit = new()
    {
        Damage = 300,
        ChipDamage = 30,
        Guard = Guard.Mid,
        Hitstun = 12,
        Blockstun = 9,
        Hitstop = 8,
    };

    public static readonly HitData LowHit = MidHit with { Damage = 200, ChipDamage = 0, Guard = Guard.Low, Hitstop = 7 };

    public static readonly HitData OverheadHit = MidHit with { Damage = 500, Guard = Guard.Overhead, Hitstop = 9 };

    public static readonly HitData UnblockableHit = MidHit with { Guard = Guard.Unblockable };

    public static readonly HitData KnockdownHit = MidHit with { Reaction = HitReaction.Knockdown };

    public static readonly HitData HeavyMidHit = MidHit with { Damage = 900, Hitstop = 15 };

    private static readonly AxisAlignedBox _standingHurtbox = new(-25_000, 0, 25_000, 120_000);
    private static readonly AxisAlignedBox _crouchingHurtbox = new(-25_000, 0, 25_000, 70_000);
    private static readonly AxisAlignedBox _midHitbox = new(20_000, 70_000, 60_000, 90_000);
    private static readonly AxisAlignedBox _lowHitbox = new(20_000, 0, 60_000, 20_000);
    private static readonly AxisAlignedBox _overheadHitbox = new(20_000, 50_000, 60_000, 100_000);

    public static CharacterData Create()
    {
        var boxes = new List<BoxData>();
        var hits = new List<HitData>();
        MoveData[] moves =
        [
            Basic(boxes, _standingHurtbox),
            Basic(boxes, _crouchingHurtbox),
            Attack(boxes, hits, _midHitbox, MidHit),
            Attack(boxes, hits, _lowHitbox, LowHit),
            Attack(boxes, hits, _overheadHitbox, OverheadHit),
            Attack(boxes, hits, _midHitbox, UnblockableHit),
            Attack(boxes, hits, _midHitbox, KnockdownHit),
            Attack(boxes, hits, _midHitbox, HeavyMidHit),
            Basic(boxes, _standingHurtbox) with
            {
                Category = MoveCategory.Normal, TotalFrames = 10, InvulnerableFirstFrame = 1, InvulnerableLastFrame = 10,
            },
        ];
        int[] basicStateMoves = new int[Enum.GetValues<BasicState>().Length];
        basicStateMoves[(int)BasicState.Crouch] = Crouch;
        basicStateMoves[(int)BasicState.CrouchHitstun] = Crouch;
        basicStateMoves[(int)BasicState.CrouchBlockstun] = Crouch;
        return new CharacterData(
            "combat", "Combat", new CharacterStats { Health = Health },
            ["idle", "crouch", "mid", "low", "overhead", "unblockable", "knockdown", "heavy_mid", "invulnerable"],
            moves, boxes.ToArray(), [], [], hits.ToArray(), [], basicStateMoves);
    }

    /// <summary>A grounded fighter at <paramref name="x"/> on <paramref name="moveFrame"/> of <paramref name="move"/>.</summary>
    public static FighterState At(int x, int facing, int move = Idle, int moveFrame = 1) => new()
    {
        Position = new IntVector2(x, 0),
        Facing = facing,
        Health = Health,
        Action = move switch
        {
            Idle => FighterAction.Idle,
            Crouch => FighterAction.Crouch,
            _ => FighterAction.Attack,
        },
        Move = move,
        MoveFrame = moveFrame,
    };

    /// <summary>An attacker on the first active frame of <paramref name="move"/>.</summary>
    public static FighterState Attacking(int x, int facing, int move) => At(x, facing, move, ActiveFrame);

    private static MoveData Basic(List<BoxData> boxes, AxisAlignedBox hurtbox)
    {
        int start = boxes.Count;
        boxes.Add(Box(BoxKind.Hurt, 1, 60, hurtbox));
        return new MoveData { Category = MoveCategory.Basic, TotalFrames = 60, BoxStart = start, BoxCount = 1 };
    }

    private static MoveData Attack(List<BoxData> boxes, List<HitData> hits, AxisAlignedBox hitbox, HitData hit)
    {
        int boxStart = boxes.Count;
        boxes.Add(Box(BoxKind.Hurt, 1, 10, _standingHurtbox));
        boxes.Add(Box(BoxKind.Hit, ActiveFrame, ActiveFrame + 1, hitbox));
        hits.Add(hit);
        return new MoveData
        {
            Category = MoveCategory.Normal,
            TotalFrames = 10,
            FirstActiveFrame = ActiveFrame,
            LastActiveFrame = ActiveFrame + 1,
            BoxStart = boxStart,
            BoxCount = 2,
            HitStart = hits.Count - 1,
            HitCount = 1,
        };
    }

    private static BoxData Box(BoxKind kind, int firstFrame, int lastFrame, AxisAlignedBox area) =>
        new() { Kind = kind, FirstFrame = firstFrame, LastFrame = lastFrame, Area = area };
}
