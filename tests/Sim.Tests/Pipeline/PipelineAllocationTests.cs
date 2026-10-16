using System;
using Sim.Input;
using Sim.Modules;
using Sim.Pipeline;
using Sim.State;
using Xunit;

namespace Sim.Tests.Pipeline;

public sealed class PipelineAllocationTests
{
    [Fact]
    public void ATickAllocatesNothingWhileModulesMakeRequests()
    {
        var module = new ScriptedModule(1)
        {
            OnAfterInput = static context =>
            {
                context.RequestTimeScale(1, new TimeScaleRequest { Numerator = 1, Denominator = 2, Frames = 3 });
                context.RequestCancel(0, new CancelRequest { TargetMove = 1, Priority = 1 });
            },
            OnEndOfFrame = static context =>
            {
                context.RequestHitstopOverride(0, new HitstopOverrideRequest { Frames = 1 });
                context.RequestFreeze(new FreezeRequest { Frames = 2 });
            },
        };
        TickPipeline pipeline = Pipelines.Create(module);
        MatchState state = default;
        pipeline.Tick(ref state, InputWord.Neutral, InputWord.Neutral);

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int frame = 0; frame < 1000; frame++)
        {
            pipeline.Tick(ref state, new InputWord((ushort)(frame & 15)), InputWord.Neutral);
        }

        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
    }
}
