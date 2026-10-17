using System;
using System.Collections.Generic;

namespace FightingGame.Addons.CharacterEditor.Documents;

/// <summary>
/// Where a frame of a move lands when its phases change length, so boxes, cancel windows and other frame ranges
/// stay on the same part of the move: a range that ran to the end of the active frames still does.
/// </summary>
/// <remarks>
/// A frame keeps its phase and its offset from the phase start, clamped to the new length. A range's first frame on
/// a phase start stays on the start, and its last frame on a phase end stays on the end. When a phase shrinks to
/// nothing, its frames move to the end of the phase before it. Frames outside the move shift with its end.
/// </remarks>
public sealed class FrameRemap
{
    private readonly IReadOnlyList<int> _from;
    private readonly IReadOnlyList<int> _to;

    /// <exception cref="ArgumentException">One is a basic move and the other an attack.</exception>
    public FrameRemap(FramePhases from, FramePhases to)
    {
        if (from.IsBasic != to.IsBasic)
        {
            throw new ArgumentException("Both must be basic moves or both attacks.", nameof(to));
        }

        _from = from.Lengths;
        _to = to.Lengths;
        From = from;
        To = to;
    }

    public FramePhases From { get; }

    public FramePhases To { get; }

    /// <summary>The new frame of a range's first frame (or of a single frame, such as a projectile spawn).</summary>
    public int First(int frame) => Map(frame, preferEnd: false);

    /// <summary>The new frame of a range's last frame.</summary>
    public int Last(int frame) => Map(frame, preferEnd: true);

    private int Map(int frame, bool preferEnd)
    {
        if (frame < 1)
        {
            return frame;
        }

        int oldStart = 1;
        int newStart = 1;
        for (int phase = 0; phase < _from.Count; phase++)
        {
            int oldLength = _from[phase];
            int newLength = _to[phase];
            if (frame < oldStart + oldLength)
            {
                return InPhase(frame - oldStart, oldLength, newStart, newLength, preferEnd);
            }

            oldStart += oldLength;
            newStart += newLength;
        }

        return frame + To.TotalFrames - From.TotalFrames;
    }

    private static int InPhase(int offset, int oldLength, int newStart, int newLength, bool preferEnd)
    {
        if (newLength == 0)
        {
            return Math.Max(1, newStart - 1);
        }

        int newEnd = newStart + newLength - 1;
        bool onStart = offset == 0;
        bool onEnd = offset == oldLength - 1;
        if (onEnd && (preferEnd || !onStart))
        {
            return newEnd;
        }

        return onStart ? newStart : Math.Min(newStart + offset, newEnd);
    }
}
