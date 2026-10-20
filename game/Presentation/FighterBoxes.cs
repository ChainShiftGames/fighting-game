using System.Collections.Generic;
using Sim.Characters;
using Sim.Collision;
using Sim.Math;
using Sim.State;

namespace FightingGame.Presentation;

/// <summary>Reads which boxes a fighter has on the current frame of its move, straight from the character data.</summary>
public static class FighterBoxes
{
    /// <summary>Adds the fighter's boxes on its current move frame to <paramref name="boxes"/>, in world space.</summary>
    public static void Collect(CharacterData character, in FighterState fighter, List<WorldBox> boxes)
    {
        MoveData move = character.Moves[fighter.Move];
        foreach (BoxData box in character.BoxesOf(in move))
        {
            if (fighter.MoveFrame < box.FirstFrame || fighter.MoveFrame > box.LastFrame)
            {
                continue;
            }

            boxes.Add(new WorldBox(box.Kind, ActiveBoxes.Place(box.Area, fighter.Position, fighter.Facing)));
        }
    }

    /// <summary>
    /// The fighter's placeholder body: its pushboxes, or its hurtboxes when it has none; empty when it has neither.
    /// </summary>
    /// <remarks>Data-driven, so a crouch is shorter and a move that shifts the body shows it.</remarks>
    public static AxisAlignedBox Body(List<WorldBox> boxes)
    {
        AxisAlignedBox body = Union(boxes, BoxKind.Push);
        return body.IsEmpty ? Union(boxes, BoxKind.Hurt) : body;
    }

    /// <summary>The fighter's height: from its feet (its position) to the top of its hurt- and pushboxes.</summary>
    /// <remarks>Hitboxes are left out, so an attack's reach never moves the camera. Neither box: just its feet.</remarks>
    public static VerticalSpan Height(IntVector2 position, List<WorldBox> boxes)
    {
        int top = position.Y;
        foreach (WorldBox box in boxes)
        {
            if ((box.Kind == BoxKind.Hurt || box.Kind == BoxKind.Push) && !box.Area.IsEmpty)
            {
                top = System.Math.Max(top, box.Area.MaxY);
            }
        }

        return new VerticalSpan(position.Y, top);
    }

    private static AxisAlignedBox Union(List<WorldBox> boxes, BoxKind kind)
    {
        AxisAlignedBox union = default;
        foreach (WorldBox box in boxes)
        {
            if (box.Kind != kind || box.Area.IsEmpty)
            {
                continue;
            }

            union = union.IsEmpty
                ? box.Area
                : new AxisAlignedBox(
                    System.Math.Min(union.MinX, box.Area.MinX),
                    System.Math.Min(union.MinY, box.Area.MinY),
                    System.Math.Max(union.MaxX, box.Area.MaxX),
                    System.Math.Max(union.MaxY, box.Area.MaxY));
        }

        return union;
    }
}
