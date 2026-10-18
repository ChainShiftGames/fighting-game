using System.Collections.Generic;
using Sim.Math;

namespace FightingGame.Presentation.World;

/// <summary>Shows one fighter in the 3D scene for the pose the logic is on (ADR-0027 §6).</summary>
/// <remarks>Reads only; called once per displayed frame, so it must not allocate.</remarks>
public interface IFighterArt
{
    /// <param name="pose">The fighter's move and frame of it: the animation frame, never elapsed time.</param>
    /// <param name="position">The fighter's logic position, in subpixels.</param>
    /// <param name="facing">1 facing right, -1 facing left.</param>
    /// <param name="boxes">The fighter's boxes on this frame, in world space.</param>
    void ShowPose(FighterPose pose, IntVector2 position, int facing, List<WorldBox> boxes);
}
