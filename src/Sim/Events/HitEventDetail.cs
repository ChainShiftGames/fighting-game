namespace Sim.Events;

/// <summary>
/// The detail of a <see cref="FrameEventKind.Hit"/>, <see cref="FrameEventKind.Block"/> or
/// <see cref="FrameEventKind.Clash"/> event: the attacking move and which of its hits connected, packed into one int.
/// </summary>
/// <remarks>
/// Presentation needs the hit, not just the move, to draw a multi-hit move's sparks per hit. A move's first hit packs
/// to the move index alone, so single-hit moves read the same as before the hit was added.
/// </remarks>
public static class HitEventDetail
{
    /// <summary>Move indexes below this pack; far above any character's move count.</summary>
    public const int MoveLimit = 1 << 16;

    /// <param name="move">The attacking move's index in its character's moves.</param>
    /// <param name="hit">The index of the hit in that move's hits; a negative index (none) packs as 0.</param>
    public static int Of(int move, int hit) => (hit > 0 ? hit * MoveLimit : 0) + move;

    public static int MoveOf(int detail) => detail % MoveLimit;

    public static int HitOf(int detail) => detail / MoveLimit;
}
