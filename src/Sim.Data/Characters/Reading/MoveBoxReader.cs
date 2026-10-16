using System.Collections.Generic;
using Sim.Characters;
using Sim.Data.Validation;
using Sim.Math;

namespace Sim.Data.Characters.Reading;

/// <summary>Reads a move's <c>boxes</c>: kind, frame range and area, checked against the move's frames.</summary>
public static class MoveBoxReader
{
    public static IReadOnlyList<BoxData> Read(FieldReader move, MoveCategory category, MoveFrames frames)
    {
        List<BoxData> boxes = [];
        foreach (FieldReader? box in move.ObjectArray("boxes", required: true))
        {
            if (box is not null)
            {
                boxes.Add(ReadBox(box, category, frames));
            }
        }

        return boxes;
    }

    private static BoxData ReadBox(FieldReader box, MoveCategory category, MoveFrames frames)
    {
        BoxKind kind = box.RequiredChoice("kind", CharacterJsonNames.BoxKinds);
        int firstFrame = box.RequiredInt("firstFrame", 1, frames.Total);
        int lastFrame = box.RequiredInt("lastFrame", firstFrame, frames.Total);
        AxisAlignedBox area = ReadArea(box);
        box.Finish();

        if (kind is BoxKind.Hit or BoxKind.Throw)
        {
            CheckAttackBox(box, category, frames, firstFrame, lastFrame);
        }

        return new BoxData
        {
            Kind = kind,
            FirstFrame = firstFrame,
            LastFrame = lastFrame,
            Area = area,
        };
    }

    /// <summary>Reads a box's <c>x</c>, <c>y</c>, <c>width</c> and <c>height</c> (move and projectile boxes).</summary>
    public static AxisAlignedBox ReadArea(FieldReader box)
    {
        const int MaxDistance = CharacterSchema.MaxDistance;
        int x = box.RequiredInt("x", -MaxDistance, MaxDistance);
        int y = box.RequiredInt("y", -MaxDistance, MaxDistance);
        int width = box.RequiredInt("width", 1, MaxDistance);
        int height = box.RequiredInt("height", 1, MaxDistance);
        return AxisAlignedBox.FromCorner(x, y, width, height);
    }

    // A hit or throw box outside the active frames would contradict the frame data shown to players.
    private static void CheckAttackBox(FieldReader box, MoveCategory category, MoveFrames frames, int first, int last)
    {
        if (!frames.HasActiveFrames)
        {
            box.Error("kind", category == MoveCategory.Basic
                ? "basic moves cannot have hit or throw boxes"
                : "hit and throw boxes need active frames");
        }
        else if (first < frames.FirstActive || last > frames.LastActive)
        {
            box.Error("firstFrame", "hit and throw boxes must stay within the active frames "
                + $"{frames.FirstActive} to {frames.LastActive}, got {first} to {last}");
        }
    }
}
