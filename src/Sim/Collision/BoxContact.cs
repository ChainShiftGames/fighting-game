using Sim.Characters;
using Sim.Math;
using Sim.State;

namespace Sim.Collision;

/// <summary>Whether two fighters' boxes of given kinds touch this frame, e.g. one's hitboxes and the other's hurtboxes.</summary>
public static class BoxContact
{
    /// <summary>True when any <paramref name="kind"/> box of the first fighter overlaps any <paramref name="otherKind"/> box of the second.</summary>
    public static bool Overlaps(
        CharacterData character,
        in FighterState fighter,
        BoxKind kind,
        CharacterData otherCharacter,
        in FighterState other,
        BoxKind otherKind)
    {
        foreach (AxisAlignedBox box in new ActiveBoxes(character, in fighter, kind))
        {
            foreach (AxisAlignedBox otherBox in new ActiveBoxes(otherCharacter, in other, otherKind))
            {
                if (box.Overlaps(otherBox))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
