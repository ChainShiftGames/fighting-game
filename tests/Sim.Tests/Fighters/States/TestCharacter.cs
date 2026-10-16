using System;
using System.Collections.Generic;
using Sim.Characters;

namespace Sim.Tests.Fighters.States;

/// <summary>A small character built in code for state machine tests (Sim.Tests can't load JSON).</summary>
/// <remarks>
/// Moves in priority order: 6c (special), 5a, 2b, j_b, then the basic moves. 5a: startup 5, active 3, recovery 8;
/// cancels into specials on hit or block during frames 5–12, and into 5a on whiff during frames 8–15.
/// </remarks>
internal static class TestCharacter
{
    public const ushort ButtonA = 1 << 4;
    public const ushort ButtonB = 1 << 5;
    public const ushort ButtonC = 1 << 6;

    public const int Special6C = 0;
    public const int Normal5A = 1;
    public const int Normal2B = 2;
    public const int AirB = 3;
    public const int PrejumpFrames = 3;
    public const int LandingFrames = 4;
    public const int DashFrames = 10;
    public const int KnockdownFrames = 20;
    public const int WakeUpFrames = 8;

    private static readonly BasicState[] _basicStates = Enum.GetValues<BasicState>();

    /// <param name="dashStyle">Forward dash style.</param>
    /// <param name="without">Basic states the character leaves undefined.</param>
    public static CharacterData Create(DashStyle dashStyle = DashStyle.Run, params BasicState[] without)
    {
        var moveIds = new List<string> { "6c", "5a", "2b", "j_b" };
        var moves = new List<MoveData>
        {
            Attack(MoveCategory.Special, MoveStance.Ground, motion: 6, ButtonC, startup: 10, active: 2, recovery: 20),
            Attack(MoveCategory.Normal, MoveStance.Ground, motion: 5, ButtonA, startup: 5, active: 3, recovery: 8) with
            {
                CancelStart = 0,
                CancelCount = 2,
            },
            Attack(MoveCategory.Normal, MoveStance.Ground, motion: 2, ButtonB, startup: 6, active: 2, recovery: 10),
            Attack(MoveCategory.Normal, MoveStance.Air, motion: 5, ButtonB, startup: 7, active: 4, recovery: 5),
        };
        int[] basicStateMoves = new int[_basicStates.Length];
        foreach (BasicState state in _basicStates)
        {
            if (Array.IndexOf(without, state) >= 0)
            {
                basicStateMoves[(int)state] = CharacterData.NoMove;
                continue;
            }

            basicStateMoves[(int)state] = moves.Count;
            moveIds.Add(state.ToString());
            moves.Add(new MoveData { Category = MoveCategory.Basic, TotalFrames = DurationOf(state) });
        }

        CancelData[] cancels =
        [
            new()
            {
                FirstFrame = 5, LastFrame = 12, On = CancelOn.Hit | CancelOn.Block, IntoCategories = MoveCategory.Special,
            },
            new() { FirstFrame = 8, LastFrame = 15, On = CancelOn.Whiff, TargetStart = 0, TargetCount = 1 },
        ];
        CharacterStats stats = new() { Health = 10_000, DashStyle = dashStyle, PrejumpFrames = PrejumpFrames };
        return new CharacterData(
            "test_man", "Test Man", stats, [.. moveIds], [.. moves], [], cancels, [Normal5A], [], basicStateMoves);
    }

    private static int DurationOf(BasicState state) => state switch
    {
        BasicState.Landing => LandingFrames,
        BasicState.Dash or BasicState.Backdash => DashFrames,
        BasicState.Knockdown => KnockdownFrames,
        BasicState.WakeUp => WakeUpFrames,
        _ => 6,
    };

    private static MoveData Attack(
        MoveCategory category, MoveStance stance, int motion, ushort button, int startup, int active, int recovery) =>
        new()
        {
            Category = category,
            Stance = stance,
            Motion = motion,
            ButtonMask = button,
            TotalFrames = startup - 1 + active + recovery,
            FirstActiveFrame = startup,
            LastActiveFrame = startup - 1 + active,
        };
}
