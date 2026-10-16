using System;
using System.Collections.Generic;
using Sim.Characters;

namespace Sim.Tests.Fighters.States;

/// <summary>A small character built in code for state machine tests (Sim.Tests can't load JSON).</summary>
/// <remarks>
/// Moves in priority order: 6c (special), 5a, 2b, j_b, then the basic moves. 5a: startup 5, active 3, recovery 8;
/// cancels into specials on hit or block during frames 5–12, and into 5a on whiff during frames 8–15.
/// Dash, backdash and air dashes travel by their move velocity; the backdash stops after
/// <see cref="BackdashMovingFrames"/> and is invulnerable for its first <see cref="BackdashInvulnerableFrames"/>.
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
    public const int AirDashFrames = 8;

    public const int WalkForwardSpeed = 4000;
    public const int WalkBackSpeed = 3000;
    public const int JumpVelocity = 20_000;
    public const int HighJumpVelocity = 26_000;
    public const int JumpForwardSpeed = 5000;
    public const int JumpBackSpeed = 4000;
    public const int Gravity = 1000;
    public const int AirActions = 2;
    public const int DashSpeed = 9000;
    public const int BackdashSpeed = 8000;
    public const int BackdashMovingFrames = 7;
    public const int BackdashInvulnerableFrames = 5;
    public const int AirDashSpeed = 12_000;
    public const int AirBackdashSpeed = 10_000;

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
        var velocities = new List<VelocityData>();
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
            MoveData move = new() { Category = MoveCategory.Basic, TotalFrames = DurationOf(state) };
            if (VelocityOf(state) is VelocityData velocity)
            {
                move = move with { VelocityStart = velocities.Count, VelocityCount = 1 };
                velocities.Add(velocity);
            }

            if (state == BasicState.Backdash)
            {
                move = move with { InvulnerableFirstFrame = 1, InvulnerableLastFrame = BackdashInvulnerableFrames };
            }

            moves.Add(move);
        }

        CancelData[] cancels =
        [
            new()
            {
                FirstFrame = 5, LastFrame = 12, On = CancelOn.Hit | CancelOn.Block, IntoCategories = MoveCategory.Special,
            },
            new() { FirstFrame = 8, LastFrame = 15, On = CancelOn.Whiff, TargetStart = 0, TargetCount = 1 },
        ];
        CharacterStats stats = new()
        {
            Health = 10_000,
            WalkForwardSpeed = WalkForwardSpeed,
            WalkBackSpeed = WalkBackSpeed,
            DashStyle = dashStyle,
            PrejumpFrames = PrejumpFrames,
            JumpVelocity = JumpVelocity,
            HighJumpVelocity = HighJumpVelocity,
            JumpForwardSpeed = JumpForwardSpeed,
            JumpBackSpeed = JumpBackSpeed,
            Gravity = Gravity,
            AirActions = AirActions,
        };
        return new CharacterData(
            "test_man", "Test Man", stats, [.. moveIds], [.. moves], [], cancels, [Normal5A], [], [.. velocities],
            basicStateMoves);
    }

    private static VelocityData? VelocityOf(BasicState state) => state switch
    {
        BasicState.Dash => Travel(DashFrames, DashSpeed),
        BasicState.Backdash => Travel(BackdashMovingFrames, -BackdashSpeed),
        BasicState.AirDash => Travel(AirDashFrames, AirDashSpeed),
        BasicState.AirBackdash => Travel(AirDashFrames, -AirBackdashSpeed),
        _ => null,
    };

    private static VelocityData Travel(int frames, int speed) =>
        new() { FirstFrame = 1, LastFrame = frames, X = speed };

    private static int DurationOf(BasicState state) => state switch
    {
        BasicState.Landing => LandingFrames,
        BasicState.Dash or BasicState.Backdash => DashFrames,
        BasicState.Knockdown => KnockdownFrames,
        BasicState.WakeUp => WakeUpFrames,
        BasicState.AirDash or BasicState.AirBackdash => AirDashFrames,
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
