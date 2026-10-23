using System;
using System.Collections.Generic;
using FightingGame.Online;
using FightingGame.Presentation;
using FightingGame.Presentation.World;
using Godot;
using Sim.Input;
using Sim.Math;
using Sim.State;
using Xunit;

namespace FightingGame.Tests.Presentation;

/// <summary>Testman's real jumps, from the game logic through the vertical follow to the 2D screen and the 3D camera.</summary>
public sealed class JumpFramingTests
{
    private static readonly Vector2 _viewport = new(1920f, 1080f);
    private static readonly InputWord _up = new(InputWord.Up);
    private static readonly InputWord _down = new(InputWord.Down);

    private readonly MatchLineup _lineup = MatchLineup.From(TestGameData.Data);
    private readonly VerticalFollow _follow = new();
    private readonly List<WorldBox> _boxes = [];
    private readonly float _groundScale;
    private readonly float _groundFloor;

    public JumpFramingTests()
    {
        StageCamera ground = StageCamera.Fit(_viewport, _lineup.Screen, focusX: 0);
        _groundScale = ground.PixelsPerSubpixel;
        _groundFloor = ground.FloorCentre.Y;
    }

    [Fact]
    public void GroundedPlayKeepsTheFloorWhereItIs()
    {
        var forward = new InputWord(InputWord.Right);

        Play(frame => frame % 30 < 15 ? forward : _down, frame => InputWord.Neutral, (state, camera) =>
        {
            Assert.Equal(_groundFloor, camera.FloorCentre.Y, 2);
            Assert.Equal(_groundScale, camera.PixelsPerSubpixel, 6);
        });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AJumperAndItsStandingOpponentStayWholeOnScreenWithTheFloor(bool highJump)
    {
        InputWord[] jump = Jump(highJump);
        float closest = _groundScale;

        Play(frame => jump[frame], frame => InputWord.Neutral, (state, camera) =>
        {
            AssertBothWhole(in state, camera);
            Assert.True(camera.ToScreen(new IntVector2(0, 0)).Y < _viewport.Y, $"frame {state.Frame}: floor in view");
            closest = Math.Min(closest, camera.PixelsPerSubpixel);
        });

        // Only the high jump climbs past the round-start view; Testman's normal jump fits it.
        if (highJump)
        {
            Assert.True(closest < _groundScale, "zoomed out to keep both");
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TwoJumpersStayWholeOnScreenAndTheViewRisesWithoutZooming(bool highJump)
    {
        InputWord[] jump = Jump(highJump);
        float lowestFloor = 0f;

        Play(frame => jump[frame], frame => jump[frame], (state, camera) =>
        {
            AssertBothWhole(in state, camera);
            Assert.Equal(_groundScale, camera.PixelsPerSubpixel, 6);
            lowestFloor = Math.Max(lowestFloor, camera.ToScreen(new IntVector2(0, 0)).Y);
        });

        if (highJump)
        {
            Assert.True(lowestFloor > _viewport.Y, "the view rose past the floor");
        }
    }

    [Fact]
    public void The3DCameraShowsTheHighJumpersHeadAtThePeak()
    {
        InputWord[] jump = Jump(highJump: true);
        int highestHead = 0;
        CameraFraming atPeak = default;

        Play(frame => jump[frame], frame => InputWord.Neutral, (state, camera) =>
        {
            int head = Height(0, in state).Top;
            if (head > highestHead)
            {
                highestHead = head;
                atPeak = CameraFraming.Of(camera, _viewport);
            }
        });

        float viewTop = atPeak.Centre.Y + (atPeak.VisibleHeight / 2f);
        // Above the jump's apex from Testman's stats (v^2 / 2g): it really jumped, whatever the jump is tuned to.
        Sim.Characters.CharacterStats stats = _lineup.Characters[0].Stats;
        int apex = stats.HighJumpVelocity * stats.HighJumpVelocity / (2 * stats.Gravity);
        Assert.True(highestHead > apex, $"a high jump: head at {highestHead}, apex {apex}");
        Assert.True(WorldScale.ToMetres(highestHead) < viewTop, $"head at {highestHead} is above the 3D view");
    }

    private void AssertBothWhole(in MatchState state, StageCamera camera)
    {
        for (int player = 0; player < MatchState.PlayerCount; player++)
        {
            VerticalSpan body = Height(player, in state);
            float head = camera.ToScreen(new IntVector2(0, body.Top)).Y;
            float feet = camera.ToScreen(new IntVector2(0, body.Bottom)).Y;
            Assert.True(head > 0f, $"frame {state.Frame}: P{player + 1}'s head is above the screen");
            Assert.True(feet < _viewport.Y, $"frame {state.Frame}: P{player + 1}'s feet are below the screen");
        }
    }

    // Runs the real game logic, framing each frame as MatchView does.
    private void Play(Func<int, InputWord> player1, Func<int, InputWord> player2, Action<MatchState, StageCamera> check)
    {
        MatchState state = _lineup.Simulation.StartState();
        for (int frame = 0; frame < 90; frame++)
        {
            _lineup.Simulation.Tick(ref state, player1(frame), player2(frame));
            check(state, Frame(in state));
        }
    }

    private StageCamera Frame(in MatchState state)
    {
        Span<VerticalSpan> bodies = stackalloc VerticalSpan[MatchState.PlayerCount];
        for (int player = 0; player < MatchState.PlayerCount; player++)
        {
            bodies[player] = Height(player, in state);
        }

        VerticalSpan view = _follow.Follow(state.Frame, StageCamera.GroundView(_viewport, _lineup.Screen), bodies);
        return StageCamera.Fit(_viewport, _lineup.Screen, state.Camera.X, view);
    }

    private VerticalSpan Height(int player, in MatchState state)
    {
        _boxes.Clear();
        FighterBoxes.Collect(_lineup.Characters[player], in state.Fighters[player], _boxes);
        return FighterBoxes.Height(state.Fighters[player].Position, _boxes);
    }

    // Straight up; a high jump is down, then up (data/input/motions.json: "28"). 90 frames, landing included.
    private static InputWord[] Jump(bool highJump)
    {
        var inputs = new InputWord[90];
        int start = highJump ? 2 : 0;
        for (int frame = 0; frame < start; frame++)
        {
            inputs[frame] = _down;
        }

        for (int frame = start; frame < start + 5; frame++)
        {
            inputs[frame] = _up;
        }

        return inputs;
    }
}
