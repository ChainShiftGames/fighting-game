using System.Collections.Generic;
using Sim.Characters;
using Sim.Data.Validation;

namespace Sim.Data.Characters.Reading;

/// <summary>Reads a move's <c>throw</c> object (where the thrown fighter lands) and checks throw data rules.</summary>
/// <remarks>
/// A move throws when it has throw boxes; category <c>throw</c> marks the normal throws (their buttons are the throw
/// tech input) and must have one. In schema v1 a move hits or throws, never both, and throws are ground throws.
/// </remarks>
public static class MoveThrowReader
{
    public static MoveData Read(FieldReader file, IReadOnlyList<BoxData> boxes, MoveData move)
    {
        if (!Has(boxes, BoxKind.Throw))
        {
            file.Forbid("throw", "only moves with throw boxes have throw data");
            if (move.Category == MoveCategory.Throw)
            {
                file.Error("boxes", "a throw move needs a throw box");
            }

            return move;
        }

        if (Has(boxes, BoxKind.Hit))
        {
            file.Error("boxes", "a move either hits or throws (one hit per move); move the hit boxes to another move");
        }

        if (move.Stance == MoveStance.Air)
        {
            file.Error("usableIn", "throws are ground throws; air throws aren't supported yet");
        }

        FieldReader? data = file.RequiredObject("throw");
        if (data is null)
        {
            return move;
        }

        const int MaxDistance = CharacterSchema.MaxDistance;
        int releaseX = data.RequiredInt("releaseX", -MaxDistance, MaxDistance);
        data.Finish();
        return move with { ThrowReleaseX = releaseX };
    }

    private static bool Has(IReadOnlyList<BoxData> boxes, BoxKind kind)
    {
        foreach (BoxData box in boxes)
        {
            if (box.Kind == kind)
            {
                return true;
            }
        }

        return false;
    }
}
