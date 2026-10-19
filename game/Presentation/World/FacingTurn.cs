namespace FightingGame.Presentation.World;

/// <summary>
/// Turns the model round when the logic switches its facing (ADR-0028): it swings to face the camera, swaps sides
/// there, where a mirrored pose looks the same, and swings on to the new side. The logic's facing switches at once.
/// At rest the model is cheated partway toward the camera, as fighting games show their 3D fighters.
/// </summary>
/// <remarks>Counted by match frames; a match frame going back drops the turn (cosmetic).</remarks>
public sealed class FacingTurn
{
    // A quarter turn: the model faces +X, and this yaw makes it face the camera (+Z) on either side.
    private const float TowardCamera = -90f;

    private int _facing;
    private int _from;
    private int _startFrame = int.MinValue;

    public void Observe(int facing, int matchFrame)
    {
        if (facing != _facing)
        {
            if (_facing != 0)
            {
                _from = _facing;
                _startFrame = matchFrame;
            }

            _facing = facing;
        }
        else if (matchFrame < _startFrame)
        {
            _startFrame = int.MinValue;
        }
    }

    /// <summary>The side to mirror to and the yaw, in degrees, on <paramref name="matchFrame"/>.</summary>
    /// <param name="frames">How many frames a turn takes; 0 switches at once.</param>
    /// <param name="cameraTurnDegrees">How far the model turns toward the camera at rest, 0 to 90.</param>
    public (int Facing, float YawDegrees) PoseAt(int matchFrame, int frames, float cameraTurnDegrees = 0f)
    {
        float rest = -cameraTurnDegrees;
        long frame = (long)matchFrame - _startFrame + 1;
        if (_startFrame == int.MinValue || frames <= 0 || frame > frames)
        {
            return (_facing, rest);
        }

        float progress = frame / (frames + 1f);
        return progress < 0.5f
            ? (_from, rest + ((TowardCamera - rest) * progress * 2f))
            : (_facing, rest + ((TowardCamera - rest) * (1f - progress) * 2f));
    }
}
