using Sim.Input;
using Sim.Math;
using Sim.State;
using Xunit;

namespace Sim.Tests.State;

public sealed class MatchStateSnapshotTests
{
    [Fact]
    public void SaveMutateRestoreGivesAnIdenticalChecksum()
    {
        MatchState live = CreateBusyState();
        ulong savedChecksum = StateChecksum.Compute(in live);

        MatchState snapshot = live;
        Mutate(ref live);
        Assert.NotEqual(savedChecksum, StateChecksum.Compute(in live));

        live = snapshot;
        Assert.Equal(savedChecksum, StateChecksum.Compute(in live));
    }

    [Fact]
    public void ASnapshotIsACopyNotAView()
    {
        MatchState live = CreateBusyState();
        MatchState snapshot = live;

        Mutate(ref live);

        MatchState untouched = CreateBusyState();
        Assert.Equal(StateChecksum.Compute(in untouched), StateChecksum.Compute(in snapshot));
    }

    [Fact]
    public void ChangingAFieldInAnyPartChangesTheChecksum()
    {
        MatchState baseline = CreateBusyState();
        ulong baselineChecksum = StateChecksum.Compute(in baseline);

        MatchState fighter = baseline;
        fighter.Fighters[1].Velocity = new IntVector2(0, 1);
        MatchState input = baseline;
        input.Inputs[1].Record(new InputWord(InputWord.Up));
        MatchState module = baseline;
        module.Modules[ModuleStates.MaxModules - 1][ModuleStateSlice.Length - 1] = 1;
        MatchState random = baseline;
        random.Random.NextUInt();
        MatchState round = baseline;
        round.Round.Wins[1] = 1;

        Assert.NotEqual(baselineChecksum, StateChecksum.Compute(in fighter));
        Assert.NotEqual(baselineChecksum, StateChecksum.Compute(in input));
        Assert.NotEqual(baselineChecksum, StateChecksum.Compute(in module));
        Assert.NotEqual(baselineChecksum, StateChecksum.Compute(in random));
        Assert.NotEqual(baselineChecksum, StateChecksum.Compute(in round));
    }

    private static MatchState CreateBusyState()
    {
        MatchState state = default;
        state.Frame = 120;
        state.Round.Round = 1;
        state.Round.TimerFramesLeft = 99 * 60;
        state.Random = RandomState.FromSeed(42);
        state.Fighters[0].Position = new IntVector2(-150_000, 0);
        state.Fighters[0].Facing = 1;
        state.Fighters[1].Position = new IntVector2(150_000, 0);
        state.Fighters[1].Facing = -1;
        state.Inputs[0].Record(new InputWord(InputWord.Right));
        state.Modules[0][0] = 7;
        return state;
    }

    private static void Mutate(ref MatchState state)
    {
        state.Frame++;
        state.Fighters[0].Position += new IntVector2(1_000, 0);
        state.Fighters[1].Health -= 50;
        state.Inputs[0].Record(new InputWord(InputWord.Down));
        state.Random.NextUInt();
        state.Modules[3][5] = 9;
    }
}
