using Sim.Fighters.States;

namespace FightingGame.Presentation.World;

/// <summary>
/// A pin in an attack animation's timing (ADR-0028): frame <paramref name="Frame"/> of the
/// <paramref name="Phase"/>'s <paramref name="Of"/> frames shows clip frame <paramref name="Clip"/>, and the clip plays
/// straight between pins. Written against the phase's length when it was pinned, so it stretches with the phase when
/// the frame data changes.
/// </summary>
/// <param name="Frame">From 2 (frame 1 is the phase's own start: the clip start or a contact frame) to <paramref name="Of"/>.</param>
/// <param name="Clip">In clip frames at 60 fps, as an animator reads them.</param>
/// <remarks>Presentation only, like contact frames: it never changes the logic, the data hash or a replay.</remarks>
public readonly record struct TimingKey(MovePhase Phase, int Frame, int Of, int Clip)
{
    /// <summary>Where the key sits in its phase: 0 is the phase's first frame, 1 the start of the next phase.</summary>
    public float Position => (Frame - 1) / (float)Of;

    /// <summary>The frame of a <paramref name="phaseFrames"/>-frame phase the key falls on now, from 1.</summary>
    public int FrameIn(int phaseFrames) => 1 + (int)System.MathF.Round(Position * phaseFrames);
}
