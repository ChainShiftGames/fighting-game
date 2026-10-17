using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace FightingGame.Addons.CharacterEditor.Documents;

/// <summary>
/// A move's <c>boxes</c> as the box editor works with them: per frame. A box in the file covers a frame range, so
/// editing one frame of it first splits that frame out (<see cref="Isolate"/>), and copied boxes that match a
/// neighbour join its range again (<see cref="Merge"/>), keeping files as short as a person would write them.
/// </summary>
public static class MoveBoxes
{
    public const string HurtKind = "hurt";
    public const string PushKind = "push";

    /// <summary>A new box's size when the frame has no body to copy it from: 50 px square.</summary>
    public const int FallbackSize = 50_000;

    /// <summary>The move's boxes; entries that aren't objects are skipped (validation reports them).</summary>
    public static IReadOnlyList<JsonObject> All(JsonObject move)
    {
        List<JsonObject> boxes = [];
        foreach (JsonNode? box in move[MoveFields.Boxes] as JsonArray ?? [])
        {
            if (box is JsonObject obj)
            {
                boxes.Add(obj);
            }
        }

        return boxes;
    }

    /// <summary>The boxes whose range covers <paramref name="frame"/>, in file order (later ones drawn on top).</summary>
    public static IReadOnlyList<JsonObject> OnFrame(JsonObject move, int frame)
    {
        List<JsonObject> boxes = [];
        foreach (JsonObject box in All(move))
        {
            if (Covers(box, frame))
            {
                boxes.Add(box);
            }
        }

        return boxes;
    }

    /// <summary>
    /// The placeholder body on the frame, as the game draws it: its push boxes, or its hurt boxes when it has
    /// none; null when it has neither.
    /// </summary>
    public static BoxArea? Body(JsonObject move, int frame) =>
        Union(move, frame, PushKind) ?? Union(move, frame, HurtKind);

    /// <summary>Adds a box of the kind on this frame only, over the body (or a fallback square on the origin).</summary>
    /// <param name="area">Where to put it; null for the body's area.</param>
    /// <param name="fieldOrder">The move's field names in schema order, for a move without boxes.</param>
    public static JsonObject Add(JsonObject move, string kind, int frame, BoxArea? area, IReadOnlyList<string> fieldOrder)
    {
        BoxArea placed = area ?? Body(move, frame) ?? new BoxArea(-FallbackSize / 2, 0, FallbackSize, FallbackSize);
        JsonObject box = new()
        {
            [MoveFields.Kind] = kind,
            [MoveFields.FirstFrame] = frame,
            [MoveFields.LastFrame] = frame,
        };
        placed.WriteTo(box);
        List(move, fieldOrder).Add(box);
        return box;
    }

    /// <summary>Removes the box from the move (all of its frames).</summary>
    public static void Remove(JsonObject move, JsonObject box) => (move[MoveFields.Boxes] as JsonArray)?.Remove(box);

    /// <summary>
    /// Splits <paramref name="frame"/> out of the box's range, so an edit changes that frame only. The frames before
    /// and after stay as copies around it, in the box's place in the file.
    /// </summary>
    /// <returns>The box covering only <paramref name="frame"/> (the box itself when it already does).</returns>
    public static JsonObject Isolate(JsonObject move, JsonObject box, int frame)
    {
        if (Range(box) is not (int first, int last) || first > frame || frame > last || first == last
            || move[MoveFields.Boxes] is not JsonArray list)
        {
            return box;
        }

        int index = list.IndexOf(box);
        if (last > frame)
        {
            list.Insert(index + 1, WithRange(box, frame + 1, last));
        }

        if (first < frame)
        {
            list.Insert(index, WithRange(box, first, frame - 1));
        }

        box[MoveFields.FirstFrame] = frame;
        box[MoveFields.LastFrame] = frame;
        return box;
    }

    /// <summary>
    /// Makes the boxes on <paramref name="to"/> the same as those on <paramref name="from"/>: the boxes on
    /// <paramref name="to"/> lose that frame, then each box of <paramref name="from"/> is copied onto it and joined
    /// with a matching neighbour range.
    /// </summary>
    public static void CopyFrame(JsonObject move, int from, int to, IReadOnlyList<string> fieldOrder)
    {
        if (from == to)
        {
            return;
        }

        List<JsonObject> source = [];
        foreach (JsonObject box in OnFrame(move, from))
        {
            source.Add((JsonObject)box.DeepClone());
        }

        foreach (JsonObject box in OnFrame(move, to))
        {
            Remove(move, Isolate(move, box, to));
        }

        JsonArray list = List(move, fieldOrder);
        foreach (JsonObject box in source)
        {
            box[MoveFields.FirstFrame] = to;
            box[MoveFields.LastFrame] = to;
            list.Add(box);
        }

        Merge(move);
    }

    /// <summary>
    /// Joins boxes of the same kind and area whose ranges touch or overlap into one, kept at the first one's place.
    /// </summary>
    public static void Merge(JsonObject move)
    {
        if (move[MoveFields.Boxes] is not JsonArray list)
        {
            return;
        }

        bool merged = true;
        while (merged)
        {
            merged = false;
            IReadOnlyList<JsonObject> boxes = All(move);
            for (int i = 0; i < boxes.Count && !merged; i++)
            {
                for (int j = i + 1; j < boxes.Count && !merged; j++)
                {
                    merged = TryJoin(list, boxes[i], boxes[j]);
                }
            }
        }
    }

    /// <summary>Whether the box's range covers the frame.</summary>
    public static bool Covers(JsonObject box, int frame) => Range(box) is (int first, int last) && first <= frame && frame <= last;

    /// <summary>The box's frame range; null when either end isn't a whole number.</summary>
    public static (int First, int Last)? Range(JsonObject box) =>
        JsonNodeValue.Int(box[MoveFields.FirstFrame]) is int first && JsonNodeValue.Int(box[MoveFields.LastFrame]) is int last
            ? (first, last)
            : null;

    private static bool TryJoin(JsonArray list, JsonObject keep, JsonObject other)
    {
        // Everything but the frames must match (kind, area, any later field), or joining would lose a value.
        if (Range(keep) is not (int first, int last) || Range(other) is not (int otherFirst, int otherLast)
            || otherFirst > last + 1 || first > otherLast + 1
            || !JsonNode.DeepEquals(WithRange(keep, 0, 0), WithRange(other, 0, 0)))
        {
            return false;
        }

        keep[MoveFields.FirstFrame] = Math.Min(first, otherFirst);
        keep[MoveFields.LastFrame] = Math.Max(last, otherLast);
        list.Remove(other);
        return true;
    }

    private static JsonObject WithRange(JsonObject box, int first, int last)
    {
        JsonObject copy = (JsonObject)box.DeepClone();
        copy[MoveFields.FirstFrame] = first;
        copy[MoveFields.LastFrame] = last;
        return copy;
    }

    private static BoxArea? Union(JsonObject move, int frame, string kind)
    {
        BoxArea? union = null;
        foreach (JsonObject box in OnFrame(move, frame))
        {
            if (JsonNodeValue.Text(box[MoveFields.Kind]) == kind && BoxArea.Of(box) is BoxArea area)
            {
                union = union is BoxArea soFar ? soFar.Union(area) : area;
            }
        }

        return union;
    }

    private static JsonArray List(JsonObject move, IReadOnlyList<string> fieldOrder)
    {
        if (move[MoveFields.Boxes] is not JsonArray list)
        {
            list = [];
            JsonFieldOrder.Set(move, MoveFields.Boxes, list, fieldOrder);
        }

        return list;
    }
}
