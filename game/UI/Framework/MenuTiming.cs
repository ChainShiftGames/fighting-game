namespace FightingGame.UI.Framework;

/// <summary>How menus react in time: input grace, held-direction repeat and screen transitions.</summary>
/// <remarks>Developer settings (ARCHITECTURE.md §6); <see cref="Default"/> is the one place they are set.</remarks>
/// <param name="GraceMilliseconds">Presses this soon after a menu opens are ignored: they belong to the fight that
/// just ended (button mashing).</param>
/// <param name="RepeatDelayMilliseconds">How long a direction is held before it starts repeating.</param>
/// <param name="RepeatIntervalMilliseconds">Time between repeats after that.</param>
/// <param name="TransitionMilliseconds">Length of the fade between screens; 0 turns it off.</param>
/// <param name="StickDeadzone">How far a stick moves before it counts as a direction, 0 to 1.</param>
public sealed record MenuTiming(
    long GraceMilliseconds,
    long RepeatDelayMilliseconds,
    long RepeatIntervalMilliseconds,
    long TransitionMilliseconds,
    float StickDeadzone)
{
    public static MenuTiming Default { get; } = new(
        GraceMilliseconds: 600,
        RepeatDelayMilliseconds: 350,
        RepeatIntervalMilliseconds: 90,
        TransitionMilliseconds: 120,
        StickDeadzone: 0.5f);
}
