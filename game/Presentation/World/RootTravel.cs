using Godot;

namespace FightingGame.Presentation.World;

/// <summary>
/// Takes the horizontal travel out of a root bone key, for clips not authored in place (ADR-0028): the logic sets the
/// fighter's position, so the clip may only sway, never walk off. Height is kept, so a crouch or a jump still moves.
/// </summary>
public static class RootTravel
{
    /// <param name="key">The root bone's position at this key.</param>
    /// <param name="first">Its position at the clip's first key.</param>
    /// <param name="last">Its position at the clip's last key.</param>
    /// <param name="progress">How far through the clip the key is, 0 at the first key and 1 at the last.</param>
    public static Vector3 Removed(Vector3 key, Vector3 first, Vector3 last, float progress)
    {
        Vector3 travel = last - first;
        return key - (new Vector3(travel.X, 0f, travel.Z) * progress);
    }
}
