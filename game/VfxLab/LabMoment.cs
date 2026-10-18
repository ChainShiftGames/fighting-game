namespace FightingGame.VfxLab;

/// <summary>Where the Lab's timeline is: the phase, the frame within it (from 1) and the frame within the loop (from 0).</summary>
public readonly record struct LabMoment(LabPhase Phase, int PhaseFrame, int LoopFrame);
