using System;
using Sim.Input;
using Sim.Modules;
using Sim.Pipeline;
using Sim.State;
using Xunit;

namespace Sim.Tests.Pipeline;

public sealed class InvalidRequestTests
{
    public static TheoryData<string> InvalidRequests => ["cancel", "speed-up", "zero denominator", "hitstop", "freeze"];

    [Theory]
    [MemberData(nameof(InvalidRequests))]
    public void AnInvalidRequestFailsLoudlyNamingTheSlot(string kind)
    {
        var module = new ScriptedModule(1)
        {
            OnAfterInput = context =>
            {
                switch (kind)
                {
                    case "cancel":
                        context.RequestCancel(0, new CancelRequest { TargetMove = -1 });
                        break;
                    case "speed-up":
                        context.RequestTimeScale(
                            0, new TimeScaleRequest { Numerator = 2, Denominator = 1, Frames = 5 });
                        break;
                    case "zero denominator":
                        context.RequestTimeScale(0, new TimeScaleRequest { Numerator = 1, Frames = 5 });
                        break;
                    case "hitstop":
                        context.RequestHitstopOverride(0, new HitstopOverrideRequest { Frames = -1 });
                        break;
                    default:
                        context.RequestFreeze(new FreezeRequest { Frames = 3, ExemptFighters = 1 << 2 });
                        break;
                }
            },
        };
        TickPipeline pipeline = Pipelines.Create(module);
        MatchState state = default;

        InvalidOperationException error = Assert.Throws<InvalidOperationException>(
            () => pipeline.Tick(ref state, InputWord.Neutral, InputWord.Neutral));
        Assert.Contains("slot 0", error.Message, StringComparison.Ordinal);
    }
}
