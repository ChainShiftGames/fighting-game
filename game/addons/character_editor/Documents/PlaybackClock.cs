namespace FightingGame.Addons.CharacterEditor.Documents;

/// <summary>
/// Which frame of a move the editor's preview shows while it plays: the move's frames at the logic's 60 per second
/// (or slower), then the last frame held a moment, then from frame 1 again.
/// </summary>
/// <remarks>Editor only: elapsed time drives the preview, never the logic.</remarks>
public sealed class PlaybackClock
{
    /// <summary>The logic's fixed rate (ARCHITECTURE.md §3).</summary>
    public const int FramesPerSecond = 60;

    private readonly int _endHoldFrames;
    private double _position;

    /// <param name="endHoldFrames">How long the last frame stays before the move starts over, in frames.</param>
    public PlaybackClock(int endHoldFrames)
    {
        _endHoldFrames = endHoldFrames;
    }

    /// <summary>The move's length; it may change while playing (frame data edited).</summary>
    public int TotalFrames { get; set; } = 1;

    /// <summary>1 plays at match speed, 0.5 at half speed.</summary>
    public double Speed { get; set; } = 1d;

    /// <summary>The frame shown now, from 1.</summary>
    public int Frame
    {
        get
        {
            int frame = (int)_position + 1;
            return frame > TotalFrames ? TotalFrames : frame;
        }
    }

    /// <summary>Plays on from <paramref name="frame"/>, e.g. the frame picked when Play is pressed.</summary>
    public void StartAt(int frame) => _position = frame < 1 || frame >= TotalFrames ? 0d : frame - 1;

    /// <returns>The frame to show after <paramref name="seconds"/> more.</returns>
    public int Advance(double seconds)
    {
        _position += seconds * FramesPerSecond * Speed;
        int loop = (TotalFrames < 1 ? 1 : TotalFrames) + _endHoldFrames;
        _position %= loop;
        return Frame;
    }
}
