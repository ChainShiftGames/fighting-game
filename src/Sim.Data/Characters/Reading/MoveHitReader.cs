using System.Collections.Generic;
using Sim.Data.Characters.Compiled;
using Sim.Data.Validation;

namespace Sim.Data.Characters.Reading;

/// <summary>Reads a move's <c>hit</c> object: damage, guard, stun, hitstop and pushback.</summary>
/// <remarks>
/// One hit per move in schema v1. Moves already point at a range of hits, so multi-hit moves can turn this into a
/// list without changing the compact layout.
/// </remarks>
public static class MoveHitReader
{
    public static IReadOnlyList<HitData> Read(FieldReader move, IReadOnlyList<BoxData> boxes)
    {
        FieldReader? hit = move.OptionalObject("hit");
        bool hasAttackBox = HasAttackBox(boxes);
        if (hit is null)
        {
            if (hasAttackBox)
            {
                move.Error("hit", "required because the move has hit or throw boxes");
            }

            return [];
        }

        if (!hasAttackBox)
        {
            move.Error("hit", "the move has no hit or throw box for this hit to come from");
        }

        const int MaxFrames = CharacterSchema.MaxMoveFrames;
        int damage = hit.RequiredInt("damage", 0, CharacterSchema.MaxDamage);
        HitData data = new()
        {
            Damage = damage,
            ChipDamage = hit.OptionalInt("chipDamage", 0, 0, damage),
            Guard = hit.RequiredChoice("guard", CharacterJsonNames.Guards),
            Hitstun = hit.RequiredInt("hitstun", 0, MaxFrames),
            Blockstun = hit.RequiredInt("blockstun", 0, MaxFrames),
            Hitstop = hit.RequiredInt("hitstop", 0, MaxFrames),
            Pushback = hit.RequiredInt("pushback", 0, CharacterSchema.MaxDistance),
            Reaction = hit.OptionalChoice("reaction", CharacterJsonNames.Reactions, HitReaction.Hitstun),
        };
        hit.Finish();
        return [data];
    }

    private static bool HasAttackBox(IReadOnlyList<BoxData> boxes)
    {
        foreach (BoxData box in boxes)
        {
            if (box.Kind is BoxKind.Hit or BoxKind.Throw)
            {
                return true;
            }
        }

        return false;
    }
}
