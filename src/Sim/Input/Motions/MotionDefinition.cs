namespace Sim.Input.Motions;

/// <summary>
/// One motion as data defines it: its numpad notation, its steps in order, its own window and its special button
/// shortcut direction, if any.
/// </summary>
/// <param name="Notation">Numpad notation read as a number (236), as moves refer to it.</param>
/// <param name="Steps">Oldest first. Other directions may come between two steps (so 6236 contains 623).</param>
/// <param name="Window">Frames from first step to trigger, or <see cref="UsesMatchWindow"/>.</param>
/// <param name="Shortcut">
/// Numpad direction 1–9 that, held with the special button and a move's button, starts that move instead of this
/// motion (ADR-0020), or <see cref="NoShortcut"/>.
/// </param>
public readonly record struct MotionDefinition(
    int Notation,
    MotionStep[] Steps,
    int Window = MotionDefinition.UsesMatchWindow,
    int Shortcut = MotionDefinition.NoShortcut)
{
    /// <summary>The motion has no special button shortcut.</summary>
    public const int NoShortcut = 0;

    /// <summary>The motion uses <c>MatchRules.MotionWindow</c>.</summary>
    public const int UsesMatchWindow = 0;
}
