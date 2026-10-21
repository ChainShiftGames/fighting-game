using FightingGame.Debug;
using FightingGame.Presentation.Feel;
using Sim.Math;

namespace FightingGame.Presentation.Events;

/// <summary>
/// Jitters a fighter who is being hit or blocking while hitstop freezes it, so the freeze reads as impact instead of
/// a stall (the defender shake fighting games use).
/// </summary>
/// <remarks>
/// Read from the fighter's own state (stun and hitstop left), so it follows rollback with nothing to cancel. It flips
/// sideways every match frame. Drawing only: the logic position is untouched (ARCHITECTURE.md §9).
/// </remarks>
public sealed class HitstopShake
{
    private readonly int _subpixels;
    private readonly DeveloperToggle _toggle;

    /// <param name="settings">How far the defender jitters (developer settings, presentation).</param>
    /// <param name="toggle">The screen shake switch, which turns this off too.</param>
    public HitstopShake(ShakeSettings settings, DeveloperToggle toggle)
    {
        _subpixels = (int)(settings.DefenderPixels * Subpixels.PerPixel);
        _toggle = toggle;
    }

    /// <summary>Where to draw the fighter relative to its logic position, in subpixels.</summary>
    public IntVector2 Offset(in FighterPose pose)
    {
        if (!_toggle.Enabled || pose.HitstopFramesLeft <= 0 || pose.StunFramesLeft <= 0)
        {
            return default;
        }

        return new IntVector2(pose.MatchFrame % 2 == 0 ? _subpixels : -_subpixels, 0);
    }
}
