namespace Sim.Input.Motions;

/// <summary>One motion as data defines it: its numpad notation, its steps in order and its own window, if any.</summary>
/// <param name="Notation">Numpad notation read as a number (236), as moves refer to it.</param>
/// <param name="Steps">Oldest first. Other directions may come between two steps (so 6236 contains 623).</param>
/// <param name="Window">Frames from first step to trigger, or <see cref="UsesMatchWindow"/>.</param>
public readonly record struct MotionDefinition(int Notation, MotionStep[] Steps, int Window = MotionDefinition.UsesMatchWindow)
{
    /// <summary>The motion uses <c>MatchRules.MotionWindow</c>.</summary>
    public const int UsesMatchWindow = 0;
}
