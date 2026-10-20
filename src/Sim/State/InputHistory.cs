using Sim.Input;

namespace Sim.State;

/// <summary>One player's recent input words, newest last, for motion detection and the button buffer.</summary>
/// <remarks>
/// A ring buffer of <see cref="Capacity"/> match frames. Older frames are overwritten; asking for a frame further
/// back than what has been recorded returns neutral, as if the player held nothing before the match started.
/// Each frame also notes whether the player's fighter skipped it (hitstop, slowdown: <c>Sim.Pipeline.TimeControl</c>
/// marks them), so the button buffer and motion windows count only the fighter's own frames
/// (<see cref="OldestFrameWithin"/>): a press made while the fighter is stopped doesn't grow older. A recorded frame
/// counts as advanced until <see cref="MarkNewestSkipped"/> says otherwise; unrecorded frames count as advanced.
/// </remarks>
public struct InputHistory
{
    /// <summary>Frames kept. Must cover the longest motion window plus the button buffer (P1-06).</summary>
    public const int Capacity = InputHistoryFrames.Length;

    private InputHistoryFrames _frames;
    private InputHistorySkips _skips;
    private int _newestIndex;
    private int _count;

    /// <summary>Frames recorded so far, up to <see cref="Capacity"/>.</summary>
    public readonly int Count => _count;

    public void Record(InputWord input)
    {
        _newestIndex = (_newestIndex + 1) % Capacity;
        _frames[_newestIndex] = input;
        SetSkipped(_newestIndex, false);
        if (_count < Capacity)
        {
            _count++;
        }
    }

    /// <summary>The fighter didn't advance on the newest frame, so that frame doesn't age older presses.</summary>
    public void MarkNewestSkipped() => SetSkipped(_newestIndex, true);

    /// <summary>The input <paramref name="framesAgo"/> frames back; 0 is the newest.</summary>
    /// <remarks>Neutral when <paramref name="framesAgo"/> is negative or not recorded (yet or anymore).</remarks>
    public readonly InputWord FramesAgo(int framesAgo)
    {
        if (!IsRecorded(framesAgo))
        {
            return InputWord.Neutral;
        }

        return _frames[IndexOf(framesAgo)];
    }

    /// <summary>The fighter didn't advance on the frame <paramref name="framesAgo"/> back.</summary>
    /// <remarks>False when the frame isn't recorded (yet or anymore).</remarks>
    public readonly bool IsSkipped(int framesAgo)
    {
        if (!IsRecorded(framesAgo))
        {
            return false;
        }

        int index = IndexOf(framesAgo);
        return (_skips[index / InputHistorySkips.BitsPerElement] & BitOf(index)) != 0;
    }

    /// <summary>
    /// The oldest frame, in frames ago, at most <paramref name="ownFrames"/> of the fighter's own frames older than
    /// <paramref name="newestFramesAgo"/>; frames it skipped don't count.
    /// </summary>
    /// <remarks>
    /// The button buffer and motion windows are measured with this, so hitstop and slowdown don't shorten them.
    /// Without skipped frames it is <paramref name="newestFramesAgo"/> + <paramref name="ownFrames"/>; skipped frames
    /// just before the result are included, so with 0 it covers every frame since the fighter last advanced. It may
    /// lie beyond the recorded frames, which read as neutral; a fighter stopped for longer than the history loses
    /// what came before.
    /// </remarks>
    public readonly int OldestFrameWithin(int newestFramesAgo, int ownFrames)
    {
        int frame = newestFramesAgo;
        for (int age = 0; age < ownFrames || IsSkipped(frame + 1); frame++)
        {
            if (!IsSkipped(frame + 1))
            {
                age++;
            }
        }

        return frame;
    }

    private static uint BitOf(int index) => 1u << (index % InputHistorySkips.BitsPerElement);

    private readonly bool IsRecorded(int framesAgo) => framesAgo >= 0 && framesAgo < _count;

    private readonly int IndexOf(int framesAgo) => (_newestIndex - framesAgo + Capacity) % Capacity;

    private void SetSkipped(int index, bool isSkipped)
    {
        ref uint element = ref _skips[index / InputHistorySkips.BitsPerElement];
        element = isSkipped ? element | BitOf(index) : element & ~BitOf(index);
    }
}
