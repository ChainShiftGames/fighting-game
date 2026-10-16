using Sim.Characters;
using Sim.Math;
using Sim.State;

namespace Sim.Collision;

/// <summary>The space a fighter takes up for pushing and walls this frame: the union of its pushboxes.</summary>
public static class Pushbox
{
    /// <summary>The fighter's pushbox in the stage.</summary>
    /// <remarks>
    /// A move frame without pushboxes gives an empty box at the fighter's origin: it pushes nothing and only the
    /// origin is kept inside the walls. Data can use that for moves that pass through the opponent.
    /// </remarks>
    public static AxisAlignedBox Of(CharacterData character, in FighterState fighter)
    {
        IntVector2 origin = fighter.Position;
        AxisAlignedBox union = new(origin.X, origin.Y, origin.X, origin.Y);
        bool hasBox = false;
        foreach (AxisAlignedBox box in new ActiveBoxes(character, in fighter, BoxKind.Push))
        {
            union = hasBox ? Union(union, box) : box;
            hasBox = true;
        }

        return union;
    }

    private static AxisAlignedBox Union(AxisAlignedBox first, AxisAlignedBox second) => new(
        first.MinX < second.MinX ? first.MinX : second.MinX,
        first.MinY < second.MinY ? first.MinY : second.MinY,
        first.MaxX > second.MaxX ? first.MaxX : second.MaxX,
        first.MaxY > second.MaxY ? first.MaxY : second.MaxY);
}
