namespace Sim.State;

/// <summary>Where the gameplay camera is: the centre of what the screen shows, in subpixels (ADR-0004).</summary>
/// <remarks>
/// Logic state because the screen edges hold the fighters (the max player distance). Only horizontal: zoom, shake
/// and any vertical follow are presentation. 0 is the stage centre, where rounds start.
/// </remarks>
public struct CameraState
{
    public int X;
}
