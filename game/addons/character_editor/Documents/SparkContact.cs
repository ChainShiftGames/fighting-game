using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace FightingGame.Addons.CharacterEditor.Documents;

/// <summary>
/// Where a hit's spark starts on a frame of the move, as the logic places it (<c>FrameEventKind.Hit</c>): the centre
/// of the first hit box and point-blank opponent hurt box that overlap (data order), or the first hit box's centre
/// when none reaches. The art file's offset goes on top of this.
/// </summary>
public static class SparkContact
{
    public const string HitKind = "hit";

    /// <summary>In subpixels, the move's coordinates (facing right); null when the frame has no hit box.</summary>
    public static (int X, int Y)? Of(JsonObject move, int frame, IReadOnlyList<BoxArea> opponent)
    {
        (int X, int Y)? fallback = null;
        foreach (JsonObject box in MoveBoxes.OnFrame(move, frame))
        {
            if (JsonNodeValue.Text(box[MoveFields.Kind]) != HitKind || BoxArea.Of(box) is not BoxArea hit)
            {
                continue;
            }

            foreach (BoxArea hurt in opponent)
            {
                if (Overlap(hit, hurt) is BoxArea overlap)
                {
                    return Centre(overlap);
                }
            }

            fallback ??= Centre(hit);
        }

        return fallback;
    }

    private static BoxArea? Overlap(BoxArea a, BoxArea b)
    {
        int left = Math.Max(a.X, b.X);
        int bottom = Math.Max(a.Y, b.Y);
        int right = Math.Min(a.Right, b.Right);
        int top = Math.Min(a.Top, b.Top);
        return right > left && top > bottom ? new BoxArea(left, bottom, right - left, top - bottom) : null;
    }

    private static (int X, int Y) Centre(BoxArea area) => (area.X + (area.Width / 2), area.Y + (area.Height / 2));
}
