using System;

namespace FightingGame.Presentation;

/// <summary>A stretch of world height in subpixels, y up, 0 on the floor: a fighter's body or what the camera shows.</summary>
public readonly record struct VerticalSpan(int Bottom, int Top)
{
    public int Height => Top - Bottom;

    /// <summary>This span moved, then grown, as little as needed to cover <paramref name="other"/>.</summary>
    /// <remarks>
    /// Moving comes first and only as far as it uncovers nothing of <paramref name="other"/>, so a view keeps its
    /// height (its zoom) when it can. Both steps change it by no more than <paramref name="other"/> sticks out, so a
    /// view covering a body that moves smoothly also moves smoothly.
    /// </remarks>
    public VerticalSpan Covering(VerticalSpan other)
    {
        int raise = 0;
        if (Top < other.Top)
        {
            raise = Math.Min(other.Top - Top, Math.Max(0, other.Bottom - Bottom));
        }
        else if (Bottom > other.Bottom)
        {
            raise = -Math.Min(Bottom - other.Bottom, Math.Max(0, Top - other.Top));
        }

        return new VerticalSpan(Math.Min(Bottom + raise, other.Bottom), Math.Max(Top + raise, other.Top));
    }
}
