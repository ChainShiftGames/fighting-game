namespace Sim.Input.Motions;

/// <summary>The motions that dash, backdash and high jump, plus an optional dash button (developer settings).</summary>
/// <remarks>
/// Motions are numpad notations that must exist in the <see cref="MotionSet"/>. A dash button mask of 0 means dashes
/// come from motions only. With the dash button, pressed with back it backdashes, otherwise it dashes.
/// </remarks>
public readonly record struct MovementInputs(
    int DashMotion, int BackdashMotion, int HighJumpMotion, int DashButtonMask);
