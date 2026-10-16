using Sim.Rules;

namespace Sim.Data.Rules;

/// <summary>The throw values of <c>rules/match.json</c> (developer settings); rules in <c>Sim.Throws</c>.</summary>
/// <param name="TechWindowFrames">Frames after a grab in which the thrown fighter can tech.</param>
/// <param name="WakeUpInvulnerableFrames">Frames after wake-up in which a fighter can't be thrown.</param>
/// <param name="LandingInvulnerableFrames">Frames from landing in which a fighter can't be thrown.</param>
/// <param name="ThrowVsStrike">Whether a throw or a strike wins on the same frame.</param>
public sealed record ThrowFormat(
    int TechWindowFrames, int WakeUpInvulnerableFrames, int LandingInvulnerableFrames, ThrowPriority ThrowVsStrike);
