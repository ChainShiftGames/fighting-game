using System;
using System.Globalization;

namespace Scenarios.Tests.Runner;

/// <summary>The frames a statement applies to: <c>frame 5</c> or <c>frames 1-10</c> (1-based, inclusive).</summary>
/// <remarks>Frame N is the Nth logic frame: its input is read on that tick and its expectations checked after it.</remarks>
public readonly record struct FrameRange(int First, int Last)
{
    public bool Contains(int frame) => frame >= First && frame <= Last;

    public static FrameRange Parse(string head, ScenarioLocation location)
    {
        string[] words = head.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 2 && words[0] == "frame" && TryParseFrame(words[1], out int frame))
        {
            return new FrameRange(frame, frame);
        }

        if (words.Length == 2 && words[0] == "frames")
        {
            string[] ends = words[1].Split('-');
            if (ends.Length == 2 && TryParseFrame(ends[0], out int first) && TryParseFrame(ends[1], out int last))
            {
                return first <= last
                    ? new FrameRange(first, last)
                    : throw new ScenarioSyntaxException(location, $"frames {first}-{last} runs backwards");
            }
        }

        throw new ScenarioSyntaxException(
            location, $"'{head}:' isn't a statement start; use 'setup:', 'frame N:' or 'frames A-B:' (frames from 1)");
    }

    private static bool TryParseFrame(string text, out int frame) =>
        int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out frame) && frame >= 1;
}
