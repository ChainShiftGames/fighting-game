using System;
using Sim.Input;
using Sim.State;
using Sim.Toy;
using Xunit;

namespace Sim.Tests.Toy;

public sealed class ToySimulationTests
{
    private const int Seed = 1234;
    private const int Frames = 2_000;

    private static readonly InputWord _right = new(InputWord.Right);
    private static readonly InputWord _left = new(InputWord.Left);
    private static readonly InputWord _up = new(InputWord.Up);

    private readonly ToyRules _rules = ToyRules.Default;
    private readonly ToySimulation _simulation = new(ToyRules.Default);

    [Fact]
    public void SameInputsGiveTheSameChecksumEveryFrame()
    {
        InputWord[] inputs = RandomInputs(Seed, Frames);
        ToyState first = _simulation.CreateInitialState();
        ToyState second = _simulation.CreateInitialState();

        for (int frame = 0; frame < Frames; frame++)
        {
            _simulation.Tick(ref first, inputs[frame * 2], inputs[(frame * 2) + 1]);
            _simulation.Tick(ref second, inputs[frame * 2], inputs[(frame * 2) + 1]);
            Assert.Equal(StateChecksum.Compute(in first), StateChecksum.Compute(in second));
        }
    }

    [Fact]
    public void DifferentInputsGiveADifferentChecksum()
    {
        ToyState walked = _simulation.CreateInitialState();
        ToyState stood = _simulation.CreateInitialState();

        _simulation.Tick(ref walked, _right, InputWord.Neutral);
        _simulation.Tick(ref stood, InputWord.Neutral, InputWord.Neutral);

        Assert.NotEqual(StateChecksum.Compute(in walked), StateChecksum.Compute(in stood));
    }

    [Fact]
    public void RestoringASnapshotAndReplayingGivesIdenticalState()
    {
        InputWord[] inputs = RandomInputs(Seed, Frames);
        ToyState state = _simulation.CreateInitialState();
        Run(ref state, inputs, 0, Frames / 2);

        ToyState snapshot = state;
        ulong snapshotChecksum = StateChecksum.Compute(in snapshot);
        Run(ref state, inputs, Frames / 2, Frames);
        ulong firstRun = StateChecksum.Compute(in state);

        state = snapshot;
        Assert.Equal(snapshotChecksum, StateChecksum.Compute(in state));
        Run(ref state, inputs, Frames / 2, Frames);
        Assert.Equal(firstRun, StateChecksum.Compute(in state));
    }

    [Fact]
    public void TickDoesNotAllocate()
    {
        InputWord[] inputs = RandomInputs(Seed, Frames);
        ToyState state = _simulation.CreateInitialState();
        Run(ref state, inputs, 0, 10);

        long before = GC.GetAllocatedBytesForCurrentThread();
        Run(ref state, inputs, 10, Frames);
        _ = StateChecksum.Compute(in state);
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.Equal(0, allocated);
    }

    [Fact]
    public void HoldingRightWalksRightAtWalkSpeed()
    {
        ToyState state = _simulation.CreateInitialState();
        int start = state.Fighters[0].PositionX;

        _simulation.Tick(ref state, _right, InputWord.Neutral);

        Assert.Equal(start + _rules.WalkSpeed, state.Fighters[0].PositionX);
        Assert.Equal(1, state.Frame);
    }

    [Fact]
    public void LeftPlusRightIsNeutral()
    {
        ToyState state = _simulation.CreateInitialState();
        int start = state.Fighters[0].PositionX;

        _simulation.Tick(ref state, new InputWord(InputWord.Left | InputWord.Right), InputWord.Neutral);

        Assert.Equal(start, state.Fighters[0].PositionX);
    }

    [Fact]
    public void JumpRisesThenLandsBackOnTheGround()
    {
        ToyState state = _simulation.CreateInitialState();

        _simulation.Tick(ref state, _up, InputWord.Neutral);
        Assert.Equal(_rules.JumpVelocity, state.Fighters[0].PositionY);

        int airborneFrames = 1;
        while (state.Fighters[0].PositionY > 0)
        {
            _simulation.Tick(ref state, InputWord.Neutral, InputWord.Neutral);
            airborneFrames++;
            Assert.True(airborneFrames < 1_000, "Fighter never landed.");
        }

        Assert.Equal(0, state.Fighters[0].VelocityY);
    }

    [Fact]
    public void FightersStayInsideTheStage()
    {
        ToyState state = _simulation.CreateInitialState();

        for (int frame = 0; frame < Frames; frame++)
        {
            _simulation.Tick(ref state, _left, _right);
        }

        Assert.Equal(-_rules.StageHalfWidth, state.Fighters[0].PositionX);
        Assert.Equal(_rules.StageHalfWidth, state.Fighters[1].PositionX);
    }

    [Fact]
    public void BothPlayersAreProcessedSymmetrically()
    {
        InputWord[] inputs = RandomInputs(Seed, Frames);
        ToyState state = _simulation.CreateInitialState();

        for (int frame = 0; frame < Frames; frame++)
        {
            // Player 2 gets the mirror of player 1's input, so the fighters must stay mirror images.
            InputWord input = inputs[frame * 2];
            _simulation.Tick(ref state, input, Mirror(input));
            Assert.Equal(state.Fighters[0].PositionX, -state.Fighters[1].PositionX);
            Assert.Equal(state.Fighters[0].PositionY, state.Fighters[1].PositionY);
        }
    }

    private void Run(ref ToyState state, InputWord[] inputs, int fromFrame, int toFrame)
    {
        for (int frame = fromFrame; frame < toFrame; frame++)
        {
            _simulation.Tick(ref state, inputs[frame * 2], inputs[(frame * 2) + 1]);
        }
    }

    private static InputWord[] RandomInputs(int seed, int frames)
    {
        var random = new Random(seed);
        var inputs = new InputWord[frames * 2];
        for (int i = 0; i < inputs.Length; i++)
        {
            inputs[i] = new InputWord((ushort)random.Next(0, 16));
        }

        return inputs;
    }

    private static InputWord Mirror(InputWord input)
    {
        ushort bits = (ushort)(input.Bits & (InputWord.Up | InputWord.Down));
        bits |= input.IsHeld(InputWord.Left) ? InputWord.Right : (ushort)0;
        bits |= input.IsHeld(InputWord.Right) ? InputWord.Left : (ushort)0;
        return new InputWord(bits);
    }
}
