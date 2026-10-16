using Sim.Input;
using Sim.Math;
using Sim.Modules;
using Sim.Pipeline;
using Sim.State;
using Xunit;

namespace Sim.Tests.Pipeline;

/// <summary>P1 and P2 are processed symmetrically: swapping (and mirroring) the players mirrors the result.</summary>
public sealed class SymmetryTests
{
    private const int Frames = 600;

    [Fact]
    public void SwappingPlayer1AndPlayer2InputsMirrorsTheResult()
    {
        MatchState start = default;
        start.Fighters[0].Position = new IntVector2(-50_000, 0);
        start.Fighters[1].Position = new IntVector2(120_000, 0);
        start.Fighters[0].Health = 900;
        start.Fighters[1].Health = 1000;

        MatchState original = start;
        MatchState swapped = Mirror(start);
        TickPipeline originalPipeline = CreatePipeline();
        TickPipeline swappedPipeline = CreatePipeline();
        RandomState player1Inputs = RandomState.FromSeed(11);
        RandomState player2Inputs = RandomState.FromSeed(22);
        for (int frame = 0; frame < Frames; frame++)
        {
            InputWord player1 = RandomDirection(ref player1Inputs);
            InputWord player2 = RandomDirection(ref player2Inputs);
            originalPipeline.Tick(ref original, player1, player2);
            swappedPipeline.Tick(ref swapped, MirrorInput(player2), MirrorInput(player1));
        }

        AssertMirrored(original, swapped);
    }

    // Movement that reads both fighters, plus a module using per-fighter time scale, freeze and hitstop, so the test
    // covers stages, hooks, request application and time control.
    private static TickPipeline CreatePipeline() =>
        Pipelines.Create(new TickStages { Movement = new ChaseStage() }, new CapabilityUserModule());

    private static InputWord RandomDirection(ref RandomState random) => new((ushort)random.NextBelow(16));

    private static InputWord MirrorInput(InputWord input)
    {
        int bits = input.Bits & ~(InputWord.Left | InputWord.Right);
        bits |= input.IsHeld(InputWord.Left) ? InputWord.Right : 0;
        bits |= input.IsHeld(InputWord.Right) ? InputWord.Left : 0;
        return new InputWord((ushort)bits);
    }

    private static MatchState Mirror(MatchState state)
    {
        MatchState mirrored = state;
        mirrored.Fighters[0] = MirrorFighter(state.Fighters[1]);
        mirrored.Fighters[1] = MirrorFighter(state.Fighters[0]);
        return mirrored;
    }

    private static FighterState MirrorFighter(FighterState fighter)
    {
        fighter.Position = fighter.Position.MirrorX();
        fighter.Velocity = fighter.Velocity.MirrorX();
        fighter.Facing = -fighter.Facing;
        return fighter;
    }

    private static void AssertMirrored(MatchState original, MatchState swapped)
    {
        Assert.Equal(original.Frame, swapped.Frame);
        Assert.Equal(original.Freeze.FramesLeft, swapped.Freeze.FramesLeft);
        for (int fighter = 0; fighter < MatchState.PlayerCount; fighter++)
        {
            int other = 1 - fighter;
            Assert.Equal(MirrorFighter(original.Fighters[fighter]), swapped.Fighters[other]);
            Assert.Equal(original.Inputs[fighter].Count, swapped.Inputs[other].Count);
            for (int framesAgo = 0; framesAgo < original.Inputs[fighter].Count; framesAgo++)
            {
                Assert.Equal(
                    MirrorInput(original.Inputs[fighter].FramesAgo(framesAgo)),
                    swapped.Inputs[other].FramesAgo(framesAgo));
            }
        }

        Assert.NotEqual(original.Fighters[0].Position, original.Fighters[1].Position.MirrorX());
    }

    // Walks forward or back relative to the opponent and faces them; both fighters are read before either moves.
    private sealed class ChaseStage : ITickStage
    {
        private const int WalkSpeed = 1_500;
        private const int DistanceDivisor = 64;

        public void Run(ref MatchState state, in FrameActivity activity)
        {
            FighterState player1 = Step(in state, 0, 1, activity.Advances(0));
            FighterState player2 = Step(in state, 1, 0, activity.Advances(1));
            state.Fighters[0] = player1;
            state.Fighters[1] = player2;
        }

        private static FighterState Step(ref readonly MatchState state, int fighter, int opponent, bool advances)
        {
            FighterState self = state.Fighters[fighter];
            if (!advances)
            {
                return self;
            }

            int distance = state.Fighters[opponent].Position.X - self.Position.X;
            int towardOpponent = IntMath.Sign(distance);
            self.Facing = towardOpponent == 0 ? self.Facing : towardOpponent;
            InputWord input = state.Inputs[fighter].FramesAgo(0);
            int direction = (input.IsHeld(InputWord.Right) ? 1 : 0) - (input.IsHeld(InputWord.Left) ? 1 : 0);

            // Faster when far apart, so a stage that let one fighter see the other's new position would break the
            // mirror at once.
            int speed = WalkSpeed + (IntMath.Abs(distance) / DistanceDivisor);
            self.Velocity = new IntVector2(direction * speed, 0);
            self.Position += self.Velocity;
            return self;
        }
    }

    // Holding down slows the fighter down; up gives a short hitstop; when both hold down a freeze starts that only
    // the fighter with less health is exempt from. Every request depends only on that fighter's own data.
    private sealed class CapabilityUserModule : IMechanicModule, IAfterInputHook, IEndOfFrameHook
    {
        public Sim.Rules.ModuleId Id => new(1);

        public string Name => "capability_user";

        public void AfterInput(ModuleContext context)
        {
            for (int fighter = 0; fighter < MatchState.PlayerCount; fighter++)
            {
                if (context.State.Inputs[fighter].FramesAgo(0).IsHeld(InputWord.Down))
                {
                    context.RequestTimeScale(
                        fighter, new TimeScaleRequest { Numerator = 2, Denominator = 3, Frames = 5 });
                }
            }
        }

        public void EndOfFrame(ModuleContext context)
        {
            ref readonly MatchState state = ref context.State;
            for (int fighter = 0; fighter < MatchState.PlayerCount; fighter++)
            {
                if (state.Inputs[fighter].FramesAgo(0).IsHeld(InputWord.Up))
                {
                    context.RequestHitstopOverride(fighter, new HitstopOverrideRequest { Frames = 2 });
                }
            }

            bool bothDown = state.Inputs[0].FramesAgo(0).IsHeld(InputWord.Down) &&
                state.Inputs[1].FramesAgo(0).IsHeld(InputWord.Down);
            if (bothDown && state.Freeze.FramesLeft == 0)
            {
                int weaker = state.Fighters[0].Health < state.Fighters[1].Health ? 0 : 1;
                context.RequestFreeze(new FreezeRequest { Frames = 3, ExemptFighters = 1 << weaker });
            }
        }
    }
}
