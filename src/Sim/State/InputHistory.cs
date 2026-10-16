using Sim.Input;

namespace Sim.State;

/// <summary>One player's recent input words, newest last, for motion detection and the button buffer.</summary>
/// <remarks>
/// A ring buffer of <see cref="Capacity"/> frames. Older frames are overwritten; asking for a frame further back
/// than what has been recorded returns neutral, as if the player held nothing before the match started.
/// </remarks>
public struct InputHistory
{
    /// <summary>Frames kept. Must cover the longest motion window plus the button buffer (P1-06).</summary>
    public const int Capacity = InputHistoryFrames.Length;

    private InputHistoryFrames _frames;
    private int _newestIndex;
    private int _count;

    /// <summary>Frames recorded so far, up to <see cref="Capacity"/>.</summary>
    public readonly int Count => _count;

    public void Record(InputWord input)
    {
        _newestIndex = (_newestIndex + 1) % Capacity;
        _frames[_newestIndex] = input;
        if (_count < Capacity)
        {
            _count++;
        }
    }

    /// <summary>The input <paramref name="framesAgo"/> frames back; 0 is the newest.</summary>
    /// <remarks>Neutral when <paramref name="framesAgo"/> is negative or not recorded (yet or anymore).</remarks>
    public readonly InputWord FramesAgo(int framesAgo)
    {
        if (framesAgo < 0 || framesAgo >= _count)
        {
            return InputWord.Neutral;
        }

        return _frames[(_newestIndex - framesAgo + Capacity) % Capacity];
    }
}
