using System;
using Sim.Characters;
using Sim.Input;
using Sim.Modules;
using Sim.Pipeline;
using Sim.State;
using Xunit;

namespace Sim.Tests.Pipeline;

public sealed class InvalidRequestTests
{
    public static TheoryData<string> InvalidRequests =>
        [
            "cancel", "speed-up", "zero denominator", "hitstop", "gravity", "stun", "move lock", "event without module",
            "event code", "freeze", "pushback scale",
        ];

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
                        context.RequestCancel(0, new CancelRequest { TargetMove = CancelRequest.Neutral - 1 });
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
                    case "gravity":
                        context.RequestGravityScale(0, new GravityScaleRequest { ExtraPermille = -1 });
                        break;
                    case "move lock":
                        context.RequestMoveLock(0, new MoveLockRequest { Categories = MoveCategory.Basic });
                        break;
                    case "pushback scale":
                        context.RequestPushbackScale(0, new PushbackScaleRequest { Permille = -1 });
                        break;
                    case "stun":
                        context.RequestStunOverride(0, new StunOverrideRequest { Frames = -1 });
                        break;
                    case "event without module":
                        context.RequestEvent(0, new ModuleEventRequest { Code = 1 });
                        break;
                    case "event code":
                        context.RequestEvent(
                            0, new ModuleEventRequest { Module = new(1), Code = ModuleEventRequest.CodesPerModule });
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
