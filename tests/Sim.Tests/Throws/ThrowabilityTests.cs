using Sim.Characters;
using Sim.Fighters.States;
using Sim.State;
using Sim.Throws;
using Xunit;
using static Sim.Tests.Throws.ThrowCharacter;

namespace Sim.Tests.Throws;

public sealed class ThrowabilityTests
{
    private static readonly CharacterData _character = Create();

    [Theory]
    [InlineData(FighterAction.Idle, true)]
    [InlineData(FighterAction.WalkBack, true)]
    [InlineData(FighterAction.Crouch, true)]
    [InlineData(FighterAction.Prejump, true)]
    [InlineData(FighterAction.Dash, true)]
    [InlineData(FighterAction.Hitstun, false)]
    [InlineData(FighterAction.CrouchHitstun, false)]
    [InlineData(FighterAction.Blockstun, false)]
    [InlineData(FighterAction.CrouchBlockstun, false)]
    [InlineData(FighterAction.Knockdown, false)]
    [InlineData(FighterAction.Thrown, false)]
    [InlineData(FighterAction.ThrowTech, false)]
    public void StunnedAndThrownFightersCannotBeThrown(FighterAction action, bool canBeThrown)
    {
        FighterState fighter = At(0, 1) with { Action = action };

        Assert.Equal(canBeThrown, Throwability.CanBeThrown(in fighter, _character));
    }

    [Fact]
    public void AttackStartupCanBeThrown()
    {
        Assert.True(Throwability.CanBeThrown(At(0, 1, Throw, moveFrame: 1), _character));
    }

    [Fact]
    public void AnAirborneFighterCannotBeThrown()
    {
        FighterState fighter = At(0, 1) with { Action = FighterAction.Jump, IsAirborne = 1 };

        Assert.False(Throwability.CanBeThrown(in fighter, _character));
    }

    [Fact]
    public void InvulnerableFramesCannotBeThrown()
    {
        Assert.False(Throwability.CanBeThrown(At(0, 1, Invulnerable, moveFrame: 3), _character));
    }

    [Fact]
    public void WakeUpAndLandingProtectionPreventThrows()
    {
        FighterState fighter = At(0, 1) with { ThrowInvulnerableFramesLeft = 1 };

        Assert.False(Throwability.CanBeThrown(in fighter, _character));
    }
}
