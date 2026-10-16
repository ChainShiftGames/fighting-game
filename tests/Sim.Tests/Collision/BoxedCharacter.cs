using System;
using Sim.Characters;
using Sim.Fighters.States;
using Sim.Math;
using Sim.State;

namespace Sim.Tests.Collision;

/// <summary>A character built in code with boxes, for collision tests (Sim.Tests can't load JSON).</summary>
/// <remarks>
/// Moves: <see cref="Idle"/> (every basic state) with a centred pushbox and hurtbox; <see cref="Jab"/>, whose hitbox
/// reaches forward on frames 2–3 and which has a second, higher pushbox; <see cref="PassThrough"/> without pushbox.
/// </remarks>
internal static class BoxedCharacter
{
    public const int Idle = 0;
    public const int Jab = 1;
    public const int PassThrough = 2;

    /// <summary>Half the pushbox width: two fighters closer than twice this overlap.</summary>
    public const int PushHalfWidth = 20_000;
    public const int PushHeight = 110_000;
    public const int HitReach = 60_000;

    public static CharacterData Create()
    {
        AxisAlignedBox push = new(-PushHalfWidth, 0, PushHalfWidth, PushHeight);
        AxisAlignedBox hurt = new(-25_000, 0, 25_000, 120_000);
        BoxData[] boxes =
        [
            Box(BoxKind.Hurt, 1, 60, hurt),
            Box(BoxKind.Push, 1, 60, push),
            Box(BoxKind.Push, 1, 10, push),
            Box(BoxKind.Push, 1, 10, new AxisAlignedBox(-10_000, 100_000, 30_000, 140_000)),
            Box(BoxKind.Hit, 2, 3, new AxisAlignedBox(20_000, 70_000, HitReach, 90_000)),
            Box(BoxKind.Hurt, 1, 10, hurt),
        ];
        MoveData[] moves =
        [
            new() { Category = MoveCategory.Basic, TotalFrames = 60, BoxStart = 0, BoxCount = 2 },
            new() { Category = MoveCategory.Normal, TotalFrames = 10, BoxStart = 2, BoxCount = 4 },
            new() { Category = MoveCategory.Normal, TotalFrames = 10, BoxStart = 5, BoxCount = 1 },
        ];
        int[] basicStateMoves = new int[Enum.GetValues<BasicState>().Length];
        return new CharacterData(
            "boxed", "Boxed", new CharacterStats { Health = 10_000 }, ["idle", "jab", "pass_through"], moves, boxes,
            [], [], [], [], basicStateMoves);
    }

    /// <summary>A fighter at (<paramref name="x"/>, <paramref name="y"/>) on frame 1 of <paramref name="move"/>.</summary>
    public static FighterState At(int x, int facing, int move = Idle, int y = 0) => new()
    {
        Position = new IntVector2(x, y),
        Facing = facing,
        Action = move == Idle ? FighterAction.Idle : FighterAction.Attack,
        Move = move,
        MoveFrame = 1,
        IsAirborne = y > 0 ? 1 : 0,
    };

    private static BoxData Box(BoxKind kind, int firstFrame, int lastFrame, AxisAlignedBox area) =>
        new() { Kind = kind, FirstFrame = firstFrame, LastFrame = lastFrame, Area = area };
}
