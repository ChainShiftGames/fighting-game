namespace Sim.Diagnostics;

/// <summary>One fighter's frame for a frame meter: its kind and whether its move makes it invincible on it.</summary>
public readonly record struct FighterFrameState(FrameKind Kind, bool IsInvincible);
