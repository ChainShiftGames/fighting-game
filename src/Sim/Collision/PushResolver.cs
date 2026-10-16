using Sim.Math;
using Sim.State;

namespace Sim.Collision;

/// <summary>Moves two fighters so their pushboxes don't overlap and both stay inside the walls.</summary>
/// <remarks>
/// Each fighter is first kept inside the walls, so who is on which side is judged from where the walls let them be
/// (an opponent can't be crossed by moving into the wall behind it). Overlapping pushboxes then push both apart by
/// half the overlap each (rounded up, so both move alike), which makes a walking fighter push a standing one at half
/// speed, and the walls apply again. A fighter the wall stops can't give way, so the other takes the rest of the
/// push: that's the corner. Only x changes; fighters never push each other up or down. Both results come from the
/// positions as given, so neither port goes first.
/// </remarks>
public static class PushResolver
{
    /// <param name="first">One fighter; its x is corrected.</param>
    /// <param name="firstBox">Its pushbox at its current position (<see cref="Pushbox.Of"/>).</param>
    /// <param name="second">The other fighter.</param>
    /// <param name="secondBox">Its pushbox.</param>
    /// <param name="walls">The stage walls.</param>
    public static void Resolve(
        ref FighterState first,
        AxisAlignedBox firstBox,
        ref FighterState second,
        AxisAlignedBox secondBox,
        in StageWalls walls)
    {
        KeepInside(ref first, ref firstBox, in walls);
        KeepInside(ref second, ref secondBox, in walls);

        // -1: first is on the left, so pushes move it left (negative) and the second right.
        int side = FighterSides.Of(in first, in second);
        int half = side == 0 ? 0 : (OverlapX(firstBox, secondBox) + 1) / 2;
        int firstShift = side * half;
        int secondShift = -side * half;

        int firstWall = walls.CorrectionFor(Shifted(firstBox, firstShift));
        int secondWall = walls.CorrectionFor(Shifted(secondBox, secondShift));
        firstShift += firstWall;
        secondShift += secondWall;

        int rest = side == 0 ? 0 : OverlapX(Shifted(firstBox, firstShift), Shifted(secondBox, secondShift));
        if (rest > 0 && firstWall == 0 && secondWall != 0)
        {
            firstShift += side * rest;
            firstShift += walls.CorrectionFor(Shifted(firstBox, firstShift));
        }
        else if (rest > 0 && secondWall == 0 && firstWall != 0)
        {
            secondShift -= side * rest;
            secondShift += walls.CorrectionFor(Shifted(secondBox, secondShift));
        }

        first.Position = new IntVector2(first.Position.X + firstShift, first.Position.Y);
        second.Position = new IntVector2(second.Position.X + secondShift, second.Position.Y);
    }

    private static void KeepInside(ref FighterState fighter, ref AxisAlignedBox box, in StageWalls walls)
    {
        int correction = walls.CorrectionFor(box);
        fighter.Position = new IntVector2(fighter.Position.X + correction, fighter.Position.Y);
        box = Shifted(box, correction);
    }

    // How far the boxes overlap along x, or 0 when they don't overlap at all (also when apart vertically).
    private static int OverlapX(AxisAlignedBox first, AxisAlignedBox second)
    {
        if (!first.Overlaps(second))
        {
            return 0;
        }

        int right = first.MaxX < second.MaxX ? first.MaxX : second.MaxX;
        int left = first.MinX > second.MinX ? first.MinX : second.MinX;
        return right - left;
    }

    private static AxisAlignedBox Shifted(AxisAlignedBox box, int x) => box.Translate(new IntVector2(x, 0));
}
