using System;
using Sim.Characters;
using Sim.Fighters.States;
using Sim.Math;
using Sim.State;

namespace Sim.Tests.Throws;

/// <summary>A character built in code with a throw, for throw tests (Sim.Tests can't load JSON).</summary>
/// <remarks>
/// <see cref="Throw"/> is 10 frames on buttons <see cref="ThrowButtons"/>, active on frames 2-3 with a throw box from
/// 0 to 45000 in front, released <see cref="ReleaseX"/> in front when it lands. Hurtboxes are 25000 each side, so the
/// throw reaches an opponent less than 70000 away. <see cref="Thrown"/> has no hurtbox.
/// </remarks>
internal static class ThrowCharacter
{
    public const int Idle = 0;
    public const int Throw = 1;
    public const int Invulnerable = 2;
    public const int Thrown = 3;
    public const int ThrowTech = 4;

    public const int Health = 10_000;
    public const int ActiveFrame = 2;
    public const int ReleaseX = 70_000;

    /// <summary>A and B in the default button set (bits 4 and 5).</summary>
    public const ushort ThrowButtons = (1 << 4) | (1 << 5);

    public static readonly HitData ThrowHit = new()
    {
        Damage = 1000,
        Guard = Guard.Unblockable,
        Hitstop = 6,
        Reaction = HitReaction.Knockdown,
    };

    private static readonly AxisAlignedBox _hurtbox = new(-25_000, 0, 25_000, 120_000);
    private static readonly AxisAlignedBox _throwBox = new(0, 20_000, 45_000, 80_000);

    public static CharacterData Create()
    {
        BoxData[] boxes =
        [
            Box(BoxKind.Hurt, 1, 60, _hurtbox),
            Box(BoxKind.Throw, ActiveFrame, ActiveFrame + 1, _throwBox),
        ];
        MoveData[] moves =
        [
            Basic(boxCount: 1),
            new()
            {
                Category = MoveCategory.Throw,
                Motion = 5,
                ButtonMask = ThrowButtons,
                TotalFrames = 10,
                FirstActiveFrame = ActiveFrame,
                LastActiveFrame = ActiveFrame + 1,
                BoxStart = 0,
                BoxCount = 2,
                HitStart = 0,
                HitCount = 1,
                ThrowReleaseX = ReleaseX,
            },
            Basic(boxCount: 1) with
            {
                Category = MoveCategory.Normal,
                TotalFrames = 10,
                InvulnerableFirstFrame = 1,
                InvulnerableLastFrame = 10,
            },
            Basic(boxCount: 0),
            Basic(boxCount: 1),
        ];
        int[] basicStateMoves = new int[Enum.GetValues<BasicState>().Length];
        basicStateMoves[(int)BasicState.Thrown] = Thrown;
        basicStateMoves[(int)BasicState.ThrowTech] = ThrowTech;
        return new CharacterData(
            "throws", "Throws", new CharacterStats { Health = Health },
            ["idle", "throw", "invulnerable", "thrown", "throw_tech"],
            moves, boxes, [], [], [ThrowHit], [], basicStateMoves);
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

    /// <summary>A thrower on the first active frame of its throw.</summary>
    public static FighterState Throwing(int x, int facing) => At(x, facing, Throw, ActiveFrame);

    private static MoveData Basic(int boxCount) =>
        new() { Category = MoveCategory.Basic, TotalFrames = 60, BoxStart = 0, BoxCount = boxCount };

    private static BoxData Box(BoxKind kind, int firstFrame, int lastFrame, AxisAlignedBox area) =>
        new() { Kind = kind, FirstFrame = firstFrame, LastFrame = lastFrame, Area = area };
}
