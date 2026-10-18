using System.Collections.Generic;

namespace FightingGame.Presentation.World;

/// <summary>
/// A clip the model plays over the first frames of a move entered from certain other moves, e.g. standing up when
/// leaving crouch (ADR-0028). Looks only: the logic and the boxes change on the frame the move starts.
/// </summary>
/// <param name="From">The moves it plays after.</param>
/// <param name="To">The moves it plays at the start of.</param>
/// <param name="Clip">The clip: its file, name and in-place setting, as for a move.</param>
/// <param name="Frames">How many logic frames the whole clip is squeezed or stretched into.</param>
/// <param name="Reverse">Plays it backwards, so one clip serves both ways (stand → crouch and crouch → stand).</param>
public sealed record ClipTransition(
    IReadOnlyList<string> From,
    IReadOnlyList<string> To,
    AnimationSettings Clip,
    int Frames,
    bool Reverse)
{
    public bool Joins(string fromMove, string toMove) => Contains(From, fromMove) && Contains(To, toMove);

    private static bool Contains(IReadOnlyList<string> moves, string moveId)
    {
        for (int i = 0; i < moves.Count; i++)
        {
            if (moves[i] == moveId)
            {
                return true;
            }
        }

        return false;
    }
}
