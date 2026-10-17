using System;
using System.Text.Json.Nodes;

namespace FightingGame.Addons.CharacterEditor.Documents;

/// <summary>
/// A box's area as the move file holds it: minimum corner <see cref="X"/>, <see cref="Y"/> (forward and up from the
/// fighter's origin, facing right) and its size, in subpixels (1 px = 1000).
/// </summary>
public readonly record struct BoxArea(int X, int Y, int Width, int Height)
{
    public const string XField = "x";
    public const string YField = "y";
    public const string WidthField = "width";
    public const string HeightField = "height";

    /// <summary>The smallest size the schema allows: a box is never dragged to nothing or inside out.</summary>
    public const int MinSize = 1;

    public int Right => X + Width;

    public int Top => Y + Height;

    /// <summary>The box's area; null when a field is missing or not a whole number (validation reports it).</summary>
    public static BoxArea? Of(JsonObject box) =>
        JsonNodeValue.Int(box[XField]) is int x && JsonNodeValue.Int(box[YField]) is int y
        && JsonNodeValue.Int(box[WidthField]) is int width && JsonNodeValue.Int(box[HeightField]) is int height
            ? new BoxArea(x, y, width, height)
            : null;

    /// <summary>Writes the area into the box's fields, which keep their place in the file.</summary>
    public void WriteTo(JsonObject box)
    {
        box[XField] = X;
        box[YField] = Y;
        box[WidthField] = Width;
        box[HeightField] = Height;
    }

    public bool Contains(int x, int y) => x >= X && x <= Right && y >= Y && y <= Top;

    /// <summary>The smallest area holding both.</summary>
    public BoxArea Union(BoxArea other)
    {
        int left = Math.Min(X, other.X);
        int bottom = Math.Min(Y, other.Y);
        return new BoxArea(left, bottom, Math.Max(Right, other.Right) - left, Math.Max(Top, other.Top) - bottom);
    }

    /// <summary>The area after dragging <paramref name="handle"/> by the offset, in subpixels.</summary>
    /// <remarks>
    /// The dragged edges move by the offset rounded to <paramref name="snap"/>, so a box drawn on whole pixels stays
    /// on them. An edge dragged past the opposite one stops <see cref="MinSize"/> short of it.
    /// </remarks>
    public BoxArea Dragged(BoxHandle handle, int offsetX, int offsetY, int snap)
    {
        int dx = Snap(offsetX, snap);
        int dy = Snap(offsetY, snap);
        if (handle == BoxHandle.Move)
        {
            return this with { X = X + dx, Y = Y + dy };
        }

        int left = X;
        int right = Right;
        int bottom = Y;
        int top = Top;
        if (handle is BoxHandle.Left or BoxHandle.TopLeft or BoxHandle.BottomLeft)
        {
            left = Math.Min(left + dx, right - MinSize);
        }

        if (handle is BoxHandle.Right or BoxHandle.TopRight or BoxHandle.BottomRight)
        {
            right = Math.Max(right + dx, left + MinSize);
        }

        if (handle is BoxHandle.Bottom or BoxHandle.BottomLeft or BoxHandle.BottomRight)
        {
            bottom = Math.Min(bottom + dy, top - MinSize);
        }

        if (handle is BoxHandle.Top or BoxHandle.TopLeft or BoxHandle.TopRight)
        {
            top = Math.Max(top + dy, bottom + MinSize);
        }

        return new BoxArea(left, bottom, right - left, top - bottom);
    }

    /// <summary>The area spanned by two corners dragged out on the canvas, snapped, at least <see cref="MinSize"/>.</summary>
    public static BoxArea Between(int x1, int y1, int x2, int y2, int snap)
    {
        int left = Snap(Math.Min(x1, x2), snap);
        int bottom = Snap(Math.Min(y1, y2), snap);
        int right = Math.Max(Snap(Math.Max(x1, x2), snap), left + MinSize);
        int top = Math.Max(Snap(Math.Max(y1, y2), snap), bottom + MinSize);
        return new BoxArea(left, bottom, right - left, top - bottom);
    }

    private static int Snap(int value, int snap) =>
        snap <= 1 ? value : (int)Math.Round(value / (double)snap, MidpointRounding.AwayFromZero) * snap;
}
