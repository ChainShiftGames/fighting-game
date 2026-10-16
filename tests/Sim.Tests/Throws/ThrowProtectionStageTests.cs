using Sim.Characters;
using Sim.Fighters.States;
using Sim.Pipeline;
using Sim.Rules;
using Sim.State;
using Sim.Throws;
using Xunit;
using static Sim.Tests.Throws.ThrowCharacter;

namespace Sim.Tests.Throws;

public sealed class ThrowProtectionStageTests
{
    private const int AfterWakeUp = 3;
    private const int AfterLanding = 2;

    private readonly ThrowProtectionStage _stage;

    public ThrowProtectionStageTests()
    {
        CharacterData character = Create();
        MatchRules rules = default;
        rules.WakeUpThrowInvulnerableFrames = AfterWakeUp;
        rules.LandingThrowInvulnerableFrames = AfterLanding;
        _stage = new ThrowProtectionStage(character, character, in rules);
    }

    [Fact]
    public void AWakingFighterIsProtectedForTheSetFramesAfterTheWakeUp()
    {
        MatchState state = default;
        state.Fighters[0] = At(0, 1) with { Action = FighterAction.WakeUp };

        _stage.Run(ref state, FrameActivity.Everyone);
        Assert.Equal(AfterWakeUp + 1, state.Fighters[0].ThrowInvulnerableFramesLeft);

        state.Fighters[0].Action = FighterAction.Idle;
        for (int frame = AfterWakeUp; frame >= 0; frame--)
        {
            _stage.Run(ref state, FrameActivity.Everyone);
            Assert.Equal(frame, state.Fighters[0].ThrowInvulnerableFramesLeft);
        }
    }

    [Fact]
    public void TouchingDownStartsTheLandingProtection()
    {
        MatchState state = default;
        state.Fighters[1] = At(0, -1) with { Action = FighterAction.Jump, IsAirborne = 0 };

        _stage.Run(ref state, FrameActivity.Everyone);

        Assert.Equal(AfterLanding, state.Fighters[1].ThrowInvulnerableFramesLeft);
    }

    [Fact]
    public void AnAirborneFighterIsNotLandingYet()
    {
        MatchState state = default;
        state.Fighters[0] = At(0, 1) with { Action = FighterAction.Jump, IsAirborne = 1 };

        _stage.Run(ref state, FrameActivity.Everyone);

        Assert.Equal(0, state.Fighters[0].ThrowInvulnerableFramesLeft);
    }

    [Fact]
    public void AFighterThatDoesNotAdvanceKeepsItsProtection()
    {
        MatchState state = default;
        state.Fighters[1] = At(0, -1) with { ThrowInvulnerableFramesLeft = 2 };

        _stage.Run(ref state, new FrameActivity(advancingFighters: 1, isFrozen: false));

        Assert.Equal(2, state.Fighters[1].ThrowInvulnerableFramesLeft);
    }
}
